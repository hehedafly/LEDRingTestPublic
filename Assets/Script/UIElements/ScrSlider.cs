using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrSlider : MonoBehaviour
{
    // Start is called before the first frame update
    public UIUpdate ui_update;
    private Text valueText;

    public void OnValueChanged(float value){
        if(valueText != null){valueText.text = value.ToString();}
        if(ui_update != null){ui_update.ControlsParse(name, value);}
    }
    
    void Start()
    {
        foreach(Transform childTf in GetComponentInChildren<Transform>()){
            if(childTf.name == "SliderValue"){
                valueText = childTf.gameObject.GetComponent<Text>();
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

}
