using System.Collections.Generic;
using System.IO;
using System.Text;

/// <summary>
/// 保序 INI 文本解析器。
/// 与 Win32 profile API 不同：保留 section/key 的原始出现顺序，并区分注释行，
/// 供菜单横条取值与配置预览（“原始文本顺序”展示）使用。
/// </summary>
public class IniKey
{
    public string Key;
    public string Value;
    public bool IsCommented;
    /// <summary>在整个文件中的出现顺序（行序）。</summary>
    public int Order;
}

public class IniSection
{
    public string Name;
    public List<IniKey> Keys = new List<IniKey>();
}

public class IniFileParser
{
    /// <summary>按出现顺序保存的所有 section（含重复名会各自独立，一般不重复）。</summary>
    public List<IniSection> Sections = new List<IniSection>();

    /// <summary>文件是否存在（解析的来源文件）。</summary>
    public bool FileExists { get; private set; }

    public IniFileParser() { }

    /// <summary>从文件解析；文件不存在时保持为空（FileExists=false）。</summary>
    public static IniFileParser FromFile(string path)
    {
        IniFileParser parser = new IniFileParser();
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                parser.FileExists = true;
                parser.Parse(File.ReadAllText(path, Encoding.UTF8));
            }
        }
        catch (System.Exception)
        {
            // 读取失败时视为空文件
        }
        return parser;
    }

    /// <summary>解析 INI 文本内容。</summary>
    public void Parse(string text)
    {
        Sections.Clear();
        if (string.IsNullOrEmpty(text)) { return; }

        IniSection current = null;
        int order = 0;
        string[] lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0) { continue; }

            bool commented = line[0] == '#' || line[0] == ';';
            string body = commented ? line.TrimStart('#', ';').Trim() : line;

            if (body.Length > 0 && body[0] == '[' && body.Contains("]"))
            {
                string name = body.Substring(1, body.IndexOf(']') - 1).Trim();
                current = new IniSection { Name = name };
                Sections.Add(current);
                continue;
            }

            int eq = body.IndexOf('=');
            if (eq <= 0) { continue; } // 无 = 或 = 在行首，忽略

            string key = body.Substring(0, eq).Trim();
            string value = body.Substring(eq + 1).Trim();
            if (key.Length == 0) { continue; }

            if (current == null)
            {
                // 出现在任何 section 之前的键，归入匿名 section
                current = new IniSection { Name = "" };
                Sections.Add(current);
            }

            current.Keys.Add(new IniKey { Key = key, Value = value, IsCommented = commented, Order = order++ });
        }
    }

    /// <summary>查找指定 section（大小写敏感，与 Win32 近似）。</summary>
    IniSection FindSection(string section)
    {
        foreach (IniSection s in Sections)
        {
            if (s.Name == section) { return s; }
        }
        return null;
    }

    /// <summary>
    /// 是否存在“已写入”的键：仅统计非注释行。
    /// </summary>
    public bool Exists(string section, string key)
    {
        IniSection s = FindSection(section);
        if (s == null) { return false; }
        foreach (IniKey k in s.Keys)
        {
            if (!k.IsCommented && k.Key == key) { return true; }
        }
        return false;
    }

    /// <summary>
    /// 取指定 section/key 的值：忽略注释行，取首个匹配（与 Win32 GetPrivateProfileString 行为一致）。
    /// </summary>
    public bool TryGetValue(string section, string key, out string value)
    {
        value = "";
        IniSection s = FindSection(section);
        if (s == null) { return false; }
        foreach (IniKey k in s.Keys)
        {
            if (!k.IsCommented && k.Key == key) { value = k.Value; return true; }
        }
        return false;
    }
}
