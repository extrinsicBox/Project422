using System;
using System.Windows.Forms;
using System.Drawing;
using RDR2;
using RDR2.Math;
using RDR2.Native;

public class GCodeFlightPath : Script
{
    private float smoothedTargetZ = 0.0f;
    private const float AltitudeSmoothingFactor = 0.1f;
    private float previousAGL = 0.0f;
    private const float AltitudeChangeThreshold = 100.0f; // Reduced from 1000.0f
    private bool isRotating = false;
    private float rotationStartTime = 0.0f;
    private float rotationDuration = 1.0f;
    private float initialHeading = 0.0f;
    private float targetHeading = 0.0f;

    public GCodeFlightPath()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
        
        // Load G-code data at startup
        SharedUtilities.LoadGCode("scripts/flightpath.gcode");
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F10)
        {
            SharedUtilities.IsMoving = !SharedUtilities.IsMoving;

            if (SharedUtilities.IsMoving)
            {
                StartFlightPath();
            }
            else
            {
                StopFlightPath();
            }
        }
        else if (e.KeyCode == Keys.F11)
        {
            SharedUtilities.ShowDebugInfo = !SharedUtilities.ShowDebugInfo;
        }
    }

    private void StartFlightPath()
    {
        Ped player = Game.Player.Character;
        
        Function.Call(0x7D9EFB7AD6B19754, player.Handle, true); // FREEZE_ENTITY_POSITION
        TeleportToWaypoint(0); // Ensure player is above ground before starting movement
        SharedUtilities.CurrentWaypointIndex = 0;
        
        if (SharedUtilities.Waypoints.Count > 0 && SharedUtilities.Speeds.Count > 0)
        {
            smoothedTargetZ = SharedUtilities.Waypoints[0].Z; // Set initial altitude
            Function.Call(0xCF2B9C0645C4651B, player.Handle, SharedUtilities.PlayerHeadings[0] - 90); // SET_ENTITY_HEADING
            Function.Call(0x449995EA846D3FC2, SharedUtilities.CameraPitches[0]); // SET_GAMEPLAY_CAM_INITIAL_PITCH
        }
        
        Function.Call(0x14F3947318CA8AD2, 0, 0); // Lock camera heading to 0 relative to player heading
        Function.Call(0xA5C38736C426FCB8, player.Handle, true); // SET_ENTITY_INVINCIBLE
        Function.Call(0x1794B4FCC84D812F, player.Handle, false); // SET_ENTITY_VISIBLE
        Function.Call(0x7D9EFB7AD6B19754, player.Handle, false); // UNFREEZE_ENTITY_POSITION
                
        // Set weather to sunny and freeze it
        Function.Call(0xFA3E3CA8A1DE6D5D, 0x614A1F91, true, false, true, 0.0f, false); // MISC.SET_CURR_WEATHER_STATE (Sunny)
        Function.Call(0xD74ACDF7DB8114AF, true); // _SET_WEATHER_TYPE_FROZEN

        // Freeze time at 10:30 AM
        Function.Call(0x3A52C59FFB2DEED8, 10, 30, 0); // CLOCK.SET_CLOCK_TIME (10:30 AM)
        Function.Call(0x4D1A590C92BF377E, true, 0); // PAUSE_CLOCK
    }

    private void StopFlightPath()
    {
        Ped player = Game.Player.Character;
        Function.Call(0x7D9EFB7AD6B19754, player.Handle, true); // FREEZE_ENTITY_POSITION
    }

    private void OnTick(object sender, EventArgs e)
    {
        Function.Call(0x36CDD81627A6FCD2); // HIDE_HUD_AND_RADAR_THIS_FRAME
        Function.Call(0x8910C24B7E0046EC); // _DISABLE_CINEMATIC_MODE_THIS_FRAME
        
        if (SharedUtilities.IsMoving && 
            SharedUtilities.CurrentWaypointIndex < SharedUtilities.Waypoints.Count && 
            SharedUtilities.Waypoints.Count > 0)
        {
            MoveAlongPath();
        }
        
        if (SharedUtilities.ShowDebugInfo)
        {
            DisplayDebugInfo();
        }
    }

    private void MoveAlongPath()
    {
        Ped player = Game.Player.Character;
        Vector3 currentPosition = new Vector3(
            (float)Math.Round(player.Position.X, 2),
            (float)Math.Round(player.Position.Y, 2),
            (float)Math.Round(player.Position.Z, 2)
        );
        
        int idx = SharedUtilities.CurrentWaypointIndex;
        Vector3 targetPosition = SharedUtilities.Waypoints[idx];

        if (isRotating)
        {
            HandleRotation();
            return;
        }

        Vector3 direction = (targetPosition - currentPosition).Normalized;
        float frameTime = Function.Call<float>(0x5E72022914CE3C38); // GET_FRAME_TIME
        float currentMoveSpeed = SharedUtilities.Speeds[idx];
        
        Vector3 newPosition = new Vector3(
            (float)Math.Round(currentPosition.X + direction.X * currentMoveSpeed * frameTime, 2),
            (float)Math.Round(currentPosition.Y + direction.Y * currentMoveSpeed * frameTime, 2),
            (float)Math.Round(currentPosition.Z + direction.Z * currentMoveSpeed * frameTime, 2)
        );

        // Ensure trailing zero for .1 to .10
        newPosition = new Vector3(
            float.Parse(newPosition.X.ToString("0.00")),
            float.Parse(newPosition.Y.ToString("0.00")),
            float.Parse(newPosition.Z.ToString("0.00"))
        );

        // Get Above Ground Level height
        float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, player.Handle);
        float groundHeight = player.Position.Z - currentAGL;
        float rawTargetZ = groundHeight + targetPosition.Z;

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

        // Move forward in the direction while keeping altitude stable
        newPosition.Z = (float)Math.Round(smoothedTargetZ, 2);

        float distanceToTargetXY = new Vector2(targetPosition.X, targetPosition.Y)
            .DistanceTo(new Vector2(currentPosition.X, currentPosition.Y));

        if (distanceToTargetXY < currentMoveSpeed * frameTime)
        {
            player.Position = new Vector3(targetPosition.X, targetPosition.Y, smoothedTargetZ);
            Function.Call(0xCF2B9C0645C4651B, player.Handle, SharedUtilities.PlayerHeadings[idx] - 90);
            Function.Call(0x449995EA846D3FC2, SharedUtilities.CameraPitches[idx]);
            Function.Call(0x14F3947318CA8AD2, 0, 0);

            // Start rotation
            isRotating = true;
            rotationStartTime = Game.GameTime;
            initialHeading = Function.Call<float>(0xC230DD956E2F5507, player.Handle);
            targetHeading = SharedUtilities.PlayerHeadings[idx] - 90;

            // Trigger random speech line
            string[] speechLines = { "GENERIC_CURSE_HIGH", "GENERIC_CURSE_MED" };
            Random random = new Random();
            string randomSpeechLine = speechLines[random.Next(speechLines.Length)];
            Function.Call(0x1E6F9A9FE1A99F36, randomSpeechLine);
        }
        else
        {
            player.Position = newPosition;
            Function.Call(0xCF2B9C0645C4651B, player.Handle, SharedUtilities.PlayerHeadings[idx] - 90);
            Function.Call(0x449995EA846D3FC2, SharedUtilities.CameraPitches[idx]);
            Function.Call(0x14F3947318CA8AD2, 0, 0);
        }
    }
    
    private void HandleRotation()
    {
        Ped player = Game.Player.Character;
        int idx = SharedUtilities.CurrentWaypointIndex;
        
        float elapsedTime = Game.GameTime - rotationStartTime;
        float t = elapsedTime / rotationDuration;
        
        if (t >= 1.0f)
        {
            t = 1.0f;
            isRotating = false;
            SharedUtilities.CurrentWaypointIndex++;
        }
        
        float smoothedHeading = initialHeading + (targetHeading - initialHeading) * t;
        Function.Call(0xCF2B9C0645C4651B, player.Handle, smoothedHeading);
        Function.Call(0x449995EA846D3FC2, SharedUtilities.CameraPitches[idx]);
        Function.Call(0x14F3947318CA8AD2, 0, 0);
    }

    private void TeleportToWaypoint(int waypointIndex)
    {
        if (waypointIndex >= SharedUtilities.Waypoints.Count) return;

        Ped player = Game.Player.Character;
        Vector3 waypoint = SharedUtilities.Waypoints[waypointIndex];

        // First teleport to a high altitude
        player.Position = new Vector3(waypoint.X, waypoint.Y, 600.0f);
        Wait(1000); // Wait for a moment to ensure the teleport is complete

        // Get height above ground
        float heightAboveGround = Function.Call<float>(0x0D3B5BAEA08F63E9, player.Handle);
        Wait(1000); // Wait for a moment to ensure the height is correctly calculated

        // Calculate the final Z position
        waypoint.Z = 600.0f - heightAboveGround + waypoint.Z;

        // Teleport to the final position
        player.Position = new Vector3(waypoint.X, waypoint.Y, waypoint.Z);
        Wait(1000); // Wait for a moment to ensure the teleport is complete
    }
    
    private void DisplayDebugInfo()
    {
        if (!SharedUtilities.ShowDebugInfo || SharedUtilities.CurrentWaypointIndex >= SharedUtilities.Waypoints.Count)
            return;
            
        Ped player = Game.Player.Character;
        Vector3 currentPosition = player.Position;
        Vector3 targetPosition = SharedUtilities.Waypoints[SharedUtilities.CurrentWaypointIndex];
        
        float xOffset = 0f;
        float yOffset = -175f;

        SharedUtilities.DrawText(string.Format("Current Position:"), new PointF(730F + xOffset, 520f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("X: {0:F2}", currentPosition.X), new PointF(800F + xOffset, 520f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Y: {0:F2}", currentPosition.Y), new PointF(850F + xOffset, 520f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Z: {0:F2}", currentPosition.Z), new PointF(910F + xOffset, 520f + yOffset), 0.2F, Color.White);

        SharedUtilities.DrawText(string.Format("Target Position:"), new PointF(730F + xOffset, 540f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("X: {0:F2}", targetPosition.X), new PointF(800F + xOffset, 540f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Y: {0:F2}", targetPosition.Y), new PointF(850F + xOffset, 540f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Z: {0:F2}", targetPosition.Z), new PointF(910F + xOffset, 540f + yOffset), 0.2F, Color.White);
        
        float progress = (SharedUtilities.CurrentWaypointIndex + 1) / (float)SharedUtilities.Waypoints.Count * 100;
        SharedUtilities.DrawText(string.Format("Line: {0}/{1} ({2:F1}%)", 
            SharedUtilities.CurrentWaypointIndex + 1, 
            SharedUtilities.Waypoints.Count, 
            progress), new PointF(730F + xOffset, 620f + yOffset), 0.2F, Color.White);

        // Draw progress bar background
        float progressBarWidth = 135f;
        SharedUtilities.DrawRect(new PointF(885F + xOffset, 629f + yOffset), new SizeF(progressBarWidth, 10f), Color.Gray);

        // Draw progress bar foreground
        float filledWidth = progressBarWidth * (progress / 100f);
        SharedUtilities.DrawRect(new PointF(885F + xOffset - progressBarWidth / 2 + filledWidth / 2, 629f + yOffset), 
            new SizeF(filledWidth, 10f), Color.White);

        // Display current raw G-code line
        if (SharedUtilities.CurrentWaypointIndex < SharedUtilities.GCodeLines.Count)
        {
            SharedUtilities.DrawText(string.Format("Next: {0}", 
                SharedUtilities.GCodeLines[SharedUtilities.CurrentWaypointIndex]), 
                new PointF(730F + xOffset, 600f + yOffset), 0.2F, Color.LightGray);
        }
    }
}