//PREVENT CINEMATIC
//EMIT FLAMES FROM BOOTS
//Add slight BG under telemetry
//Nudge telemetry down for screen cap
//Add ground Z plot with no smoothing
    //GOOD COORDS
    //(-1562f, 1325f, 300f);
    // User-defined settings
using System;
using System.Drawing;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;

public class PhotogrammetryFlight : Script
{
    // User-defined settings
    private const float TargetAltitude = 200.0f; // Fixed altitude in meters
    private static readonly Vector3 NW_Corner = new Vector3(-2642.58f, 1058.02f, 250f); // Start of flight grid
    private const float MoveSpeed = 2f; // Reduced speed for accurate flight
    private float InitialHeading = 90; // Default: East
    private float CameraPitchLock = -90.0f; // Default: -85 degrees
    private bool isFlying = false; // Tracks if flight mode is active
    private bool isTeleported = false; // Tracks if teleportation has occurred
    private bool isFirstPerson = false; // Tracks first-person mode
    private float lastCameraHeading = 90f; // Store last stable camera heading
    private float previousAGL = 0.0f; // Store previous Above Ground Level (AGL) height
    private const float AltitudeChangeThreshold = 500.0f; // Limit altitude jumps (AKA HOW QUCK ACENT/DECENTs are)
    
    public PhotogrammetryFlight()
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

            // Teleport player to start position at altitude 500
            Vector3 startPosition = new Vector3(NW_Corner.X, NW_Corner.Y, 500);
            playerPed.Position = startPosition;
            Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, true); // FREEZE_ENTITY_POSITION
            Function.Call(0x4D89D607CB3DD1D2, playerPed.Handle, false); // SET_ENTITY_HAS_GRAVITY

            Wait(100);

            Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, false); // FREEZE_ENTITY_POSITION
            Function.Call(0x4D89D607CB3DD1D2, playerPed.Handle, true); // SET_ENTITY_HAS_GRAVITY

            // Derive ground height from AGL
            float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle); // GET_ENTITY_HEIGHT_ABOVE_GROUND
            float groundZ = playerPed.Position.Z - currentAGL;
            playerPed.Position = new Vector3(NW_Corner.X, NW_Corner.Y, groundZ + 200f); // Place player slightly above ground

            // Set player heading to the variable InitialHeading
            Function.Call(0xCF2B9C0645C4651B, playerPed.Handle, InitialHeading-180); // SET_ENTITY_HEADING
            
            // Re-enable gravity so player naturally lands
            Function.Call(0x4D89D607CB3DD1D2, playerPed.Handle, true); // SET_ENTITY_HAS_GRAVITY
            Function.Call(0xA5C38736C426FCB8, playerPed.Handle, true); // SET_ENTITY_INVINCIBLE

            // Set weather to sunny
            Function.Call(0xFA3E3CA8A1DE6D5D, 0x614A1F91, true, false, true, 0.0f, false); // MISC.SET_CURR_WEATHER_STATE (Sunny)
            Function.Call(0xD74ACDF7DB8114AF, 1.0f);
            // Freeze time at 10:30 AM
            Function.Call(0x3A52C59FFB2DEED8, 9, 15, 0); // CLOCK.SET_CLOCK_TIME (10:30 AM)
        }
        else if (e.KeyCode == Keys.F10 && isTeleported) // Start/Pause Flight (only if teleported first)
        {
            // Spawn explosion at player's feet
           Function.Call(0x7D6F58F69DA92530, Game.Player.Character.Position.X, Game.Player.Character.Position.Y, Game.Player.Character.Position.Z, 28, 5.0f, true, false, 1.0f);
      Wait(100);
         // Start/Pause Flight (only if teleported first)
        {
            isFlying = !isFlying;

            if (isFlying)
            {
                Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, true); // UNFREEZE_ENTITY_POSITION
                Vector3 homePosition = new Vector3(NW_Corner.X, NW_Corner.Y, playerPed.Position.Z);
                playerPed.Position = homePosition;
                
                // Enable T-Pose (noclip) and rotate entity pitch -90 degrees
                //Function.Call(0x9CC8314DFEDE441E, playerPed.Handle, -90f, 0f, InitialHeading, 2, true); // SET_ENTITY_ROTATION
                Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, false); // UNFREEZE_ENTITY_POSITION
            }
            else
            {
                Function.Call(0x7D9EFB7AD6B19754, playerPed.Handle, true); // FREEZE_ENTITY_POSITION
            }
        }
    }}

    private float smoothedTargetZ = TargetAltitude;
    private const float AltitudeSmoothingFactor = .006f; // Adjust for smoother altitude transitions

    private void OnTick(object sender, EventArgs e)
    {
        Ped playerPed = Game.Player.Character; // Ensure player entity is referenced

        if (isFlying)
        {
            // Get Above Ground Level height
            float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle); // GET_ENTITY_HEIGHT_ABOVE_GROUND
            float groundHeight = playerPed.Position.Z - currentAGL;
            float rawTargetZ = groundHeight + TargetAltitude;

            // Prevent extreme altitude jumps
            if (Math.Abs(currentAGL - previousAGL) >= AltitudeChangeThreshold)
            {
                rawTargetZ = smoothedTargetZ; // Ignore large jumps
            }
            else
            {
                previousAGL = currentAGL; // Update stable AGL
            }

            // Apply smoothing to prevent sudden altitude changes
            smoothedTargetZ = smoothedTargetZ + (rawTargetZ - smoothedTargetZ) * AltitudeSmoothingFactor;

            // Move forward in X-direction while keeping altitude stable
            Vector3 newPosition = new Vector3(playerPed.Position.X + MoveSpeed, playerPed.Position.Y, smoothedTargetZ);
            playerPed.Position = newPosition;

            Function.Call(0xCF2B9C0645C4651B, playerPed.Handle, InitialHeading-180); // SET_ENTITY_HEADING
        }

        if (isFirstPerson)
        {
            Function.Call(0x36CDD81627A6FCD2); // HIDE_HUD_AND_RADAR_THIS_FRAME
        }
    }
}
