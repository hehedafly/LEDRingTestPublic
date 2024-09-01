using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrScrollBar : MonoBehaviour
{
    public UIUpdate ui_update;

    public void OnValueChanged(float value){
        ui_update.ControlsParse(name, value);
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Debug.Log(GetComponent<Scrollbar>().value);
    }
}
