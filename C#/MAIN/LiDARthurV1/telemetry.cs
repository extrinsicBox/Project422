using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;

public class PhotogrammetryHUD : Script
{
    private bool showHUD = false;
    private int fps = 0;
    private DateTime lastFPSUpdate = DateTime.Now;
    private int frameCount = 0;
    private int drawCallCount = 0;
    private List<float> altitudeHistory = new List<float>();
    private List<float> groundZHistory = new List<float>();
    private const int maxHistorySize = 200;
    private float lastGroundZ = 0f;
    private Vector3 lastPosition = Vector3.Zero;
    private DateTime lastUpdateTime = DateTime.Now;

    public PhotogrammetryHUD()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            showHUD = !showHUD;
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (showHUD)
        {
            drawCallCount = 0;
            DrawHUD();
            DrawAltitudeGraph();
            UpdateFPS();
            DisplayPerformanceMetrics();
        }
    }

    private void DrawHUD()
    {
        Ped playerPed = Game.Player.Character;
        Vector3 pos = playerPed.Position;
        float livePlayerHeading = Function.Call<float>(0xC230DD956E2F5507, playerPed.Handle);
        float cameraHeading = Function.Call<float>(0xC4ABF536048998AA);
        float cameraPitch = Function.Call<float>(0x99AADEBBA803F827);
        float altitude = playerPed.Position.Z;
        float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle);
        float groundZ = playerPed.Position.Z - currentAGL + 1;
        PointF boxPosition = new PointF(825f, 575f);
        float globalTextOffsetY = -395f;
        float globalTextOffsetX = -90f;

        SharedUtilities.DrawText(string.Format("X: {0:F2}", pos.X), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y - 10f + globalTextOffsetY), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Y: {0:F2}", pos.Y), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 10f + globalTextOffsetY), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("Z: {0:F2}m", altitude), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 30f + globalTextOffsetY), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("ALT: {0:F2}m", currentAGL), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 50f + globalTextOffsetY), 0.2F, Color.SkyBlue);
        SharedUtilities.DrawText(string.Format("GND: {0:F2}m", groundZ), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 70f + globalTextOffsetY), 0.2F, Color.LightGreen);
        SharedUtilities.DrawText(string.Format("CAM H: {0:F2}°", cameraHeading), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 90f + globalTextOffsetY), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("CAM P: {0:F2}°", cameraPitch), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 110f + globalTextOffsetY), 0.2F, Color.White);
        SharedUtilities.DrawText(string.Format("PLAYER H: {0:F2}°", livePlayerHeading), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 130f + globalTextOffsetY), 0.2F, Color.White);
    }

    private void DisplayPerformanceMetrics()
    {
        PointF boxPosition = new PointF(825f, 575f);
        float globalTextOffsetY = -395f;
        float globalTextOffsetX = -90f;
        float PerfTextOffsetY = 10f;
        float PerfTextOffsetX = 95f;

        SharedUtilities.DrawText(string.Format("FPS: {0}", fps), 
            new PointF(boxPosition.X + globalTextOffsetX + PerfTextOffsetX, boxPosition.Y - PerfTextOffsetY + globalTextOffsetY), 
            0.2F, Color.White);
            
        SharedUtilities.DrawText(string.Format("DRAW CALL: {0}", drawCallCount), 
            new PointF(boxPosition.X + globalTextOffsetX + 60f + PerfTextOffsetX, boxPosition.Y - PerfTextOffsetY + globalTextOffsetY), 
            0.2F, Color.White);
            
        SharedUtilities.DrawText(string.Format("V: {0:F2} m/s", ComputeVelocity()),
            new PointF(boxPosition.X + globalTextOffsetX + 150f + PerfTextOffsetX, boxPosition.Y - PerfTextOffsetY + globalTextOffsetY),
            0.2F, Color.White);
      
    }

    private void DrawAltitudeGraph()
    {
        int graphX = 825;
        int graphY = 175;
        int graphWidth = 300;
        int graphHeight = 150;
        int minAltitude = -100, maxAltitude = 800;
        float pointSpacing = .66f;

        Ped playerPed = Game.Player.Character;
        float altitude = playerPed.Position.Z;
        float heightAboveGround = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle);
        float groundZ = altitude - heightAboveGround;

        if (Math.Abs(groundZ - lastGroundZ) > 1.0f)
        {
            lastGroundZ = groundZ;
        }

        altitudeHistory.Add(altitude);
        groundZHistory.Add(groundZ);

        // Limit history size
        while (altitudeHistory.Count > maxHistorySize)
        {
            altitudeHistory.RemoveAt(0);
            groundZHistory.RemoveAt(0);
        }

        SharedUtilities.DrawRect(new PointF(880f, 320f), new SizeF(330f, 325f), Color.FromArgb(100, 0, 0, 0));
        // Draw X-axis
        SharedUtilities.DrawRect(new PointF(graphX + 100, graphY + graphHeight), new SizeF(graphWidth - 100, 1), Color.White);
        // Draw Y-axis
        SharedUtilities.DrawRect(new PointF(graphX, graphY + 75), new SizeF(1, graphHeight), Color.White);
        drawCallCount += 3;

        // Only render the last points
        int startIdx = Math.Max(0, altitudeHistory.Count - maxHistorySize);

        for (int i = startIdx + 1; i < altitudeHistory.Count; i++)
        {
            float x1 = graphX + (i - startIdx - 1) * pointSpacing;
            float y1 = graphY + graphHeight - ((altitudeHistory[i - 1] - minAltitude) / (maxAltitude - minAltitude) * graphHeight);
            float groundY1 = graphY + graphHeight - ((groundZHistory[i - 1] - minAltitude) / (maxAltitude - minAltitude) * graphHeight);

            if (i % 2 == 1) // Optimize draw calls by only drawing every other point
            {
                SharedUtilities.DrawRect(new PointF(x1, groundY1), new SizeF(2, 1), Color.LightGreen);
                SharedUtilities.DrawRect(new PointF(x1, y1), new SizeF(2, 1), Color.SkyBlue);
                drawCallCount += 2;
            }
        }
    }

    private float ComputeVelocity()
    {
        Ped playerPed = Game.Player.Character;
        Vector3 currentPosition = playerPed.Position;

        DateTime currentTime = DateTime.Now;
        float deltaTime = (float)(currentTime - lastUpdateTime).TotalSeconds;

        float velocity = 0.0f;

        if (deltaTime > 0)
        {
            velocity = (currentPosition - lastPosition).Length() / deltaTime;
        }

        lastPosition = currentPosition;
        lastUpdateTime = currentTime;

        return velocity;
    }

    private void UpdateFPS()
    {
        frameCount++;
        if ((DateTime.Now - lastFPSUpdate).TotalSeconds >= 1)
        {
            fps = frameCount;
            frameCount = 0;
            lastFPSUpdate = DateTime.Now;
        }
    }
}