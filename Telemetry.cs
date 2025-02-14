using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;

public class PhotogrammetryHUD : Script
{
    private bool showHUD = false; // Toggle for HUD visibility
    private int fps = 0;
    private DateTime lastFPSUpdate = DateTime.Now;
    private int frameCount = 0;
    private int drawCallCount = 0;
    private List<float> altitudeHistory = new List<float>();
    private List<float> aglHistory = new List<float>();
    private List<float> groundZHistory = new List<float>();
private const int maxAltitudeHistorySize = 200; // Increase from 600 to 1500 to scale draw calls
private const int maxAGLHistorySize = 200;
private const int maxGroundZHistorySize = 200;
 private float lastGroundZ = 0f;

    public PhotogrammetryHUD()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F11)
        {
            showHUD = !showHUD; // Toggle HUD visibility
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
        float velocity = playerPed.Velocity.Length();
        float livePlayerHeading = Function.Call<float>(0xC230DD956E2F5507, playerPed.Handle);
        float cameraHeading = Function.Call<float>(0xC4ABF536048998AA);
        float heightAboveGround = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle);
        float cameraPitch = Function.Call<float>(0x99AADEBBA803F827);
        float altitude = playerPed.Position.Z;
        float currentAGL = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle); // GET_ENTITY_HEIGHT_ABOVE_GROUND
        float groundZ = playerPed.Position.Z - currentAGL+1;
        float heightAboveGround2=heightAboveGround-150;
        float heightAboveGround3=heightAboveGround+150;
        PointF boxPosition = new PointF(825f, 575f);
        float globalTextOffsetY = -395f;
        float globalTextOffsetX = -90f;

        Color groundColor = Color.FromArgb(255, 174, 111, 0);
        
        DrawText(string.Format("X: {0:F2}", pos.X), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y -10f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("Y: {0:F2}", pos.Y), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +10f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("Z: {0:F2}m", altitude), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +30f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("ALT: {0:F2}m", currentAGL), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +50f + globalTextOffsetY), 0.2F, Color.Red);
        //DrawText(string.Format("Δ: {0:F2}m", heightAboveGround2), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +70f + globalTextOffsetY), 0.2F, Color.LightGreen);
        DrawText(string.Format("GND: {0:F2}m", groundZ), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +70f + globalTextOffsetY), 0.2F, Color.LightGreen);
        DrawText(string.Format("CAM H: {0:F2}°", cameraHeading), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +90f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("CAM P: {0:F2}°", cameraPitch), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +110f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("PLAYER H: {0:F2}°", livePlayerHeading), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y +130f + globalTextOffsetY), 0.2F, Color.White);
        
    }


    private void DisplayPerformanceMetrics()
    {
        
        Ped playerPed = Game.Player.Character;PointF boxPosition = new PointF(825f, 575f);
        float globalTextOffsetY = -395f;
        float globalTextOffsetX = -90f;
        float velocity = playerPed.Velocity.Length();
        float PerfTextOffsetY = 10f;
        float PerfTextOffsetX = 95f;


        DrawText(string.Format("FPS: {0}", fps), new PointF(boxPosition.X + globalTextOffsetX+PerfTextOffsetX, boxPosition.Y -PerfTextOffsetY + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("DRAW CALL: {0}", drawCallCount), new PointF(boxPosition.X + globalTextOffsetX+60f+PerfTextOffsetX, boxPosition.Y -PerfTextOffsetY + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("V: {0:F2} m/s", ComputeVelocity()), 
    new PointF(boxPosition.X + globalTextOffsetX + 150f + PerfTextOffsetX, 
    boxPosition.Y - PerfTextOffsetY + globalTextOffsetY), 
    0.2F, Color.White);
    }

    private void DrawAltitudeGraph()
{
    int graphX = 825; // Right side of the screen
    int graphY = 175;  // Upper right corner
    int graphWidth = 300;
    int graphHeight = 150;
    int minAltitude = 0, maxAltitude = 500;
    
    // Ensure history size is properly limited
    int historySize = graphWidth;
    float pointSpacing = .66f; // Ensure points span full width

    Ped playerPed = Game.Player.Character;
    float altitude = playerPed.Position.Z;
    float heightAboveGround = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle); // THIS IS CAUSING THE GND LINE ISSUES
    float groundZ = altitude - heightAboveGround; // Ground Z calculation
   

   if (Math.Abs(groundZ - lastGroundZ) > 1.0f) // Ignore small fluctuations
        {
            lastGroundZ = groundZ;
        }


    altitudeHistory.Add(altitude);
    groundZHistory.Add(groundZ);

    // Ensure all lists remain the same length
    while (altitudeHistory.Count > historySize)
    {
        altitudeHistory.RemoveAt(0);
        groundZHistory.RemoveAt(0);
    }


    DrawRect(new PointF(880f, 255f), new SizeF(320f, 210f), Color.FromArgb(100, 0, 0, 0)); // Semi-transparent black box
    // Draw X-axis
    DrawRect(new PointF(graphX+100, graphY + graphHeight), new SizeF(graphWidth-100, 1), Color.White);
    // Draw Y-axis
    DrawRect(new PointF(graphX, graphY+75), new SizeF(1, graphHeight), Color.White);

    // Only render the last `historySize` points
     int startIdx = Math.Max(0, altitudeHistory.Count - historySize);

        for (int i = startIdx + 1; i < altitudeHistory.Count; i++)
        {
            float x1 = graphX + (i - startIdx - 1) * pointSpacing;
            float y1 = graphY + graphHeight - ((altitudeHistory[i - 1] - minAltitude) / (maxAltitude - minAltitude) * graphHeight);
            float groundY1 = graphY + graphHeight - ((groundZHistory[i-1] - minAltitude) / (maxAltitude - minAltitude) * graphHeight);

            if (i % 2 == 1)
            {
                DrawRect(new PointF(x1, groundY1), new SizeF(2, 1), Color.LightGreen);
                DrawRect(new PointF(x1, y1), new SizeF(2, 1), Color.Red);
    }
}

}

private Vector3 lastPosition = Vector3.Zero;
private DateTime lastUpdateTime = DateTime.Now;

private float ComputeVelocity()
{
    Ped playerPed = Game.Player.Character;
    Vector3 currentPosition = playerPed.Position;

    DateTime currentTime = DateTime.Now;
    float deltaTime = (float)(currentTime - lastUpdateTime).TotalSeconds;
    
    float velocity = 0.0f;
    
    if (deltaTime > 0) // Prevent division by zero
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

    private void DrawText(string text, PointF position, float scale, Color color)
    {
        TextElement textElement = new TextElement(text, position, scale, color, Alignment.Left);
        textElement.Outline = true;
        textElement.Draw();
    }

    private void DrawRect(PointF position, SizeF size, Color color)
    {
        Function.Call(0x405224591DF02025, position.X / 1920f, position.Y / 1080f, size.Width / 1920f, size.Height / 1080f, color.R, color.G, color.B, color.A);
          drawCallCount++;
    }
}
