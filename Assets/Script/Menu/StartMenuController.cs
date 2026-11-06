using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 单个配置文件在菜单中的展示数据。
/// </summary>
public class ConfigEntry
{
    public string Path;
    public string DisplayName;
    /// <summary>barDisplayKeys 指定的 section:key -> 读取到的值（缺失则为空串）。</summary>
    public Dictionary<string, string> BarValues = new Dictionary<string, string>();
}

/// <summary>
/// 初始菜单主控：负责进入时的 skip 判定、场景切换、预览数据持有与派发、打开配置文件、
/// 副屏遮罩、加载遮罩与退出。列表/横条/交互职责已交给 ConfigListPresenter；
/// 预览 UI 通过 ConfigPreviewController 与控制器解耦。
/// 控件派发采用 UIUpdate.ControlsParse 风格的 switch(elementsName)。
/// </summary>
public class StartMenuController : MonoBehaviour
{
    [Tooltip("横条展示的键，格式 section:key")]
    [HideInInspector]
    List<string> barDisplayKeys = new List<string>
    {
        "settings:MouseName",
        "logSettings:logPath"
    };

    [Tooltip("LinearTrack 模式横条展示的键，格式 section:key")]
    [HideInInspector]
    List<string> linearTrackBarDisplayKeys = new List<string>
    {
        "trail_content:context_for_repeat",
        "trail_content:repeat_num"
    };

    [Tooltip("配置列表 UI 根节点（脚本只做显隐）")]
    [SerializeField] GameObject configListRoot;

    [Tooltip("预览 UI 根节点（脚本只做显隐）")]
    [SerializeField] GameObject previewRoot;

    [Tooltip("可选：横条预制体父节点，配合 barPrefab 自动实例化")]
    [SerializeField] Transform barContainer;

    [Tooltip("可选：横条预制体（需挂 ConfigBarView），赋值后自动实例化并绑定")]
    [SerializeField] GameObject barPrefab;

    [Tooltip("可选：配置列表所在的滑动窗口；为空时从 barContainer 向上查找 ScrollRect")]
    [SerializeField] ScrollRect configScrollRect;

    [Tooltip("可选：刷新按钮，预览期间自动置灰禁用")]
    [SerializeField] Button refreshButton;

    [Tooltip("可选：点击配置后覆盖全屏的加载遮罩（灰底 Image + loading 文本）；默认应为隐藏")]
    [SerializeField] GameObject loadingOverlay;
    [Tooltip("可选：预览视图；为空时运行时自动在场景中查找 ConfigPreviewView")]
    [SerializeField] ConfigPreviewView previewView;

    [Tooltip("可选：Go To VR 按钮根节点（仅 LEDRing 页显示）；未赋值则不改其显隐")]
    [SerializeField] GameObject goToVrRoot;
    [Tooltip("可选：Go To LEDRing 按钮根节点（仅 LinearTrack 页显示）；未赋值则不改其显隐")]
    [SerializeField] GameObject goToLedRingRoot;

    /// <summary>只读预览数据控制器（供预览 UI 读取）。</summary>
    public ConfigPreviewController Preview { get; } = new ConfigPreviewController();

    /// <summary>配置列表表现层（扫描/横条/显隐/交互开关）。</summary>
    ConfigListPresenter listPresenter;

    /// <summary>当前列出的配置条目（转发自表现层）。</summary>
    public IReadOnlyList<ConfigEntry> Entries => listPresenter.Entries;

    // UI 可订阅的钩子事件
    public event Action OnConfigListBuilt;
    public event Action OnPreviewOpened;
    public event Action OnPreviewClosed;
    public event Action OnPreviewRefreshed;

    /// <summary>构建表现层并把预览数据注入预览视图，完成 View 与 Controller 的解耦。</summary>
    void Awake()
    {
        listPresenter = new ConfigListPresenter(
            this, configListRoot, previewRoot, barContainer, barPrefab, configScrollRect, refreshButton, barDisplayKeys);

        ConfigPreviewView view = previewView != null ? previewView : FindObjectOfType<ConfigPreviewView>(true);
        if (view != null) { view.Bind(Preview); }
    }

    /// <summary>入口：首次进入按 prefer_context 选页；LEDRing 模式且 skip=true 直接进 MainScene；否则显示配置列表。</summary>
    void Start()
    {
        if (!LaunchContext.MenuModeInitialized)
        {
            LaunchContext.Mode = ReadPreferredMode();
            LaunchContext.MenuModeInitialized = true;
        }

        if (LaunchContext.Mode == LaunchMode.LedRing && ShouldSkipMenu())
        {
            // skip=true：直接以默认 config.ini 进入 MainScene，退出时直接关闭应用（表现得没有菜单）
            LaunchContext.SelectedConfigPath = LaunchContext.DefaultConfigPath();
            LaunchContext.ReturnToMenuOnExit = false;
            LaunchContext.HasSelected = true;
            SceneManager.LoadScene(LaunchContext.TargetSceneName());
            return;
        }

        ApplyMode(LaunchContext.Mode);
        BlankSecondaryDisplays();
        RefreshList();
    }

    /// <summary>供 Start 与 Refresh 复用：仅重扫配置、重建横条、回列表；不读 metaConfig、不判 skip。</summary>
    public void RefreshList()
    {
        listPresenter.Build();
        listPresenter.ShowList();
        OnConfigListBuilt?.Invoke();
    }

    /// <summary>应用菜单模式：同步静态 Mode、互斥显隐两个跳转按钮、切换横条展示键。</summary>
    void ApplyMode(LaunchMode mode)
    {
        LaunchContext.Mode = mode;
        if (goToVrRoot != null) { goToVrRoot.SetActive(mode == LaunchMode.LedRing); }
        if (goToLedRingRoot != null) { goToLedRingRoot.SetActive(mode == LaunchMode.LinearTrack); }
        listPresenter.SetBarDisplayKeys(mode == LaunchMode.LinearTrack ? linearTrackBarDisplayKeys : barDisplayKeys);
    }

    /// <summary>切换开始页模式并重扫该模式配置目录（预览期间忽略，避免重走流程）。</summary>
    public void SwitchMenuMode(LaunchMode mode)
    {
        if (listPresenter.IsPreviewing) { return; }
        ApplyMode(mode);
        RefreshList();
    }

    /// <summary>读取 metaConfig.ini 的 [settings] prefer_context 作为首次进入页；无值/无效值回退 LEDRing。</summary>
    LaunchMode ReadPreferredMode()
    {
        IniFileParser meta = IniFileParser.FromFile(LaunchContext.MetaConfigPath());
        if (!meta.TryGetValue("settings", "prefer_context", out string raw) || string.IsNullOrEmpty(raw))
        {
            return LaunchMode.LedRing;
        }

        // 忽略大小写与下划线
        string normalized = raw.Replace("_", "").Trim();
        if (string.Equals(normalized, "LinearTrack", StringComparison.OrdinalIgnoreCase)) { return LaunchMode.LinearTrack; }
        if (string.Equals(normalized, "LEDRing", StringComparison.OrdinalIgnoreCase)) { return LaunchMode.LedRing; }

        Debug.LogWarning($"[StartMenu] metaConfig prefer_context 值无效: {raw}，使用默认 LEDRing");
        return LaunchMode.LedRing;
    }

    /// <summary>读取 metaConfig.ini 的 [settings] skip 是否为 true。</summary>
    bool ShouldSkipMenu()
    {
        IniFileParser meta = IniFileParser.FromFile(LaunchContext.MetaConfigPath());
        if (meta.TryGetValue("settings", "skip", out string v))
        {
            return string.Equals(v.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    /// <summary>菜单期间遮住已激活的副屏：Unity 无 Display.Deactivate，为每个 active 副屏放一个纯黑相机。
    /// 该物体属于 StartMenu 场景，进入 MainScene 时随场景卸载自动销毁，Moving 无需关心。</summary>
    void BlankSecondaryDisplays()
    {
        int n = Display.displays.Length;
        if (n <= 1) { return; }
        GameObject blanker = null;
        for (int i = 1; i < n; i++)
        {
            if (!Display.displays[i].active) { continue; }   // 仅遮已激活的副屏
            if (blanker == null)
            {
                blanker = new GameObject("MenuDisplayBlanker");
                blanker.transform.SetParent(transform, false);
            }
            Camera cam = new GameObject("BlankCam" + i).AddComponent<Camera>();
            cam.transform.SetParent(blanker.transform, false);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;      // 只清黑，不渲染物体
            cam.targetDisplay = i;
        }
    }

    /// <summary>
    /// ControlsParse 风格的菜单派发入口。
    /// </summary>
    /// <param name="elementName">控件名</param>
    /// <param name="value">数值参数（保留）</param>
    /// <param name="stringArg">字符串参数，如 "left;0" / "right;1"</param>
    public int MenuControlsParse(string elementName, float _ = 0, string stringArg = "")
    {
        switch (elementName)
        {
            case "ConfigBar":
            {
                // stringArg 形如 left;{index} 或 right;{index}
                string[] parts = stringArg.Split(';');
                string button = parts.Length > 0 ? parts[0] : "left";
                int index = parts.Length > 1 && int.TryParse(parts[1], out int idx) ? idx : -1;
                if (index < 0 || index >= listPresenter.Count) { return -1; }
                if (button == "right") { OpenPreview(index); }
                else { LaunchConfig(index); }
                return 1;
            }
            case "PreviewArrowLeft":
                Preview.PrevPage();
                OnPreviewRefreshed?.Invoke();
                return 1;
            case "PreviewArrowRight":
                Preview.NextPage();
                OnPreviewRefreshed?.Invoke();
                return 1;
            case "PreviewSortToggle":
                Preview.ToggleSort();
                OnPreviewRefreshed?.Invoke();
                return 1;
            case "PreviewBack":
                ClosePreview();
                return 1;
            case "OpenCurrentConfig":
                OpenCurrentConfigFile();
                return 1;
            case "GoToVR":
                SwitchMenuMode(LaunchMode.LinearTrack);
                return 1;
            case "GoToLedRing":
                SwitchMenuMode(LaunchMode.LedRing);
                return 1;
            case "Refresh":
                if (listPresenter.IsPreviewing) { return 0; }   // 预览期忽略刷新，避免重走流程
                RefreshList();
                return 1;
            case "QuitApp":
                QuitApp();
                return 1;
            default:
                Debug.LogWarning($"[StartMenu] unknown element: {elementName}");
                return -1;
        }
    }

    /// <summary>以指定配置进入当前模式对应的运行场景（退出时返回菜单）。</summary>
    public void LaunchConfig(int index)
    {
        if (index < 0 || index >= listPresenter.Count) { return; }
        LaunchContext.SelectedConfigPath = listPresenter.Entries[index].Path;
        LaunchContext.ReturnToMenuOnExit = true;
        LaunchContext.HasSelected = true;
        if (loadingOverlay != null)
        {
            // 先显示加载遮罩，等一帧渲染后再切场景（同步 LoadScene 会阻塞主线程，画面停在遮罩帧）
            loadingOverlay.SetActive(true);
            loadingOverlay.transform.SetAsLastSibling();   // 压到所有兄弟物体之上，否则 GoTo 按钮仍可见可点
            StartCoroutine(LoadTargetSceneNextFrame());
        }
        else
        {
            SceneManager.LoadScene(LaunchContext.TargetSceneName());
        }
    }

    /// <summary>协程：等一帧让加载遮罩渲染后再切换场景。</summary>
    IEnumerator LoadTargetSceneNextFrame()
    {
        yield return null;   // 让遮罩至少渲染一帧
        SceneManager.LoadScene(LaunchContext.TargetSceneName());
    }

    /// <summary>打开某个配置的只读预览（先显示预览根再加载数据，确保布局有效）。</summary>
    public void OpenPreview(int index)
    {
        if (index < 0 || index >= listPresenter.Count) { return; }
        listPresenter.ShowPreview();
        Preview.Open(listPresenter.Entries[index].Path, LaunchContext.Mode);
        OnPreviewOpened?.Invoke();
        OnPreviewRefreshed?.Invoke();
    }

    /// <summary>关闭预览，回到列表。</summary>
    public void ClosePreview()
    {
        listPresenter.ShowList();
        OnPreviewClosed?.Invoke();
    }

    /// <summary>退出应用（编辑器下停止播放）。</summary>
    void QuitApp()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    /// <summary>用 Windows 默认关联程序打开当前预览的配置文件（仅 Windows，ShellExecute open）。</summary>
    void OpenCurrentConfigFile()
    {
        string path = Preview != null ? Preview.CurrentPath : null;
        if (string.IsNullOrEmpty(path)) { Debug.LogWarning("[StartMenu] 当前没有预览中的配置文件"); return; }
        try
        {
            string full = Path.GetFullPath(path);   // 编辑器下把相对路径转绝对（CWD=工程根）
            if (!File.Exists(full))                 // 文件不存在则在该位置创建空文件（如 default 条目）
            {
                string dir = Path.GetDirectoryName(full);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) { Directory.CreateDirectory(dir); }
                File.WriteAllText(full, "");
            }
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(full)
            {
                UseShellExecute = true,             // 走系统关联程序；勿引入 using System.Diagnostics 以免与 UnityEngine.Debug 冲突
                Verb = "open"
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[StartMenu] 打开配置文件失败: {e.Message}");
        }
    }

    // ==== 供 Button.onClick 在 Inspector 中直接绑定的无参包装 ====
    // （MenuControlsParse 有三个参数，无法直接出现在 onClick 下拉里，故提供以下入口）
    /// <summary>预览上一子页。</summary>
    public void OnPreviewArrowLeft()  { MenuControlsParse("PreviewArrowLeft"); }
    /// <summary>预览下一子页。</summary>
    public void OnPreviewArrowRight() { MenuControlsParse("PreviewArrowRight"); }
    /// <summary>切换预览排序。</summary>
    public void OnPreviewSortToggle() { MenuControlsParse("PreviewSortToggle"); }
    /// <summary>关闭预览返回列表。</summary>
    public void OnPreviewBack()       { MenuControlsParse("PreviewBack"); }
    /// <summary>打开当前预览的配置文件。</summary>
    public void OnOpenCurrentConfig() { MenuControlsParse("OpenCurrentConfig"); }
    /// <summary>进入 LinearTrack（VR）开始页。</summary>
    public void OnGoToVR()            { MenuControlsParse("GoToVR"); }
    /// <summary>返回 LEDRing 开始页。</summary>
    public void OnGoToLedRing()       { MenuControlsParse("GoToLedRing"); }
    /// <summary>刷新配置列表。</summary>
    public void OnRefresh()           { MenuControlsParse("Refresh"); }
    /// <summary>退出应用。</summary>
    public void OnQuitApp()           { MenuControlsParse("QuitApp"); }
}
