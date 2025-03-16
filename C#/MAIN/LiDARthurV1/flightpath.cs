using System;
using System.Windows.Forms;
using System.Drawing;
using RDR2;
using RDR2.Math;
using RDR2.Native;

public class GCodeFlightPath : Script
{
    private float smoothedTargetZ = 0.0f;
    private const float AltitudeSmoothingFactor = .05f;
    private float previousAGL = 0.0f;
    private const float AltitudeChangeThreshold = 250.0f; // Reduced from 1000.0f
    private bool isRotating = false;
    private float rotationStartTime = 0.0f;
    private float rotationDuration = 500f;
    private float initialHeading = 0.0f;
    private float targetHeading = 0.0f;
    private Vector3 newPosition = Vector3.Zero; // Added to store the new position for debugging display
    
    // Flag to determine if we're using AGL-relative or absolute Z values
    // If true, Z values in G-code are treated as heights above ground
    // If false, Z values in G-code are treated as absolute world coordinates
    private static bool UseRelativeAltitude = true; 

    // New flag to control deceleration
    private bool disableDeceleration = false;

    public GCodeFlightPath()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
        
        // Load G-code data at startup
        SharedUtilities.LoadGCode("scripts/flightpath.gcode");
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F9)
        {
            SharedUtilities.IsMoving = !SharedUtilities.IsMoving;

            if (SharedUtilities.IsMoving)
            {
                StartFlightPath();
                SharedUtilities.DrawText("Drone flight started.", new PointF(500f, 500f), 0.4F, Color.White);
            }
            else
            {
                StopFlightPath();
                SharedUtilities.DrawText("Drone flight stopped.", new PointF(500f, 500f), 0.4F, Color.White);
            }
        }
        else if (e.KeyCode == Keys.F10)
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
            // Initialize smoothedTargetZ with the first waypoint's Z value
            smoothedTargetZ = player.Position.Z; // Use actual player Z after teleport instead of waypoint Z
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

    // Calculate distance first - this is key for debugging
    float distanceToTargetXY = new Vector2(targetPosition.X, targetPosition.Y)
        .DistanceTo(new Vector2(currentPosition.X, currentPosition.Y));
    
    // DEBUG: When distance is near the crawl threshold
    if (distanceToTargetXY < 5.0f)
    {
        if (SharedUtilities.ShowDebugInfo)
    {
        SharedUtilities.DrawText(
            string.Format("CLOSE APPROACH: Dist={0:F2}, X={1:F2}, Y={2:F2}", 
                distanceToTargetXY, 
                Math.Abs(targetPosition.X - currentPosition.X),
                Math.Abs(targetPosition.Y - currentPosition.Y)), 
            new PointF(500f, 540f), 0.4F, Color.Yellow);
    }
        // If we're very close but still moving (the crawl situation)
        if (distanceToTargetXY < 1.5f && distanceToTargetXY > 0.1f)
        {
            // FORCE TELEPORT TO WAYPOINT
            player.Position = new Vector3(targetPosition.X, targetPosition.Y, smoothedTargetZ);
            Function.Call(0xCF2B9C0645C4651B, player.Handle, SharedUtilities.PlayerHeadings[idx] - 90);
            Function.Call(0x449995EA846D3FC2, SharedUtilities.CameraPitches[idx]);
            
            // Start rotation
            isRotating = true;
            rotationStartTime = Game.GameTime;
            initialHeading = Function.Call<float>(0xC230DD956E2F5507, player.Handle);
            targetHeading = SharedUtilities.PlayerHeadings[idx] - 90;
            
            // Log and return
            if (SharedUtilities.ShowDebugInfo)
    {
            SharedUtilities.DrawText("TELEPORTED TO WAYPOINT: Crawl detected", new PointF(500f, 560f), 0.4F, Color.Orange);
    }
            return;
        }
    }

    Vector3 direction = (targetPosition - currentPosition).Normalized;
    if (distanceToTargetXY < 5.0f)
{
    // Create a horizontally-focused direction vector when close to waypoint
    Vector2 directionXY = new Vector2(targetPosition.X - currentPosition.X, 
                                     targetPosition.Y - currentPosition.Y).Normalized;
    
    // Replace the full direction vector with one that prioritizes horizontal movement
    direction = new Vector3(directionXY.X, directionXY.Y, 0);
    
    // Optionally add back a small vertical component if needed
    direction.Z = (targetPosition.Z - currentPosition.Z) * 0.1f;
    if (SharedUtilities.ShowDebugInfo)
    {
    SharedUtilities.DrawText(
        string.Format("USING XY DIRECTION: X={0:F4}, Y={1:F4}", 
            direction.X, direction.Y), 
        new PointF(500f, 620f), 0.4F, Color.Magenta);
    }
}
    // Debug the direction vector when close
    if (distanceToTargetXY < 5.0f)
    {
        if (SharedUtilities.ShowDebugInfo)
    {
        SharedUtilities.DrawText(
            string.Format("DIR VECTOR: X={0:F4}, Y={1:F4}, Z={2:F4}, Length={3:F4}", 
                direction.X, direction.Y, direction.Z, direction.Length()), 
            new PointF(500f, 580f), 0.4F, Color.Cyan);
    }
    }
    
    // FIX: Protect against zero frame time
    float frameTime = Function.Call<float>(0x5E72022914CE3C38); // GET_FRAME_TIME
    frameTime = Math.Max(frameTime, 0.001f);
    
    float currentMoveSpeed = SharedUtilities.Speeds[idx];
    
    // For close approaches, ensure minimum speed
    if (distanceToTargetXY < 5.0f)
    {
        currentMoveSpeed = Math.Max(currentMoveSpeed, 5.0f);
        if (SharedUtilities.ShowDebugInfo)
    {
        SharedUtilities.DrawText(
            string.Format("SPEED BOOST: {0:F2}", currentMoveSpeed), 
            new PointF(500f, 600f), 0.4F, Color.LightGreen);
    }
    }
    
    // Rest of your existing movement code...
    newPosition = new Vector3(
        (float)Math.Round(currentPosition.X + direction.X * currentMoveSpeed * frameTime, 2),
        (float)Math.Round(currentPosition.Y + direction.Y * currentMoveSpeed * frameTime, 2),
        (float)Math.Round(currentPosition.Z + direction.Z * currentMoveSpeed * frameTime, 2)
    );

    // Ensure trailing zero for .1 to .10
    newPosition = new Vector3(
        (float)Math.Round(newPosition.X * 100f) / 100f,
        (float)Math.Round(newPosition.Y * 100f) / 100f,
        (float)Math.Round(newPosition.Z * 100f) / 100f
    );

    // Your existing altitude calculation logic...
    float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, player.Handle);
    float groundHeight = player.Position.Z - currentAGL;
    
    float rawTargetZ;
    if (UseRelativeAltitude)
    {
        rawTargetZ = groundHeight + targetPosition.Z;
    }
    else
    {
        rawTargetZ = targetPosition.Z;
    }

    // Prevent extreme altitude jumps
    if (Math.Abs(currentAGL - previousAGL) >= AltitudeChangeThreshold)
    {
        if (SharedUtilities.ShowDebugInfo)
        {
            SharedUtilities.DrawText(
                string.Format("JUMP DETECTED: {0:F2} -> {1:F2}", previousAGL, currentAGL), 
                new PointF(500f, 520f), 0.4F, Color.Red);
        }
        
        rawTargetZ = smoothedTargetZ;
    }
    else
    {
        previousAGL = currentAGL;
    }

    smoothedTargetZ = smoothedTargetZ + (rawTargetZ - smoothedTargetZ) * AltitudeSmoothingFactor;
    newPosition.Z = (float)Math.Round(smoothedTargetZ, 2);

    // IMPORTANT MODIFICATION: Use a much larger movement threshold near the target
    float moveThreshold = Math.Min(currentMoveSpeed * frameTime, 0.5f);
    if (distanceToTargetXY < 5.0f)
    {
        moveThreshold = 1.5f; // Force reaching the waypoint when within 1.5 units
    }

    if (distanceToTargetXY < moveThreshold)
    {
        // We've reached the waypoint
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
    
    // Calculate the most efficient rotation direction
    float headingDiff = targetHeading - initialHeading;
    
    // Normalize the difference to be in the range [-180, 180]
    while (headingDiff > 180.0f) headingDiff -= 360.0f;
    while (headingDiff < -180.0f) headingDiff += 360.0f;
    
    // Now apply rotation in the correct direction
    float smoothedHeading = initialHeading + headingDiff * t;
    
    // Normalize the final heading to be in the range [0, 360]
    while (smoothedHeading >= 360.0f) smoothedHeading -= 360.0f;
    while (smoothedHeading < 0.0f) smoothedHeading += 360.0f;
    
    // Debug info for rotation
    if (SharedUtilities.ShowDebugInfo)
    {
        SharedUtilities.DrawText(
            string.Format("ROTATION: Initial={0:F1}° Target={1:F1}° Diff={2:F1}° Current={3:F1}°", 
                initialHeading, targetHeading, headingDiff, smoothedHeading), 
            new PointF(500f, 640f), 0.4F, Color.Orange);
    }
    
    // Update player position to prevent falling
    player.Position = new Vector3(player.Position.X, player.Position.Y, smoothedTargetZ);
    
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

        // FIX: Proper teleport Z calculation based on whether Z is relative or absolute
        float finalZ;
        if (UseRelativeAltitude)
        {
            // If Z values are heights above ground, calculate absolute Z
            float groundZ = 600.0f - heightAboveGround;
            finalZ = groundZ + waypoint.Z;
        }
        else
        {
            // If Z values are absolute, use them directly
            finalZ = waypoint.Z;
        }

        // Teleport to the final position
        player.Position = new Vector3(waypoint.X, waypoint.Y, finalZ);
        Wait(1000); // Wait for a moment to ensure the teleport is complete
    }
    
    private void DisplayDebugInfo()
    {
        if (!SharedUtilities.ShowDebugInfo || SharedUtilities.CurrentWaypointIndex >= SharedUtilities.Waypoints.Count)
            return;
            
        Ped player = Game.Player.Character;
        Vector3 currentPosition = player.Position;
        int idx = SharedUtilities.CurrentWaypointIndex;
        Vector3 targetPosition = SharedUtilities.Waypoints[idx];
        
        float xOffset = 0f;
        float yOffset = -175f;


        // Current Position
        SharedUtilities.DrawText("Current Position:", new PointF(730F + xOffset, 520f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("X: {0:F2}", currentPosition.X), new PointF(800F + xOffset, 520f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Y: {0:F2}", currentPosition.Y), new PointF(850F + xOffset, 520f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Z: {0:F2}", currentPosition.Z), new PointF(910F + xOffset, 520f + yOffset), 0.2F, Color.White);

        // Target Position
        SharedUtilities.DrawText("Target Position:", new PointF(730F + xOffset, 540f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("X: {0:F2}", targetPosition.X), new PointF(800F + xOffset, 540f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Y: {0:F2}", targetPosition.Y), new PointF(850F + xOffset, 540f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Z: {0:F2}", targetPosition.Z), new PointF(910F + xOffset, 540f + yOffset), 0.2F, Color.White);

        // New Position (calculated position for this frame)
        SharedUtilities.DrawText("New Position:", new PointF(730F + xOffset, 560f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("X: {0:F2}", newPosition.X), new PointF(800F + xOffset, 560f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Y: {0:F2}", newPosition.Y), new PointF(850F + xOffset, 560f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Z: {0:F2}", newPosition.Z), new PointF(910F + xOffset, 560f + yOffset), 0.2F, Color.White);

        // Distance information
        float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, player.Handle);
        float groundHeight = player.Position.Z - currentAGL;
        // Get Above Ground Level for altitude calculations
         Vector3 adjustedTargetPosition = targetPosition;

            if (UseRelativeAltitude) {
             // Convert target's relative Z to absolute world Z for proper comparison
            adjustedTargetPosition.Z = groundHeight + targetPosition.Z;
            }
            
        Vector3 distanceToTarget = adjustedTargetPosition - currentPosition;
        SharedUtilities.DrawText("Distance:", new PointF(730F + xOffset, 580f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("X: {0:F2}", distanceToTarget.X), new PointF(800F + xOffset, 580f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Y: {0:F2}", distanceToTarget.Y), new PointF(850F + xOffset, 580f + yOffset), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Z: {0:F2}", distanceToTarget.Z), new PointF(910F + xOffset, 580f + yOffset), 0.2F, Color.White);
        
        // Add altitude info showing the ground height and AGL
        
        SharedUtilities.DrawText(string.Format("Mode: {0}", UseRelativeAltitude ? "Relative" : "Absolute"), 
            new PointF(820F + xOffset, 500f + yOffset), 0.2F, Color.Yellow);
        
        // Current G-code line
        if (idx < SharedUtilities.GCodeLines.Count)
        {
            SharedUtilities.DrawText(string.Format("Next: {0}", SharedUtilities.GCodeLines[idx]), 
                new PointF(730F + xOffset, 600f + yOffset), 0.2F, Color.LightGray);
        }
        
        // Progress information
        float progress = (idx + 1) / (float)SharedUtilities.Waypoints.Count * 100;
        SharedUtilities.DrawText(string.Format("Line: {0}/{1} ({2:F1}%)", 
            idx + 1, SharedUtilities.Waypoints.Count, progress), 
            new PointF(730F + xOffset, 620f + yOffset), 0.2F, Color.White);

        // Draw progress bar background
        float progressBarWidth = 135f;
        SharedUtilities.DrawRect(new PointF(885F + xOffset, 629f + yOffset), new SizeF(progressBarWidth, 10f), Color.Gray);

        // Draw progress bar foreground
        float filledWidth = progressBarWidth * (progress / 100f);
        SharedUtilities.DrawRect(
            new PointF(885F + xOffset - progressBarWidth / 2 + filledWidth / 2, 629f + yOffset), 
            new SizeF(filledWidth, 10f), 
            Color.White);
    }
}