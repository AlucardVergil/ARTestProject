import os
import argparse

# Set up command-line arguments
parser = argparse.ArgumentParser(description='Remove images without matching text files')
parser.add_argument('-p', '--path', type=str, help='Directory path', default=os.getcwd() + '/')

# Parse the command-line arguments
args = parser.parse_args()

# Loop through all the files in the directory
for filename in os.listdir(args.path):
    
    # Check if the file is an image file
    if filename.endswith('.jpg') or filename.endswith('.png') or filename.endswith('.jpeg'):
        
        # Extract the filename without the extension
        basename = os.path.splitext(filename)[0]
        
        # Check if a matching text file exists
        if os.path.exists(os.path.join(args.path + '/', basename + '.txt')):
            continue  # The image has a matching text file, so move on to the next file
        else:
            os.remove(os.path.join(args.path + '/', filename))  # The image does not have a matching text file, so delete it