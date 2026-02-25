using System.Runtime.InteropServices;
using System.Windows.Forms;
// ════════════════════════════════════════════════════════════════
//  Program Entry Point
// ════════════════════════════════════════════════════════════════

namespace TrafficPilot;

internal static class Program
{
	[STAThread]
	static void Main()
	{
		// Check Windows platform
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			MessageBox.Show("This program only supports Windows.", "Platform Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		// Configure high DPI mode for Windows 10 1703+
		Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm());
	}
}
