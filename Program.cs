using System.Runtime.InteropServices;

// ════════════════════════════════════════════════════════════════
//  Program Entry Point
// ════════════════════════════════════════════════════════════════

namespace VSifier;

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

		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm());
	}
}
