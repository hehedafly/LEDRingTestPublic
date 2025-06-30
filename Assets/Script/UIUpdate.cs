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
    public Scrollbar logScrollBar;
    int logWindowDraging = 0;//-1: another page, 0: no draging, 1: draging
    int logPage = 0;
    public List<InputField> inputFields = new List<InputField>();
    Dictionary<string, string> inputFieldContent = new Dictionary<string, string>();
    List<Dropdown> dropdowns = new List<Dropdown>();
    List<Dropdown> soundDropdowns = new List<Dropdown>();
    public List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();

    Moving moving;
    InputField focus_input_field;
    Alarm alarm;
    float manualWaitSec = 5;
    
    private int _FrameCount = 0; private float _TimeCount = 0; private float _FrameRate = 0;

    //public Image LineChartImage;
    public int AddSelf(UnityEngine.UI.Button button){
        if(buttons.Contains(button)){return 0;}

        buttons.Add(button);
        return 1;
    }

    public int SetButtonColor(string buttonName, Color color){
        foreach(UnityEngine.UI.Button button in buttons){
            if (button.name == buttonName){
                button.GetComponent<ScrButton>().ChangeColor(color);
                return 1;
            }
        }
        return -1;
    }
    void SetButtonColor(UnityEngine.UI.Button _button, Color color){
        if(_button == null){return;}
        _button.GetComponent<ScrButton>().ChangeColor(color);
    }
    
    /// <summary>
    /// for inputfield, stringArg is the content of IF, value is 0 if failed to parse to float
    /// </summary>
    /// <param name="elementsName"></param>
    /// <param name="value"></param>
    /// <param name="stringArg"></param>
    public void ControlsParse(string elementsName,float value, string stringArg=""){
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
                moving.Exit();
                break;
            }
            case "SliderPos":{
                moving.SetBarPos(moving.DegToPos(value));
                break;
            }
            case "IFSerialMessage":{
                string temp_str=serialMessageInputs.text;
                if(serialMessageInputs.text.StartsWith("//")){
                    moving.DataSendRaw(temp_str);
                }else{
                    if(moving.DataSend(temp_str, serialMessageInputs.text.StartsWith("/"), true)==-1){Debug.LogError("missing variable name: "+temp_str);}
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
                moving.DataSend("p_INDEBUGMODE=" + (moving.DebugMode? "0": "1"), true, true);
                moving.DebugMode = !moving.DebugMode;
                foreach(UnityEngine.UI.Button button in buttons){
                    if(button.name == "DebugButton"){
                        SetButtonColor(button, moving.DebugMode? Color.green: Color.grey);
                    }
                }
                break;
            }
            case "IPCRefreshButton":{
                if(alarm.GetAlarm("ipcRefresh") < 0 && moving.IsIPCInNeed()){
                    moving.Ipcclient.Silent = true;
                    moving.Ipcclient.Activated = false;
                    alarm.TrySetAlarm("ipcRefresh", 2.0f, out _);
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
                                    SetButtonColor(button, Color.gray);
                                    button.GetComponent<ScrButton>().pressCount = 0;
                                }
                            }else{//其他
                                if(moving.audioPlayModeNow.Contains(0)){
                                    moving.audioPlayModeNow.Remove(0);
                                    SetButtonColor(soundOptionsDict[0], Color.gray);
                                    soundOptionsDict[0].GetComponent<ScrButton>().pressCount ++;
                                }
                                if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)){
                                    if(moving.TrialSoundPlayModeExplain[tempId] != "InPos"){
                                        moving.PlaySoundPublic(tempId, "manual", true);
                                    }
                                }else{
                                    bool selected = soundOptionsDict[tempId].GetComponent<ScrButton>().pressCount % 2 == 1;
                                    int _result = moving.ChangeSoundPlayMode(tempId, selected? 1: 0, soundOptionsDict[tempId].GetComponentInChildren<Dropdown>().captionText.text);
                                    SetButtonColor(soundOptionsDict[tempId], selected? Color.green: Color.gray);
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
                        if(res){
                            SetButtonColor(buttons.Find(button => button.name == $"{_type}Start"), Color.green);
                            // alarm.TrySetAlarm("OGStartToGrey", 0.5f, out _);
                        }
                    }else if(_content == "Stop"){
                        bool res = _type == "OG"? moving.OGSet(0): moving.MSSet(0);
                        if(res){
                            SetButtonColor(buttons.Find(button => button.name == $"{_type}Start"), Color.grey);
                        }
                    }else if(_content == "Enable"){
                        if(moving.DeviceEnableDict.TryGetValue(_type, out bool _enabled)){
                            _enabled = ! _enabled;
                            moving.DeviceEnableDict[_type] = _enabled;
                            SetButtonColor(buttons.Find(button => button.name == $"{_type}Enable"), _enabled? Color.green: Color.grey);

                        }else{
                            moving.DeviceEnableDict.Add(_type, true);
                            SetButtonColor(buttons.Find(button => button.name == $"{_type}Enable"), Color.green);
                        }
                    }
                }
                // else if(elementsName.StartsWith("MS")){
                //     string _content = elementsName.Substring(2);
                //     if(_content == "Start"){
                //         if(!int.TryParse(inputFieldContent["MSTime"], out int _sec)){_sec = 1000;}
                //         if(moving.MSSet(_sec)){
                //             SetButtonColor(buttons.Find(button => button.name == "MSStart"), Color.green);
                //             // alarm.TrySetAlarm("MSStartToGrey", 0.5f, out _);
                //         }
                //     }else if(_content == "Stop"){
                //         if(moving.MSSet(0)){
                //             SetButtonColor(buttons.Find(button => button.name == "MSStart"), Color.grey);
                //         }
                //     }else if(_content == "Enable"){
                //         if(moving.DeviceEnableDict.TryGetValue("MS", out bool _enabled)){
                //             _enabled = ! _enabled;
                //             moving.DeviceEnableDict["MS"] = _enabled;
                //             SetButtonColor(buttons.Find(button => button.name == "MSEnable"), _enabled? Color.green: Color.grey);

                //         }else{
                //             moving.DeviceEnableDict.Add("MS", true);
                //             SetButtonColor(buttons.Find(button => button.name == "MSEnable"), Color.green);
                //         }
                //     }
                // }
                break;
            }
        }

        if(elementsName.StartsWith("LickSpout")){
            string Spout = elementsName.Substring(elementsName.Length-1);
            moving.CommandParsePublic($"lick:{Spout}:1");
        }else if (elementsName.StartsWith("WaterSpout")){
            string Spout = elementsName.Substring(elementsName.Length-1);
            moving.alarmPublic.TrySetAlarm($"sw={Spout}", _sec:0.2f, out _, elementsName.Contains("Single")? 0: 99);
        }
    }

    // public void CheckBoxControlsParse(string elementsName,float value, string stringArg=""){
    //     if(elementsName.StartsWith("MS")){
    //         string _name = elementsName.Substring(2);
    //         switch(_name){
    //             case "trialStart":{
    //                 break;
    //             }
    //             default:{
    //                 break;
    //             }
    //         }
            
    //     }else if(elementsName.StartsWith("OG")){
    //         string _name = elementsName.Substring(2);
    //         switch(_name){
    //             case "":{
    //                 break;
    //             }
    //             default:{
    //                 break;
    //             }
    //         }
    //     }
    // }

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
            if(logMessage.Contains(_time) && logMessage.Contains(_time+add_log_message)){return "";}
            
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
        soundOptionsDict = new Dictionary<int, UnityEngine.UI.Button>();
        logScrollBar.GetComponent<ScrScrollBar>().ui_update = this;

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
            if (focus_input_field.name=="IFSerialMessage" || focus_input_field.name=="IFConfigValue"){focus_input_field.text="";}
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
                case "ipcRefresh":{
                    moving.Ipcclient.Silent = false;
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
                default:{
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
