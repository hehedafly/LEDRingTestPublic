using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ScrFunctionalButton : MonoBehaviour
{
    // Start is called before the first frame update
    public UIUpdate ui_update;
    public string Type = "delete";
    Transform parentItem;
    Transform parentDropdown;
    ScrDropDown parentScrDropDown;
    public string parentDropdownName;
    public int Id = -1;
    public bool preActiveStatus = false;
    // bool added = false;
    public void OnClick(){
        if(ui_update != null){
            // if(isCheckBox){ui_update.CheckBoxControlsParse(name, 1);}
            // else{ui_update.ControlsParse(name, 1);}
            Debug.Log($"{Type} from {parentItem.name} id is {Id}");
            ui_update.ControlsParsePublic(parentDropdownName, Id, ignoreTiming:false, stringArg:$"{Type};{parentItem.position.x}:{parentItem.position.y}");
        }
    }

    void Awake(){
    }

    void Start()
    {
        parentItem = this.transform.parent;
        if(parentItem.name.StartsWith("Item ")){
            string _name = parentItem.name;
            _name = _name[5.._name.IndexOf(":")];
            int.TryParse(_name, out int index);
        }
        parentDropdown = this.transform.parent.parent.parent.parent.parent;
        // Debug.Log($"{this.transform.parent.name}/{this.transform.parent.parent.name}/{this.transform.parent.parent.parent.name}/{parentDropdown.name}");
        parentScrDropDown = parentDropdown.GetComponent<ScrDropDown>();
        ui_update = parentScrDropDown.ui_update;
        parentDropdownName = parentDropdown.name;
        // Debug.Log($"{parentItem.name} position: {parentItem.position}");
        // if(parentDropdownName == "TimingButtonsBaseSelect"){
        //     if(index == 0){
        //         Destroy(this.gameObject);
        //     }
        // }
        // Debug.Log(parentDropdownName + " " + index);
    }

    // Update is called once per frame
    void Update()
    {
        if(preActiveStatus != this.gameObject.activeSelf){
            preActiveStatus = this.gameObject.activeSelf;
            parentScrDropDown.IsShow = preActiveStatus;
            // Debug.Log($"{parentScrDropDown.name} show is changed by type:{Type}, index:{index}");
        }
    }

    void OnDestroy(){
        if(parentScrDropDown != null){
            parentScrDropDown.IsShow=false;
        }
    }
}
