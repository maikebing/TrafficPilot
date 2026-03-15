using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TrafficPilot;
internal static class Program
{
	[STAThread]
	static void Main(string[] args)
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
        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, MyDllImportResolver);
        
        // Check for startup mode (minimized to tray)
        bool startMinimized = args.Length > 0 && 
            (args[0].Equals("--minimized", StringComparison.OrdinalIgnoreCase) ||
             args[0].Equals("--startup", StringComparison.OrdinalIgnoreCase) ||
             args[0].Equals("/minimized", StringComparison.OrdinalIgnoreCase) ||
             args[0].Equals("/startup", StringComparison.OrdinalIgnoreCase));
        
        Application.Run(new MainForm(startMinimized));
	}
    static nint windivhandle= nint.Zero;
    private static IntPtr MyDllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        var appdir = Path.Combine(
             Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
             "TrafficPilot");
        if (!Directory.Exists(appdir))
        {
            Directory.CreateDirectory(appdir);
        }
        // 尝试从自定义路径加载
        if (libraryName == "WinDivert.dll")
        {
            if (windivhandle == nint.Zero)
            {
                var windiv = new FileInfo(Path.Combine(appdir, "WinDivert.dll"));
                var winsys = new FileInfo(Path.Combine(appdir, "WinDivert64.sys"));
                if (!windiv.Exists)
                {
                    System.IO.File.WriteAllBytes(windiv.FullName, Properties.Resources.WinDivert);
                }
                if (!winsys.Exists)
                {
                    System.IO.File.WriteAllBytes(winsys.FullName, Properties.Resources.WinDivertSys);
                }
                if (NativeLibrary.TryLoad(windiv.FullName, out nint _windivhandle))
                {
                    windivhandle = _windivhandle;
                }
            }
            else
            {
                return windivhandle;
            }
            }
        // 返回 IntPtr.Zero 表示让运行时继续使用默认搜索逻辑
        return IntPtr.Zero;
    }
}
