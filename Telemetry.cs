using System;
using System.Drawing;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using RDR2.UI;

public class flightTelemetry : Script
{
    private bool showHUD = false; // Toggle for HUD visibility

    public flightTelemetry()
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
            DrawHUD();
        }
    }

    private void DrawHUD()
    {
        Ped playerPed = Game.Player.Character;
        Vector3 pos = playerPed.Position;
        float livePlayerHeading = Function.Call<float>(0xC230DD956E2F5507, playerPed.Handle); // GET_ENTITY_HEADING
        float cameraHeading = Function.Call<float>(0xC4ABF536048998AA); // GET_GAMEPLAY_CAM_RELATIVE_HEADING
        float heightAboveGround = Function.Call<float>(0x0D3B5BAEA08F63E9, playerPed.Handle); // GET_ENTITY_HEIGHT_ABOVE_GROUND
        float cameraPitch = Function.Call<float>(0x99AADEBBA803F827); // GET_GAMEPLAY_CAM_RELATIVE_PITCH

        PointF boxPosition = new PointF(150f, 450f); // Center of 1920x1080 screen
        SizeF boxSize = new SizeF(200f, 800f);
        
        // Draw background box
        DrawRect(boxPosition, boxSize, Color.FromArgb(50, 0, 0, 0)); // Semi-transparent black box
        
        // Draw border
        // Draw ground glass box in center of screen (16:9 ratio, 1f border thickness)
        PointF centerBoxPosition = new PointF(960f, 540f); // Center of 1920x1080 screen
        SizeF centerBoxSize = new SizeF(1280f, 1080f); // 16:9 ratio
        DrawBorder(centerBoxPosition, centerBoxSize, .25f, Color.White);
        DrawBorder(boxPosition, boxSize, .5f, Color.White);
        float globalTextOffsetY = -(boxSize.Height / 2f) + 5f;
        float globalTextOffsetX = -(boxSize.Width / 2f) + 10f;

        // Draw crosshair
        float centerX = 960f;
        float centerY = 540f;
        float crossSize = 10f;
        
        // Horizontal line
       // DrawRect(new PointF(centerX - crossSize / 2, centerY), new SizeF(crossSize, 2f), Color.White);
        
        // Vertical line
       // DrawRect(new PointF(centerX, centerY - crossSize / 2), new SizeF(2f, crossSize), Color.White);
        
        // Draw flight data inside the box
        DrawText(string.Format("X: {0:F2}", pos.X), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 10f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("Y: {0:F2}", pos.Y), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 30f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("ALT: {0:F2}m", pos.Z), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 50f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("PLAYER H: {0:F2}°", livePlayerHeading), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 130f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("CAM H: {0:F2}°", cameraHeading), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 90f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("AGL: {0:F2}m", heightAboveGround), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 70f + globalTextOffsetY), 0.2F, Color.White);
        DrawText(string.Format("CAM P: {0:F2}°", cameraPitch), new PointF(boxPosition.X + globalTextOffsetX, boxPosition.Y + 110f + globalTextOffsetY), 0.2F, Color.White);
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
    }
    
    private void DrawBorder(PointF position, SizeF size, float thickness, Color color)
    {
        float borderThickness = thickness;
        DrawRect(new PointF(position.X, position.Y - (size.Height / 2f)), new SizeF(size.Width, borderThickness), color); // Top
        DrawRect(new PointF(position.X, position.Y + (size.Height / 2f)), new SizeF(size.Width, borderThickness), color); // Bottom
        DrawRect(new PointF(position.X - (size.Width / 2f), position.Y), new SizeF(borderThickness, size.Height), color); // Left
        DrawRect(new PointF(position.X + (size.Width / 2f), position.Y), new SizeF(borderThickness, size.Height), color); // Right
    }
}
