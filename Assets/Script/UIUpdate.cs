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
    public Dropdown TimingBaseDropdown;
    ScrDropDown TimingBaseScrDropdown;
    /// <summary>
    /// 包含不同层级定时字典，字典键为alarm名称，值以“;”分割，包含一个或多个定时方式（按钮名称:定时方式:具体值）
    /// </summary> 
    TimingCollection timings = null;
    /// <summary>
    /// like {"delete", "spread"} or...
    /// </summary>
    public List<string> buttonFunctionsLs = new List<string>{"delete", "spread"};
    bool[] buttonFunctionsEnableDefault = new bool[]{true, false};
    public GameObject pref_buttonTimingBaseSubDropdown;
    List<GameObject> createdTimingBaseSubDropdowns = new List<GameObject>();
    public string IFContentLoaded = "";
    
    private int _FrameCount = 0; private float _TimeCount = 0; private float _FrameRate = 0;

    //public Image LineChartImage;

    struct Timing {
        public string type;//button, dropdown...
        public string name;//pass to ControlsParse
        public int hierarchy;
        public float time;//time been set in seconds in unity time
        public string timingMethod;
        public int Id;//unique id for each timingcollection
        public int parentId;
        public string parentName;

        public Timing SetLowerHierarchy() {
            hierarchy--;
            return this;
        }
        // public Timing(string _type, string _name, int _hierarchy, float _time, string _timingMethod, int _Id, int _parentId) {
        //     type = _type;
        //     name = _name;
        //     hierarchy = _hierarchy;
        //     time = _time;
        //     timingMethod = _timingMethod;
        //     Id = _Id;
        //     parentId = _parentId;

        // }
    }

    class TimingCollection{

        public TimingCollection(List<Timing> _timings = null){
            if (_timings == null){
                return;
            }
            _timings.Sort((t1, t2) => t1.hierarchy.CompareTo(t2.hierarchy));
            foreach (Timing timing in _timings){
                timings.Add(timing.Id, timing);
                maxId = Math.Max(maxId, timing.Id);
            }
        }

        public Timing? this[int hierarchy, string name]{

            get{
                List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.hierarchy == hierarchy);
                if (_timings.Count == 0){
                    return null;
                }
                foreach (Timing timing in _timings){
                    if (timing.name == name){
                        return timing;
                    }
                }
                return null;
            }

            set{
                if (value.HasValue){
                    Remove(name, hierarchy);
                }
                else{
                    timings[GetId(hierarchy, name)] = value.Value;
                }
            }
        }

        public TimingCollection this[int hierarchy]{
            get{
                List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.hierarchy == hierarchy);
                if (_timings.Count == 0){
                    return new TimingCollection();
                }
                return new TimingCollection(_timings);
            }
        }

        // public string this[string name]
        public TimingCollection this[string name]
        {//不强制但建议在仅有一阶选项时使用
            get{
                List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.name == name);
                // _timings.Sort((t1, t2) => t1.hierarchy.CompareTo(t2.hierarchy));
                if (_timings.Count == 0){
                    return null;
                }
                // return _timings[0].timingMethod;
                return new TimingCollection(_timings);
            }
        }

        public List<string> Names(){
            return timings.Values.Select(t => t.name).ToList();
        }

        public List<int> Keys(){
            return timings.Values.Select(t => t.Id).ToList();
        }

        public Dictionary<int, string> Values() {
            return timings.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.name);
        }

        public bool ContainsKey(string key) {
            return timings.Values.Where(t => t.name == key).Any();
        }

        public List<int> Hierarchies(){
            return timings.Values.Select(t => t.hierarchy).ToList();
        }

        public Timing Add(string name, string timingMethod, string type, int hierarchy = 0, int parentId = -1, float time = -1, string parentName = "") {//hierarchy默认为一阶选项，当parentId不为-1时，hierarchy自动指定为父项hierarchy+1
            int Id = ++maxId;
            if (parentId != -1) {
                Timing? parentTiming = GetTiming(parentId);
                if (parentTiming != null) {
                    hierarchy = parentTiming.Value.hierarchy + 1;
                }
            }
            if (time == -1) { time = Time.fixedUnscaledTime; }
            Timing timing = new Timing { type = type, name = name, hierarchy = hierarchy, time = time, timingMethod = timingMethod, Id = Id, parentId = parentId, parentName = parentName };
            timings.Add(Id, timing);
            return timing;
        }
        
        List<Timing> GetTimingsByHierarchyAndName(int hierarchy, string name) {
            int maxHierarchy = hierarchy == -1 ? 999 : hierarchy;
            var res = timings.Values.ToList().Where(t => t.name == name && t.hierarchy >= hierarchy && t.hierarchy <= maxHierarchy).ToList();
            res.Sort((t1, t2) => t1.hierarchy.CompareTo(t2.hierarchy));

            return res;
        }

        public List<Timing> Remove(string name, int hierarchy = -1, bool iterate = false) {//如未指定hierarchy，则删除第一个hierarchy下第一个对应name的定时
            var selectedTiming = GetTimingsByHierarchyAndName(hierarchy, name);
            if (selectedTiming.Count == 0) { return new List<Timing>(); }
            return Remove(selectedTiming[0].Id, iterate);
        }
        public List<Timing> Remove(int timingId, bool iterate = false) {
            List<int> keysInOrder = timings.Keys.ToList();
            keysInOrder.Sort((t1, t2) => timings[t1].hierarchy.CompareTo(timings[t2].hierarchy));
            List<int> removedId = new List<int>{timingId};
            List<Timing> removedTiming = new List<Timing>();
            List<int> childId = new List<int>();
            foreach (int key in keysInOrder) {
                if (removedId.Contains(key)) {
                    removedTiming.Add(timings[key]);
                    timings.Remove(key);
                }
                else if (removedId.Contains(timings[key].parentId)) {
                    if (iterate) {
                        removedTiming.Add(timings[key]);
                        removedId.Add(timings[key].Id);
                        timings.Remove(key);
                    }
                    else {
                        childId.Add(key);
                        timings[key] = timings[key].SetLowerHierarchy();
                    }
                }
                else if (childId.Contains(key)) {
                    childId.Add(key);
                    timings[key] = timings[key].SetLowerHierarchy();
                }
            }
            while (maxId > 100 && removedId.Contains(maxId)) {
                maxId--;
            }
            return removedTiming;
        }

        public List<Timing> Clear() {
            if (timings.Count == 0) { return new List<Timing>(); }
            var res = timings.Values.ToList();
            timings.Clear();
            maxId = 0;
            return res;
        }

        public Timing? GetTiming(int id) {
            if (timings.ContainsKey(id)) {
                return timings[id];
            }
            return null;
        }
        

        public int GetId(int hierarchy, string name){
            var _timings = GetTimingsByHierarchyAndName(hierarchy, name);
            if (_timings.Count == 0){
                return -1;
            }
            return _timings[0].Id;
        }

        public string GetTimingMethod(int hierarchy, string name) {
            var _timings = GetTimingsByHierarchyAndName(hierarchy, name);
            var _timingMethods = _timings.Select(t => t.timingMethod).ToList();
            if (_timingMethods.Count == 0) { return ""; }
            else { return _timingMethods[0].ToString(); }
        }

        public List<Timing> GetTimingChildren(int parentId) {
            List<Timing> _timings = this.timings.Values.ToList().FindAll(t => t.parentId == parentId);
            return _timings.OrderBy(t => t.Id).ToList();
        }

        Dictionary<int, Timing> timings = new Dictionary<int, Timing>();
        int maxId = -1;
        public int Count { get { return timings.Count; } }
    }

    public int ButtonAddSelf(UnityEngine.UI.Button button){
        if (buttons.Contains(button)) { return 0; }

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
        if(_button == null || (!ignoreTiming && timings.ContainsKey(_button.name))){return;}
        _button.GetComponent<ScrButton>().ChangeColor(color, setToDefault, setToPrevious, forcePreviousUpdate);
        Debug.Log($"SetButtonColor {_button.name} to {color}");
    }
    
    bool GetButtonColor(string buttonName, out Color color){
        UnityEngine.UI.Button button = buttons.Find(button => button.name == buttonName);
        if(button != null){
            color = button.GetComponent<Image>().color;
            return true;
        }
        color = Color.white;
        return false;
    }

    public void SetButtonTiming(string _buttonNameWithInd){
        alarm.TrySetAlarm(_buttonNameWithInd, 1, out _, addInfo: "FromTiming", force: true);
    }

    int ClearComingButtonTiming(int timingId = -1) {
        if (timingId == -1) {
            foreach (Timing timing in timings.Clear()) {
                MessageUpdate($"button timing:{timing.timingMethod} removed");
            }
            return 1;
        }

        var removedTimings = timings.Remove(timingId, true);
        foreach (Timing timing in removedTimings) {
            MessageUpdate($"button timing:{timing.timingMethod} removed");
        }
        UpdateOptions();
        return 1;
    }
    
    int UpdateOptions(int hierarchy = 0) {
        int _timingCount = timings[0].Count;
        if (_timingCount > 0) {
            return TimingBaseScrDropdown.UpdateOptions(timings[hierarchy].Values(), timings[hierarchy].Keys().Select(t => timings.GetTimingChildren(t).Count > 0 ? 1 : 0).ToList());
        }
        else {
            return TimingBaseScrDropdown.UpdateOptions();
        }
    }

    public void ControlsParsePublic(string elementsName, float value, string stringArg = "", bool ignoreTiming = true, bool forceTiming = false) {//scrButton中调用时ignoreTiming为false
        ControlsParse(elementsName, value, stringArg, ignoreTiming, forceTiming);
    }
    
    /// <summary>
    /// for inputfield, stringArg is the method of timing, value is 0 if failed to parse to float
    /// for ButtonTiming, stringArg is the timing value(in seconds)
    /// </summary>
    /// <param name="elementsName"></param>
    /// <param name="value"></param>
    /// <param name="stringArg"></param>
    void ControlsParse(string elementsName, float value, string stringArg="", bool ignoreTiming = true, bool forceTiming = false, bool ignoreSecondTiming = false, Timing? timing = null){

        if(! ignoreTiming && (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) || forceTiming) || stringArg.Contains("cancelTiming")){//按trial定时事件执行在DeviceTriggerExecute最后处理
            bool isTiming = timings.ContainsKey(elementsName);
            
            float _sec = -1;int _trial = -1;
            string timingElementType = timing.HasValue? timing.Value.type: "";
            if (stringArg.StartsWith("type_")){ timingElementType = stringArg.Split(";")[0][5..]; stringArg = stringArg.Contains(";")? string.Join(";", stringArg.Split(";")[1]) : "";}
            if (stringArg.Contains(";")) {
                int useSecInArg = Convert.ToInt16(stringArg.StartsWith("sec")) - Convert.ToInt16(stringArg.StartsWith("trial"));
                if (useSecInArg == 1) { _sec = float.Parse(stringArg.Split(";")[1]); }
                else if (useSecInArg == -1) { _trial = int.Parse(stringArg.Split(";")[1]); }
            }
            else {
                if (!float.TryParse(inputFieldContent["IFTimingBySec"], out _sec)) { _sec = -1; }
                if (!int.TryParse(inputFieldContent["IFTimingByTrial"], out _trial)) { _trial = -1; }
            }
            if(_sec < 0 && _trial < 0 && !timing.HasValue){
                MessageUpdate("please input a valid number for button timing");
                return;
            }
            
            int _TimingId = TimingBaseScrDropdown.nowSelectedTimingId;
                // string _timingBaseName = buttonTimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy;
            string _timingBaseName = timing.HasValue? timing.Value.parentName: TimingBaseScrDropdown.optionSelectedIncludeHigerHierarchy;
                // if(!ignoreSecondTiming && (buttonTimingBaseDropdown.value!=0 || buttonTimingList.Contains(_timingBaseName))){
            bool useSec = _sec > 0;
            string _timingMethod = timing.HasValue? timing.Value.timingMethod: $"type_{timingElementType};{elementsName}:{(useSec? "sec": "trial")}:{(useSec? _sec: _trial)};";
            Timing _timing = timing.HasValue? timing.Value: timings.Add(elementsName, _timingMethod, timingElementType, parentId:_TimingId, parentName:_timingBaseName);
            string targetElementTimingName = $"Timing{_timing.name};{_timing.Id}";
            if (!ignoreSecondTiming && _TimingId != -1) {
                MessageUpdate($"button {elementsName} timing set to {(useSec ? _sec : _trial)} {(useSec ? "s " : "trial")} after {_timingBaseName}");
                UpdateOptions();
                return;
            }
            
            if(_sec > 0){
                alarm.TrySetAlarm(targetElementTimingName, _sec, out _, addInfo:"FromTiming", force:true);
                MessageUpdate($"button {elementsName} timing set to {_sec}s");
                inputFields.Find(inputfield => inputfield.name == "IFTimingBySec").text = "";
                inputFieldContent[$"IFTimingBySec"] = "null";
                
            }else if(_trial > 0){//默认按trial start，暂时不考虑finish和end
                MessageUpdate($"button {elementsName} timing set to trial {Math.Max(0, moving.NowTrial) + _trial}");
                moving.ButtonTriggerDict.Add($"targetButtonTimingName", Math.Max(0, moving.NowTrial) + _trial);
                inputFields.Find(inputfield => inputfield.name == "IFTimingByTrial").text = "";
                inputFieldContent[$"IFTimingByTrial"] = "null";
            }else{
                return;
            }

            UpdateOptions();

            SetButtonColor(elementsName, Color.yellow, forcePreviousUpdate:true, ignoreTiming:true);
            return;
        }
        else{
            if (Input.GetKey(KeyCode.LeftControl) && timings.ContainsKey(elementsName)) {
                SetButtonColor(elementsName, Color.yellow, forcePreviousUpdate:true, ignoreTiming:true);
                foreach (Timing _timing in timings.Remove(elementsName)) {
                    MessageUpdate($"button timing:{_timing.timingMethod} removed");
                }
                return;
            }
        }
        
        if (stringArg == "cancelTiming") { return; }//应当在之前处理好
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
            default:{
                if (elementsName.StartsWith("sound")) {
                    if (stringArg.StartsWith("passive")) {//format: passive;add;soundName/-
                        MessageUpdate($"cue sound play mode now: {string.Join(", ", moving.audioPlayModeNow)}\n");
                        foreach (int buttonInd in moving.audioPlayModeNow) {
                            soundOptionsDict[buttonInd].GetComponent<ScrButton>().pressCount++;
                            SetButtonColor(soundOptionsDict[buttonInd], Color.green);
                            if (stringArg.Split(";").Count() == 3) {
                                string _soundName = stringArg.Split(";")[2];
                                if (moving.audioClips.ContainsKey(_soundName)) {
                                    int tempSoundInd = moving.audioClips.Keys.ToList().IndexOf(_soundName);
                                    soundOptionsDict[buttonInd].GetComponentInChildren<Dropdown>().value = tempSoundInd;
                                }
                            }
                        }
                        //triggerModeSelect.GetComponent<ScrDropDown>().isPassive = true;
                    }
                    else {
                        string soundOptionSelected = elementsName.Substring(5);//0 无声音、1 trial开始前、2 开始后可以舔之前、3 trial中、4 可获取奖励、5 失败
                        if (moving.TrialSoundPlayModeExplain.Contains(soundOptionSelected)) {//buttons
                            int tempId = moving.TrialSoundPlayModeExplain.IndexOf(soundOptionSelected);
                            if (tempId == 0) {//"Off"
                                moving.ChangeSoundPlayMode(0, 0, "", true);
                                foreach (UnityEngine.UI.Button button in soundOptionsDict.Values) {
                                    SetButtonColor(button, setToDefault: true);
                                    button.GetComponent<ScrButton>().pressCount = 0;
                                }
                            }
                            else {//其他
                                if (moving.audioPlayModeNow.Contains(0)) {
                                    moving.audioPlayModeNow.Remove(0);
                                    SetButtonColor(soundOptionsDict[0], setToDefault: true);
                                    soundOptionsDict[0].GetComponent<ScrButton>().pressCount++;
                                }
                                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                                    if (moving.TrialSoundPlayModeExplain[tempId] != "InPos") {
                                        moving.PlaySoundPublic(tempId, "manual", true);
                                    }
                                }
                                else {
                                    bool selected = soundOptionsDict[tempId].GetComponent<ScrButton>().pressCount % 2 == 1;
                                    int _result = moving.ChangeSoundPlayMode(tempId, selected ? 1 : 0, soundOptionsDict[tempId].GetComponentInChildren<Dropdown>().captionText.text);
                                    SetButtonColor(soundOptionsDict[tempId], selected ? Color.green : soundOptionsDict[tempId].GetComponent<ScrButton>().defaultColor);
                                    // if(_result == -1){Debug.LogWarning($"mode change failed because the sound is playing");}
                                    MessageUpdate($"cue sound play mode now: {string.Join(", ", moving.audioPlayModeNow)}\n");
                                }
                            }
                        }
                        else if (soundOptionSelected.StartsWith("Dropdown")) {//dropdowns
                            string parentNameAfter = soundOptionSelected.Substring(8);
                            int _tempId = moving.TrialSoundPlayModeExplain.IndexOf(parentNameAfter);

                            int _result = moving.ChangeSoundPlayMode(_tempId, 2, soundOptionsDict[_tempId].GetComponentInChildren<Dropdown>().captionText.text);
                        }
                    }
                    break;
                }
                else if (elementsName.StartsWith("MouseInfo")) {
                    string _content = elementsName.Substring(9);
                    Dictionary<string, string> headCorrespond = new Dictionary<string, string> { { "Name", "userName" }, { "Index", "mouseInd" } };
                    if (stringArg.StartsWith("passive")) {//format: passive

                    }
                    else {
                        if (headCorrespond.TryGetValue(_content, out string _info)) {
                            moving.SetMouseInfo(_info + ":" + stringArg);
                        }
                    }
                }
                else if (elementsName.StartsWith("OG") || elementsName.StartsWith("MS")) {
                    string _type = elementsName.Substring(0, 2);
                    string _content = elementsName.Substring(2);
                    if (_content == "Start") {
                        moving.trialStatus = -3;
                        if (!int.TryParse(inputFieldContent[$"{_type}Time"], out int _mills)) { _mills = _type == "OG" ? 10000 : 480; }
                        bool res = _type == "OG" ? moving.OGSet(_mills) : moving.MSSet(_mills);
                        // if(res){
                        //     SetButtonColor(buttons.Find(button => button.name == $"{_type}Start"), Color.green);
                        //     // alarm.TrySetAlarm("OGStartToGrey", 0.5f, out _);
                        // }
                    }
                    else if (_content == "Stop") {
                        bool res = _type == "OG" ? moving.OGSet(0) : moving.MSSet(0);
                        // if(res){
                        //     SetButtonColor(buttons.Find(button => button.name == $"{_type}Start"), setToDefault:true);
                        // }
                    }
                    else if (_content == "Enable") {
                        if (moving.DeviceEnableDict.TryGetValue(_type, out bool _enabled)) {
                            _enabled = !_enabled;
                            moving.DeviceEnableDict[_type] = _enabled;
                            if (_enabled) { }
                            // SetButtonColor(buttons.Find(button => button.name == $"{_type}Enable"), Color.green, !_enabled);
                            // SetButtonColor($"{_type}Enable", Color.green, !_enabled);

                        }
                        else {
                            moving.DeviceEnableDict.Add(_type, true);
                            // SetButtonColor(buttons.Find(button => button.name == $"{_type}Enable"), Color.green);
                        }
                        SetButtonColor($"{_type}Enable", Color.green, !_enabled);
                    }
                }
                else if (elementsName.StartsWith("TimingBaseSelect")) {
                    string[] stringArgSplit = stringArg.Split(";");
                    if(stringArgSplit[0] == "hide"){return;}

                    int _nowTimingId = stringArgSplit[0] == "type_dropdown"? (stringArgSplit.Count() > 2? int.Parse(stringArgSplit[2]): -1): (int)value;
                    var _t = timings.GetTiming(_nowTimingId);
                    if(_nowTimingId != -1 && !_t.HasValue){ Debug.Log($"invalid Id:{_nowTimingId}"); return; }
                    int hierarchy = _t.Value.hierarchy;

                    if (stringArg.StartsWith("delete")) {
                        ClearComingButtonTiming(_nowTimingId);
                        UpdateOptions(hierarchy);
                        if (createdTimingBaseSubDropdowns.Count > _nowTimingId - 1 && createdTimingBaseSubDropdowns[_nowTimingId - 1] != null) { Destroy(createdTimingBaseSubDropdowns[_nowTimingId - 1]); }
                    }
                    else if (stringArg.StartsWith("spread")) {
                        float[] spreadPos = (from pos in stringArgSplit[1].Split(':') select Convert.ToSingle(pos)).ToArray();
                        Dropdown timingBaseSelectDropdown = hierarchy == 0 ? TimingBaseDropdown : createdTimingBaseSubDropdowns.Last().GetComponent<Dropdown>();
                        if (createdTimingBaseSubDropdowns.Count == hierarchy) {
                            GameObject _buttonTimingBaseSubDropdown = Instantiate(pref_buttonTimingBaseSubDropdown);
                            _buttonTimingBaseSubDropdown.name = "TimingButtonsBaseSelectSubDropdown";
                            _buttonTimingBaseSubDropdown.transform.position = new Vector3(spreadPos[0] + 160, spreadPos[1], 0);
                            _buttonTimingBaseSubDropdown.transform.SetParent(timingBaseSelectDropdown.transform, true);
                            ScrDropDown _tempScrDropdown = _buttonTimingBaseSubDropdown.GetComponent<ScrDropDown>();

                            // _buttonTimingBaseSubDropdown.GetComponent<Dropdown>().AddOptions(timings[hierarchy + 1].Names());
                            if(TimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count == 0) {
                                _buttonTimingBaseSubDropdown.GetComponent<Dropdown>().value = 0;
                            }
                            else if (TimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count >= hierarchy) { _buttonTimingBaseSubDropdown.GetComponent<Dropdown>().value = TimingBaseScrDropdown.subSelectedIndexesExcludeNone[hierarchy - 1] + 1; }
                            _tempScrDropdown.ui_update = this;
                            _tempScrDropdown.nowSubHierarchyIndex = hierarchy + 1;
                            _tempScrDropdown.buttonFunctionsLs = buttonFunctionsLs;
                            // _tempScrDropdown.UpdateOptions();
                            _tempScrDropdown.UpdateOptions(timings[hierarchy + 1].Values(), timings[hierarchy + 1].Keys().Select(t => timings.GetTimingChildren(t).Count > 0 ? 1 : 0).ToList());
                            alarm.TrySetAlarm("showButtonTimingSubDropdown", 1, out _, addInfo: _tempScrDropdown.nowSubHierarchyIndex.ToString());
                            createdTimingBaseSubDropdowns.Add(_buttonTimingBaseSubDropdown);
                            TimingBaseScrDropdown.subSelectedIndexesExcludeNone.Add((int)(value - 1));

                        }
                    }
                    else if (stringArg.StartsWith("destroy")) {
                        foreach (GameObject _go in createdTimingBaseSubDropdowns) {
                            if (_go != null) {
                                Destroy(_go);
                            }
                        }
                        createdTimingBaseSubDropdowns.Clear();
                        TimingBaseDropdown.Hide();
                    }
                    else if (stringArg.StartsWith("hide")) {

                    }
                    else {
                        if (value == 0) {
                            TimingBaseScrDropdown.nowSelectedTimingId = -1;
                            TimingBaseScrDropdown.nowSelectedSubHierarchy = 0;
                            TimingBaseDropdown.value = 0;
                            // buttonTimingBaseDropdown.RefreshShownValue();
                            TimingBaseScrDropdown.UpdateCaptionText();

                        }
                        else {
                            TimingBaseScrDropdown.nowSelectedTimingId = _nowTimingId;
                            TimingBaseScrDropdown.nowSelectedSubHierarchy = hierarchy;
                            if (TimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count == 0) {
                                TimingBaseScrDropdown.subSelectedIndexesExcludeNone.Add((int)value - 1);
                            }
                            else if (TimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count > hierarchy - 1) {
                                TimingBaseScrDropdown.subSelectedIndexesExcludeNone[_nowTimingId - 1] = (int)(value - 1);
                                TimingBaseScrDropdown.subSelectedIndexesExcludeNone.RemoveRange(_nowTimingId, TimingBaseScrDropdown.subSelectedIndexesExcludeNone.Count - 1);
                            }//timing默认第一个为None
                            TimingBaseScrDropdown.UpdateCaptionText();
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
                // if(GetButtonColor(elementsName, out Color buttonColor)){
                //     SetButtonColor(elementsName, buttonColor != Color.green? Color.green: Color.green);
                // }
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
        dropdowns.Add(TimingBaseDropdown);
        soundOptionsDict = new Dictionary<int, UnityEngine.UI.Button>();
        logScrollBar.GetComponent<ScrScrollBar>().ui_update = this;
        TimingBaseScrDropdown = TimingBaseDropdown.GetComponent<ScrDropDown>();
        timings = new TimingCollection();

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
                    GameObject buttonTimingBaseSubDropdown = createdTimingBaseSubDropdowns[Convert.ToInt16(alarm.GetAlarmAddInfo("showButtonTimingSubDropdown")) - 1];
                    buttonTimingBaseSubDropdown.GetComponent<Dropdown>().Show();
                    buttonTimingBaseSubDropdown.transform.GetChild(3).gameObject.AddComponent<ScrDestroyParent>();

                    // buttonTimingBaseSubDropdown.GetComponent<ScrDropDown>().UpdateOptionsFunctionEnableStatus(0);
                    break;
                }
                default:{
                    if(alarmFinished.StartsWith("Timing")){
                        string timingElementName = alarmFinished[6..];
                        int TimingId = int.Parse(timingElementName.Split(';')[1]);
                        timingElementName = timingElementName.Split(';')[0];
                        Timing? _tempTiming = timings.GetTiming(TimingId);
                        if (_tempTiming.HasValue){
                            if(timings.Count > 0 && timings[0].ContainsKey(timingElementName)){
                                // string _tempTimingMethod = timings[0].GetTimingMethod(-1, timingButtonName).Trim(';');
                                foreach(Timing _timing in timings.GetTimingChildren(TimingId)){
                                    ControlsParse(_timing.name, 1, ignoreTiming:false, forceTiming:true, ignoreSecondTiming:true, timing: _timing);
                                }
                                timings.Remove(TimingId);
                            }
                            alarm.DeleteAlarm(alarmFinished);
                            UpdateOptions();

                            if(_tempTiming.Value.type == "button"){
                                UnityEngine.UI.Button button = buttons.Find(b => b.name == _tempTiming.Value.name);
                                button.GetComponent<ScrButton>().pressCount++;
                                SetButtonColor(timingElementName, setToPrevious:true, ignoreTiming:true);
                            }
                            ControlsParse(timingElementName, 1);
                            // ControlsParse("TimingButtonsBaseSelect", buttonTimingBaseDropdown.options.IndexOf(buttonTimingBaseDropdown.options.Where(o => o.text == timingButtonName).First()), stringArg:"delete;finish");
                        }
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
