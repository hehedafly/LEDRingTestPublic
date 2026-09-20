using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 配置只读预览控制器（不涉及任何写入/修改）。
/// 两类内容：
///   「已设值」：目标配置文件中已写入且值非空的键；
///   「默认未写入」：程序会读取（ConfigSchema）、但该文件中未写入的键，展示其默认值。
/// 分页模型：两类内容各自按 LinesPerPage 拆成若干子页，拼成扁平序列
///   [已设值子页 x N] + [默认子页 x M]，左右翻页在该序列上循环（末页 -> 首页）。
/// 支持“原始文本顺序 / 字母顺序”排序切换，注释行不展示。
/// UI 由外部搭建，本类只提供数据与翻页/排序状态。
/// </summary>
public class ConfigPreviewController
{
    public string CurrentPath { get; private set; }

    /// <summary>true=字母顺序，false=原始文本顺序。</summary>
    public bool AlphabeticalSort { get; private set; } = false;

    /// <summary>每页行数，由 View 依据 pageText 文本框高度写入；&lt;=0 视为不分页。</summary>
    public int LinesPerPage { get; set; } = 20;

    /// <summary>每行最大字符数，由 View 依据 pageText 文本框宽度写入；&lt;=0 表示不截断。</summary>
    public int MaxCharsPerLine { get; set; } = 0;

    /// <summary>超长行截断后追加的省略号。</summary>
    public string Ellipsis { get; set; } = "…";

    /// <summary>当前扁平子页索引（0 基，范围 [0,TotalSubPages)）。</summary>
    public int SubPageIndex { get; private set; } = 0;

    IniFileParser parser;
    List<SchemaEntry> schema;

    // 解析后的两类原始行数据
    readonly List<PreviewLine> setLines = new List<PreviewLine>();
    readonly List<PreviewLine> defaultLines = new List<PreviewLine>();

    // 依当前排序生成的显示字符串（含 [section] 头），随数据/排序变化重建
    List<string> setDisplay = new List<string>();
    List<string> defaultDisplay = new List<string>();

    public class PreviewLine
    {
        public string Section;
        public string Key;
        public string Value;
        public int Order; // 原始文本顺序（默认页用 schema 顺序）
    }

    /// <summary>打开某个配置文件进行预览，重置到首个子页。</summary>
    public void Open(string path)
    {
        CurrentPath = path;
        parser = IniFileParser.FromFile(path);
        schema = ConfigSchema.BuildForFile(parser);
        SubPageIndex = 0;
        Rebuild();
    }

    void Rebuild()
    {
        setLines.Clear();
        defaultLines.Clear();

        // 已设值：文件中已写入且值非空的键（忽略注释行）
        if (parser != null)
        {
            foreach (IniSection s in parser.Sections)
            {
                foreach (IniKey k in s.Keys)
                {
                    if (k.IsCommented) { continue; }
                    if (string.IsNullOrEmpty(k.Value)) { continue; }
                    setLines.Add(new PreviewLine { Section = s.Name, Key = k.Key, Value = k.Value, Order = k.Order });
                }
            }
        }

        // 默认未写入：程序默认读取、但文件未写入的键
        if (schema != null)
        {
            int idx = 0;
            foreach (SchemaEntry e in schema)
            {
                bool written = parser != null && parser.Exists(e.Section, e.Key);
                if (!written)
                {
                    defaultLines.Add(new PreviewLine { Section = e.Section, Key = e.Key, Value = e.DefaultValue, Order = idx });
                }
                idx++;
            }
        }

        RebuildDisplay();
    }

    /// <summary>按当前排序重建两类显示字符串，并把子页索引钳制到有效范围。</summary>
    void RebuildDisplay()
    {
        setDisplay = BuildDisplay(Order(setLines, false));
        defaultDisplay = BuildDisplay(Order(defaultLines, true));
        SubPageIndex = Mathf.Clamp(SubPageIndex, 0, TotalSubPages - 1);
    }

    List<PreviewLine> Order(List<PreviewLine> source, bool isDefault)
    {
        IEnumerable<PreviewLine> ordered;
        if (AlphabeticalSort)
        {
            ordered = source.OrderBy(l => l.Section).ThenBy(l => l.Key);
        }
        else
        {
            // 原始顺序：已设值按文件出现顺序；默认按 schema 顺序，同 section 聚合
            ordered = isDefault
                ? source.OrderBy(l => l.Section).ThenBy(l => l.Order)
                : source.OrderBy(l => l.Order);
        }
        return ordered.ToList();
    }

    /// <summary>把有序行转为带 [section] 头的显示字符串；section 之间以一个空行（两个换行）隔开。</summary>
    List<string> BuildDisplay(List<PreviewLine> lines)
    {
        List<string> result = new List<string>();
        string lastSection = null;
        foreach (PreviewLine l in lines)
        {
            if (l.Section != lastSection)
            {
                // 非首个 section 前插入一个空行，join("\n") 后即两个换行
                if (lastSection != null) { result.Add(""); }
                result.Add($"[{l.Section}]");
                lastSection = l.Section;
            }
            result.Add($"{l.Key} = {l.Value}");
        }
        return result;
    }

    /// <summary>按 MaxCharsPerLine 截断单行，超出部分去掉并以省略号替代。</summary>
    string Truncate(string s)
    {
        if (MaxCharsPerLine <= 0 || string.IsNullOrEmpty(s) || s.Length <= MaxCharsPerLine) { return s; }
        int ellLen = Ellipsis == null ? 0 : Ellipsis.Length;
        int keep = Mathf.Max(0, MaxCharsPerLine - ellLen);
        if (keep >= s.Length) { return s; }
        return s.Substring(0, keep) + Ellipsis;
    }

    int SubCount(int lineCount)
    {
        if (LinesPerPage > 0) { return Mathf.CeilToInt(lineCount / (float)LinesPerPage); }
        return lineCount > 0 ? 1 : 0;
    }

    public int SetSubPageCount => SubCount(setDisplay.Count);
    public int DefaultSubPageCount => SubCount(defaultDisplay.Count);
    public int TotalSubPages => Mathf.Max(1, SetSubPageCount + DefaultSubPageCount);

    /// <summary>当前子页是否属于「默认未写入」类别。</summary>
    public bool IsDefaultPage => SubPageIndex >= SetSubPageCount;
    /// <summary>当前类别的子页总数。</summary>
    public int SubCountInCategory => IsDefaultPage ? DefaultSubPageCount : SetSubPageCount;
    /// <summary>当前类别内的子页序号（1 基）。</summary>
    public int SubIndexInCategory => (IsDefaultPage ? SubPageIndex - SetSubPageCount : SubPageIndex) + 1;

    public void NextPage()
    {
        SubPageIndex = (SubPageIndex + 1) % TotalSubPages;
    }

    public void PrevPage()
    {
        SubPageIndex = (SubPageIndex - 1 + TotalSubPages) % TotalSubPages;
    }

    public void ToggleSort()
    {
        AlphabeticalSort = !AlphabeticalSort;
        RebuildDisplay();
    }

    /// <summary>当前子页要显示的文本行（分页模式，供 pageText 使用）；每行按 MaxCharsPerLine 截断。</summary>
    public List<string> GetCurrentPageLines()
    {
        List<string> source;
        int local;
        if (IsDefaultPage) { source = defaultDisplay; local = SubPageIndex - SetSubPageCount; }
        else { source = setDisplay; local = SubPageIndex; }

        List<string> slice;
        if (LinesPerPage <= 0)
        {
            slice = new List<string>(source);
        }
        else
        {
            int start = local * LinesPerPage;
            if (start >= source.Count) { slice = new List<string>(); }
            else
            {
                int end = Mathf.Min(start + LinesPerPage, source.Count);
                slice = source.GetRange(start, end - start);
            }
        }

        for (int i = 0; i < slice.Count; i++) { slice[i] = Truncate(slice[i]); }
        return slice;
    }

    /// <summary>全部内容行（已设值 + 默认未写入，不分页，供滚动列表模式使用）。</summary>
    public List<string> GetAllLines()
    {
        List<string> all = new List<string>(setDisplay);
        all.AddRange(defaultDisplay);
        return all;
    }
}
