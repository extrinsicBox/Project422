import os
import shutil
import cv2
import numpy as np

# === CONFIGURATION ===
source_folder = r"E:\RDR2 Maps\2025\PHOTOGRAMMETRY\CAPTURES\Valentine Depth Test"
renamed_folder = os.path.join(source_folder, "RENAMED")
final_folder = os.path.join(source_folder, "FINAL")

depth_out_bitdepth = 16
white_threshold = 0.999  # 99.9%

# Create folders
os.makedirs(renamed_folder, exist_ok=True)
os.makedirs(final_folder, exist_ok=True)

# Filter only files
files = sorted([
    f for f in os.listdir(source_folder)
    if os.path.isfile(os.path.join(source_folder, f))
])

def is_depth_map_valid(depth_path):
    """Check if depth map is flat white"""
    img = cv2.imread(depth_path, cv2.IMREAD_GRAYSCALE)
    if img is None:
        print(f"ERROR: Could not read {depth_path}")
        return False

    min_val = np.min(img)
    max_val = np.max(img)
    mean_val = np.mean(img)
    std_dev = np.std(img)

    white_pixels = np.sum(img >= 250)
    total_pixels = img.size
    white_ratio = white_pixels / total_pixels

    print(f"Checking: {os.path.basename(depth_path)}")
    print(f"  Min: {min_val}, Max: {max_val}, Mean: {mean_val:.2f}, StdDev: {std_dev:.2f}")
    print(f"  White Ratio: {white_ratio:.4f}")

    if std_dev < 1.0 or white_ratio >= white_threshold:
        print("  --> Skipped: flat or too white.\n")
        return False
    else:
        print("  --> Accepted.\n")
        return True

def convert_depth_to_16bit(input_path, output_path):
    """Convert 8-bit depth map to 16-bit PNG"""
    img = cv2.imread(input_path, cv2.IMREAD_GRAYSCALE)
    if img is None:
        print(f"ERROR: Could not read {input_path}")
        return False
    img_16bit = (img.astype(np.uint16)) * 257  # Scale 0-255 to 0-65535
    cv2.imwrite(output_path, img_16bit)
    return True

# === STEP 1: RENAME (Color + Depth Pairs) ===
print("\n=== Step 1: Rename Files to RENAMED ===")
frame_count = 1
i = 0

while i < len(files) - 1:
    color_file = files[i]
    depth_file = files[i + 1]

    color_src = os.path.join(source_folder, color_file)
    depth_src = os.path.join(source_folder, depth_file)

    renamed_color = f"Frame_{frame_count:03d}_Color.png"
    renamed_depth = f"Frame_{frame_count:03d}_Depth.png"

    renamed_color_path = os.path.join(renamed_folder, renamed_color)
    renamed_depth_path = os.path.join(renamed_folder, renamed_depth)

    shutil.copy2(color_src, renamed_color_path)
    shutil.copy2(depth_src, renamed_depth_path)

    print(f"[{frame_count:03d}] Renamed Color: {color_file} --> {renamed_color}")
    print(f"[{frame_count:03d}] Renamed Depth: {depth_file} --> {renamed_depth}")

    i += 2
    frame_count += 1

# === STEP 2: FILTER & CONVERT AFTER FILTERING ===
print("\n=== Step 2: Filter Depth Maps & Export to FINAL ===")

color_files = sorted([
    f for f in os.listdir(renamed_folder)
    if "_Color.png" in f
])

final_frame_count = 1
skipped_depths = 0

for color_file in color_files:
    base_name = color_file.replace("_Color.png", "")
    depth_file = f"{base_name}_Depth.png"

    color_src = os.path.join(renamed_folder, color_file)
    depth_src = os.path.join(renamed_folder, depth_file)

    new_color_name = f"Frame_{final_frame_count:03d}_Color.png"
    new_color_dst = os.path.join(final_folder, new_color_name)

    # Always copy color image
    shutil.copy2(color_src, new_color_dst)
    print(f"[{final_frame_count:03d}] Final Color saved: {new_color_name}")

    # Check if depth exists
    if os.path.exists(depth_src):
        if is_depth_map_valid(depth_src):
            new_depth_name = f"Frame_{final_frame_count:03d}_Depth.png"
            new_depth_dst = os.path.join(final_folder, new_depth_name)

            success = convert_depth_to_16bit(depth_src, new_depth_dst)
            if success:
                print(f"[{final_frame_count:03d}] Final Depth (16-bit) saved: {new_depth_name}")
            else:
                print(f"[{final_frame_count:03d}] Depth conversion failed. Skipping depth.")
                skipped_depths += 1
        else:
            print(f"[{final_frame_count:03d}] Invalid depth map skipped.")
            skipped_depths += 1
    else:
        print(f"[{final_frame_count:03d}] No depth map found. Skipped.")
        skipped_depths += 1

    final_frame_count += 1

# === Summary ===
print("\n=== Processing Complete ===")
print(f"Total frames processed: {final_frame_count - 1}")
print(f"Depth maps skipped (invalid or missing): {skipped_depths}")
