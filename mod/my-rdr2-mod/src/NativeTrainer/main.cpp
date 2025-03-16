// filepath: /my-rdr2-mod/my-rdr2-mod/src/NativeTrainer/main.cpp
#include "ScriptHookRDR2.h"
#include "keyboard.h"

void scriptRegister();
void scriptUnregister();

extern "C" __declspec(dllexport) void DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved) {
    switch (fdwReason) {
        case DLL_PROCESS_ATTACH:
            keyboardHandlerRegister();
            scriptRegister();
            break;
        case DLL_PROCESS_DETACH:
            scriptUnregister();
            keyboardHandlerUnregister();
            break;
    }
}

void scriptRegister() {
    // Register your script commands here
}

void scriptUnregister() {
    // Unregister your script commands here
}