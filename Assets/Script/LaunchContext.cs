using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// 跨场景存活的静态启动上下文。
/// 负责：确定 config 文件夹位置、枚举 config*.ini、把菜单所选配置与退出策略传递给 MainScene 的 Moving。
/// 静态字段在同一 play 会话内跨场景加载存活。
/// </summary>
public static class LaunchContext
{
    /// <summary>菜单选中的配置文件完整路径。</summary>
    public static string SelectedConfigPath;

    /// <summary>退出 MainScene 时是否返回菜单（false 表示直接退出应用）。</summary>
    public static bool ReturnToMenuOnExit = false;

    /// <summary>是否已经确定了一个配置（区分“未选择”与“显式选择默认”）。</summary>
    public static bool HasSelected = false;

    /// <summary>
    /// config 文件夹：编辑器 Assets/Resources/，构建 Application.dataPath+"/Resources/"。
    /// 逻辑对齐 Moving.Awake 中对 config.ini 路径的处理。
    /// </summary>
    public static string ConfigFolder()
    {
        #if UNITY_EDITOR
            return "Assets/Resources/";
        #else
            return Application.dataPath + "/Resources/";
        #endif
    }

    /// <summary>默认 config.ini 路径。</summary>
    public static string DefaultConfigPath()
    {
        return ConfigFolder() + "config.ini";
    }

    /// <summary>metaConfig.ini 路径（用于 skip 判定）。</summary>
    public static string MetaConfigPath()
    {
        return ConfigFolder() + "metaConfig.ini";
    }

    /// <summary>
    /// 扫描 config 文件夹下以 config 开头、.ini 结尾的文件，按名排序。
    /// 若无匹配文件，返回仅含默认 config.ini 路径的数组（对应 default 条目）。
    /// </summary>
    public static string[] FindConfigFiles()
    {
        string folder = ConfigFolder();
        try
        {
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder, "config*.ini")
                    .Where(f => f.EndsWith(".ini"))
                    .OrderBy(f => f)
                    .ToArray();
                if (files.Length > 0) { return files; }
            }
        }
        catch (System.Exception)
        {
            // 扫描失败时回退到默认条目
        }
        return new string[] { DefaultConfigPath() };
    }

    /// <summary>Moving 使用的实际配置路径：已选择则用所选，否则用默认。</summary>
    public static string ResolveConfigPath()
    {
        return (HasSelected && !string.IsNullOrEmpty(SelectedConfigPath)) ? SelectedConfigPath : DefaultConfigPath();
    }
}
