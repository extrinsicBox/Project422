import os
import cv2
import numpy as np

# === CONFIGURATION ===
depth_folder = r"E:/RDR2 Maps/2025/PHOTOGRAMMETRY/CAPTURES/Valentine Depth Test/DEPTH"
output_folder = os.path.join(depth_folder, "XYZ_EXPORT")

depth_file_suffix = "_Depth.png"  # Match your naming convention

scale_xy = 1.0  # X/Y pixel spacing (world units)
scale_z = 0.1   # Z depth scale factor (world units)

invert_depth = False  # Set True if white = far in your depth maps
subsample_rate = 4    # Keep every Nth point (increase to thin out the cloud)

# Optional resize
resize_to = None  # Example: (2048, 1024), set to None to skip

# === Create Output Folder ===
os.makedirs(output_folder, exist_ok=True)

# === Function to Convert Depth Map to XYZ Point Cloud ===
def depth_to_pointcloud(depth_path, output_path, scale_xy=1.0, scale_z=0.1, invert=False):
    print(f"Processing {depth_path}")

    # Load depth map
    depth_img = cv2.imread(depth_path, cv2.IMREAD_UNCHANGED)

    if depth_img is None:
        print(f"Failed to read: {depth_path}")
        return False

    # Optional resize
    if resize_to:
        depth_img = cv2.resize(depth_img, resize_to, interpolation=cv2.INTER_AREA)

    height, width = depth_img.shape

    # Normalize depth values
    if depth_img.dtype == np.uint16:
        depth = depth_img.astype(np.float32) / 65535.0
    else:
        depth = depth_img.astype(np.float32) / 255.0

    if invert:
        depth = 1.0 - depth

    # Apply Z scaling
    z = depth * scale_z

    # Generate X and Y grids
    x_range = np.arange(0, width) * scale_xy
    y_range = np.arange(0, height) * scale_xy
    x_grid, y_grid = np.meshgrid(x_range, y_range)

    # Flatten to 1D arrays
    x_flat = x_grid.flatten()
    y_flat = y_grid.flatten()
    z_flat = z.flatten()

    # Combine into Nx3 array
    points = np.vstack((x_flat, y_flat, z_flat)).transpose()

    # Subsample points
    points_subsampled = points[::subsample_rate]

    print(f"Original points: {len(points)}, Subsampled: {len(points_subsampled)}")

    # Save as XYZ text file
    np.savetxt(output_path, points_subsampled, fmt="%.6f")

    print(f"Saved point cloud: {output_path}")

    return True

# === Process All Depth Maps in Folder ===
depth_files = [
    f for f in os.listdir(depth_folder)
    if f.endswith(depth_file_suffix)
]

if not depth_files:
    print("No depth maps found.")
else:
    print(f"Found {len(depth_files)} depth maps.")

for depth_file in depth_files:
    depth_path = os.path.join(depth_folder, depth_file)
    base_name = os.path.splitext(depth_file)[0].replace(depth_file_suffix.replace(".png", ""), "")
    output_xyz = os.path.join(output_folder, f"{base_name}.xyz")

    depth_to_pointcloud(depth_path, output_xyz, scale_xy, scale_z, invert_depth)

print("\n✅ Done! Subsampled XYZ files saved to:", output_folder)
