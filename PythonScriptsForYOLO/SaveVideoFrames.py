import os
import cv2
import argparse

# Set up the argument parser
parser = argparse.ArgumentParser(description='Extract every nth frame from a video and save it to a folder.')
parser.add_argument('-v', '--video', type=str, help='path to the video file')
parser.add_argument('-d', '--destination', type=str, help='path to the destination folder', default=os.getcwd() + '/OutputFrames/')
parser.add_argument('-n', type=int, help='select every nth frame', default=10)

# Parse the arguments
args = parser.parse_args()

# Extract the video path, destination folder, and n from the arguments
video_path = args.video
destination = args.destination
n = args.n

# Open the video file for reading
cap = cv2.VideoCapture(video_path)

# Get the total number of frames in the video
total_frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))

# Set the initial frame index to 0
frame_index = 0

# Loop through the video frames and save every nth frame to the destination folder
while True:      

	# Set the video file position to the next nth frame
	cap.set(cv2.CAP_PROP_POS_FRAMES, frame_index)
    
	# Read the next frame from the video file
	ret, frame = cap.read()
    
	# If we've reached the end of the video, break out of the loop
	if not ret:
		break	
	
    # Construct the file name for the saved frame
	file_name = os.path.join(destination, f"frame_{frame_index}.jpg")
        
    # Save the frame to the destination folder
	cv2.imwrite(file_name, frame)
		
	# Print a progress message indicating the current progress
	print(f"Processed frame {frame_index} of {total_frames} ({frame_index/total_frames:.2%})")		
		
    # Increment the frame index
	frame_index += n
    

    # Check if we've reached the end of the video
	if frame_index >= total_frames:
		break

# Release the video file handle
cap.release()