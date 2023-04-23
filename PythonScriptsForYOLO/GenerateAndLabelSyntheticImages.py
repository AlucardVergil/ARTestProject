import cv2
import numpy as np
import random
import os

# Define paths and file names
object_path = os.getcwd() + '/cropped/object.png' #'path/to/cropped/object.png'
background_path = os.getcwd() + '/backgrounds/image.png'  #'path/to/background/image.png'
output_path = os.getcwd() + '/output/' #'path/to/output/directory/'
label_file = os.getcwd() + '/labels/label.txt'  #'path/to/label/file.txt'

# Load object and background images
object_img = cv2.imread(object_path, cv2.IMREAD_UNCHANGED)
background_img = cv2.imread(background_path)

# Define object size
object_width, object_height = object_img.shape[1], object_img.shape[0]

# Define maximum x and y offsets for object placement
max_x_offset = background_img.shape[1] - object_width
max_y_offset = background_img.shape[0] - object_height

# Open label file for writing
with open(label_file, 'w') as f:

    # Generate 10 synthetic images
    for i in range(10):
        
        # Randomly scale, rotate and place the object
        scale_factor = random.uniform(0.5, 1.5)
        angle = random.randint(0, 360)
        x_offset = random.randint(0, max_x_offset)
        y_offset = random.randint(0, max_y_offset)
        M = cv2.getRotationMatrix2D((object_width//2, object_height//2), angle, scale_factor)
        M[0,2] += x_offset
        M[1,2] += y_offset
        object_img_rotated = cv2.warpAffine(object_img, M, (object_width, object_height))
        
        # Transform object coordinates based on scaling and rotation
        object_corners = np.array([[0, 0], [object_width, 0], [object_width, object_height], [0, object_height]])
        object_corners_rotated = np.matmul(np.concatenate([object_corners, np.ones((4, 1))], axis=-1), M.T)
        x_min, y_min = int(np.min(object_corners_rotated[:, 0])), int(np.min(object_corners_rotated[:, 1]))
        x_max, y_max = int(np.max(object_corners_rotated[:, 0])), int(np.max(object_corners_rotated[:, 1]))
        
        # Place the object in a new background image
        background_img_copy = background_img.copy()
        object_img_copy = object_img_rotated.copy()
        object_img_mask = object_img_copy[:,:,3]
        object_img_mask = cv2.merge((object_img_mask, object_img_mask, object_img_mask))
        background_img_copy[y_min:y_max, x_min:x_max] = cv2.bitwise_and(object_img_copy[:,:,0:3], object_img_mask[:,:,0:3]) + \
                                                        cv2.bitwise_and(background_img_copy[y_min:y_max, x_min:x_max], cv2.bitwise_not(object_img_mask[:,:,0:3]))
        
        # Save the synthetic image
        filename = 'synthetic_' + str(i) + '.png'
        output_file_path = os.path.join(output_path, filename)
        cv2.imwrite(output_file_path, background_img_copy)
        
        # Write the label information to the label file
        label_string = '0 ' + str(x_min) + ' ' + str(y_min) + ' ' + str(x_max) + ' ' + str(y_max) + '\n'
        f.write(label_string)