#include <Windows.h>
#include "ScriptHookRDR2.h"
#include "GCodeParser.h"
#include "other_headers.h"

// Function prototypes
void InitMod();
void CleanupMod();
void RegisterKeyboardHandler();
void UnregisterKeyboardHandler();
void simulateDroneFlight(const std::vector<GCodeCommand>& commands);
void onKeyPress();
void keyboardHandler(DWORD key, WORD repeats, BYTE scanCode, BOOL isExtended, BOOL isWithAlt, BOOL wasDownBefore, BOOL isUpNow);

// DllMain function - entry point for the mod
extern "C" __declspec(dllexport) int DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    switch (fdwReason) {
        case DLL_PROCESS_ATTACH:
            InitMod();
            break;
        case DLL_PROCESS_DETACH:
            CleanupMod();
            break;
    }
    return TRUE;
}

// Initialize the mod
void InitMod() {
    RegisterKeyboardHandler();
    // Additional initialization code can go here
}

// Cleanup the mod
void CleanupMod() {
    UnregisterKeyboardHandler();
    // Additional cleanup code can go here
}

// Register the keyboard handler
void RegisterKeyboardHandler() {
    keyboardHandlerRegister(keyboardHandler);
}

// Unregister the keyboard handler
void UnregisterKeyboardHandler() {
    keyboardHandlerUnregister(keyboardHandler);
}

// Simulate drone flight
void simulateDroneFlight(const std::vector<GCodeCommand>& commands) {
    for (const auto& cmd : commands) {
        // Use native functions to move the drone to the specified coordinates
        // Example: moveDroneTo(cmd.x, cmd.y, cmd.z, cmd.a, cmd.b, cmd.f);
        std::this_thread::sleep_for(std::chrono::milliseconds(static_cast<int>(cmd.f)));
    }
}

// Handle key press
void onKeyPress() {
    GCodeParser parser("scripts/flightpath.gcode");
    std::vector<GCodeCommand> commands = parser.parse();
    simulateDroneFlight(commands);
}

// Keyboard handler function
void keyboardHandler(DWORD key, WORD repeats, BYTE scanCode, BOOL isExtended, BOOL isWithAlt, BOOL wasDownBefore, BOOL isUpNow) {
    if (key == VK_F7 && !wasDownBefore && !isUpNow) {
        onKeyPress();
    }
}