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
/// 初始菜单主控脚本。设计参照 UIUpdate.ControlsParse 的 switch(elementsName) 派发风格。
/// 职责：
///  1. 进入时按 metaConfig.ini 的 [settings] skip 决定是否跳过菜单直接启动；
///  2. 扫描并列出 config*.ini，每个横条展示指定 key-value；
///  3. 左键 -> 以该配置进入 MainScene；右键 -> 进入只读预览；
///  4. 提供预览翻页/排序、返回列表、退出应用等派发入口。
/// UI 组件由外部搭建并绑定到本脚本的公开方法/字段/事件。
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

    [Tooltip("可选：点击配置后覆盖全屏的加载遮罩（灰底 Image + loading 文本），进入 MainScene 前显示；默认应为隐藏")]
    [SerializeField] GameObject loadingOverlay;

    // 预览状态与自动创建的横条（供刷新时清理，避免重复堆积）
    bool isPreviewing = false;
    readonly List<GameObject> createdBars = new List<GameObject>();

    /// <summary>只读预览控制器（供预览 UI 读取数据）。</summary>
    public ConfigPreviewController Preview { get; } = new ConfigPreviewController();

    /// <summary>当前列出的配置条目。</summary>
    public IReadOnlyList<ConfigEntry> Entries => entries;
    readonly List<ConfigEntry> entries = new List<ConfigEntry>();

    // UI 可订阅的钩子事件
    public event Action OnConfigListBuilt;
    public event Action OnPreviewOpened;
    public event Action OnPreviewClosed;
    public event Action OnPreviewRefreshed;

    void Start()
    {
        if (ShouldSkipMenu())
        {
            // skip=true：直接以默认 config.ini 进入 MainScene，退出时直接关闭应用（表现得没有菜单）
            LaunchContext.SelectedConfigPath = LaunchContext.DefaultConfigPath();
            LaunchContext.ReturnToMenuOnExit = false;
            LaunchContext.HasSelected = true;
            SceneManager.LoadScene("MainScene");
            return;
        }

        RefreshList();
    }

    /// <summary>供 Start 与 Refresh 复用：仅重扫配置、重建横条、回列表；不读 metaConfig、不判 skip。</summary>
    public void RefreshList()
    {
        BuildConfigList();
        ShowList();
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

    /// <summary>扫描配置文件并构建展示数据。</summary>
    public void BuildConfigList()
    {
        ClearBars();
        entries.Clear();
        string[] files = LaunchContext.FindConfigFiles();
        string defaultPath = LaunchContext.DefaultConfigPath();

        foreach (string file in files)
        {
            IniFileParser parser = IniFileParser.FromFile(file);
            ConfigEntry entry = new ConfigEntry
            {
                Path = file,
                DisplayName = BuildDisplayName(file, defaultPath, parser.FileExists),
            };

            foreach (string sk in barDisplayKeys)
            {
                if (string.IsNullOrEmpty(sk) || !sk.Contains(":")) { continue; }
                int split = sk.IndexOf(':');
                string section = sk.Substring(0, split).Trim();
                string key = sk.Substring(split + 1).Trim();
                parser.TryGetValue(section, key, out string value);
                entry.BarValues[sk] = value ?? "";
            }

            entries.Add(entry);
        }

        // 自动实例化横条；手动模式（无 barPrefab）则按索引重绑场景中已有的横条
        if (barPrefab != null && barContainer != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                GameObject go = Instantiate(barPrefab, barContainer);
                createdBars.Add(go);
                ConfigBarView view = go.GetComponent<ConfigBarView>();
                if (view != null) { view.Bind(i, this, entries[i]); }
            }
        }
        else
        {
            RebindExistingBars();
        }

        OnConfigListBuilt?.Invoke();
    }

    /// <summary>销毁本控制器自动创建的横条。</summary>
    void ClearBars()
    {
        foreach (GameObject go in createdBars) { if (go != null) { Destroy(go); } }
        createdBars.Clear();
    }

    /// <summary>手动模式：把场景中已有的 ConfigBarView 按索引重绑到最新 entries（多出的隐藏）。</summary>
    void RebindExistingBars()
    {
        if (barContainer == null) { return; }
        ConfigBarView[] views = barContainer.GetComponentsInChildren<ConfigBarView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            if (i < entries.Count) { views[i].gameObject.SetActive(true); views[i].Bind(i, this, entries[i]); }
            else { views[i].gameObject.SetActive(false); }
        }
    }

    static string BuildDisplayName(string file, string defaultPath, bool exists)
    {
        // 无匹配文件时返回的默认条目（文件可能不存在），显示为 default
        if (!exists && file == defaultPath) { return "default"; }
        return Path.GetFileNameWithoutExtension(file);
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
                if (index < 0 || index >= entries.Count) { return -1; }
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
            case "Refresh":
                if (isPreviewing) { return 0; }   // 预览期忽略刷新，避免重走流程
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

    /// <summary>以指定配置进入 MainScene（退出时返回菜单）。</summary>
    public void LaunchConfig(int index)
    {
        if (index < 0 || index >= entries.Count) { return; }
        LaunchContext.SelectedConfigPath = entries[index].Path;
        LaunchContext.ReturnToMenuOnExit = true;
        LaunchContext.HasSelected = true;
        if (loadingOverlay != null)
        {
            // 先显示加载遮罩，等一帧渲染后再切场景（同步 LoadScene 会阻塞主线程，画面停在遮罩帧）
            loadingOverlay.SetActive(true);
            StartCoroutine(LoadMainSceneNextFrame());
        }
        else
        {
            SceneManager.LoadScene("MainScene");
        }
    }

    IEnumerator LoadMainSceneNextFrame()
    {
        yield return null;   // 让遮罩至少渲染一帧
        SceneManager.LoadScene("MainScene");
    }

    /// <summary>打开某个配置的只读预览。</summary>
    public void OpenPreview(int index)
    {
        if (index < 0 || index >= entries.Count) { return; }
        Preview.Open(entries[index].Path);
        ShowPreview();
        OnPreviewOpened?.Invoke();
        OnPreviewRefreshed?.Invoke();
    }

    public void ClosePreview()
    {
        ShowList();
        OnPreviewClosed?.Invoke();
    }

    void ShowList()
    {
        if (configListRoot != null) { configListRoot.SetActive(true); }
        if (previewRoot != null) { previewRoot.SetActive(false); }
        EnableListInteraction();
    }

    void ShowPreview()
    {
        // 预览期间列表保持可见（由 previewRoot 叠加在上层），仅禁用其交互
        if (configListRoot != null) { configListRoot.SetActive(true); }
        if (previewRoot != null) { previewRoot.SetActive(true); }
        DisableListInteraction();
    }

    /// <summary>预览期间禁用配置列表交互：滑动窗口不可拖动 + 横条（含子文本）不响应点击。</summary>
    void DisableListInteraction()
    {
        ScrollRect sr = ResolveScrollRect();
        if (sr != null) { sr.enabled = false; }
        if (barContainer != null)
        {
            foreach (ConfigBarView bar in barContainer.GetComponentsInChildren<ConfigBarView>(true))
            {
                bar.SetInteractable(false);
            }
        }
        isPreviewing = true;
        if (refreshButton != null) { refreshButton.interactable = false; }
    }

    /// <summary>返回列表时恢复交互。</summary>
    void EnableListInteraction()
    {
        ScrollRect sr = ResolveScrollRect();
        if (sr != null) { sr.enabled = true; }
        if (barContainer != null)
        {
            foreach (ConfigBarView bar in barContainer.GetComponentsInChildren<ConfigBarView>(true))
            {
                bar.SetInteractable(true);
            }
        }
        isPreviewing = false;
        if (refreshButton != null) { refreshButton.interactable = true; }
    }

    ScrollRect ResolveScrollRect()
    {
        if (configScrollRect != null) { return configScrollRect; }
        if (barContainer != null) { return barContainer.GetComponentInParent<ScrollRect>(); }
        return null;
    }

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
    public void OnPreviewArrowLeft()  { MenuControlsParse("PreviewArrowLeft"); }
    public void OnPreviewArrowRight() { MenuControlsParse("PreviewArrowRight"); }
    public void OnPreviewSortToggle() { MenuControlsParse("PreviewSortToggle"); }
    public void OnPreviewBack()       { MenuControlsParse("PreviewBack"); }
    public void OnOpenCurrentConfig() { MenuControlsParse("OpenCurrentConfig"); }
    public void OnRefresh()           { MenuControlsParse("Refresh"); }
    public void OnQuitApp()           { MenuControlsParse("QuitApp"); }
}
