import os
import shutil
import cv2
import numpy as np
import OpenEXR
import Imath
from tqdm import tqdm

# === CONFIGURATION ===
source_folder = r"E:\RDR2 Maps\2025\PHOTOGRAMMETRY\CAPTURES\Valentine Depth Test"

# Step folders
renamed_folder = os.path.join(source_folder, "RENAMED")
final_folder = os.path.join(source_folder, "FINAL")
exr_folder = os.path.join(source_folder, "EXR")

# Depth settings
depth_out_bitdepth = 16
white_threshold = 0.999  # 99.9%
depth_scale = 1000.0     # Convert mm ➜ meters for EXR export

# Create folders
os.makedirs(renamed_folder, exist_ok=True)
os.makedirs(final_folder, exist_ok=True)
os.makedirs(exr_folder, exist_ok=True)

# === OpenEXR Setup ===
FLOAT = Imath.PixelType(Imath.PixelType.FLOAT)

def write_exr(output_path, R, G, B, Z):
    height, width = R.shape
    header = OpenEXR.Header(width, height)

    header['channels'] = {
        'R': Imath.Channel(FLOAT),
        'G': Imath.Channel(FLOAT),
        'B': Imath.Channel(FLOAT),
        'Z': Imath.Channel(FLOAT)
    }

    exr = OpenEXR.OutputFile(output_path, header)

    exr.writePixels({
        'R': R.tobytes(),
        'G': G.tobytes(),
        'B': B.tobytes(),
        'Z': Z.tobytes()
    })

    exr.close()

# === UTILITIES ===

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
files = sorted([
    f for f in os.listdir(source_folder)
    if os.path.isfile(os.path.join(source_folder, f))
])

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

# === STEP 2: FILTER DEPTH & CONVERT TO 16-BIT ===
print("\n=== Step 2: Filter Depth Maps & Export to FINAL ===")

color_files = sorted([
    f for f in os.listdir(renamed_folder)
    if "_Color.png" in f
])

valid_frames = []
final_frame_count = 1
skipped_depths = 0

for color_file in tqdm(color_files, desc="Filtering frames", unit="frame"):
    base_name = color_file.replace("_Color.png", "")
    depth_file = f"{base_name}_Depth.png"

    color_src = os.path.join(renamed_folder, color_file)
    depth_src = os.path.join(renamed_folder, depth_file)

    new_color_name = f"Frame_{final_frame_count:03d}_Color.png"
    new_color_dst = os.path.join(final_folder, new_color_name)

    # Always copy color image
    shutil.copy2(color_src, new_color_dst)
    print(f"[{final_frame_count:03d}] Final Color saved: {new_color_name}")

    # Check depth
    valid_depth = False
    if os.path.exists(depth_src):
        if is_depth_map_valid(depth_src):
            new_depth_name = f"Frame_{final_frame_count:03d}_Depth.png"
            new_depth_dst = os.path.join(final_folder, new_depth_name)

            success = convert_depth_to_16bit(depth_src, new_depth_dst)
            if success:
                print(f"[{final_frame_count:03d}] Final Depth (16-bit) saved: {new_depth_name}")
                valid_depth = True
            else:
                print(f"[{final_frame_count:03d}] Depth conversion failed. Skipping depth.")
                skipped_depths += 1
        else:
            print(f"[{final_frame_count:03d}] Invalid depth map skipped.")
            skipped_depths += 1
    else:
        print(f"[{final_frame_count:03d}] No depth map found. Skipped.")
        skipped_depths += 1

    if valid_depth:
        valid_frames.append(f"Frame_{final_frame_count:03d}")

    final_frame_count += 1

# === STEP 3: EXPORT EXR ===
print("\n=== Step 3: Export to EXR ===")

for frame_id in tqdm(valid_frames, desc="Exporting EXRs", unit="frame"):
    color_path = os.path.join(final_folder, f"{frame_id}_Color.png")
    depth_path = os.path.join(final_folder, f"{frame_id}_Depth.png")

    # Load RGB color (already validated)
    color_img = cv2.imread(color_path, cv2.IMREAD_COLOR)
    if color_img is None:
        print(f"[ERROR] Failed to load color image: {color_path}")
        continue

    color_img = cv2.cvtColor(color_img, cv2.COLOR_BGR2RGB)
    color_img = color_img.astype(np.float32) / 255.0
    color_img = np.power(color_img, 2.2)  # Linearize sRGB ➜ Linear RGB

    R = color_img[:, :, 0]
    G = color_img[:, :, 1]
    B = color_img[:, :, 2]

    # Load depth (already validated and converted to 16-bit)
    depth_img = cv2.imread(depth_path, cv2.IMREAD_UNCHANGED)
    if depth_img is None:
        print(f"[ERROR] Failed to load depth image: {depth_path}")
        continue

    # Convert to float32 meters
    depth_img = depth_img.astype(np.float32) / depth_scale

    output_exr = os.path.join(exr_folder, f"{frame_id}.exr")

    try:
        write_exr(output_exr, R, G, B, depth_img)
        print(f"[OK] EXR saved: {output_exr}")
    except Exception as e:
        print(f"[ERROR] Failed EXR write for {frame_id}: {e}")

# === SUMMARY ===
print("\n=== Processing Complete ===")
print(f"Total frames processed: {final_frame_count - 1}")
print(f"Depth maps skipped (invalid or missing): {skipped_depths}")
print(f"EXRs generated: {len(valid_frames)}")
