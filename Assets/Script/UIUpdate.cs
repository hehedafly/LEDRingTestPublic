using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class UIUpdate : MonoBehaviour
{
    public List<UnityEngine.UI.Slider> sliders;
    public InputField serialMessageInputs;
    public UnityEngine.UI.Button startButton;
    public Dropdown modeSelect;
    public Dropdown triggerModeSelect;
    // public Dropdown SoundrModeSelect;
    public GameObject soundOptions;
    public Dictionary<int, UnityEngine.UI.Button> soundOptionsDict;
    public Text TexContextHighFreqInfo;
    public Text TexContextFixInfo;
    public Text TexContextInfo;
    string logMessage = "";
    public Text PageNum;
    public Text logMessageShow;
    List<string> logMessageList = new List<string>();
    string LastAddedLogMessage = "";
    public Scrollbar logScrollBar;
    int logWindowDraging = 0;//-1: another page, 0: no draging, 1: draging
    int logPage = 0;
    public List<InputField> inputFields = new List<InputField>();
    public Dropdown backgroundSwitch;
    Dictionary<string, string> inputFieldContent = new Dictionary<string, string>();
    List<Dropdown> dropdowns = new List<Dropdown>();
    List<Dropdown> soundDropdowns = new List<Dropdown>();
    public List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();

    Moving moving;
    InputField focus_input_field;
    Alarm alarm;
    float manualWaitSec = 5;
    List<string> buttonTimingList = new List<string>();
    public Dropdown buttonTimingBaseDropdown;
    ScrDropDown buttonTimingBaseScrDropdown;
    /// <summary>
    /// 包含不同层级定时字典，字典键为alarm名称，值以“;”分割，包含一个或多个定时方式（按钮名称:定时方式:具体值）
    /// </summary> 
    List<Dictionary<string, string>> buttonTimingBasedOnPreviousTimingDicts = new List<Dictionary<string, string>>();
    /// <summary>
    /// like {"delete", "spread"} or...
    /// </summary>
    public List<string> buttonFunctionsLs = new List<string>{"delete", "spread"};
    bool[] buttonFunctionsEnableDefault = new bool[]{true, false};
    public GameObject pref_buttonTimingBaseSubDropdown;
    List<GameObject> createdButtonTimingBaseSubDropdowns = new List<GameObject>();
    public string IFContentLoaded = "";
    
    private int _FrameCount = 0; private float _TimeCount = 0; private float _FrameRate = 0;

    //public Image LineChartImage;
    public int AddSelf(UnityEngine.UI.Button button){
        if(buttons.Contains(button)){return 0;}

        buttons.Add(button);
        return 1;
    }

    public int SetButtonColor(string buttonName, Color color = default, bool setToDefault = false, bool setToPrevious = false, bool forcePreviousUpdate= false, bool ignoreTiming = false){
        UnityEngine.UI.Button button = buttons.Find(button => button.name == buttonName);
        if(button != null){
            // if(ignoreTiming || buttonTimingList.Contains(buttonName)){
                SetButtonColor(button, color, setToDefault, setToPrevious, forcePreviousUpdate, ignoreTiming);
            // }
            return 1;
        }
        return -1;
    }
    void SetButtonColor(UnityEngine.UI.Button _button, Color color = default, bool setToDefault = false, bool setToPrevious = false, bool forcePreviousUpdate = false, bool ignoreTiming = false){
        // if(_button == null || (!ignoreTiming && buttonTimingList.Contains(_button.name))){return;}
        if(_button == null){return;}
        _button.GetComponent<ScrButton>().ChangeColor(color, setToDefault, setToPrevious, forcePreviousUpdate);
        Debug.Log($"SetButtonColor {_button.name} to {color}");
    }

    public void SetButtonTiming(string _buttonNameWithInd){
        alarm.TrySetAlarm(_buttonNameWithInd, 1, out _, addInfo:"FromTiming", force:true);
    }

    public void ControlsParsePublic(string elementsName,float value, string stringArg="", bool ignoreTiming = true, bool forceTiming = false){//scrButton中调用时ignoreTiming为false
        ControlsParse(elementsName, value, stringArg, ignoreTiming, forceTiming);
    }
    
    void ClearComingButtonTiming(string buttonName, int hierarchy){
        string[] buttonsForSearch = new string[]{buttonName};
        List<string> buttonsForSearchNext = new List<string>{};

        for(int _hierarchy = hierarchy; _hierarchy <= buttonTimingBasedOnPreviousTimingDicts.Count; _hierarchy++){
            List<string> timingDicKeys = _hierarchy > 0? buttonTimingBasedOnPreviousTimingDicts[_hierarchy - 1].Keys.ToList(): (from option in buttonTimingBaseDropdown.options where option.text != "None" select option.text).ToList();
            foreach(string button in buttonsForSearch){
                foreach(string key in timingDicKeys){

                    string[] relatedButtons = _hierarchy > 0? buttonTimingBasedOnPreviousTimingDicts[_hierarchy - 1][key].Trim(';').Split(';'): new string[]{""};
                    buttonsForSearchNext = (buttonTimingBasedOnPreviousTimingDicts.Count > _hierarchy && buttonTimingBasedOnPreviousTimingDicts[_hierarchy].ContainsKey(button))? (from timing in buttonTimingBasedOnPreviousTimingDicts[_hierarchy][button].Trim(';').Split(";") select timing.Split(':')[0]).ToList(): new List<string>{};
                    string _tempRelatedButtonInValue = "";
                    for(int i = 0; i < relatedButtons.Length; i++){
                        string relatedButtonsInValue = relatedButtons[i].Split(':')[0];
                        if(relatedButtons[i].StartsWith(button)){
                            // buttonTimingBasedOnPreviousTimingDicts[_hierarchy - 1].Remove(key);
                            // buttonTimingBaseScrDropdown.higherHierarchyOptions[_hierarchy - 1].Remove(button);
                            Debug.Log($"deleted {button}");
                            if(_hierarchy >= hierarchy){
                                if(buttonTimingBaseScrDropdown.nowSelectedHierarchy == _hierarchy && buttonTimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy == button){
                                    buttonTimingBaseScrDropdown.nowSelectedHierarchy = 0;
                                    buttonTimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy = "None";
                                    buttonTimingBaseDropdown.value = 0;
                                    buttonTimingBaseScrDropdown.UpdateCaptionText();
                                }
                            }
                            // buttonTimingBaseScrDropdown.UpdateOptionsFunctionEnableStatus(_hierarchy, timingDicKeys.IndexOf(key));
                        }else if(relatedButtons[i].Length > 0){
                            _tempRelatedButtonInValue += relatedButtons[i] + ";";
                        }
                    }
                    if(_tempRelatedButtonInValue.Length > 0){
                        buttonTimingBasedOnPreviousTimingDicts[_hierarchy - 1][key] = _tempRelatedButtonInValue;
                    }else{
                        if(hierarchy > 0){
                            buttonTimingBasedOnPreviousTimingDicts[_hierarchy - 1].Remove(key);
                        }
                    }
                }
            }

            buttonsForSearch = buttonsForSearchNext.ToArray();
            buttonsForSearchNext.Clear();
        }
        
    }

    List<string> GetRelatedButtonTimingInPreviousTiming(int _hierarchy, string _button){
        if(buttonTimingBasedOnPreviousTimingDicts.Count < _hierarchy || _hierarchy < 0){return new List<string>();}
        if(!buttonTimingBasedOnPreviousTimingDicts[_hierarchy].ContainsKey(_button)){return new List<string>();}
        return (from timing in buttonTimingBasedOnPreviousTimingDicts[_hierarchy][_button].Trim(';').Split(";") select timing.Split(':')[0]).ToList();
    }
    
    /// <summary>
    /// for inputfield, stringArg is the method of timing, value is 0 if failed to parse to float
    /// for ButtonTiming, stringArg is the timing value(in seconds)
    /// </summary>
    /// <param name="elementsName"></param>
    /// <param name="value"></param>
    /// <param name="stringArg"></param>
    void ControlsParse(string elementsName, float value, string stringArg="", bool ignoreTiming = true, bool forceTiming = false, bool ignoreSecondTiming = false){

        if(! ignoreTiming){//目前仅支持button延时点击,按trial定时事件执行在DeviceTriggerExecute最后处理
            UnityEngine.UI.Button selectedButton = buttons.Find(button => button.name == elementsName);
            if(selectedButton != null){
                List<string> buttonTimingSetInSec = alarm.GetAlarmNames(true).Where(name => name.StartsWith($"buttonTiming{elementsName}")).ToList();
                buttonTimingSetInSec.Sort();
                int lastSetInSecInd = buttonTimingSetInSec.Count > 0? int.Parse(buttonTimingSetInSec.Last().Split(";")[1]): -1;
                List<string> buttonTimingSetInTrial = moving.ButtonTriggerDict.Keys.Where(name => name.StartsWith($"{elementsName}")).ToList();
                buttonTimingSetInTrial.Sort();
                int lastSetInTrialInd = buttonTimingSetInTrial.Count > 0? int.Parse(buttonTimingSetInTrial.Last().Split(";")[1]): -1;

                string targetButtonTimingNameInSec = $"buttonTiming{elementsName};{lastSetInSecInd+1}";
                string targetButtonTimingNameInTrial = $"buttonTiming{elementsName};{lastSetInTrialInd+1}";
                int isTiming = buttonTimingSetInSec.Count  + buttonTimingSetInTrial.Count;//也等于buttonTimingList.where(n => n==elementsName).Count
                
                if(stringArg != "cancelTiming" && Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) || forceTiming){
                    float _sec = -1;int _trial = -1;
                    if(stringArg.Contains(";")){
                        bool useSecInArg = stringArg.StartsWith("sec");
                        if(useSecInArg){_sec = float.Parse(stringArg.Split(";")[1]);}
                        else{_trial = int.Parse(stringArg.Split(";")[1]);}
                    }else{
                        if(!float.TryParse(inputFieldContent["IFTimingBySec"], out _sec)){_sec = -1;};
                        if(!int.TryParse(inputFieldContent["IFTimingByTrial"], out _trial)){_trial = -1;};
                    }
                    if(_sec < 0 && _trial < 0){
                        MessageUpdate("please input a valid number for button timing");
                        return;
                    }
                    
                    int _hierarchyTimingBaseInDropdown = buttonTimingBaseScrDropdown.nowSelectedHierarchy;
                    // string _timingBaseName = buttonTimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy;
                    string _timingBaseName = "None";
                    // if(!ignoreSecondTiming && (buttonTimingBaseDropdown.value!=0 || buttonTimingList.Contains(_timingBaseName))){
                    bool useSec = _sec > 0;
                    string _timingMethod = $"{elementsName}:{(useSec? "sec": "trial")}:{(useSec? _sec: _trial)};";
                    if(!ignoreSecondTiming && _timingBaseName != "None"){
                        while(buttonTimingBasedOnPreviousTimingDicts.Count <= _hierarchyTimingBaseInDropdown){
                            buttonTimingBasedOnPreviousTimingDicts.Add(new Dictionary<string, string>{});
                        }
                        if (buttonTimingBasedOnPreviousTimingDicts[_hierarchyTimingBaseInDropdown].ContainsKey(_timingBaseName)){
                            _timingMethod = buttonTimingBasedOnPreviousTimingDicts[_hierarchyTimingBaseInDropdown][_timingBaseName] + _timingMethod;
                            buttonTimingBasedOnPreviousTimingDicts[_hierarchyTimingBaseInDropdown].Remove(_timingBaseName);
                        }
                        buttonTimingBasedOnPreviousTimingDicts[_hierarchyTimingBaseInDropdown].Add(_timingBaseName, _timingMethod);
                        // buttonTimingBaseScrDropdown.UpdateOptionsFunctionEnableStatus(buttonTimingBasedOnPreviousTimingDicts[0]);
                        MessageUpdate($"button {elementsName} timing set to {(useSec? _sec: _trial)} {(useSec? "s ": "trial")} after button {_timingBaseName}");
                        return;
                    }
                    
                    if(_sec > 0){
                        alarm.TrySetAlarm(targetButtonTimingNameInSec, _sec, out _, addInfo:"FromTiming", force:true);
                        MessageUpdate($"button {elementsName} timing set to {_sec}s");
                        inputFields.Find(inputfield => inputfield.name == "IFTimingBySec").text = "";
                        inputFieldContent[$"IFTimingBySec"] = "null";
                        
                    }else if(_trial > 0){//默认按trial start，暂时不考虑finish和end
                        MessageUpdate($"button {elementsName} timing set to trial {Math.Max(0, moving.NowTrial) + _trial}");
                        moving.ButtonTriggerDict.Add($"{targetButtonTimingNameInTrial};", Math.Max(0, moving.NowTrial) + _trial);
                        inputFields.Find(inputfield => inputfield.name == "IFTimingByTrial").text = "";
                        inputFieldContent[$"IFTimingByTrial"] = "null";
                    }else{
                        return;
                    }
                    buttonTimingList.Add(elementsName);
                    // buttonTimingBaseDropdown.AddOptions(new List<string>{elementsName});
                    // buttonTimingBaseScrDropdown.UpdateOptions();
                    // buttonTimingBaseScrDropdown.UpdateOptionsFunctionEnableStatus(null);
                    // buttonTimingBaseScrDropdown.UpdateOptionsFunctionEnableStatus(hierarchy:0, index:buttonTimingList.Count, addOrRemove:true, buttonFunctions:buttonFunctionsEnableDefault);
                    SetButtonColor(elementsName, Color.yellow, forcePreviousUpdate:true, ignoreTiming:true);
                    return;
                }else{//直接点击而非定时
                    if(isTiming > 0){
                        // int buttonIndexInTimingList = buttonTimingList.IndexOf(elementsName);
                        if(buttonTimingSetInTrial.Count > 0){
                            MessageUpdate($"button {elementsName} timing at trial{moving.ButtonTriggerDict[$"{elementsName};{lastSetInTrialInd};"]} cancelled");
                            moving.ButtonTriggerDict.Remove($"buttonTiming{elementsName};{lastSetInTrialInd}");
                            // buttonTimingList.Remove(elementsName);
                            ClearComingButtonTiming(elementsName, 0);
                        }
                        else{
                            MessageUpdate($"button {elementsName} timing after {(int)(alarm.GetAlarm($"buttonTiming{elementsName};{lastSetInSecInd};")* Time.fixedUnscaledDeltaTime)} sec cancelled");
                            alarm.DeleteAlarm($"{elementsName};{lastSetInSecInd}", forceDelete:true);
                            // buttonTimingList.Remove(elementsName);
                            ClearComingButtonTiming(elementsName, 0);
                        }

                        if(buttonTimingBasedOnPreviousTimingDicts.Count > 0 && buttonTimingBasedOnPreviousTimingDicts[0].ContainsKey(elementsName)){//如果有相关的后续定时，则删除
                            ClearComingButtonTiming(elementsName, 0);
                            MessageUpdate($"button timing:{buttonTimingBasedOnPreviousTimingDicts[0][elementsName]} after button {elementsName} removed");
                            // buttonTimingBasedOnPreviousTimingDict.Remove(elementsName);
                        }
                        if(isTiming == 1){SetButtonColor(selectedButton, setToPrevious:true);}
                        return;
                    }else{
                        //无定时等，直接正常进入按钮parse部分
                    }
                }
            }
        }

        if(stringArg == "cancelTiming"){return;}//应当在之前处理好
        //if(string_arg==""){return;}
        switch (elementsName){
            case "StartButton":{
                moving.SetTrial(manual: true, waitSoundCue: true);
                break;
            }
            case "WaitButton":{
                moving.ForceWaiting = !moving.ForceWaiting;
                MessageUpdate($"{(moving.ForceWaiting ? "pause" : "continue")}");
                break;
            }
            case "FinishButton":{
                moving.LickingCheckPubic(lickInd: -2);
                break;
            }
            case "SkipButton":{
                moving.LickingCheckPubic(lickInd: -1);
                break;
            }
            case "ExitButton":{
                if(stringArg == "FromTiming"){
                    moving.Exit();
                }else{
                    moving.PreExit();
                    alarm.TrySetAlarm("Exit", 1.0f, out _, addInfo:"FromTiming");
                    SetButtonColor("ExitButton", Color.yellow);
                }
                break;
            }
            case "SliderPos":{
                moving.SetBarPos(moving.DegToPos(value));
                break;
            }
            case "IFSerialMessage":{
                // string temp_str=serialMessageInputs.text;
                string temp_str=stringArg.Length >0? stringArg: inputFieldContent[serialMessageInputs.name];
                MessageUpdate($"serial message sent: {temp_str}");
                if(temp_str.StartsWith("//")){
                    moving.DataSendRaw(temp_str);
                }else{
                    if(moving.DataSend(temp_str, temp_str.StartsWith("/"), true)==-1){Debug.LogError("missing variable name: "+temp_str);}
                }
                break;
            }
            case "ModeSelect":{
                if(stringArg.StartsWith("passive")){//format: passive
                    MessageUpdate($"mode now: 0x{moving.TrialMode.ToString("X2")}\n");
                    modeSelect.value = Convert.ToInt16(moving.TrialModes.IndexOf((int)value));
                    //modeSelect.GetComponent<ScrDropDown>().isPassive = true;
                }else{
                    if(moving.TrialModes[(int)value] != moving.TrialMode){
                        while(moving.ChangeMode(moving.TrialModes[(int)value]) == -1){Debug.LogWarning("mode sync failed at once");continue;}
                        MessageUpdate($"mode now: 0x{moving.TrialMode.ToString("X2")}\n");
                    }else{}
                }
                modeSelect.RefreshShownValue();
                
                break;
            }
            case "TriggerModeSelect":{
                if(stringArg.StartsWith("passive")){//format: passive
                    MessageUpdate($"Trigger mode now: {moving.TrialStartTriggerMode}\n");
                    triggerModeSelect.value = moving.TrialStartTriggerMode;
                    //triggerModeSelect.GetComponent<ScrDropDown>().isPassive = true;
                }else{
                    if(value != moving.TrialStartTriggerMode){
                        int _result = moving.ChangeTriggerMode((int)value);
                        if(_result == -1){Debug.LogWarning($"mode change failed because the next trial has already set, error code:{_result}");}
                        else if(_result == -2){Debug.LogWarning("No connection to Python Script");}
                        MessageUpdate($"Trigger mode now: {moving.TrialStartTriggerMode}\n");
                        triggerModeSelect.value = moving.TrialStartTriggerMode;
                    }else{}
                }
                triggerModeSelect.RefreshShownValue();
                
                break;
            }
            case "IFConfigValue":{

                break;
            }
            case "InfraRedIn":{
                moving.CommandParsePublic("entrance:-1:In");
                moving.CommandParsePublic("entrance:-1:Leave");
                break;
            }
            case "PressLever":{
                moving.CommandParsePublic("press:-1");
                break;
            }
            case "logScroll":{
                if(stringArg.StartsWith("passive")){//format: passive
                    if(alarm.GetAlarm("manualScrollWait") == -1){
                        logScrollBar.value = value;
                    }
                }else{
                    if(logScrollBar.value - value >= 0.01 && Input.GetMouseButton(0)){//避免手动操作时onValueChanged死循环
                        logScrollBar.value = value;
                        if(alarm.GetAlarm("setBarToZeroAfterSizeChange") == -1){
                            alarm.TrySetAlarm("manualScrollWait", _sec:manualWaitSec, out _);
                        }//Debug.Log("触发LogScroll");
                    }else{
                    }
                }
                break ;
            }
            case "DebugButton":{
                moving.DataSend($"{moving.ArduinoVarList[5]}={(moving.DebugMode? "0": "1")}", true, true);
                moving.DebugMode = !moving.DebugMode;
                foreach(UnityEngine.UI.Button button in buttons){
                    if(button.name == "DebugButton"){
                        SetButtonColor(button, moving.DebugMode? Color.green: button.GetComponent<ScrButton>().defaultColor);
                    }
                }
                break;
            }
            case "IPCRefreshButton":{
                if(moving.IsIPCInNeed()){
                    if(alarm.GetAlarm("ipcRefresh") < 0 && alarm.GetAlarm("checkIPCConnectStatus") < 0){
                        moving.Ipcclient.Silent = true;
                        moving.Ipcclient.Activated = false;
                        alarm.TrySetAlarm("ipcRefresh", 1.0f, out _);
                        SetButtonColor(elementsName, Color.green);
                    }else{
                        moving.Ipcclient.Silent = true;
                        moving.Ipcclient.Activated = false;
                        alarm.DeleteAlarm("ipcRefresh", forceDelete:true);
                        alarm.DeleteAlarm("checkIPCConnectStatus", forceDelete:true);
                        SetButtonColor(elementsName, setToDefault:true);
                    }
                }
                break;
            }
            case "IPCDisconnect":{
                if(moving.IsIPCInNeed()){
                    moving.Ipcclient.Silent = true;
                    moving.Ipcclient.Activated = false;
                }
                break;
            }
            case"PageUp":{
                logPage = Math.Max(logPage - 1, 0);
                if(logMessageList.Count > 0){
                    logMessageShow.text = logMessageList[logPage];
                    logWindowDraging = -1;
                }
                PageNum.text = $"{logPage+1}/{logMessageList.Count+1}";
                break;
            }
            case "PageDown":{
                logPage = Math.Min(logPage + 1, Math.Max(0, logMessageList.Count));
                if(logPage != logMessageList.Count){
                    logMessageShow.text = logMessageList[logPage];
                    logWindowDraging = -1;
                }else{
                    logMessageShow.text = logMessage;
                    logWindowDraging = 1;
                    alarm.TrySetAlarm("setlogWindowDragingToZeorAfter5sec", 5f, out _);
                }
                PageNum.text = $"{logPage+1}/{logMessageList.Count+1}";
                break;
            }
            case "BackgroundSwitch":{
                string matName = backgroundSwitch.captionText.text;
                moving.SetBackgroundMaterial(moving.Backgrounds.Find(m => m.name == matName));
                break;
            }
            case "TimingButtonsBaseSelect":{
                // Dropdown timingBaseSelectDropdown = dropdowns.Find(dropdown => dropdown.name == elementsName);
                if(stringArg.StartsWith("delete")){
                    if(value >= buttonTimingBaseDropdown.options.Count || value == 0){break;}
                    List<string> _options = buttonTimingBaseDropdown.options.Select(x => x.text).ToList();
                    if(!stringArg.EndsWith("finish")){ControlsParse(_options[(int)value], 1, stringArg:$"cancelTiming", ignoreTiming:false);}//取消对应按键的延时
                    _options.RemoveAt((int)value);
                    buttonTimingBaseDropdown.ClearOptions();
                    buttonTimingBaseDropdown.AddOptions(_options);
                    buttonTimingBaseDropdown.Hide();
                    buttonTimingBaseDropdown.value = 0;
                    buttonTimingBaseDropdown.RefreshShownValue();
                    // buttonTimingBaseScrDropdown.UpdateOptions();
                    buttonTimingBaseScrDropdown.UpdateOptionsFunctionEnableStatus(buttonTimingBasedOnPreviousTimingDicts.Count > 0? buttonTimingBasedOnPreviousTimingDicts[0]: null);
                }else if(stringArg.StartsWith("spread")){
                    float[] spreadPos = (from pos in stringArg.Split(";")[2].Split(':') select Convert.ToSingle(pos)).ToArray();
                    string _key = buttonTimingBaseDropdown.options[(int)value].text;
                        if (buttonTimingBasedOnPreviousTimingDicts.Count > 0 && buttonTimingBasedOnPreviousTimingDicts[0].ContainsKey(_key)) {
                            if (createdButtonTimingBaseSubDropdowns.Count != 0) {
                                Destroy(createdButtonTimingBaseSubDropdowns[0]);
                            }
                                //新建dorpdown
                            // Debug.Log("新建dorpdown");
                            GameObject _buttonTimingBaseSubDropdown = Instantiate(pref_buttonTimingBaseSubDropdown);
                            _buttonTimingBaseSubDropdown.name = "TimingButtonsBaseSelectSubDropdown";
                            _buttonTimingBaseSubDropdown.transform.position = new Vector3(spreadPos[0] + 160, spreadPos[1], 0);
                            _buttonTimingBaseSubDropdown.transform.SetParent(buttonTimingBaseDropdown.transform, true);
                            _buttonTimingBaseSubDropdown.GetComponent<Dropdown>().AddOptions(GetRelatedButtonTimingInPreviousTiming(0, _key));
                            if (buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count > 0) { _buttonTimingBaseSubDropdown.GetComponent<Dropdown>().value = buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone[0] + 1; }
                            ScrDropDown _tempScrDropdown = _buttonTimingBaseSubDropdown.GetComponent<ScrDropDown>();
                            _tempScrDropdown.ui_update = this;
                            // Debug.Log("新建dorpdown ui_update set");
                            _tempScrDropdown.OptionsNowHierarchy = buttonTimingBaseScrDropdown.OptionsNowHierarchy.GetRange(1, buttonTimingBaseScrDropdown.OptionsNowHierarchy.Count - 1);
                            _tempScrDropdown.nowSubHierarchyIndex = 1;
                            _tempScrDropdown.buttonFunctionsLs = buttonFunctionsLs;
                            // _tempScrDropdown.UpdateOptions();
                            _tempScrDropdown.UpdateOptionsFunctionEnableStatus(buttonTimingBasedOnPreviousTimingDicts.Count > 1 ? buttonTimingBasedOnPreviousTimingDicts[1] : null);
                            // Debug.Log("新建dorpdown finish");
                            alarm.TrySetAlarm("showButtonTimingSubDropdown", 1, out _, addInfo: "1");
                            createdButtonTimingBaseSubDropdowns.Add(_buttonTimingBaseSubDropdown);
                            buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Add((int)(value - 1));
                        }
                } else if (stringArg.StartsWith("hide")) {
                    foreach (GameObject _go in createdButtonTimingBaseSubDropdowns) {
                            Destroy(_go);
                        }
                        createdButtonTimingBaseSubDropdowns.Clear();
                        if (buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count > buttonTimingBaseScrDropdown.nowSelectedHierarchy) {
                            buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.RemoveRange(buttonTimingBaseScrDropdown.nowSelectedHierarchy, buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count - 1);
                        }
                    }
                    else {
                        //已在onvlauechanged中处理
                        foreach (GameObject _go in createdButtonTimingBaseSubDropdowns) {
                            Destroy(_go);
                        }
                        createdButtonTimingBaseSubDropdowns.Clear();
                    }
                break;
            }
            case "TimingButtonsBaseSelectSubDropdown":{
                string[] stringArgSplit = stringArg.Split(";");
                int _nowHierarchy = Convert.ToInt16(stringArgSplit[1]);

                if(stringArg.StartsWith("delete")){
                    ClearComingButtonTiming(buttonTimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy, _nowHierarchy);
                    // buttonTimingBaseScrDropdown.UpdateOptions();
                    buttonTimingBaseScrDropdown.UpdateOptionsFunctionEnableStatus(buttonTimingBasedOnPreviousTimingDicts[_nowHierarchy]);
                    if(createdButtonTimingBaseSubDropdowns.Count > _nowHierarchy - 1 && createdButtonTimingBaseSubDropdowns[_nowHierarchy - 1] != null){Destroy(createdButtonTimingBaseSubDropdowns[_nowHierarchy - 1]);}
                }
                else if(stringArg.StartsWith("spread")){
                    float[] spreadPos = (from pos in stringArgSplit[2].Split(':') select Convert.ToSingle(pos)).ToArray();
                    Dropdown timingBaseSelectDropdown = createdButtonTimingBaseSubDropdowns.Last().GetComponent<Dropdown>();
                    if(createdButtonTimingBaseSubDropdowns.Count == _nowHierarchy){
                        GameObject _buttonTimingBaseSubDropdown = Instantiate(pref_buttonTimingBaseSubDropdown);
                        _buttonTimingBaseSubDropdown.name = "TimingButtonsBaseSelectSubDropdown";
                        _buttonTimingBaseSubDropdown.transform.position = new Vector3(spreadPos[0] + 160, spreadPos[1], 0);
                        _buttonTimingBaseSubDropdown.transform.SetParent(timingBaseSelectDropdown.transform, true);
                        ScrDropDown _tempScrDropdown = _buttonTimingBaseSubDropdown.GetComponent<ScrDropDown>();
                        _buttonTimingBaseSubDropdown.GetComponent<Dropdown>().AddOptions(GetRelatedButtonTimingInPreviousTiming(_nowHierarchy, stringArgSplit[2]));
                        if(buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count >= _nowHierarchy){_buttonTimingBaseSubDropdown.GetComponent<Dropdown>().value = buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone[_nowHierarchy - 1] + 1;}
                        _tempScrDropdown.ui_update = this;
                        _tempScrDropdown.nowSubHierarchyIndex = _nowHierarchy + 1;
                        _tempScrDropdown.buttonFunctionsLs= buttonFunctionsLs;
                        // _tempScrDropdown.UpdateOptions();
                        _tempScrDropdown.UpdateOptionsFunctionEnableStatus(buttonTimingBasedOnPreviousTimingDicts[_nowHierarchy]);
                        alarm.TrySetAlarm("showButtonTimingSubDropdown", 1, out _, addInfo:_tempScrDropdown.nowSubHierarchyIndex.ToString());
                        createdButtonTimingBaseSubDropdowns.Add(_buttonTimingBaseSubDropdown);
                        buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Add((int)(value - 1));

                    }
                }
                else if(stringArg.StartsWith("destroy")){
                    foreach(GameObject _go in createdButtonTimingBaseSubDropdowns){
                        if(_go != null){
                            Destroy(_go);
                        }
                    }
                    createdButtonTimingBaseSubDropdowns.Clear();
                    // createdButtonTimingBaseSubDropdowns.RemoveAt(Convert.ToInt16(stringArgSplit[1]) - 1);
                    buttonTimingBaseDropdown.Hide();
                }else{
                    if(value == 0){
                        buttonTimingBaseScrDropdown.nowSelectedHierarchy = 0;
                        buttonTimingBaseDropdown.value = 0;
                        buttonTimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy = "None";
                        // buttonTimingBaseDropdown.RefreshShownValue();
                        buttonTimingBaseScrDropdown.UpdateCaptionText();

                    }else{
                        buttonTimingBaseScrDropdown.nowSelectedHierarchy = Convert.ToInt16(stringArgSplit[1]);
                        buttonTimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy = stringArgSplit[0];
                        if(buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count > _nowHierarchy - 1){
                            buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone[_nowHierarchy - 1] = (int)(value - 1);
                            buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.RemoveRange(_nowHierarchy, buttonTimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count - 1);
                        }//timing默认第一个为None
                        buttonTimingBaseScrDropdown.UpdateCaptionText();
                    }
                }
                break;
            }
            default:{
                if(elementsName.StartsWith("sound")){
                    if(stringArg.StartsWith("passive")){//format: passive;add;soundName/-
                        MessageUpdate($"cue sound play mode now: {string.Join(", ", moving.audioPlayModeNow)}\n");
                        foreach(int buttonInd in moving.audioPlayModeNow){
                            soundOptionsDict[buttonInd].GetComponent<ScrButton>().pressCount ++;
                            SetButtonColor(soundOptionsDict[buttonInd], Color.green);
                            if(stringArg.Split(";").Count() == 3){
                                string _soundName = stringArg.Split(";")[2];
                                if(moving.audioClips.ContainsKey(_soundName)){
                                    int tempSoundInd = moving.audioClips.Keys.ToList().IndexOf(_soundName);
                                    soundOptionsDict[buttonInd].GetComponentInChildren<Dropdown>().value = tempSoundInd;
                                }
                            }
                        }
                        //triggerModeSelect.GetComponent<ScrDropDown>().isPassive = true;
                    }else{
                        string soundOptionSelected = elementsName.Substring(5);//0 无声音、1 trial开始前、2 开始后可以舔之前、3 trial中、4 可获取奖励、5 失败
                        if(moving.TrialSoundPlayModeExplain.Contains(soundOptionSelected)){//buttons
                            int tempId = moving.TrialSoundPlayModeExplain.IndexOf(soundOptionSelected);
                            if(tempId == 0){//"Off"
                                moving.ChangeSoundPlayMode(0, 0, "", true);
                                foreach(UnityEngine.UI.Button button in soundOptionsDict.Values){
                                    SetButtonColor(button, setToDefault:true);
                                    button.GetComponent<ScrButton>().pressCount = 0;
                                }
                            }else{//其他
                                if(moving.audioPlayModeNow.Contains(0)){
                                    moving.audioPlayModeNow.Remove(0);
                                    SetButtonColor(soundOptionsDict[0], setToDefault:true);
                                    soundOptionsDict[0].GetComponent<ScrButton>().pressCount ++;
                                }
                                if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)){
                                    if(moving.TrialSoundPlayModeExplain[tempId] != "InPos"){
                                        moving.PlaySoundPublic(tempId, "manual", true);
                                    }
                                }else{
                                    bool selected = soundOptionsDict[tempId].GetComponent<ScrButton>().pressCount % 2 == 1;
                                    int _result = moving.ChangeSoundPlayMode(tempId, selected? 1: 0, soundOptionsDict[tempId].GetComponentInChildren<Dropdown>().captionText.text);
                                    SetButtonColor(soundOptionsDict[tempId], selected? Color.green: soundOptionsDict[tempId].GetComponent<ScrButton>().defaultColor);
                                    // if(_result == -1){Debug.LogWarning($"mode change failed because the sound is playing");}
                                    MessageUpdate($"cue sound play mode now: {string.Join(", ", moving.audioPlayModeNow)}\n");
                                }
                            }
                        }else if(soundOptionSelected.StartsWith("Dropdown")){//dropdowns
                            string parentNameAfter = soundOptionSelected.Substring(8);
                            int _tempId = moving.TrialSoundPlayModeExplain.IndexOf(parentNameAfter);

                            int _result = moving.ChangeSoundPlayMode(_tempId, 2, soundOptionsDict[_tempId].GetComponentInChildren<Dropdown>().captionText.text);
                        }
                    }
                    break;
                }
                else if(elementsName.StartsWith("MouseInfo")){
                    string _content = elementsName.Substring(9);
                    Dictionary<string, string> headCorrespond = new Dictionary<string, string>{{"Name", "userName"}, {"Index", "mouseInd"}};
                    if(stringArg.StartsWith("passive")){//format: passive

                    }
                    else{
                        if(headCorrespond.TryGetValue(_content, out string _info)){
                            moving.SetMouseInfo(_info + ":" + stringArg);
                        }
                    }
                }
                else if(elementsName.StartsWith("OG") || elementsName.StartsWith("MS")){
                    string _type = elementsName.Substring(0, 2);
                    string _content = elementsName.Substring(2);
                    if(_content == "Start"){
                        moving.trialStatus = -3;
                        if(!int.TryParse(inputFieldContent[$"{_type}Time"], out int _mills)){_mills = _type == "OG"? 10000: 480;}
                        bool res = _type == "OG"? moving.OGSet(_mills): moving.MSSet(_mills);
                        // if(res){
                        //     SetButtonColor(buttons.Find(button => button.name == $"{_type}Start"), Color.green);
                        //     // alarm.TrySetAlarm("OGStartToGrey", 0.5f, out _);
                        // }
                    }else if(_content == "Stop"){
                        bool res = _type == "OG"? moving.OGSet(0): moving.MSSet(0);
                        // if(res){
                        //     SetButtonColor(buttons.Find(button => button.name == $"{_type}Start"), setToDefault:true);
                        // }
                    }else if(_content == "Enable"){
                        if(moving.DeviceEnableDict.TryGetValue(_type, out bool _enabled)){
                            _enabled = ! _enabled;
                            moving.DeviceEnableDict[_type] = _enabled;
                            if(_enabled){}
                            SetButtonColor(buttons.Find(button => button.name == $"{_type}Enable"), Color.green, !_enabled);

                        }else{
                            moving.DeviceEnableDict.Add(_type, true);
                            SetButtonColor(buttons.Find(button => button.name == $"{_type}Enable"), Color.green);
                        }
                    }
                }
                break;
            }
        }

        if(elementsName.StartsWith("LickSpout")){
            string Spout = elementsName.Substring(elementsName.Length-1);
            moving.CommandParsePublic($"{moving.LsTypes[0]}:{Spout}:1");
        }else if (elementsName.StartsWith("WaterSpout")){
            string Spout = elementsName.Substring(elementsName.Length-1);
            if(Input.GetKey(KeyCode.LeftShift)){
                ControlsParse("IFSerialMessage", 1, $"/p_water_flush[{Spout}]=1");
            }else{
                moving.alarmPublic.TrySetAlarm($"sw={Spout}", _sec:0.2f, out _, elementsName.Contains("Single")? 0: 99);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deviceName"></param> "MSTime" or "OGTime"
    /// <param name="_mills"></param>
    /// <returns></returns>
    public bool TryGetDeviceSetTime(string deviceName, out int _mills){
        List<string> _devices = new List<string>{"MSTime", "OGTime"};
        if(_devices.Contains(deviceName)){
            bool _res = inputFieldContent.TryGetValue(deviceName, out string _strMills);
            try{
                _mills = Convert.ToInt32(_strMills);
            }catch{
                _mills = 1000;
                Debug.Log($"failed to parse {deviceName} to int");
            }
            return  _res;
        }
        _mills = -2;
        return false;
    }


    public string MessageUpdate(string add_log_message="", int UpdateFreq = 0, bool returnAllMsg = false, bool attachToLastLine = false){//随时可能被调用，需要对内容做null检查
        // if (UpdateFreq == 0) { Debug.Log($"MessageUpdate called with {add_log_message}"); }
        if(returnAllMsg){
            return logMessage + "\n" + TexContextHighFreqInfo.text + "\n" + TexContextInfo.text;
        }

        if(UpdateFreq == 1){//高频
            if(moving.TrialInitTime != 0){
                int time = (int)(Time.fixedUnscaledTime - moving.TrialInitTime);
                int hour = time / 3600;
                int minute = (time - hour*3600) / 60;
                int second = time % 60;
                TexContextHighFreqInfo.text = $"{hour:D2}:{minute:D2}:{second:D2}, fps={(int)_FrameRate}";
            }
        }else if(UpdateFreq == -1){//fix
            TexContextFixInfo.text = add_log_message;
        }else{
            string _time = DateTime.Now.ToString("HH:mm:ss ");
            if(add_log_message == LastAddedLogMessage){
                return "";
            }
            
            if(add_log_message!=""){
                if(logMessage.Length > 8000){
                    //moving.WriteInfo(enqueueMsg: log_message.text);
                    moving.LogList.Add(logMessage);
                    logMessageList.Add(logMessage);
                    logMessage = "";
                    if(logPage == logMessageList.Count - 1){
                        logPage ++;
                        // logMessageShow.text = logMessage;
                    }
                    // logPage ++;
                }

                if(attachToLastLine){
                    logMessage = (logMessage.EndsWith("\n")? logMessage.Substring(0, logMessage.Length-1) :"") + "  --" + add_log_message + (add_log_message.EndsWith("\n")? "": "\n");
                }else{
                    logMessage += _time + add_log_message + (add_log_message.EndsWith("\n")? "": "\n");
                }
                LastAddedLogMessage = add_log_message;

                // Debug.Log($"logMessage added: {logMessage.Length}");
                if(logPage == logMessageList.Count){
                    logMessageShow.text = logMessage;
                }

                PageNum.text = $"{logPage+1}/{logMessageList.Count+1}";

                if(alarm != null){
                    alarm.TrySetAlarm("setBarToZeroAfterSizeChange", 0.5f, out _);
                }

            }else{
                /*
                "NowTrial"    
                "IsPausing"   
                "NowPos"      
                "lickPosCount"
                "waitSec"
                -------------------------------"lickCountArrayLength"
                "lickCount"0,1,2...
                "TrialSuccessNum"0,1,2...
                "TrialFailNum"0,1,2...
                "LickSpoutTotalTrial"0,1,2...
                */
                Dictionary<string, int> tempStatus = moving.GetTrialInfo();
                if(tempStatus["NowTrial"] == -1){
                    return "";
                }

                string  temp_context_info =  $"trial:{tempStatus["NowTrial"]}     now pos:{tempStatus["NowPos"]}    {(tempStatus["IsPausing"] == 1? "paused" : "")}\n"; 
                if(tempStatus["lickPosCount"] > 0){
                    temp_context_info += "lick count in this trial: ";
                    List<int> realPosAdded = new List<int>();
                    for(int i =0; i < 8; i ++){
                        if(tempStatus.ContainsKey("LickSpout" + i.ToString())){
                            int RealPos = tempStatus[$"LickSpout{i}"];
                            if(!realPosAdded.Contains(RealPos)){
                                if(tempStatus.ContainsKey("lickCount" + RealPos.ToString())){
                                    temp_context_info += $"{tempStatus["lickCount" + i.ToString()]}, ";
                                    realPosAdded.Add(RealPos);
                                }
                            }
                        }
                    }
                    temp_context_info += "\n";
                }

                temp_context_info += $"           Success:    Fail:    Total:    Miss:\n";
                for(int i =0; i < 8; i ++){
                    if(tempStatus.ContainsKey("LickSpout" + i.ToString())){
                        int RealPos = tempStatus[$"LickSpout{i}"];
                        if(!temp_context_info.Contains($"LickSpout{RealPos}")){
                            temp_context_info += $"LickSpout{RealPos}: {tempStatus["TrialSuccessNum" + i.ToString()]}          {tempStatus["TrialFailNum" + i.ToString()]}           {tempStatus["LickSpoutTotalTrial" + i.ToString()]}           {tempStatus["TrialMissNum" + i.ToString()]}\n";
                        }
                    }
                }
                temp_context_info += $"Total: {tempStatus["TrialSuccessNum"]}        {tempStatus["TrialFailNum"]}\n";
                
                if(tempStatus["NowTrial"] > 0){
                    float tempAccuracy = tempStatus["TrialSuccessNum"] / (float)(tempStatus["TrialSuccessNum"]+tempStatus["TrialFailNum"]) * 100;
                    temp_context_info += $"Accuracy: {tempAccuracy:f2}%\n";
                }

                if(tempStatus["waitSec"] != -1){
                    temp_context_info += $"interval now: ~{tempStatus["waitSec"]}";
                }

                TexContextInfo.text=temp_context_info; 
            }
        }
        return "";
    }

    void Awake()
    {
        moving = GetComponent<Moving>();
        inputFields.Add(serialMessageInputs);
        buttons.Add(startButton);
        dropdowns.Add(modeSelect);
        dropdowns.Add(triggerModeSelect);
        dropdowns.Add(backgroundSwitch);
        dropdowns.Add(buttonTimingBaseDropdown);
        soundOptionsDict = new Dictionary<int, UnityEngine.UI.Button>();
        logScrollBar.GetComponent<ScrScrollBar>().ui_update = this;
        buttonTimingBaseScrDropdown = buttonTimingBaseDropdown.GetComponent<ScrDropDown>();

        foreach(UnityEngine.UI.Button button in soundOptions.GetComponentsInChildren<UnityEngine.UI.Button>()){
            if(moving.TrialSoundPlayModeExplain.Contains(button.name.Substring(5))){
                soundOptionsDict.Add(moving.TrialSoundPlayModeExplain.IndexOf(button.name.Substring(5)), button);
                button.GetComponent<ScrButton>().ui_update = this;
                Dropdown _dropdown = button.GetComponentInChildren<Dropdown>();
                if(_dropdown != null){
                    _dropdown.name = "soundDropdown" + button.name.Substring(5);
                    _dropdown.AddOptions(moving.audioClips.Keys.ToList());
                    _dropdown.GetComponentInChildren<Text>().fontSize = 10;
                    // moving.soundConfig.TrialSoundPlayModeAudio.TryAdd(moving.soundConfig.TrialSoundPlayModeAudio.Count, _dropdown.captionText.text);
                    soundDropdowns.Add(_dropdown);
                }
            }
        }
        
        backgroundSwitch.AddOptions(moving.Backgrounds.Select(m => m.name).ToList());

        foreach(UnityEngine.UI.Slider slider in sliders){
            if(slider.GetComponent<ScrSlider>() != null){
                slider.GetComponent<ScrSlider>().ui_update = this;
            }
        }
        foreach(UnityEngine.UI.Button button in buttons){
            if(button.GetComponent<UnityEngine.UI.Button>() != null){
                button.GetComponent<ScrButton>().ui_update = this;
            }
        }
        foreach(UnityEngine.UI.Dropdown dropdown in dropdowns){
            if(dropdown.GetComponent<UnityEngine.UI.Dropdown>() != null){
                dropdown.GetComponent<ScrDropDown>().ui_update = this;
                //dropdown.RefreshShownValue();
            }
        }
        foreach(UnityEngine.UI.Dropdown dropdown in soundDropdowns){
            if(dropdown.GetComponent<UnityEngine.UI.Dropdown>() != null){
                dropdown.GetComponent<ScrDropDown>().ui_update = this;
                //dropdown.RefreshShownValue();
            }
        }

        alarm = new Alarm();
        alarm.TrySetAlarm("manualScrollWait", -1, out _);
        // foreach(InputField inputField in other_inputs){
        //     if (inputField.name=="IFSerialMessage"){serialMessageInputs=inputField;}
        //     else if (inputField.name=="IFConfigValue"){
        //         mode1ConfigInputs = inputField;
        //         mode1ConfigInputs.placeholder.GetComponent<Text>().text = position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, position_control.serve_water_mode)[1].ToString();
        //     }
        // }

        foreach(InputField inputField in inputFields){
            if(!inputFieldContent.TryAdd(inputField.name, inputField.text)){
                inputFieldContent[inputField.name] = "null";
            }
        }

        if(IFContentLoaded != ""){
            foreach(string content in IFContentLoaded.Split(";")){
                string IFName = content.Split(":")[0];
                if(inputFieldContent.ContainsKey(IFName)){
                    inputFieldContent[IFName] = content.Split(":")[1];
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach(InputField inputField in inputFields){
            if (inputField.isFocused == true){
                focus_input_field=inputField;
            }
        }

        if(focus_input_field!=null && Input.GetKeyDown(KeyCode.Return)){
            if(!inputFieldContent.TryAdd(focus_input_field.name, focus_input_field.text)){
                inputFieldContent[focus_input_field.name] = focus_input_field.text;
            }
            ControlsParse(focus_input_field.name, float.TryParse(inputFieldContent[focus_input_field.name], out float temp_value) ? temp_value : 0, inputFieldContent[focus_input_field.name]);
            if(focus_input_field.name=="IFSerialMessage" || focus_input_field.name=="IFConfigValue"){
                focus_input_field.text="";
                // inputFieldContent[focus_input_field.name] = "null";
            }
        }
        // MessageUpdate();
        if(Input.GetMouseButtonDown(0) && logWindowDraging >= 0){
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> raysastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raysastResults);

            for (int index = 0; index < raysastResults.Count; index++)
            {
                RaycastResult curRaysastResult = raysastResults[index];
                if(curRaysastResult.gameObject.name == "LogWindowBackground"){
                    if(logWindowDraging == 0){
                        logWindowDraging = 1;
                        alarm.TrySetAlarm("setlogWindowDragingToZeorAfter5sec", 5f, out _);
                    }
                    // alarm.TrySetAlarm("setBarToZeroAfterSizeChange", 2f, out _);

                }
            }
        }

        _FrameCount++;
        _TimeCount += Time.unscaledDeltaTime;
        if (_TimeCount >= 10){ _FrameRate = _FrameCount / _TimeCount; _FrameCount = 0; _TimeCount -= 10;}
    }

    void FixedUpdate() {
        List<string> tempFInishedLs = alarm.GetAlarmFinish();
        foreach (string alarmFinished in tempFInishedLs){
            switch(alarmFinished){
                case "Exit":{
                    ControlsParse("ExitButton", 1, alarm.GetAlarmAddInfo("Exit"), ignoreTiming:true);
                    break;
                }
                case "ipcRefresh":
                    {
                    moving.Ipcclient.Silent = false;
                    alarm.TrySetAlarm("checkIPCConnectStatus", 3.0f, out _, force:true);
                    break;
                }
                case "OGStartToGrey":{
                    SetButtonColor(buttons.Find(button => button.name == "OGStart"), Color.white);
                    break;
                }
                case "MSStartToGrey":{
                    SetButtonColor(buttons.Find(button => button.name == "MSStart"), Color.white);
                    break;
                }
                case"setlogWindowDragingToZeorAfter5sec":{
                    logWindowDraging = 0;
                    break;
                }
                case "checkIPCConnectStatus":{
                    if(moving.Ipcclient.Activated){
                        alarm.DeleteAlarm("checkIPCConnectStatus", forceDelete:true);
                        SetButtonColor("IPCRefreshButton", setToDefault:true);
                    }else{
                        moving.Ipcclient.Silent = true;
                        moving.Ipcclient.Activated = false;
                        alarm.TrySetAlarm("ipcRefresh", 0.5f, out _, force:true);
                    }
                    break;
                }
                case "showButtonTimingSubDropdown":{
                    GameObject buttonTimingBaseSubDropdown = createdButtonTimingBaseSubDropdowns[Convert.ToInt16(alarm.GetAlarmAddInfo("showButtonTimingSubDropdown")) - 1];
                    buttonTimingBaseSubDropdown.GetComponent<Dropdown>().Show();
                    buttonTimingBaseSubDropdown.transform.GetChild(3).gameObject.AddComponent<ScrDestroyParent>();

                    // buttonTimingBaseSubDropdown.GetComponent<ScrDropDown>().UpdateOptionsFunctionEnableStatus(0);
                    break;
                }
                default:{
                    if(alarmFinished.StartsWith("buttonTiming")){
                        string timingButtonName = alarmFinished[12..alarmFinished.IndexOf(";")];
                        UnityEngine.UI.Button _tempButton = buttons.Find(button => button.name == timingButtonName);
                        if(_tempButton == null){break;}
                        string _args = alarm.GetAlarmAddInfo(alarmFinished);//format:"{buttonName;ind}"，暂时不需要stringArg传递
                        _tempButton.GetComponent<ScrButton>().pressCount ++;
                        buttonTimingList.Remove(timingButtonName);
                        SetButtonColor(timingButtonName, setToPrevious:true, ignoreTiming:true);
                        ControlsParse(timingButtonName, 1, _args);
                        // ControlsParse("TimingButtonsBaseSelect", buttonTimingBaseDropdown.options.IndexOf(buttonTimingBaseDropdown.options.Where(o => o.text == timingButtonName).First()), stringArg:"delete;finish");
                        if(buttonTimingBasedOnPreviousTimingDicts.Count > 0 && buttonTimingBasedOnPreviousTimingDicts[0].ContainsKey(timingButtonName)){
                            string _tempTimingMethod = buttonTimingBasedOnPreviousTimingDicts[0][timingButtonName].Trim(';');
                            foreach(string _subTimingMethod in _tempTimingMethod.Split(";")){
                                ControlsParse(_subTimingMethod[.._subTimingMethod.IndexOf(":")], 1, _subTimingMethod[(_subTimingMethod.IndexOf(":")+1)..], ignoreTiming:false, forceTiming:true, ignoreSecondTiming:true);
                            }
                            buttonTimingBasedOnPreviousTimingDicts[0].Remove(timingButtonName);
                        }
                        alarm.DeleteAlarm(alarmFinished);
                    }
                    break;
                }
            }

        }
        alarm.AlarmFixUpdate();

        if(alarm.GetAlarm("setBarToZeroAfterSizeChange") >= 0){
            if(logWindowDraging == 0){
                logScrollBar.value = 0;
            }
        }
        //Debug.Log(logScrollBar.value);
    }
}
