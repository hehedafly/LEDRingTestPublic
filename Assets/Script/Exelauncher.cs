using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

public class ExeLauncher
{
    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();

    [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool GetOpenFileName(ref OpenFileName ofn);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct OpenFileName
    {
        public int lStructSize;
        public System.IntPtr hwndOwner;
        public System.IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public System.IntPtr lCustData;
        public System.IntPtr lpfnHook;
        public string lpTemplateName;
        public System.IntPtr pvReserved;
        public int dwReserved;
        public int flagsEx;
    }

    public string Start(string exePath, string exeName = "", bool forceLaunch = false)
    {
        return LaunchExe(exePath, exeName, forceLaunch);
    }

    private string LaunchExe(string exePath, string exeName = "", bool forceLaunch = false)
    {

        if(!IsValidExePath(exePath)){exePath = OpenFileDialog();}

        if(IsValidExePath(exePath)){
            if (System.Diagnostics.Process.GetProcessesByName(exeName).Length > 0 & !forceLaunch) {
                return exePath;
            }
            StartExe(exePath);
            return exePath;
        }

        return "";
    }

    private bool IsValidExePath(string path)
    {
        return !string.IsNullOrEmpty(path) && 
               File.Exists(path) && 
               Path.GetExtension(path).Equals(".exe", System.StringComparison.OrdinalIgnoreCase);
    }

    private void StartExe(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"启动失败: {e.Message}");
        }
    }

    private string OpenFileDialog()
    {
        string originalDirectory = Directory.GetCurrentDirectory();
        const int MAX_PATH = 260;
        OpenFileName ofn = new OpenFileName
        {
            lStructSize = Marshal.SizeOf(typeof(OpenFileName)),
            hwndOwner = GetActiveWindow(),
            lpstrFilter = "可执行文件 (*.exe)\0*.exe\0所有文件 (*.*)\0*.*\0",
            lpstrFile = new string('\0', MAX_PATH),
            nMaxFile = MAX_PATH,
            lpstrTitle = "选择要启动的EXE文件",
            Flags = 0x00080000 |  // OFN_EXPLORER
                0x00001000 |  // OFN_FILEMUSTEXIST
                0x00000008    // OFN_NOCHANGEDIR
        };
        Directory.SetCurrentDirectory(originalDirectory);

        if (GetOpenFileName(ref ofn))
        {
            return ofn.lpstrFile.Split('\0')[0];
        }
        return null;
    }
}