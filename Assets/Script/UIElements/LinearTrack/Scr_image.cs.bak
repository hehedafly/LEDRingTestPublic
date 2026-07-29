using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scr_image : MonoBehaviour
{
    // Start is called before the first frame update
    public Text info_text;
    private GameObject obj_main;

    public void set_info_text(string info){
        info_text.text = info;
    }
    void Start()
    {
        obj_main = GameObject.Find("obj_main");
        if(name == "ImgLineChart"){set_info_text(obj_main.GetComponent<LinearTrackUIUpdate>().thresholds[0].ToString());}
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
