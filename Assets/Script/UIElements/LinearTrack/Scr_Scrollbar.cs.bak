using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class Scr_Scrollbar : MonoBehaviour
{
    public ScrollRect _ScrollRect;
    //private LinearTrackUIUpdate ui_update;
    private Scrollbar scrollbar;
    private GameObject obj_main;
    private bool scrollbar_change_manual=false;
    public bool Value_change_manual {get{return scrollbar_change_manual;} set{scrollbar_change_manual=value;}}
    // Start is called before the first frame update
    // public void Change_Manual(bool DownOrExit){
    //     Debug.Log(DownOrExit);
    // }
    public void log_scrollbar_value_change(float value){
        if(!scrollbar_change_manual){
            Canvas.ForceUpdateCanvases();
            //_ScrollRect.content.GetComponent<VerticalLayoutGroup>().CalculateLayoutInputVertical();
            // _ScrollRect.content.GetComponent<ContentSizeFitter>().SetLayoutVertical();
            // _ScrollRect.verticalNormalizedPosition = 0;
            //scrollbar.value = 0;
            // if(value==0.0f){
            //     scrollbar_change_manual=false;
            // }
            // else{
            //     scrollbar_change_manual=true;
            // }
        }
    }
    public void OnDrag(){
        //Debug.Log(Input.mousePosition.y);
        float temp_value=(Input.mousePosition.y-320) / 150;
        scrollbar.value=temp_value;
        if(scrollbar.value<=0.0f){
            scrollbar_change_manual=false;
        }
        else{
            scrollbar_change_manual=true;
        }
    }

    void Start()
    {
        scrollbar=GetComponent<Scrollbar>();
        obj_main = GameObject.Find("obj_main");
        //ui_update=obj_main.GetComponent<LinearTrackUIUpdate>();
    }

    // Update is called once per frame
    void Update()
    {

    }

}
