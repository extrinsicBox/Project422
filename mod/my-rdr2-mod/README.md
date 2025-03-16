# My RDR2 Mod

## Overview
This project is a mod for Red Dead Redemption 2 that utilizes the ScriptHookRDR2 SDK to enhance gameplay through custom scripts and features.

## Features
- **Native Trainer**: A built-in trainer that allows players to access various game functionalities and commands.
- **Entity Pool Management**: Efficient handling of game entities such as vehicles, peds, and objects.
- **Keyboard Hook**: Custom keyboard handling to trigger mod actions seamlessly.

## Project Structure
```
my-rdr2-mod
├── src
│   ├── main.cpp               # Main entry point of the mod
│   ├── NativeTrainer
│   │   ├── main.cpp           # Implementation of Native Trainer functionality
│   │   └── keyboard.cpp       # Keyboard hook implementation
│   └── Pools
│       └── script.cpp         # Entity pool management
├── include
│   ├── ScriptHookRDR2.h       # Declarations for ScriptHookRDR2 SDK
│   └── other_headers.h        # Additional declarations
├── build
│   ├── Makefile               # Build instructions
│   └── build_configurations    # Additional build configurations
├── .gitignore                 # Files to ignore in version control
└── README.md                  # Project documentation
```

## Setup Instructions
1. **Clone the Repository**: 
   ```
   git clone <repository-url>
   cd my-rdr2-mod
   ```

2. **Build the Mod**:
   - Navigate to the `build` directory and run the Makefile:
   ```
   make
   ```

3. **Install the Mod**:
   - Copy the compiled `.asi` file to your Red Dead Redemption 2 game directory.

4. **Run the Game**:
   - Launch Red Dead Redemption 2 and enjoy the mod features.

## Usage
- Use the keyboard shortcuts defined in the Native Trainer to access various functionalities.
- Refer to the source code for specific commands and their mappings.

## Contribution
Feel free to contribute to the project by submitting issues or pull requests. Make sure to follow the coding standards and guidelines outlined in the project.

## License
This project is licensed under the terms specified in the repository. Please refer to the license file for more details.