using System.Drawing;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;
using System.Collections.Generic;
using System.IO;

public static class SharedUtilities
{
    // Shared waypoint data
    public static List<Vector3> Waypoints = new List<Vector3>();
    public static List<float> Speeds = new List<float>();
    public static List<float> CameraPitches = new List<float>();
    public static List<float> PlayerHeadings = new List<float>();
    public static List<string> GCodeLines = new List<string>();
    
    // Shared state data
    public static bool IsMoving = false;
    public static bool ShowDebugInfo = false;
    public static int CurrentWaypointIndex = 0;
    
    // Shared UI drawing functions
    public static void DrawText(string text, PointF position, float scale, Color color)
    {
        TextElement textElement = new TextElement(text, position, scale, color, Alignment.Left);
        textElement.Outline = true;
        textElement.Draw();
    }
    
    public static void DrawRect(PointF position, SizeF size, Color color)
    {
        Function.Call(0x405224591DF02025, position.X / 1920f, position.Y / 1080f, 
                      size.Width / 1920f, size.Height / 1080f, 
                      color.R, color.G, color.B, color.A);
    }
    
    // Shared G-code loading function
    public static void LoadGCode(string filePath)
    {
        // Clear previous data
        Waypoints.Clear();
        Speeds.Clear();
        CameraPitches.Clear();
        PlayerHeadings.Clear();
        GCodeLines.Clear();
        
        if (!File.Exists(filePath))
        {
            DrawText("G-code file not found.", new PointF(500f, 500f), 0.4F, Color.White);
            return;
        }

        bool isFirstWaypoint = true;

        foreach (string line in File.ReadLines(filePath))
        {
            if (line.StartsWith("G1")) // Look for movement commands
            {
                float x = 0, y = 0, z = 0, speed = 0, a = 0, b = 0;
                string[] parts = line.Split(' ');
                foreach (string part in parts)
                {
                    if (part.StartsWith("X")) float.TryParse(part.Substring(1), out x);
                    if (part.StartsWith("Y")) float.TryParse(part.Substring(1), out y);
                    if (part.StartsWith("Z")) float.TryParse(part.Substring(1), out z);
                    if (part.StartsWith("F")) float.TryParse(part.Substring(1), out speed);
                    if (part.StartsWith("A")) float.TryParse(part.Substring(1), out a);
                    if (part.StartsWith("B")) float.TryParse(part.Substring(1), out b);
                }

                if (isFirstWaypoint)
                {
                    // First teleport to a high altitude
                    Ped player = Game.Player.Character;
                    player.Position = new Vector3(x, y, 600.0f);
                    
                    // Get height above ground
                    float heightAboveGround = Function.Call<float>(0x0D3B5BAEA08F63E9, player.Handle);
                    
                    // Calculate the final Z position
                    z = 600.0f - heightAboveGround + z;
                    
                    isFirstWaypoint = false;
                }

                Waypoints.Add(new Vector3(x, y, z));
                Speeds.Add(speed / 10.0f); // Convert G-code speed to game units
                CameraPitches.Add(a);
                PlayerHeadings.Add(b);
                GCodeLines.Add(line);
            }
        }

        if (Waypoints.Count == 0)
        {
            DrawText("No valid waypoints found in G-code file.", new PointF(500f, 500f), 0.4F, Color.White);
        }
        else
        {
            DrawText(string.Format("Loaded {0} waypoints from G-code file.", Waypoints.Count), new PointF(500f, 500f), 0.4F, Color.White);
        }
    }
}