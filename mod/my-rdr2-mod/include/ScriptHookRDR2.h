// filepath: /my-rdr2-mod/my-rdr2-mod/include/ScriptHookRDR2.h
#ifndef SCRIPTHOOKRDR2_H
#define SCRIPTHOOKRDR2_H

// Include necessary headers
#include <cstdint>

// Function declarations for ScriptHookRDR2 SDK
extern "C" {
    void scriptRegister();
    void scriptUnregister();
    void keyboardHandlerRegister();
    void keyboardHandlerUnregister();
    
    // Entity functions
    void worldGetAllVehicles();
    void worldGetAllPeds();
    void worldGetAllObjects();
    
    // Entity management functions
    bool isEntityAMissionEntity(int32_t entity);
    void setEntityAsMissionEntity(int32_t entity, bool mission);
}

// Additional type definitions can be added here

#endif // SCRIPTHOOKRDR2_H