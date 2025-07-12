using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrButton : MonoBehaviour
{
    // Start is called before the first frame update
    public UIUpdate ui_update;
    
    bool added = false;
    public bool isCheckBox = false;
    public Sprite checkBoxYes;
    public Sprite checkBoxNo;
    public int pressCount = 0;
    public void OnClick(){
        if(ui_update != null){
            if(!Input.GetKey(KeyCode.LeftControl) & !Input.GetKey(KeyCode.RightControl) & !Input.GetKey(KeyCode.LeftShift) & !Input.GetKey(KeyCode.RightShift)){
                pressCount ++;
            }
            // if(isCheckBox){ui_update.CheckBoxControlsParse(name, 1);}
            // else{ui_update.ControlsParse(name, 1);}
            ui_update.ControlsParse(name, 1, ignoreKeyboard:false);
        }
    }

    public void ChangeColor(Color color){
        GetComponent<Image>().color = color;
    }

    public void ChangeStatus(bool yesorno = false){
        if(yesorno){GetComponent<Image>().sprite = checkBoxYes;}
        else{GetComponent<Image>().sprite = checkBoxNo;}
    }

    void Awake(){
        if(ui_update != null){
            ui_update.AddSelf(this.GetComponent<UnityEngine.UI.Button>());
            added = true;
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!added && ui_update != null){
            if(ui_update.AddSelf(this.GetComponent<UnityEngine.UI.Button>()) == 0){
                added = true;
            }
        }
    }
}
