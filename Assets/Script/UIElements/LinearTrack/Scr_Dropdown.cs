using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scr_Dropdown : MonoBehaviour
{
    private GameObject obj_main = null;
    private Dropdown dropdown;
    // Start is called before the first frame update
    public void Dropdown_value_change(int value){
        if(dropdown==null){return;}
        if(obj_main!=null){obj_main.GetComponent<LinearTrackUIUpdate>().Controls_parse(name, value);}
    }
    void Start()
    {
        dropdown=GetComponent<Dropdown>();
        obj_main = GameObject.Find("obj_main");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
