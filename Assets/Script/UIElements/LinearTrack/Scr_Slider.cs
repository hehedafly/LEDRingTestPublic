using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scr_Slider : MonoBehaviour
{
    [SerializeField] public LinearTrackUIUpdate ui_update;
    private Slider slider;
    private Text text_child;
    // Start is called before the first frame update
    public void Slider_value_change(float value){
        if(slider==null){return;}

        value=Math.Min(slider.maxValue, Math.Max(slider.minValue, Convert.ToInt32(value*10)*0.1f));
        slider.value=value;

        if(ui_update==null){return;}
        bool shift = Input.GetKey(KeyCode.LeftShift);
        bool special = (name=="SliderMaxSpd" || name=="SliderScaleFactor");
        //组件只负责转发：shift+特殊滑块时传 rolling，其余情况交由中央 Controls_parse 统一处理
        ui_update.ControlsParsePublic(name, value, (shift && special) ? "rolling" : "");

        if(text_child!=null){
            if(shift && name=="SliderMaxSpd"){
                text_child.text=(value*100*ui_update.Moving.scaleFactorRolling).ToString("0");
            }else if(shift && name=="SliderScaleFactor"){
                text_child.text=value.ToString("0");
            }else{
                text_child.text=value.ToString("0.0");
            }
        }
    }
    void Start()
    {
        slider = GetComponent<Slider>();
        if(ui_update==null){ GameObject o = GameObject.Find("obj_main"); if(o!=null){ ui_update = o.GetComponent<LinearTrackUIUpdate>(); } }
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
            if(text_child!=null){text_child.text=slider.value.ToString("0.0");}
        }
    }
}
