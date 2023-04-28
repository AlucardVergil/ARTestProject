import os

folder_path = 'objSamsung/'

# Get list of all image files
image_files = [f for f in os.listdir(folder_path) if f.endswith('.jpg')]

# Loop through each image and get the corresponding bounding box info
annotations = []
for image_file in image_files:
    image_path = os.path.join(folder_path, image_file)
    txt_file = os.path.splitext(image_file)[0] + '.txt'
    txt_path = os.path.join(folder_path, txt_file)
    with open(txt_path, 'r') as f:
        bbox_info = f.readline().strip().split(' ')
        bbox = ','.join(bbox_info[1:]) + ',' + bbox_info[0]
        annotations.append(f'{image_path} {bbox}')

# Write the annotations to a text file
with open('annotations.txt', 'w') as f:
    for annotation in annotations:
        f.write(annotation + '\n')