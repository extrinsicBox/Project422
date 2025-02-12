//PREVENT CINEMATIC
//USE CLAMP FUNCTIONS

using System;
using System.Drawing;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;

public class autoPilot : Script
{
    // User-defined settings
    private const float TargetAltitude = 100.0f; // Fixed altitude in meters
    private static readonly Vector3 NW_Corner = new Vector3(-1000f, -10f, 100f); // Start of flight grid
    private const float MoveSpeed = 0.25f; // Reduced speed for accurate flight
    private float InitialHeading = 0f; // Default: East
    private float CameraPitchLock = -90.0f; // Default: -85 degrees
    private bool isFlying = false; // Tracks if flight mode is active
    private bool isTeleported = false; // Tracks if teleportation has occurred
    private bool isFirstPerson = false; // Tracks first-person mode
    private float lastCameraHeading = -90f; // Store last stable camera heading
    
    public autoPilot()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        Ped playerPed = Game.Player.Character; // Get player entity

        if (e.KeyCode == Keys.F9) // Teleport Home & Stop Flight
        {
            isFlying = false;
            isTeleported = true;
            isFirstPerson = true;

            // Teleport player to start position at fixed altitude
            Vector3 startPosition = new Vector3(NW_Corner.X, NW_Corner.Y, TargetAltitude);
            playerPed.Position = startPosition;

            // Set player heading to the variable InitialHeading
            Function.Call(0xCF2B9C0645C4651B, playerPed.Handle, InitialHeading); // SET_ENTITY_HEADING

            // Enable first-person mode
            Function.Call(0x90DA5BA5C2635416, true); // FORCE_FIRST_PERSON_CAM_THIS_FRAME
            Function.Call(0xFB760AF4F537B8BF, CameraPitchLock, 1.0f); // SET_GAMEPLAY_CAM_RELATIVE_PITCH

            // Hide player model
            Function.Call(0x1794B4FCC84D812F, playerPed.Handle, false); // SET_ENTITY_VISIBLE (Hide Player)

            // Prevent falling, keep frozen
            playerPed.Velocity = Vector3.Zero;
            Function.Call(0xA5C38736C426FCB8, playerPed.Handle, true); // SET_ENTITY_INVINCIBLE
            Function.Call(0x4D89D607CB3DD1D2, playerPed.Handle, false); // SET_ENTITY_HAS_GRAVITY
            Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, true); // FREEZE_ENTITY_POSITION
          
            
        
    // Set weather to sunny
Function.Call(0xFA3E3CA8A1DE6D5D, 0x614A1F91, true, false, true, 0.0f, false); // MISC.SET_CURR_WEATHER_STATE (Sunny)
Function.Call(0xD74ACDF7DB8114AF, 1.0f);
// Freeze time at 10:30 AM
Function.Call(0x3A52C59FFB2DEED8, 10, 30, 0); // CLOCK.SET_CLOCK_TIME (10:30 AM)
}

        else if (e.KeyCode == Keys.F10 && isTeleported) // Start/Pause Flight (only if teleported first)
{  
    isFlying = !isFlying;

    if (isFlying)
    {
        // Preserve altitude when starting flight
        float currentZ = playerPed.Position.Z;
        
        Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, false); // UNFREEZE_ENTITY_POSITION
        
        // Ensure the player starts flying from the correct altitude
       Vector3 homePosition = new Vector3(NW_Corner.X, NW_Corner.Y, TargetAltitude);
            playerPed.Position = homePosition;
    }
    else
    {
        Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, true); // FREEZE_ENTITY_POSITION
    }

    // Maintain first-person mode and hidden player model when pausing
    Function.Call(0x90DA5BA5C2635416, true); // FORCE_FIRST_PERSON_CAM_THIS_FRAME
    Function.Call(0x1794B4FCC84D812F, playerPed.Handle, false); // Ensure Player Model Stays Hidden
    //Function.Call(0x5D1EB123EAC5D071, lastCameraHeading, 1.0f); // SET_GAMEPLAY_CAM_RELATIVE_HEADING
    //Function.Call(0xFB760AF4F537B8BF, CameraPitchLock, 1.0f); // SET_GAMEPLAY_CAM_RELATIVE_PITCH
}

    }

// Store previous ground height for stable altitude control
private float previousGroundHeight = 0.0f;

private void OnTick(object sender, EventArgs e)
{
    Ped playerPed = Game.Player.Character; // Ensure player entity is referenced

    if (isFlying)
    {
        // Get actual ground height below player
        float groundHeight = playerPed.Position.Z - Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle); // GET_ENTITY_HEIGHT_ABOVE_GROUND

        // Corrected target altitude: Always set Z to GroundHeight + 100m directly
        float targetZ = groundHeight +TargetAltitude;

        // Move forward in X-direction (due East) while keeping altitude fixed
        Vector3 newPosition = new Vector3(playerPed.Position.X + MoveSpeed, playerPed.Position.Y, targetZ);
        playerPed.Position = newPosition;

        // Lock player heading to 0 (East) for movement
        Function.Call(0xCF2B9C0645C4651B, playerPed.Handle, -90f); // SET_ENTITY_HEADING
    }

    // Ensure first-person mode and hidden HUD
    if (isFirstPerson)
    {
        Function.Call(0x90DA5BA5C2635416, true); // FORCE_FIRST_PERSON_CAM_THIS_FRAME
        Function.Call(0x36CDD81627A6FCD2); // HIDE_HUD_AND_RADAR_THIS_FRAME
        Function.Call(0x1794B4FCC84D812F, playerPed.Handle, false); // Ensure Player Model Stays Hidden
    }
}
}

