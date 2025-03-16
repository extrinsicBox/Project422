using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;

public class GCodeFlightPath : Script
{
    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool isMoving = false;
    private const float moveSpeed = 1.0f; // Movement speed
    private const float flightAltitude = 500.0f; // Fixed altitude
    private float smoothedTargetZ = flightAltitude;
    private const float AltitudeSmoothingFactor = 0.006f; // Adjust for smoother altitude transitions
    private float previousAGL = 0.0f; // Store previous Above Ground Level (AGL) height
    private const float AltitudeChangeThreshold = 500.0f; // Limit altitude jumps

    public GCodeFlightPath()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
        LoadGCode("scripts/flightpath.gcode"); // Change filename as needed
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F10)
        {
            isMoving = !isMoving;
            if (isMoving)
            {
                Game.Player.Character.Position = waypoints[0];
                currentWaypointIndex = 0;
                DrawText("Drone flight started.", new PointF(500f, 500f), 0.4F, Color.White);
            }
            else
            {
                DrawText("Drone flight stopped.", new PointF(500f, 500f), 0.4F, Color.White);
            }
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (isMoving && currentWaypointIndex < waypoints.Count)
        {
            Ped player = Game.Player.Character;
            Vector3 currentPosition = player.Position;
            Vector3 targetPosition = waypoints[currentWaypointIndex];

            Vector3 direction = (targetPosition - currentPosition).Normalized;
            Vector3 newPosition = currentPosition + direction * moveSpeed * Function.Call<float>(0x5E72022914CE3C38); // GET_FRAME_TIME

            // Get Above Ground Level height
            float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, player.Handle); // GET_ENTITY_HEIGHT_ABOVE_GROUND
            float groundHeight = player.Position.Z - currentAGL;
            float rawTargetZ = groundHeight + flightAltitude;

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
            newPosition.Z = smoothedTargetZ;

            DrawText($"Current Position: {currentPosition}, Target Position: {targetPosition}, New Position: {newPosition}", new PointF(500f, 520f), 0.4F, Color.White);

            if ((targetPosition - currentPosition).Length() < moveSpeed * Function.Call<float>(0x5E72022914CE3C38))
            {
                player.Position = targetPosition;
                currentWaypointIndex++;
                DrawText($"Reached waypoint {currentWaypointIndex}.", new PointF(500f, 500f), 0.4F, Color.White);
            }
            else
            {
                player.Position = newPosition;
            }
        }
    }

    private void LoadGCode(string filePath)
    {
        if (!File.Exists(filePath))
        {
            DrawText("G-code file not found.", new PointF(500f, 500f), 0.4F, Color.White);
            return;
        }

        foreach (string line in File.ReadLines(filePath))
        {
            if (line.StartsWith("G1")) // Look for movement commands
            {
                float x = 0, y = 0;
                string[] parts = line.Split(' ');
                foreach (string part in parts)
                {
                    if (part.StartsWith("X")) float.TryParse(part.Substring(1), out x);
                    if (part.StartsWith("Y")) float.TryParse(part.Substring(1), out y);
                }
                waypoints.Add(new Vector3(x, y, flightAltitude)); // Set altitude to 500m
                DrawText($"Added waypoint: X={x}, Y={y}, Z={flightAltitude}", new PointF(500f, 540f), 0.4F, Color.White);
            }
        }

        if (waypoints.Count == 0)
        {
            DrawText("No valid waypoints found in G-code file.", new PointF(500f, 500f), 0.4F, Color.White);
        }
        else
        {
            DrawText($"Loaded {waypoints.Count} waypoints from G-code file.", new PointF(500f, 500f), 0.4F, Color.White);
        }
    }

    private void DrawRect(PointF position, SizeF size, Color color)
    {
        Function.Call(0x405224591DF02025, position.X / 1920f, position.Y / 1080f, size.Width / 1920f, size.Height / 1080f, color.R, color.G, color.B, color.A);
          drawCallCount++;
    }
}