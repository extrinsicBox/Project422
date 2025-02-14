using System;
using System.Windows.Forms;
using RDR2;
using RDR2.Math;
using RDR2.Native;

public class SimplePointToPoint : Script
{
    private bool isMoving = false;
    private Vector3 startPoint = new Vector3(-949.2f, -1273.33f, 188.96f); // Updated start position
    private Vector3 endPoint = new Vector3(-325.86f, 888.53f, 59.81f); // Updated end position
    private float moveSpeed = 1.0f;

    public SimplePointToPoint()
    {
        KeyDown += OnKeyDown;
        Tick += OnTick;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F10)
        {
            isMoving = !isMoving;
            if (isMoving)
            {
                Game.Player.Character.Position = startPoint;
            }
        }
    }

    private void OnTick(object sender, EventArgs e)
    {
        if (isMoving)
        {
            Ped player = Game.Player.Character;
            Vector3 currentPosition = player.Position;
            
            // Move towards the endpoint
            Vector3 direction = (endPoint - currentPosition).Normalized;
            Vector3 newPosition = currentPosition + direction * moveSpeed * Function.Call<float>(0x5E72022914CE3C38);
            
            // Stop when close to the endpoint
            if ((endPoint - currentPosition).Length() < moveSpeed * Function.Call<float>(0x5E72022914CE3C38))
            {
                player.Position = endPoint;
                isMoving = false;
            }
            else
            {
                player.Position = newPosition;
            }
        }
    }
}
