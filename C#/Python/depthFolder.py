import os
import shutil

# Folder paths
source_folder = r'E:\RDR2 Maps\2025\PHOTOGRAMMETRY\CAPTURES\Valentine Depth Test\FINAL'
depth_folder = r'E:\RDR2 Maps\2025\PHOTOGRAMMETRY\CAPTURES\Valentine Depth Test\DEPTH'
color_folder = r'E:\RDR2 Maps\2025\PHOTOGRAMMETRY\CAPTURES\Valentine Depth Test\COLOR'

# Create the destination folders if they don't exist
os.makedirs(depth_folder, exist_ok=True)
os.makedirs(color_folder, exist_ok=True)

# Loop through all files in the source folder
for filename in os.listdir(source_folder):
    file_path = os.path.join(source_folder, filename)
    
    # Check if it's a file and if "depth" or "color" is in the filename (case-insensitive)
    if os.path.isfile(file_path):
        if 'depth' in filename.lower():
            # Build the destination path for depth files
            destination_path = os.path.join(depth_folder, filename)
            # Copy the file
            shutil.copy(file_path, destination_path)
            print(f'Copied to DEPTH: {filename}')
        elif 'color' in filename.lower():
            # Build the destination path for color files
            destination_path = os.path.join(color_folder, filename)
            # Copy the file
            shutil.copy(file_path, destination_path)
            print(f'Copied to COLOR: {filename}')

print('Done copying files!')