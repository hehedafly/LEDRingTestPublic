using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    public Text TexContextHighFreqInfo;
    public Text TexContextFixInfo;
    public Text TexContextInfo;
    public Text logMessage;
    public Scrollbar logScrollBar;
    List<InputField> inputFields = new List<InputField>();
    List<Dropdown> dropdowns = new List<Dropdown>();
    public List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();

    Moving moving;
    InputField focus_input_field;
    Alarm alarm;
    float manualWaitSec = 5;

    //public Image LineChartImage;
    
    public void ControlsParse(string controls_name,float value, string stringArg=""){
        //if(string_arg==""){return;}
        switch (controls_name){
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
                moving.LickResultCheckPubic(lickInd: -2);
                break;
            }
            case "SkipButton":{
                moving.LickResultCheckPubic(lickInd: -1);
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
                        MessageUpdate($"Trigger mode now: {moving.TrialStartTriggerMode}\n");
                        triggerModeSelect.value = moving.TrialStartTriggerMode;
                        triggerModeSelect.RefreshShownValue();
                    }else{}
                }
                triggerModeSelect.RefreshShownValue();
                
                break;
            }

            case "IFConfigValue":{
                // if(position_control.serve_water_mode == 0){break;}

                // float temp_value = position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, position_control.serve_water_mode)[1];
                // position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, value, position_control.serve_water_mode);
                // mode1ConfigInputs.placeholder.GetComponent<Text>().text = value.ToString();
                // MessageUpdate($"config changed:{mode1ConfigDropdown.captionText.text} from {temp_value} to {value}\n");
                break;
            }
            case "LickPort0":{
                moving.CommandParsePublic($"lick:0:{moving.NowTrial}");
                break;
            }
            case "LickPort1":{
                moving.CommandParsePublic($"lick:1:{moving.NowTrial}");
                break;
            }
            case "LickPort2":{
                moving.CommandParsePublic($"lick:2:{moving.NowTrial}");
                break;
            }
            case "LickPort3":{
                moving.CommandParsePublic($"lick:3:{moving.NowTrial}");
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
                        button.GetComponent<Image>().color = moving.DebugMode? Color.green: Color.grey;
                    }
                }
                break;
            }
            default:break;
        }
    }

    public void MessageUpdate(string add_log_message="", int UpdateFreq = 0){//随时可能被调用，需要对内容做null检查
        if(UpdateFreq == 1){//高频
            if(moving.TrialInitTime != 0){
                int time = (int)(Time.fixedUnscaledTime - moving.TrialInitTime);
                int hour = time / 3600;
                int minute = (time - hour*3600) / 60;
                int second = time % 60;
                TexContextHighFreqInfo.text = $"{hour:D2}:{minute:D2}:{second:D2}";
            }
        }else if(UpdateFreq == -1){//fix
            TexContextFixInfo.text = add_log_message;
        }else{
            if(add_log_message!=""){
                if(logMessage.text.Length > 8000){
                    //moving.WriteInfo(enqueueMsg: log_message.text);
                    moving.LogList.Add(logMessage.text);
                    logMessage.text = "";
                }

                string _time = DateTime.Now.ToString("HH:mm:ss ");
                if(logMessage.text.Contains(_time) && logMessage.text.Contains(_time+add_log_message)){
                    return;
                }else{
                    logMessage.text += _time + add_log_message + (add_log_message.EndsWith("\n")? "": "\n");
                }

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
                "lickCountArrayLength"
                "lickCount"0,1,2...
                "TrialSuccessNum"0,1,2...
                "TrialFailNum"0,1,2...
                "LickPortTotalTrial"0,1,2...
                */
                Dictionary<string, int> tempStatus = moving.GetTrialInfo();
                if(tempStatus["NowTrial"] == -1){
                    return;
                }

                string  temp_context_info =  $"trial:{tempStatus["NowTrial"]}     now pos:{tempStatus["NowPos"]}    {(tempStatus["IsPausing"] == 1? "paused" : "")}\n";
                        temp_context_info += "lick count in this trial: ";
                        if(tempStatus["lickCountArrayLength"] > 0){
                            for(int i =0; i < tempStatus["lickPosCount"]; i ++){
                                temp_context_info += $"{tempStatus["lickCount" + i.ToString()]}, ";
                            }
                        }
                        if(tempStatus["waitSec"] != -1){
                            temp_context_info += $"interval now: ~{tempStatus["waitSec"]}";
                        }

                        temp_context_info += $"\n          Success:    Fail:    Total:    Miss:\n";
                        for(int i =0; i < tempStatus["lickPosCount"]; i ++){
                            temp_context_info += $"LickPort{i}: {tempStatus["TrialSuccessNum" + i.ToString()]}          {tempStatus["TrialFailNum" + i.ToString()]}           {tempStatus["LickPortTotalTrial" + i.ToString()]}           {tempStatus["TrialMissNum" + i.ToString()]}\n";
                        }
                        temp_context_info += $"Total: {tempStatus["TrialSuccessNum"]}        {tempStatus["TrialFailNum"]}\n";
                        
                        if(tempStatus["NowTrial"] > 0){
                            float tempAccuracy = tempStatus["TrialSuccessNum"] / (float)(tempStatus["TrialSuccessNum"]+tempStatus["TrialFailNum"]) * 100;
                            temp_context_info += $"Accuracy: {tempAccuracy:f2}%\n";
                        }



                TexContextInfo.text=temp_context_info; 
            }
        }
    }

    void Awake()
    {
        moving = GetComponent<Moving>();
        inputFields.Add(serialMessageInputs);
        buttons.Add(startButton);
        dropdowns.Add(modeSelect);
        dropdowns.Add(triggerModeSelect);
        logScrollBar.GetComponent<ScrScrollBar>().ui_update = this;
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

        alarm = new Alarm();
        alarm.TrySetAlarm("manualScrollWait", -1, out _);
        // foreach(InputField inputField in other_inputs){
        //     if (inputField.name=="IFSerialMessage"){serialMessageInputs=inputField;}
        //     else if (inputField.name=="IFConfigValue"){
        //         mode1ConfigInputs = inputField;
        //         mode1ConfigInputs.placeholder.GetComponent<Text>().text = position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, position_control.serve_water_mode)[1].ToString();
        //     }
        // }
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
            ControlsParse(focus_input_field.name, float.TryParse(focus_input_field.text, out float temp_value) ? temp_value : 0, focus_input_field.text);
            if (focus_input_field.name=="IFSerialMessage" || focus_input_field.name=="IFConfigValue"){focus_input_field.text="";}
        }
        // MessageUpdate();
    }

    void FixedUpdate() {
        alarm.AlarmFixUpdate();
        // switch(alarm.GetAlarmFinish()){
        //     case "setBarToZeroAfterSizeChange":{
        //         ControlsParse("logScroll", 0, "passive");
        //         break;
        //     }
        //     default:break;
        // }
        if(alarm.GetAlarm("setBarToZeroAfterSizeChange") >= 0){
            logScrollBar.value = 0;
        }
        //Debug.Log(logScrollBar.value);
    }
}
