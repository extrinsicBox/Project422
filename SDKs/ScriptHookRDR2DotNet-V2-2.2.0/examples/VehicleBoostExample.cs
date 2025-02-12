using System;
using System.Windows.Forms;

using RDR2;             // Generic RDR2 related stuff
using RDR2.UI;          // UI related stuff
using RDR2.Native;      // RDR2 native functions (commands)
using RDR2.Math;        // Vectors, Quaternions, Matrixes

namespace VehicleBoostExample
{
	public class Main : Script
	{
		public Main()
		{
			Tick += OnTick; // Hook Tick event. This is called every frame
			Interval = 1; // Optional, default is 0
		}

		private void OnTick(object sender, EventArgs e)
		{
			Ped playerPed = Game.Player.Ped;

			if (playerPed.IsInVehicle)
			{
				Vehicle vehicle = playerPed.CurrentVehicle;

				// X (Keyboard) or Right Stick (Controller)
				if (Game.IsControlPressed(eInputType.FrontendRS))
				{
					if (vehicle.Model.IsTrain)
					{
						VEHICLE._SET_TRAIN_MAX_SPEED(vehicle.Handle, 100.0f);
						VEHICLE.SET_TRAIN_SPEED(vehicle.Handle, 100.0f);
					}
					else
					{
						VEHICLE.SET_VEHICLE_FORWARD_SPEED(vehicle.Handle, 40.0f);
					}
				}
			}
		}
	}
}
