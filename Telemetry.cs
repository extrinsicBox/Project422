using System;
using System.Drawing;
using RDR2;
using RDR2.Native;
using RDR2.UI;

public class TelemetryHUD : Script
{
    private bool showHUD = false; // Toggle for HUD visibility
    private const float baseWidth = 1280f;
    private const float baseHeight = 720f;

    public TelemetryHUD()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
    }

    private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
    {
        if (e.KeyCode == System.Windows.Forms.Keys.F11)
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
        // Hardcoded screen resolution for testing (3440x1440)
        int screenWidth = 3440;
        int screenHeight = 1440;

        // Correctly scale the rectangle size
        float rectWidth = 200f / screenWidth;  // Normalize width
        float rectHeight = 200f / screenHeight; // Normalize height

        // Correct center position using RDR2's 0.5-based UI system
        float rectX = 0.5f; // Centered horizontally
        float rectY = 0.5f; // Centered vertically

        // Draw rectangle
        DrawRect(new PointF(rectX, rectY), new SizeF(rectWidth, rectHeight), Color.FromArgb(150, 255, 0, 0));
        
        // Draw border (white)
        DrawBorder(new PointF(rectX, rectY), new SizeF(rectWidth, rectHeight), 0.002f, Color.White, screenWidth, screenHeight);
    }

    private void DrawRect(PointF position, SizeF size, Color color)
    {
        // RDR2 UI expects normalized values (0.0 - 1.0)
        Function.Call(0x405224591DF02025, 
            position.X, position.Y, // Already normalized
            size.Width, size.Height, 
            color.R, color.G, color.B, color.A);
    }
    
    private void DrawBorder(PointF position, SizeF size, float thickness, Color color, int screenWidth, int screenHeight)
    {
        // Normalize thickness separately for width and height using screen resolution
        float borderThicknessX = thickness / (screenWidth / baseWidth); // Scale X thickness correctly
        float borderThicknessY = thickness / (screenHeight / baseHeight); // Scale Y thickness correctly

        // Top & Bottom Borders
        DrawRect(new PointF(position.X, position.Y - (size.Height / 2f)), new SizeF(size.Width, borderThicknessY), color); // Top
        DrawRect(new PointF(position.X, position.Y + (size.Height / 2f)), new SizeF(size.Width, borderThicknessY), color); // Bottom

        // Left & Right Borders
        DrawRect(new PointF(position.X - (size.Width / 2f), position.Y), new SizeF(borderThicknessX, size.Height), color); // Left
        DrawRect(new PointF(position.X + (size.Width / 2f), position.Y), new SizeF(borderThicknessX, size.Height), color); // Right
    }
}