using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrButton : MonoBehaviour
{
    // Start is called before the first frame update
    public UIUpdate ui_update;
    
    bool added = false;
    public int pressCount = 0;
    public void OnClick(){
        if(ui_update != null){
            pressCount ++;
            ui_update.ControlsParse(name, 1);
        }
    }

    public void ChangeColor(Color color){
        GetComponent<Image>().color = color;
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
