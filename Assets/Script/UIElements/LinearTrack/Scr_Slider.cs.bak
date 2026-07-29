using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scr_Slider : MonoBehaviour
{
    private GameObject obj_main = null;
    private LinearTrackMoving moving;
    private Slider slider;
    private LinearTrackUIUpdate ui_update;
    private Text text_child;
    private float[] slider_value_rec;//[common_value, shift_value]
    // Start is called before the first frame update
    public void Slider_value_change(float value){
        if(slider==null || text_child==null){return;}

        value=Math.Min(slider.maxValue, Math.Max(slider.minValue, Convert.ToInt32(value*10)*0.1f));
        slider.value=value;

        if(Input.GetKey(KeyCode.LeftShift)){
            if(name=="SliderMaxSpd"){
                if(obj_main!=null){ui_update.Controls_parse(GetComponent<Transform>().name, value, "rolling");}
                text_child.text=(value*100*moving.scaleFactorRolling).ToString("0");
            }else if(name=="SliderScaleFactor"){
                if(obj_main!=null){ui_update.Controls_parse(GetComponent<Transform>().name, value, "rolling");}
                text_child.text=value.ToString("0");
            }else{
                if(obj_main!=null){ui_update.Controls_parse(GetComponent<Transform>().name, value);}
                text_child.text=value.ToString("0.0");
            }
        }else{
            if(obj_main!=null){ui_update.Controls_parse(GetComponent<Transform>().name, value);}
            text_child.text=value.ToString("0.0");
        }


    }
    void Start()
    {
        slider = GetComponent<Slider>();
        obj_main = GameObject.Find("obj_main");
        ui_update = obj_main.GetComponent<LinearTrackUIUpdate>();
        moving = obj_main.GetComponent<LinearTrackMoving>();
        slider_value_rec = new float[]{slider.value, slider.value};
        for(int i=0; i<GetComponent<Transform>().childCount; i++){
            Transform go = GetComponent<Transform>().GetChild(i);
            if(go.name=="Slider_value"){
                text_child = go.GetComponent<Text>();
                text_child.text = slider.value.ToString("0.0");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(KeyCode.LeftShift)){

        }
        else{
            text_child.text=slider.value.ToString("0.0");
        }
    }
}
