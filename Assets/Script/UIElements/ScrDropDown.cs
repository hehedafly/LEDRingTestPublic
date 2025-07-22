using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrDropDown : MonoBehaviour
{
    // Start is called before the first frame update
    public UIUpdate ui_update;
    public bool isPassive = false;
    public void OnValueChanged(){
        if(isPassive){
            isPassive = false;
            return;
        }
        if(ui_update != null){ui_update.ControlsParsePublic(name, GetComponent<Dropdown>().value, GetComponent<Dropdown>().captionText.text);}
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
