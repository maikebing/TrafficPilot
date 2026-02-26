using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
        NativeLibrary.SetDllImportResolver(typeof(Program).Assembly, MyDllImportResolver);
        Application.Run(new MainForm());
	}
    private static IntPtr MyDllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        var appdir = Path.Combine(
             Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
             "TrafficPilot");
        // 尝试从自定义路径加载
        if (libraryName == "WinDivert.dll")
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
            if (NativeLibrary.TryLoad(windiv.FullName, out nint windivhandle))
            {
                return windivhandle;
            }
        }
        // 返回 IntPtr.Zero 表示让运行时继续使用默认搜索逻辑
        return IntPtr.Zero;
    }
}
