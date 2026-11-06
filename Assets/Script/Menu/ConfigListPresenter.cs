using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单「配置列表」表现层：负责扫描 config*.ini 生成条目、横条实例化/重绑/清理、
/// 列表与预览根节点的显隐、以及列表交互的统一开关。由 StartMenuController 创建并驱动；
/// 不含场景切换、skip 判定、文件打开等控制器职责。
/// </summary>
public class ConfigListPresenter
{
    // 关联的菜单控制器（仅用于把横条点击派发回控制器）
    readonly StartMenuController owner;
    // UI 引用
    readonly GameObject listRoot;
    readonly GameObject previewRoot;
    readonly Transform barContainer;
    readonly GameObject barPrefab;
    readonly ScrollRect scrollRect;
    readonly Button refreshButton;
    // 横条需展示的键（section:key），随菜单模式切换
    List<string> barDisplayKeys;

    // 解析出的配置条目，以及自动创建的横条（供刷新时清理）
    readonly List<ConfigEntry> entries = new List<ConfigEntry>();
    readonly List<GameObject> createdBars = new List<GameObject>();
    bool isPreviewing = false;

    /// <summary>当前配置条目。</summary>
    public IReadOnlyList<ConfigEntry> Entries => entries;
    /// <summary>条目数量。</summary>
    public int Count => entries.Count;
    /// <summary>是否处于预览状态（预览期间应忽略刷新）。</summary>
    public bool IsPreviewing => isPreviewing;

    /// <summary>构造并保存控制器与 UI 引用。</summary>
    public ConfigListPresenter(StartMenuController owner, GameObject listRoot, GameObject previewRoot,
        Transform barContainer, GameObject barPrefab, ScrollRect scrollRect, Button refreshButton,
        List<string> barDisplayKeys)
    {
        this.owner = owner;
        this.listRoot = listRoot;
        this.previewRoot = previewRoot;
        this.barContainer = barContainer;
        this.barPrefab = barPrefab;
        this.scrollRect = scrollRect;
        this.refreshButton = refreshButton;
        this.barDisplayKeys = barDisplayKeys;
    }

    /// <summary>切换菜单模式时替换横条展示键（在下一次 Build 前调用）。</summary>
    public void SetBarDisplayKeys(List<string> keys)
    {
        barDisplayKeys = keys ?? new List<string>();
    }

    /// <summary>扫描配置目录、重建条目与横条（不判 skip，供初始加载与 Refresh 共用）。</summary>
    public void Build()
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
            FillBarValues(parser, entry);
            entries.Add(entry);
        }

        SpawnOrRebindBars();
    }

    /// <summary>读取 barDisplayKeys 指定的 section:key 值填入条目（缺失记为空串）。</summary>
    void FillBarValues(IniFileParser parser, ConfigEntry entry)
    {
        foreach (string sk in barDisplayKeys)
        {
            if (string.IsNullOrEmpty(sk) || !sk.Contains(":")) { continue; }
            int split = sk.IndexOf(':');
            string section = sk.Substring(0, split).Trim();
            string key = sk.Substring(split + 1).Trim();
            parser.TryGetValue(section, key, out string value);
            entry.BarValues[sk] = value ?? "";
        }
    }

    /// <summary>显示列表并恢复列表交互。</summary>
    public void ShowList()
    {
        if (listRoot != null) { listRoot.SetActive(true); }
        if (previewRoot != null) { previewRoot.SetActive(false); }
        SetInteraction(true);
    }

    /// <summary>显示预览（列表保持可见但禁止交互，供预览层叠加其上）。</summary>
    public void ShowPreview()
    {
        if (listRoot != null) { listRoot.SetActive(true); }
        if (previewRoot != null) { previewRoot.SetActive(true); }
        SetInteraction(false);
    }

    /// <summary>统一开关列表交互：滑动窗口 + 横条（含子文本）+ 刷新按钮。</summary>
    public void SetInteraction(bool enable)
    {
        ScrollRect sr = ResolveScrollRect();
        if (sr != null) { sr.enabled = enable; }
        if (barContainer != null)
        {
            foreach (ConfigBarView bar in barContainer.GetComponentsInChildren<ConfigBarView>(true))
            {
                bar.SetInteractable(enable);
            }
        }
        isPreviewing = !enable;
        if (refreshButton != null) { refreshButton.interactable = enable; }
    }

    /// <summary>销毁本表现层自动创建的横条。</summary>
    void ClearBars()
    {
        foreach (GameObject go in createdBars) { if (go != null) { Object.Destroy(go); } }
        createdBars.Clear();
    }

    /// <summary>自动模式实例化横条并绑定；手动模式则按索引重绑场景中已有横条。</summary>
    void SpawnOrRebindBars()
    {
        if (barPrefab != null && barContainer != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                GameObject go = Object.Instantiate(barPrefab, barContainer);
                createdBars.Add(go);
                ConfigBarView view = go.GetComponent<ConfigBarView>();
                if (view != null) { view.Bind(i, owner, entries[i]); }
            }
        }
        else
        {
            RebindExistingBars();
        }
    }

    /// <summary>手动模式：把场景中已有的 ConfigBarView 按索引重绑到最新条目（多出的隐藏）。</summary>
    void RebindExistingBars()
    {
        if (barContainer == null) { return; }
        ConfigBarView[] views = barContainer.GetComponentsInChildren<ConfigBarView>(true);
        for (int i = 0; i < views.Length; i++)
        {
            if (i < entries.Count) { views[i].gameObject.SetActive(true); views[i].Bind(i, owner, entries[i]); }
            else { views[i].gameObject.SetActive(false); }
        }
    }

    /// <summary>由文件路径生成显示名；无匹配文件时的默认条目显示为 default。</summary>
    static string BuildDisplayName(string file, string defaultPath, bool exists)
    {
        if (!exists && file == defaultPath) { return "default"; }
        return Path.GetFileNameWithoutExtension(file);
    }

    /// <summary>解析列表所在的 ScrollRect（优先显式引用，否则从 barContainer 向上查找）。</summary>
    ScrollRect ResolveScrollRect()
    {
        if (scrollRect != null) { return scrollRect; }
        if (barContainer != null) { return barContainer.GetComponentInParent<ScrollRect>(); }
        return null;
    }
}
