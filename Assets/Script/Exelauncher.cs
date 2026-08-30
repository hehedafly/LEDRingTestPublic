using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections;
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
        string workingDirectory = null,
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
        if(string.IsNullOrEmpty(fullCommand) || !fullCommand.Contains(">")){
            return new List<string>();
        }
        string[] CmdParts = fullCommand.Split('>')[1].Split(new[] { ' ' }, 2);
        string ExePath = CmdParts[0].Replace('/', '\\');
        string[] ScriptArgs = CmdParts.Length > 1 ? CmdParts[1].Split(new[] { ' ' }, 2) : new string[]{};
        string ScriptPath = ScriptArgs.Length > 0 ? ScriptArgs[0].Replace('/', '\\') : "";
        string Arguments = ScriptArgs.Length > 1 ? ScriptArgs[1] : "";
        string ScriptDirectory = Path.GetDirectoryName(ScriptPath) ?? "";

        return new List<string> {ExePath, ScriptPath, Arguments, ScriptDirectory};
    }
}

public static class LogEventController
{
    // ========== Win32 API 导入 ==========
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, System.Text.StringBuilder lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool IsWindowEnabled(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    private const uint BM_CLICK = 0x00F5;
    private const uint WM_GETTEXT = 0x000D;
    private const uint SMTO_NORMAL = 0x0000;
    private const uint SMTO_ABORTIFHUNG = 0x0002;
    private const int IDOK = 1;
    private const int IDYES = 6;

    private const string MainWindowTitle = "LogEvent";
    private const string MainWindowClass = "#32770";
    private const string RecordButtonText = "Record";
    private const string LoadButtonText = "Load";

    // 最近一次后台点击任务是否已执行发送，true=已尝试发送 BM_CLICK。仅供 LastRecordClickState 读取。
    private static Task<bool> pendingClickTask = null;
    private static bool pendingClickStart = false;

    // 每个方向上次真正排队点击的时间。SendMessageTimeout 超时不代表消息一定没被处理，
    // 因此不能一超时就在下一次轮询立即补发，否则会排队多个 BM_CLICK，表现为“需要点两次”。
    private static double _lastStartClickRealtime = -999d;
    private static double _lastEndClickRealtime = -999d;
    private static double _lastMissingButtonLogRealtime = -999d;

    // 后台线程查询到的状态缓存。Unity 主线程只读取该缓存，不再直接枚举窗口，
    // 避免 LogEvent 窗口不存在或挂起时 GetWindowText/EnumWindows 阻塞主线程。
    private static readonly object _stateLock = new object();
    private static int _cachedRecordButtonState = -1;
    private static double _lastStateQueryRealtime = -999d;

    private static readonly object _winEnumLock = new object();
    private static double NowRealtimeSeconds()
    {
        return (double)System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency;
    }

    // ========== 通用窗口/控件查找 ==========
    private static string GetWindowText(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(512);
        // 跨进程窗口可能挂起，GetWindowText 会阻塞调用线程。改用 SendMessageTimeout
        // 的 WM_GETTEXT，并在目标无响应时立即返回，避免“未找到按钮”时卡死。
        SendMessageTimeout(hWnd, WM_GETTEXT, (IntPtr)sb.Capacity, sb, SMTO_ABORTIFHUNG, 250, out IntPtr _);
        return sb.ToString();
    }

    private static string GetWindowClassName(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero) return "";
        System.Text.StringBuilder sb = new System.Text.StringBuilder(256);
        GetClassName(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    private static IntPtr _enumChildResult = IntPtr.Zero;
    private static string _enumChildTarget = "";
    private static readonly EnumWindowsProc _enumChildProc = EnumChildWindowsProc;

    private static bool EnumChildWindowsProc(IntPtr hWnd, IntPtr lParam)
    {
        if (GetWindowClassName(hWnd) == "Button" && GetWindowText(hWnd) == _enumChildTarget)
        {
            _enumChildResult = hWnd;
            return false;
        }
        return true;
    }

    private static IntPtr FindChildButton(IntPtr parent, string buttonText)
    {
        if (parent == IntPtr.Zero || string.IsNullOrEmpty(buttonText)) return IntPtr.Zero;
        lock (_winEnumLock)
        {
            _enumChildResult = IntPtr.Zero;
            _enumChildTarget = buttonText;
            EnumChildWindows(parent, _enumChildProc, IntPtr.Zero);
            return _enumChildResult;
        }
    }

    private static IntPtr _enumTopResult = IntPtr.Zero;
    private static readonly EnumWindowsProc _enumTopProc = EnumTopWindowsProc;

    private static bool EnumTopWindowsProc(IntPtr hWnd, IntPtr lParam)
    {
        // LogEvent 主窗口和 AfxMessageBox 都可能以 "LogEvent" 为标题。
        // 必须选择包含 "Load" 按钮的那个窗口；否则弹窗出现时会把弹窗误当成主窗口。
        if (GetWindowText(hWnd) == MainWindowTitle && GetWindowClassName(hWnd) == MainWindowClass)
        {
            if (FindChildButton(hWnd, LoadButtonText) != IntPtr.Zero)
            {
                _enumTopResult = hWnd;
                return false;
            }
        }
        return true;
    }

    private static IntPtr FindLogEventMainWindow()
    {
        lock (_winEnumLock)
        {
            _enumTopResult = IntPtr.Zero;
            EnumWindows(_enumTopProc, IntPtr.Zero);
            return _enumTopResult;
        }
    }

    private static IntPtr FindRecordButton()
    {
        IntPtr hWnd = FindLogEventMainWindow();
        if (hWnd == IntPtr.Zero) return IntPtr.Zero;
        return FindChildButton(hWnd, RecordButtonText);
    }

    private static IntPtr FindLoadButton()
    {
        IntPtr hWnd = FindLogEventMainWindow();
        if (hWnd == IntPtr.Zero) return IntPtr.Zero;
        return FindChildButton(hWnd, LoadButtonText);
    }

    // ========== 状态查询 ==========
    // 返回 1=未录制（Load 可用），0=录制中（Load 不可用），-1=未找到 Load 按钮。
    private static int QueryRecordButtonState()
    {
        IntPtr hLoad = FindLoadButton();
        int state = hLoad == IntPtr.Zero ? -1 : (IsWindowEnabled(hLoad) ? 1 : 0);
        SetCachedRecordButtonState(state);
        return state;
    }

    private static void SetCachedRecordButtonState(int state)
    {
        lock (_stateLock)
        {
            _cachedRecordButtonState = state;
            _lastStateQueryRealtime = NowRealtimeSeconds();
        }
    }

    // 仅供 Unity 主线程读取后台查询结果，不执行任何 Win32 窗口枚举，因此不会阻塞。
    public static int GetCachedRecordButtonState()
    {
        lock (_stateLock)
        {
            return _cachedRecordButtonState;
        }
    }

    private static bool IsCachedStateFresh(bool start)
    {
        lock (_stateLock)
        {
            int state = _cachedRecordButtonState;
            double age = NowRealtimeSeconds() - _lastStateQueryRealtime;
            return age >= 0d && age <= 2.0d && state != -1 && (start ? state == 0 : state == 1);
        }
    }

    public static int IsRecordButtonStateZero()
    {
        int state = GetCachedRecordButtonState();
        if (state == -1)
        {
            bool shouldLog = false;
            lock (_stateLock)
            {
                double now = NowRealtimeSeconds();
                if (now - _lastMissingButtonLogRealtime > 1.0d)
                {
                    _lastMissingButtonLogRealtime = now;
                    shouldLog = true;
                }
            }
            if (shouldLog)
            {
                UnityEngine.Debug.LogWarning("未找到 Load 按钮（后台查询中）");
            }
        }
        return state;
    }

    // ========== 自动关闭 LogEvent 模态弹窗 ==========
    private static bool _dismissedPopup = false;
    private static readonly EnumWindowsProc _enumPopupProc = EnumPopupWindowsProc;

    private static bool EnumPopupWindowsProc(IntPtr hWnd, IntPtr lParam)
    {
        if (GetWindowText(hWnd) != MainWindowTitle || GetWindowClassName(hWnd) != MainWindowClass)
            return true;

        // 主窗口包含 Load 按钮，跳过
        if (FindChildButton(hWnd, LoadButtonText) != IntPtr.Zero)
            return true;

        // 只处理标准 MessageBox：优先点“是”（覆盖文件），否则点“确定”。
        IntPtr hTarget = GetDlgItem(hWnd, IDYES);
        if (hTarget == IntPtr.Zero) hTarget = GetDlgItem(hWnd, IDOK);
        if (hTarget == IntPtr.Zero) hTarget = FindChildButton(hWnd, "Yes");
        if (hTarget == IntPtr.Zero) hTarget = FindChildButton(hWnd, "OK");
        if (hTarget == IntPtr.Zero) hTarget = FindChildButton(hWnd, "是");
        if (hTarget == IntPtr.Zero) hTarget = FindChildButton(hWnd, "确定");
        if (hTarget == IntPtr.Zero) return true;

        SendMessageTimeout(hTarget, BM_CLICK, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, 1000, out IntPtr _);
        _dismissedPopup = true;
        return true; // 继续枚举，关闭所有弹窗
    }

    private static bool DismissLogEventPopups()
    {
        lock (_winEnumLock)
        {
            _dismissedPopup = false;
            EnumWindows(_enumPopupProc, IntPtr.Zero);
            return _dismissedPopup;
        }
    }

    // ========== 后台可靠点击 ==========
    private static bool ClickRecordButtonWhenReady(bool start, int sendTimeoutMs)
    {
        // 先关闭弹窗并等待 Record 按钮可用；只有按钮可点击时才投递 BM_CLICK，
        // 避免模态弹窗阻塞导致 SendMessageTimeout 超时后消息又被延迟处理。
        int waitMs = Math.Min(sendTimeoutMs, 2000);
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < waitMs)
        {
            DismissLogEventPopups();

            int state = QueryRecordButtonState();
            bool reached = start ? (state == 0) : (state == 1);
            if (reached) return true; // 等待期间状态已到达期望值，无需再点

            IntPtr hRecord = FindRecordButton();
            if (hRecord != IntPtr.Zero && IsWindowEnabled(hRecord))
            {
                MarkClickSent(start);
                SendMessageTimeout(hRecord, BM_CLICK, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG, (uint)Math.Min(sendTimeoutMs, 2000), out IntPtr _);
                return true;
            }

            System.Threading.Thread.Sleep(50);
        }
        return false;
    }

    private static void MarkClickSent(bool start)
    {
        lock (_stateLock)
        {
            if (start) _lastStartClickRealtime = NowRealtimeSeconds();
            else _lastEndClickRealtime = NowRealtimeSeconds();
        }
    }

    // ========== 单次可靠点击录制按钮 ==========
    // start = true  : 期望开始录制，仅在状态 1（未录制）时点击。
    // start = false : 期望停止录制，仅在状态 0（录制中）时点击。
    // 返回值：0=已在期望状态(无需点击) 1=已安排后台点击/等待补发。
    // 本方法绝不直接枚举窗口，状态查询、弹窗关闭、按钮查找和点击都在后台 Task 中执行，
    // 防止 LogEvent 未启动、窗口被销毁或目标进程挂起时卡死 Unity 主线程。
    public static int ScheduleRecordClick(bool start, int sendTimeoutMs = 5000)
    {
        // 仅在缓存较新时才直接判定“已在期望状态”；缓存过期则仍启动后台任务复核，
        // 避免因上次状态残留造成漏点或误判。
        if (IsCachedStateFresh(start)) return 0;

        lock (_stateLock)
        {
            // 任一方向已有在途点击时，不启动第二个后台任务，避免开/关点击互相穿插。
            if (pendingClickTask != null && !pendingClickTask.IsCompleted) return 1;

            // 冷却时间：给 LogEvent 留出处理时间。实际发送时间由 ClickRecordButtonWhenReady 写入。
            double lastRealtime = start ? _lastStartClickRealtime : _lastEndClickRealtime;
            double minInterval = start ? 0.5d : 1.0d;
            if (NowRealtimeSeconds() - lastRealtime < minInterval) return 1;

            pendingClickStart = start;
            pendingClickTask = Task.Run(() => ClickRecordButtonWhenReady(start, sendTimeoutMs));
            return 1;
        }
    }

    // 查询最近一次该方向后台点击任务的执行结果，供需要精细控制补发的调用方使用。
    // 1=已尝试发送 BM_CLICK 0=等待窗口/按钮可用超时，未发送 -1=该方向无已完成任务/仍在途(不要立即补发)
    public static int LastRecordClickState(bool start)
    {
        if (pendingClickTask == null || pendingClickStart != start) return -1;
        if (!pendingClickTask.IsCompleted) return -1;
        return pendingClickTask.Result ? 1 : 0;
    }
}
