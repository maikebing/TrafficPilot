using System.Runtime.InteropServices;
using System.Security.Principal;

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

		// Check admin rights
		if (!IsRunAsAdmin())
		{
			MessageBox.Show(
				"This program requires administrator rights to function properly.\n\nPlease restart as administrator.",
				"Administrator Required",
				MessageBoxButtons.OK,
				MessageBoxIcon.Warning);
			return;
		}

		ApplicationConfiguration.Initialize();
		Application.Run(new MainForm());
	}

	private static bool IsRunAsAdmin()
	{
		try
		{
			using var identity = WindowsIdentity.GetCurrent();
			var principal = new WindowsPrincipal(identity);
			return principal.IsInRole(WindowsBuiltInRole.Administrator);
		}
		catch { return false; }
	}
}
