using System.Collections.Generic;

/// <summary>
/// 程序会读取的配置键清单（section, key, 默认值），从 Moving.cs 全部 ReadIniContent(section,key,default) 调用提取。
/// 用途：配置预览第二页展示“程序默认未写入的键”——即本清单中、目标配置文件里没有写入的键。
///
/// 维护说明：当 Moving.cs 新增或修改 ReadIniContent 读取（含默认值变化）时，需同步更新本清单，
/// 否则预览第二页会与程序实际使用的默认值不一致。材质 section 为动态展开，见 MaterialTemplate。
/// </summary>
public class SchemaEntry
{
    public string Section;
    public string Key;
    public string DefaultValue;

    public SchemaEntry(string section, string key, string defaultValue)
    {
        Section = section;
        Key = key;
        DefaultValue = defaultValue;
    }
}

public static class ConfigSchema
{
    /// <summary>固定 section 的键清单（不含动态材质 section）。</summary>
    public static readonly List<SchemaEntry> Fixed = new List<SchemaEntry>
    {
        // ===== displaySettings =====
        new SchemaEntry("displaySettings", "barWidth", "100"),
        new SchemaEntry("displaySettings", "barHeight", "1080"),
        new SchemaEntry("displaySettings", "disableMainDisplay", "false"),
        new SchemaEntry("displaySettings", "disableMonitorDisplay", "false"),
        new SchemaEntry("displaySettings", "displayPixelsLength", "1920"),
        new SchemaEntry("displaySettings", "displayPixelsHeight", "1080"),
        new SchemaEntry("displaySettings", "displayVerticalPos", "0.5"),
        new SchemaEntry("displaySettings", "isRing", "false"),
        new SchemaEntry("displaySettings", "separate", "false"),
        new SchemaEntry("displaySettings", "mainUICameraDisplay", "-1"),
        new SchemaEntry("displaySettings", "mainCameraDisplay", "-1"),
        new SchemaEntry("displaySettings", "secondCameraDisplay", "-1"),
        new SchemaEntry("displaySettings", "cameraMonitorDisplay", "-1"),

        // ===== settings =====
        new SchemaEntry("settings", "refSegement", "-1"),
        new SchemaEntry("settings", "start_mode", "0x00"),
        new SchemaEntry("settings", "start_method", "assign"),
        new SchemaEntry("settings", "available_pos", "0, 90, 180, 270"),
        new SchemaEntry("settings", "assign_pos", "(0+1+2+3)*100.."),
        new SchemaEntry("settings", "MatStartMethod", "assign"),
        new SchemaEntry("settings", "MatAvailable", "default"),
        new SchemaEntry("settings", "MatAssign", "default.."),
        new SchemaEntry("settings", "pump_pos", "0,1,2,3"),
        new SchemaEntry("settings", "lick_pos", "0,1,2,3"),
        new SchemaEntry("settings", "TrackPosMark", ""),
        new SchemaEntry("settings", "max_trial", "10000"),
        new SchemaEntry("settings", "backgroundLight", "0"),
        new SchemaEntry("settings", "backgroundLightRed", "-1"),
        new SchemaEntry("settings", "barDelayTime", "1"),
        new SchemaEntry("settings", "waitFromStart", "random2~5"),
        new SchemaEntry("settings", "barLastingTime", "1"),
        new SchemaEntry("settings", "waitFromLastLick", "3"),
        new SchemaEntry("settings", "triggerModeDelay", "0"),
        new SchemaEntry("settings", "trialInterval", "random5~10"),
        new SchemaEntry("settings", "success_wait_sec", "3"),
        new SchemaEntry("settings", "fail_wait_sec", "6"),
        new SchemaEntry("settings", "barShiftLs", "0"),
        new SchemaEntry("settings", "trialExpireTime", "9999"),
        new SchemaEntry("settings", "triggerMode", "0"),
        new SchemaEntry("settings", "seed", "-1"),
        new SchemaEntry("settings", "barOffset", "0"),
        new SchemaEntry("settings", "destAreaFollow", "true"),
        new SchemaEntry("settings", "standingSecInTrigger", "0.5"),
        new SchemaEntry("settings", "standingSecInTrialInDest", "0.5"),
        new SchemaEntry("settings", "OGTriggerMethod", ""),
        new SchemaEntry("settings", "MSTriggerMethod", ""),
        new SchemaEntry("settings", "countAfterLeave", "true"),
        new SchemaEntry("settings", "extraRewardTimeInSec", "0"),
        new SchemaEntry("settings", "stopExtraRewardMethod", ""),
        new SchemaEntry("settings", "stopExtraRewardUseTriggerSelectArea", "-1"),
        new SchemaEntry("settings", "stopExtraRewardLickDelaySec", "0"),
        new SchemaEntry("settings", "minIgnoreLickInterval", "0"),
        new SchemaEntry("settings", "maxExtraRewardCount", "9999"),
        new SchemaEntry("settings", "ServeRandomRewardAtEnd", "0"),
        new SchemaEntry("settings", "MSRecordDifferentiate", "false"),
        new SchemaEntry("settings", "OGtriggerRandomControl", "false"),
        new SchemaEntry("settings", "OGtriggerCompensation", "false"),
        new SchemaEntry("settings", "openLogEvent", "false"),
        new SchemaEntry("settings", "logEventPath", ""),
        new SchemaEntry("settings", "openPythonScript", "false"),
        new SchemaEntry("settings", "PythonScriptCommand", ""),
        new SchemaEntry("settings", "closePythonScriptBeforeExit", "false"),
        new SchemaEntry("settings", "strictIPCStatusUpdate", "false"),
        new SchemaEntry("settings", "IPCHeartbeat", "false"),
        new SchemaEntry("settings", "MouseNameRequired", "true"),
        new SchemaEntry("settings", "MouseName", ""),
        new SchemaEntry("settings", "checkConfigContent", "false"),

        // ===== soundSettings =====
        new SchemaEntry("soundSettings", "soundLength", "0.2"),
        new SchemaEntry("soundSettings", "cueVolume", "0.5"),
        new SchemaEntry("soundSettings", "TrialSoundPlayMode", ""),
        new SchemaEntry("soundSettings", "alarmPlayTimeInterval", "1.5"),

        // ===== defaultOptionSettings =====
        new SchemaEntry("defaultOptionSettings", "InputfieldContent", ""),

        // ===== pushSetting =====
        new SchemaEntry("pushSetting", "pushTargets", ""),

        // ===== matSettings =====
        new SchemaEntry("matSettings", "matList", "default,barMat,centerShaftMat,backgroundMat"),
        new SchemaEntry("matSettings", "centerShaft", "false"),

        // ===== centerShaft =====
        new SchemaEntry("centerShaft", "centerShaftPos", "0"),

        // ===== serialSettings =====
        new SchemaEntry("serialSettings", "blackList", ""),
        new SchemaEntry("serialSettings", "recommendPort", ""),
        new SchemaEntry("serialSettings", "compatibleVersion", ""),
        new SchemaEntry("serialSettings", "serialSpeed", "115200"),

        // ===== logSettings =====
        new SchemaEntry("logSettings", "logPath", ""),
    };

    /// <summary>材质 section 的模板键（默认值取自 Moving.cs 材质读取分支）。</summary>
    public static readonly SchemaEntry[] MaterialTemplate = new SchemaEntry[]
    {
        new SchemaEntry("", "isDriftGrating", "default"),
        new SchemaEntry("", "isCircleBar", "false"),
        new SchemaEntry("", "width", "400"),
        new SchemaEntry("", "speed", "1"),
        new SchemaEntry("", "frequency", "5"),
        new SchemaEntry("", "direction", "right"),
        new SchemaEntry("", "horizontal", "0"),
        new SchemaEntry("", "mat", "#000000"),
    };

    /// <summary>
    /// 针对某个具体配置文件构建完整清单：固定键 + 按该文件 [matSettings] matList 展开的材质 section 键。
    /// </summary>
    public static List<SchemaEntry> BuildForFile(IniFileParser parser)
    {
        List<SchemaEntry> result = new List<SchemaEntry>(Fixed);

        string matListDefault = "default,barMat,centerShaftMat,backgroundMat";
        string matList = matListDefault;
        if (parser != null)
        {
            parser.TryGetValue("matSettings", "matList", out string v);
            if (!string.IsNullOrEmpty(v)) { matList = v; }
        }

        foreach (string rawName in matList.Split(','))
        {
            string matName = rawName.Trim();
            if (matName.Length == 0) { continue; }
            foreach (SchemaEntry tpl in MaterialTemplate)
            {
                result.Add(new SchemaEntry(matName, tpl.Key, tpl.DefaultValue));
            }
        }

        return result;
    }
}
