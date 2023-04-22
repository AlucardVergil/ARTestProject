import cv2
import os
import shutil

# Define a threshold to determine whether an image is blurry or not
THRESHOLD = 120

# Define the path to the folder containing the images
IMAGE_FOLDER = './'

# Define the path to the folder where blurry images will be moved to
BLURRY_FOLDER = './blurry_images/'

# Create the blurry folder if it doesn't exist
if not os.path.exists(BLURRY_FOLDER):
    os.makedirs(BLURRY_FOLDER)

# Loop through each image in the folder
for filename in os.listdir(IMAGE_FOLDER):
    # Load the image and convert it to grayscale
    image = cv2.imread(os.path.join(IMAGE_FOLDER, filename))
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Compute the variance of Laplacian
    variance = cv2.Laplacian(gray, cv2.CV_64F).var()

    # If the variance is below the threshold, the image is blurry
    if variance < THRESHOLD:
        print(f'{filename} is blurry and will be moved to {BLURRY_FOLDER}.')
        shutil.move(os.path.join(IMAGE_FOLDER, filename), os.path.join(BLURRY_FOLDER, filename))