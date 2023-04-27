import os
import argparse
import shutil

# Set up command-line arguments
parser = argparse.ArgumentParser(description='Move images without matching text files')
parser.add_argument('-p', '--path', type=str, help='Directory path', default=os.getcwd() + '/')
parser.add_argument('-d', '--destination', type=str, help='Destination folder name', default='missing_text_files')

# Parse the command-line arguments
args = parser.parse_args()

# Create the destination folder if it doesn't exist
destination_folder = os.path.join(args.path, args.destination)
if not os.path.exists(destination_folder):
    os.makedirs(destination_folder)

# Loop through all the files in the directory
for filename in os.listdir(args.path):

    # Check if the file is an image file
    if filename.endswith('.jpg') or filename.endswith('.png') or filename.endswith('.jpeg'):

        # Extract the filename without the extension
        basename = os.path.splitext(filename)[0]

        # Check if a matching text file exists
        if os.path.exists(os.path.join(args.path, basename + '.txt')):
            continue  # The image has a matching text file, so move on to the next file
        else:
            # Move the image file to the destination folder
            shutil.move(os.path.join(args.path, filename), os.path.join(destination_folder, filename))