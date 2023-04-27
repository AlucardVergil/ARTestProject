import os
import argparse
import shutil

# Set up command-line arguments
parser = argparse.ArgumentParser(description='Move text files without matching image files')
parser.add_argument('-p', '--path', type=str, help='Directory path', default=os.getcwd() + '/')
parser.add_argument('-d', '--destination', type=str, help='Destination folder name', default='missing_image_files')

# Parse the command-line arguments
args = parser.parse_args()

# Create the destination folder if it doesn't exist
destination_folder = os.path.join(args.path, args.destination)
if not os.path.exists(destination_folder):
    os.makedirs(destination_folder)

# Loop through all the files in the directory
for filename in os.listdir(args.path):

    # Check if the file is a text file
    if filename.endswith('.txt'):

        # Extract the filename without the extension
        basename = os.path.splitext(filename)[0]

        # Check if a matching image file exists
        if os.path.exists(os.path.join(args.path, basename + '.jpg')) or \
                os.path.exists(os.path.join(args.path, basename + '.png')) or \
                os.path.exists(os.path.join(args.path, basename + '.jpeg')):
            continue  # The text file has a matching image file, so move on to the next file
        else:
            # Move the text file to the destination folder
            shutil.move(os.path.join(args.path, filename), os.path.join(destination_folder, filename))