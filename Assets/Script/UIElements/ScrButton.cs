using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Color defaultColor;
    List<Color> previousColor;
    public void OnClick(){
        if(ui_update != null){
            if(!Input.GetKey(KeyCode.LeftControl) & !Input.GetKey(KeyCode.RightControl) & !Input.GetKey(KeyCode.LeftShift) & !Input.GetKey(KeyCode.RightShift)){
                pressCount++;
            }
            // if(isCheckBox){ui_update.CheckBoxControlsParse(name, 1);}
            // else{ui_update.ControlsParse(name, 1);}
            ui_update.ControlsParsePublic(name, 1, stringArg:"type_button", ignoreTiming:false);
        }
    }

    public void ChangeColor(Color color, bool setToDefault = false, bool setToPrevious = false, bool forcePreviousUpdate = false){
        if(setToDefault && setToPrevious){Debug.Log("Error: Both setToDefault and setToPrevious are true");return;}

        if(setToPrevious){
            if(previousColor.Count > 1){
                GetComponent<Image>().color = previousColor.Last();
                previousColor.RemoveAt(previousColor.Count - 1);
            }else{
                //不变
            }
            return;
        }else{
            if(previousColor.Last() != GetComponent<Image>().color || forcePreviousUpdate){
                previousColor.Add(GetComponent<Image>().color);
                // Debug.Log($"previous color changed to {previousColor}");
            }
        }
        if(setToDefault){GetComponent<Image>().color = defaultColor;previousColor = new List<Color>{defaultColor};return;}
        GetComponent<Image>().color = color;
        // Debug.Log($"color changed to {color}");
    }

    public void ChangeStatus(bool yesorno = false){
        if(yesorno){GetComponent<Image>().sprite = checkBoxYes;}
        else{GetComponent<Image>().sprite = checkBoxNo;}
    }

    void Awake(){
        if(ui_update != null){
            ui_update.ButtonAddSelf(this.GetComponent<UnityEngine.UI.Button>());
            added = true;
        }
        defaultColor = GetComponent<Image>().color;
        previousColor = new List<Color>{defaultColor};
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!added && ui_update != null){
            if(ui_update.ButtonAddSelf(this.GetComponent<UnityEngine.UI.Button>()) == 0){
                added = true;
            }
        }
    }
}
