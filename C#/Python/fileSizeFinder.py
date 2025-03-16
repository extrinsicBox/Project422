import os

# Define the directory to scan
directory = r"E:\RDR2 Maps\2025\SCRIPTING\Project421"

# Collect file paths and sizes
file_sizes = []

# Walk through directory
for foldername, subfolders, filenames in os.walk(directory):
    for filename in filenames:
        filepath = os.path.join(foldername, filename)
        try:
            filesize = os.path.getsize(filepath)
            file_sizes.append((filesize, filepath))
        except (OSError, FileNotFoundError):
            # Skip files you can't access
            pass

# Sort by file size (descending)
file_sizes.sort(reverse=True, key=lambda x: x[0])

# Show top 10 largest files
print(f"\nTop 10 Largest Files in: {directory}\n")
print("{:<10} {:<}".format("Size (MB)", "File Path"))
print("-" * 60)

for size, path in file_sizes[:10]:
    size_mb = size / (1024 * 1024)
    print(f"{size_mb:<10.2f} {path}")

