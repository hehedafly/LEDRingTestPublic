using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;

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

    public async Task<bool> LaunchPythonAsync(
        string pythonExePath, 
        string pythonScriptPath, 
        string arguments = "",
        string workingDirectory = null, // 新增工作目录参数
        int timeoutMs = 2000)
    {
        try
        {
            using (Process process = new Process())
            {
                // 配置进程参数
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = $"\"{pythonScriptPath}\" {arguments}",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // 设置工作目录（如果提供）
                if (!string.IsNullOrEmpty(workingDirectory))
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                }

                // 启动进程
                if (!process.Start())
                {
                    UnityEngine.Debug.LogError("Python 进程启动失败");
                    return false;
                }

                // 异步读取错误流
                var errorReading = process.StandardError.ReadToEndAsync();
                var timeoutTask = Task.Delay(timeoutMs);
                
                // 等待任一任务完成
                await Task.WhenAny(errorReading, timeoutTask);

                // 检查进程状态
                if (process.HasExited)
                {
                    string errors = await errorReading;
                    UnityEngine.Debug.LogError($"Python 异常退出，代码: {process.ExitCode}\n错误: {errors}");
                    return false;
                }

                // 检查是否有错误输出
                if (errorReading.IsCompleted && !string.IsNullOrEmpty(errorReading.Result))
                {
                    UnityEngine.Debug.LogError($"Python 报告错误: {errorReading.Result}");
                    return false;
                }

                // 启动成功
                UnityEngine.Debug.Log($"Python 启动成功: {pythonScriptPath} {arguments}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"启动 Python 异常: {e.Message}");
            return false;
        }
    }
    
    // 同步启动方法（添加工作目录支持）
    public void LaunchPython(
        string pythonExePath, 
        string pythonScriptPath, 
        string arguments = "",
        string workingDirectory = null)
    {
        _ = LaunchPythonAsync(pythonExePath, pythonScriptPath, arguments, workingDirectory);
    }

    public List<string> CommandParser(string fullCommand)
    {
        string[] CmdParts = fullCommand.Split('>')[1].Split(new[] { ' ' }, 2);
        string ExePath = CmdParts[0].Replace('/', '\\');
        string[] ScriptArgs = CmdParts.Length > 1 ? CmdParts[1].Split(new[] { ' ' }, 2) : new string[]{};
        string ScriptPath = ScriptArgs.Length > 0 ? ScriptArgs[0].Replace('/', '\\') : "";
        string Arguments = ScriptArgs.Length > 1 ? ScriptArgs[1] : "";
        string ScriptDirectory = Path.GetDirectoryName(ScriptPath) ?? "";

        return new List<string> {ExePath, ScriptPath, Arguments, ScriptDirectory};
    }
}