import os
import argparse
import shutil

# Create an argument parser
parser = argparse.ArgumentParser(description="Count files starting with a number in a folder and move them to separate folders")

# Add an argument for the folder path
parser.add_argument('-p', '--path', type=str, help='Path to the folder containing the text and image files')

# Define the starting numbers to count
starting_numbers = ["0", "1", "2"]

# Initialize counters for each starting number and for empty files
counters = {}
for number in starting_numbers:
    counters[number] = 0
counters["empty"] = 0
counters["multiple"] = 0

# Parse the arguments
args = parser.parse_args()

# Define the output folder names based on the starting number
output_folders = {}
for number in starting_numbers:
    output_folders[number] = os.path.join(args.path, f"{number}_files")
    os.makedirs(output_folders[number], exist_ok=True)

# Define the output folder for empty files
output_folders["empty"] = os.path.join(args.path, "empty_files")
os.makedirs(output_folders["empty"], exist_ok=True)

# Define the output folder for files with multiple starting numbers
output_folders["multiple"] = os.path.join(args.path, "multiple_obj")
os.makedirs(output_folders["multiple"], exist_ok=True)

# Loop through all files in the folder
for filename in os.listdir(args.path):
    if filename.endswith(".txt"):
        # Read the contents of the file
        with open(os.path.join(args.path, filename), "r") as f:
            contents = f.readlines()

        # If the file is empty, increment the empty file counter
        if len(contents) == 0:
            counters["empty"] += 1
            shutil.move(os.path.join(args.path, filename), output_folders["empty"])
        else:
            # Check the first character of each line in the file
            first_numbers = set()
            for line in contents:
                if line.startswith(tuple(starting_numbers)):
                    first_numbers.add(line[0])

            # If there is only one starting number, move the file to the corresponding folder
            if len(first_numbers) == 1:
                number = first_numbers.pop()
                counters[number] += 1
                shutil.move(os.path.join(args.path, filename), output_folders[number])
                # Move the corresponding image file to the same folder
                image_filename = os.path.splitext(filename)[0] + ".jpg"
                shutil.move(os.path.join(args.path, image_filename), output_folders[number])
            else:
                # If there are multiple starting numbers, move the file to the "multiple_obj" folder
                counters["multiple"] += 1
                shutil.move(os.path.join(args.path, filename), output_folders["multiple"])
                # Move the corresponding image file to the same folder
                image_filename = os.path.splitext(filename)[0] + ".jpg"
                shutil.move(os.path.join(args.path, image_filename), output_folders["multiple"])
                # Add to the count for each starting number
                for number in first_numbers:
                    counters[number] += 1

# Print the count for each starting number and for empty files
for number, count in counters.items():
    print(f"Number of files starting with {number}: {count}")