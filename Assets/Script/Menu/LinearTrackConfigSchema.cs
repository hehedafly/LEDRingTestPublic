using System.Collections.Generic;

/// <summary>
/// LinearTrack 运行配置的程序读取键清单（section, key, 默认值），
/// 从 Context_generate.cs / LinearTrackMoving.cs 的 ReadIniContent 调用提取。
/// 用途：菜单预览第二页展示“程序默认未写入的键”；context section 按 trail_content:context_for_repeat 动态展开。
///
/// 维护说明：Context_generate / LinearTrackMoving 新增或修改 ReadIniContent（含默认值变化）时需同步更新本清单。
/// </summary>
public static class LinearTrackConfigSchema
{
    /// <summary>固定 section 的键清单（不含动态 context section）。</summary>
    static readonly List<SchemaEntry> Fixed = new List<SchemaEntry>
    {
        // ===== trail_content =====
        new SchemaEntry("trail_content", "context_for_repeat", "context0"),
        new SchemaEntry("trail_content", "repeat_num", "5"),
        new SchemaEntry("trail_content", "start_mode", "0"),

        // ===== serialSettings =====
        new SchemaEntry("serialSettings", "blackList", ""),
        new SchemaEntry("serialSettings", "recommendPort", ""),
        new SchemaEntry("serialSettings", "compatibleVersion", ""),
        new SchemaEntry("serialSettings", "serialSpeed", "115200"),
    };

    /// <summary>单个 context section 的模板键（默认值取自 Context_generate.cs）。</summary>
    static readonly SchemaEntry[] ContextTemplate = new SchemaEntry[]
    {
        new SchemaEntry("", "start", "0"),
        new SchemaEntry("", "length", "10"),
        new SchemaEntry("", "wall_paper", "point_sparse"),
        new SchemaEntry("", "reward_zone_paper", "bar"),
        new SchemaEntry("", "reward_zone_length", "3"),
        new SchemaEntry("", "reward_zone_start", "1"),
        new SchemaEntry("", "landmark_name", "none"),
        new SchemaEntry("", "landmark_start", "-1"),
        new SchemaEntry("", "lick_count_max", "8"),
        new SchemaEntry("", "lick_threshold", "4,8"),
        new SchemaEntry("", "end_context", "grey,black"),
        new SchemaEntry("", "end_arg", "3"),
    };

    /// <summary>
    /// 针对具体配置文件构建清单：固定键 + 按 context_for_repeat 展开的 context section 键。
    /// context 名称归一化规则与 Context_generate.Awake 保持一致（只认包含 "context" 的项）。
    /// </summary>
    public static List<SchemaEntry> BuildForFile(IniFileParser parser)
    {
        List<SchemaEntry> result = new List<SchemaEntry>(Fixed);

        string names = "context0";
        if (parser != null)
        {
            parser.TryGetValue("trail_content", "context_for_repeat", out string v);
            if (!string.IsNullOrEmpty(v)) { names = v; }
        }

        HashSet<string> added = new HashSet<string>();
        foreach (string raw in names.Split(','))
        {
            string name = raw.Trim();
            int idx = name.IndexOf("context");
            if (idx < 0) { continue; }
            string section = "context" + name.Substring(idx + "context".Length);
            if (!added.Add(section)) { continue; }
            foreach (SchemaEntry tpl in ContextTemplate)
            {
                result.Add(new SchemaEntry(section, tpl.Key, tpl.DefaultValue));
            }
        }

        return result;
    }
}
