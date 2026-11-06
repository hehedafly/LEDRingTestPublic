using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>菜单模式：决定配置目录与目标运行场景。</summary>
public enum LaunchMode
{
    LedRing,
    LinearTrack
}

/// <summary>
/// 跨场景存活的静态启动上下文。
/// 负责：确定当前菜单模式、config 文件夹位置、枚举 config*.ini，
/// 以及把菜单所选配置、目标场景与退出策略传递给 Moving / LinearTrackMoving。
/// 静态字段在同一 play 会话内跨场景加载存活。
/// </summary>
public static class LaunchContext
{
    /// <summary>菜单场景名；用于从运行场景退出后返回菜单。</summary>
    public const string MenuSceneName = "StartMenu";

    /// <summary>当前菜单模式（从 LinearTrack 退出回菜单后保持，便于直接回到 VR 页）。</summary>
    public static LaunchMode Mode = LaunchMode.LedRing;

    /// <summary>StartMenu 是否已应用首次进入模式（metaConfig 的 prefer_context）；返回菜单时不再重读。</summary>
    public static bool MenuModeInitialized = false;

    /// <summary>菜单选中的配置文件完整路径。</summary>
    public static string SelectedConfigPath;

    /// <summary>退出运行场景时是否返回菜单（false 表示直接退出应用）。</summary>
    public static bool ReturnToMenuOnExit = false;

    /// <summary>是否已经确定了一个配置（区分“未选择”与“显式选择默认”）。</summary>
    public static bool HasSelected = false;

    /// <summary>
    /// 指定模式的 config 文件夹：编辑器 Assets/Resources/（LinearTrack 为 linear_track 子目录），
    /// 构建 Application.dataPath+"/Resources/"（子目录结构相同）。
    /// </summary>
    public static string ConfigFolder(LaunchMode mode)
    {
        #if UNITY_EDITOR
            return mode == LaunchMode.LinearTrack ? "Assets/Resources/linear_track/" : "Assets/Resources/";
        #else
            string root = Application.dataPath + "/Resources/";
            return mode == LaunchMode.LinearTrack ? root + "linear_track/" : root;
        #endif
    }

    /// <summary>当前模式的 config 文件夹。</summary>
    public static string ConfigFolder()
    {
        return ConfigFolder(Mode);
    }

    /// <summary>指定模式的默认 config.ini 路径。</summary>
    public static string DefaultConfigPath(LaunchMode mode)
    {
        return ConfigFolder(mode) + "config.ini";
    }

    /// <summary>当前模式的默认 config.ini 路径。</summary>
    public static string DefaultConfigPath()
    {
        return DefaultConfigPath(Mode);
    }

    /// <summary>当前模式对应的运行场景名。</summary>
    public static string TargetSceneName()
    {
        return Mode == LaunchMode.LinearTrack ? "linear_track" : "MainScene";
    }

    /// <summary>metaConfig.ini 路径（仅 LEDRing 模式使用，用于 skip 判定）。</summary>
    public static string MetaConfigPath()
    {
        return ConfigFolder(LaunchMode.LedRing) + "metaConfig.ini";
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

    /// <summary>
    /// 指定模式的运行配置路径：已选择且模式匹配则用所选，否则回退到该模式默认 config.ini。
    /// 直接运行场景（未经菜单）时 HasSelected=false，仍能加载各自默认配置。
    /// </summary>
    public static string ResolveConfigPath(LaunchMode expectedMode)
    {
        return (HasSelected && Mode == expectedMode && !string.IsNullOrEmpty(SelectedConfigPath))
            ? SelectedConfigPath
            : DefaultConfigPath(expectedMode);
    }

    /// <summary>当前模式的运行配置路径。</summary>
    public static string ResolveConfigPath()
    {
        return ResolveConfigPath(Mode);
    }
}
