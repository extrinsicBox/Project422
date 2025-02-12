using System;
using System.Windows.Forms;

using RDR2;             // Generic RDR2 related stuff
using RDR2.UI;          // UI related stuff
using RDR2.Native;      // RDR2 native functions (commands)
using RDR2.Math;        // Vectors, Quaternions, Matrixes

namespace ReviveExample
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
			// Ctrl + Alt
			// Revive all dead peds
			if (Game.IsControlPressed(eInputType.Duck) && Game.IsControlJustPressed(eInputType.RevealHud))
			{
				Ped[] allPeds = World.GetAllPeds();
				foreach (Ped ped in allPeds)
				{
					if (ped.Exists() && !ped.IsAlive)
					{
						ped.Revive();
					}
				}
			}
		}
	}
}
