import Metashape
import os


final_folder = r"E:\RDR2 Maps\2025\PHOTOGRAMMETRY\CAPTURES\Valentine Depth Test\FINAL"


doc = Metashape.app.document
chunk = doc.addChunk()


photos = []
for file in sorted(os.listdir(final_folder)):
    if file.endswith("_Color.png"):
        photos.append(os.path.join(final_folder, file))

print("Collected", len(photos), "color images.")


chunk.addPhotos(photos)
print(f"Added {len(photos)} cameras to chunk.")


for camera in chunk.cameras:
    photo_path = camera.photo.path
    base_name = os.path.basename(photo_path).replace("_Color.png", "")

    depth_filename = f"{base_name}_Depth.png"
    depth_path = os.path.join(final_folder, depth_filename)

    if os.path.exists(depth_path):
        camera.loadDepth(depth_path)
        print(f"Depth map loaded for: {camera.label}")
    else:
        print(f"No depth map found for: {camera.label}")

print("\n✅ Import complete. Ready to build mesh.")
