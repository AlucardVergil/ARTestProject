import cv2
import os

# Define a threshold to determine whether an image is blurry or not
THRESHOLD = 110

# Define the path to the folder containing the images
IMAGE_FOLDER = os.getcwd()

# Loop through each image in the folder
for filename in os.listdir(IMAGE_FOLDER):
    # Load the image and convert it to grayscale
    image = cv2.imread(os.path.join(IMAGE_FOLDER, filename))
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

    # Compute the variance of Laplacian
    variance = cv2.Laplacian(gray, cv2.CV_64F).var()

    # If the variance is below the threshold, the image is blurry
    if variance < THRESHOLD:
        print(f'{filename} is blurry and will be removed.')
        os.remove(os.path.join(IMAGE_FOLDER, filename))