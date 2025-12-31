using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

public class LinearTrackUIUpdate : MonoBehaviour
{
    public int LineChartWidth = 400;
    public int LineChartHeight = 300;
    public int[] thresholds = new int[]{20, 20, 20};
    private int[] thresholdsRec = new int[]{20, 20, 20};

    public List<UnityEngine.UI.Slider> sliders;
    public List<InputField> thresholds_inputs;
    public List<InputField> smooth_etc_vars_inputs; 
    public List<InputField> other_inputs;
    public Dropdown modeSelect;
    public Dropdown mode1ConfigDropdown;
    public Scrollbar log_scrollbar;
    public Text context_info;
    public Text log_message;
    public Image LineChartImage;
    public Image posIndicateImage;
    public Text reference_info;
    public GameObject[] posIndicateLabels;// greenlight, redlight, arrow
    public Text[] posIndicateTexts; // pos, start, end, rew start, rew end
    public LineChartMultiChannel lineChart;
    public PosIndicate positionIndicator;
    private InputField serialMessageInputs;
    private InputField mode1ConfigInputs;
    private LinearTrackMoving linearTrackMoving;
    private Position_control position_control;
    private InputField focus_input_field = null;
    
    public void Controls_parse(string controls_name,float value, string string_arg=""){
        //if(string_arg==""){return;}
        switch (controls_name){
            case "SliderMaxSpd":{
                if(string_arg == "rolling"){linearTrackMoving.maxRolling_value=Math.Max(thresholds[0], (int)(value*100*linearTrackMoving.scaleFactorRolling));}
                else{linearTrackMoving.maxSpeed = value;}
                break;
            }
            case "SliderScaleFactor":{
                if(string_arg == "rolling"){linearTrackMoving.scaleFactorRolling = value;}
                linearTrackMoving.scaleFactor = value;
                break;
            }
            case "IFThreshold_x":{
                thresholds[0] = Convert.ToInt32(value);
                //position_control.dic_water_serving_speed_threshold = Convert.ToInt32(value);
                if(value >= linearTrackMoving.maxRolling_value){linearTrackMoving.maxRolling_value = (int)value+100;}
                reference_info.text=value.ToString();
                break;
            }
            case "IFThreshold_y":{
                thresholds[1] = Convert.ToInt32(value);
                break;
            }
            case "IFThreshold_z":{
                thresholds[2] = Convert.ToInt32(value);
                break;
            }
            case "IFSmooth":{
                linearTrackMoving.sensor_smooth_interval = Convert.ToInt32(value);
                break;
            }
            case "IFSensorPause":{
                linearTrackMoving.sensor_pause_interval = value;
                break;
            }
            case "IFSerialMessage":{
                string temp_str=serialMessageInputs.text;
                if(linearTrackMoving.DataSend(temp_str, serialMessageInputs.text.StartsWith("/"))==-1){Debug.LogError("missing variable name: "+temp_str);}
                break;
            }
            case "ModeSelect":{
                while(linearTrackMoving.Context_verify("p_lick_mode", Convert.ToInt32(value>1? 1: value), "p_lick_mode1_delay", -1) == -1){Debug.LogError("mode sync failed");continue;}
                // string temp_str=$"p_lick_mode={Convert.ToInt32(value)}";
                // if(linearTrackMoving.DataSend(temp_str, true)==-1){Debug.LogError("missing variable name: "+temp_str);}

                if(value>0 && (int)value != position_control.serve_water_mode){
                    mode1ConfigDropdown.ClearOptions();

                    int dic_size = Convert.ToInt32(position_control.Get_set_dic_water_serving((int)value, key_text: out string temp_text)[0]);
                    List<string> keys_arr = new List<string>();
                    for(int i=0; i<dic_size; i++){
                        _ =position_control.Get_set_dic_water_serving((int)value, out temp_text, index:i);
                        keys_arr.Add(temp_text);
                    }
                    mode1ConfigDropdown.AddOptions(keys_arr);
                    float temp_value=position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, Convert.ToInt32(value))[1];
                    mode1ConfigInputs.placeholder.GetComponent<Text>().text=temp_value.ToString();
                }
                else{
                    mode1ConfigDropdown.ClearOptions();
                }

                position_control.serve_water_mode = Convert.ToInt32(value);
                position_control.Trial_args_init(false);//更换mode时清除无用信息
                modeSelect.value = Convert.ToInt32(value);
                modeSelect.RefreshShownValue();
                
                MessageUpdate($"mode now: {value}\n");
                break;
            }
            case "Mode1Config":{
                float temp_value = position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, position_control.serve_water_mode)[1];
                mode1ConfigInputs.placeholder.GetComponent<Text>().text = temp_value.ToString();
                break;
            }
            case "IFConfigValue":{
                if(position_control.serve_water_mode == 0){break;}

                float temp_value = position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, position_control.serve_water_mode)[1];
                position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, value, position_control.serve_water_mode);
                mode1ConfigInputs.placeholder.GetComponent<Text>().text = value.ToString();
                MessageUpdate($"config changed:{mode1ConfigDropdown.captionText.text} from {temp_value} to {value}\n");
                break;
            }
            default:break;
        }
    }

    public void MessageUpdate(string add_log_message="", bool _pauseChange = false, bool _pauseMoving = false){
        if(add_log_message!=""){
            log_message.text += DateTime.Now.ToString("HH:mm:ss ")+add_log_message;
        }else{
            bool isPausingBefore = context_info.text.Contains("paused");
            if(!_pauseChange){_pauseMoving = isPausingBefore;}

            string  temp_context_info =  $"trail:{position_control.Now_trial}        now context:{position_control.NowContext}  {(position_control.Trial_syncing? "syncing" : "")}  {(_pauseMoving? "paused" : "")}\n";
                    temp_context_info += $"lick:     pre--{position_control.lick_count_rec[0]}        in--{position_control.lick_count_rec[1]}        after--{position_control.lick_count_rec[2]}\n";
                    temp_context_info += $"lick_correct:{position_control.lick_count_correct}        lick_threshold: min {position_control.lick_count_succes_threshold[0]};max {position_control.lick_count_succes_threshold[1]}";
            context_info.text=temp_context_info; 
        }
    }

    public void PositionIndicateUpdate(float[] _contextInfo){//update all
        positionIndicator.PositionIndicateUpdate(_contextInfo);
    }

    public void PositionIndicateUpdate(float relative_pos){//update position only
        positionIndicator.PositionIndicateUpdate(relative_pos);
    }

    public void LickIndicateUpdate(float relative_pos){
        positionIndicator.LickIndicate(relative_pos);
    }

    public void WaterServeIndicateUpdate(float relative_pos){
        positionIndicator.WaterServeIndicate(relative_pos);
    }

    void Awake()
    {
        linearTrackMoving=GetComponent<LinearTrackMoving>();
        position_control=GetComponent<Position_control>();
        foreach(UnityEngine.UI.Slider slider in sliders){
            switch(slider.name){
                case "SliderMaxSpd":{
                    slider.value=linearTrackMoving.maxSpeed;
                    break;
                }
                case "SliderScaleFactor":{
                    slider.value=linearTrackMoving.scaleFactor;
                    break;
                }
            }
        }

        foreach(InputField inputField in other_inputs){
            if (inputField.name=="IFSerialMessage"){serialMessageInputs=inputField;}
            else if (inputField.name=="IFConfigValue"){
                mode1ConfigInputs = inputField;
                mode1ConfigInputs.placeholder.GetComponent<Text>().text = position_control.Get_set_dic_water_serving(mode1ConfigDropdown.captionText.text, position_control.serve_water_mode)[1].ToString();
            }
        }

        lineChart = new LineChartMultiChannel(LineChartWidth, LineChartHeight, LineChartImage, linearTrackMoving, this);
        lineChart.Init();

        positionIndicator = new PosIndicate((int)posIndicateImage.GetComponent<RectTransform>().rect.width, (int)posIndicateImage.GetComponent<RectTransform>().rect.height, posIndicateImage, position_control, this);
        positionIndicator.Init();
    }

    // Update is called once per frame
    void Update()
    {
        if(!Enumerable.SequenceEqual(thresholdsRec, thresholds)){
            Array.Copy(thresholds, thresholdsRec, thresholds.Length);
            lineChart.Reference_line_Update(thresholds);
        }

        foreach(InputField inputField in thresholds_inputs){
            if (inputField.isFocused == true){
                focus_input_field=inputField;
            }
        }
        foreach(InputField inputField in smooth_etc_vars_inputs){
            if (inputField.isFocused == true){
                focus_input_field=inputField;
            }
        }
        foreach(InputField inputField in other_inputs){
            if (inputField.isFocused == true){
                focus_input_field=inputField;
            }
        }

        if(focus_input_field!=null && Input.GetKeyDown(KeyCode.Return)){
            Controls_parse(focus_input_field.name, float.TryParse(focus_input_field.text, out float temp_value) ? temp_value : 0, focus_input_field.text);
            if (focus_input_field.name=="IFSerialMessage" || focus_input_field.name=="IFConfigValue"){focus_input_field.text="";}
        }

        MessageUpdate();
    }
}
