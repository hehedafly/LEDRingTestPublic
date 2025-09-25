using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ScrDropDown : MonoBehaviour
{
    // Start is called before the first frame update
    public UIUpdate ui_update;
    // public bool isPassive = false;
    Dropdown dropdown = null;
    bool isShow = false; public bool IsShow{get{return isShow;} set{isShow = value;} }
    bool updated = false;

    // special for options function enable status

    public bool EnableOptionsFunction = false;
    public List<string> buttonFunctionsLs = null;//{"delete", "spread"}
    /// <summary>
    /// 0: new option with delete and no spread, 1: new option with delete and spread
    /// </summary>
    List<bool[]> certainButtonFunctionDefault = new List<bool[]>{new bool[]{true, false}, new bool[]{true, true}};

    public List<bool[]> functionEnableList = new List<bool[]>{};//bool[0]: delete, bool[1]:spread
    /// <summary>
    /// 存储每一个定时的按键名称及其存储其定时方式
    /// </summary>
    public List<string> OptionsNowHierarchy = new List<string>();
    Dictionary<string, string> buttonTimingBasedOnPreviousTimingDict = null;
    public int nowSelectedHierarchy = 0;
    public int nowSubHierarchyIndex = 0;
    public List<int> subSelectedIndexesExcludeNone = new List<int>();//index exclude None in default
    public string optionSelectedIncludeHigerHierarchy = "";
    public struct optionStruct{
        public ScrDropDown scrDropDown;
        public string optionName;
        public string optionIndetail;
        bool[] functionEnable;
        List<string> children;

        public optionStruct(ScrDropDown _scrDropDown, string _optionName, string _optionIndetail) {
            scrDropDown = _scrDropDown;
            optionName = _optionName;
            optionIndetail = _optionIndetail;
            children = new List<string>();
            functionEnable = new bool[scrDropDown.buttonFunctionsLs.Count];
            if(optionName != "None"){
                functionEnable[scrDropDown.buttonFunctionsLs.IndexOf("delete")] = true;
            }
        }
        public void UpdateChild(string childrenInDetail){
            children.Clear();
            foreach (var child in childrenInDetail.Trim(';').Split(';')){
                children.Add(child.Split(':')[0]); 
            }
            if (children.Count > 0){
                functionEnable[scrDropDown.buttonFunctionsLs.IndexOf("spread")] = true;
            }else{
                functionEnable[scrDropDown.buttonFunctionsLs.IndexOf("spread")] = true;
            }
            
        }
 
    }
    public void OnValueChanged(){

        if (ui_update != null)
        {
            int value = dropdown.value;
            string option = dropdown.options[value].text;
            ui_update.ControlsParsePublic(name, value, $"{option};{nowSubHierarchyIndex}");
        }
        if (EnableOptionsFunction && nowSubHierarchyIndex == 0)
        {//生成的子dropdown不需要更新captionText
            nowSelectedHierarchy = 0;
            optionSelectedIncludeHigerHierarchy = dropdown.options[dropdown.value].text;
            UpdateCaptionText();
        }
    }
    
    void UpdateOptions(List<string> timingMethods){
        UpdateOptions(dropdown.options.Select(option => option.text).ToList(), timingMethods);
    }

    public void UpdateOptions(List<string> options, List<string> timingMethods){

        OptionsNowHierarchy.Clear();
        OptionsNowHierarchy.AddRange(options);
        
    }
    
    /// <summary>
    /// call this function with buttonFunctions given after dropdown options edited, functionEnableList created from TimingBasedOnPreviousTimingDictsbgiven
    /// </summary>
    public void UpdateOptionsFunctionEnableStatus(Dictionary<string, string> _buttonTimingBasedOnPreviousTimingDict = null){

        buttonTimingBasedOnPreviousTimingDict = _buttonTimingBasedOnPreviousTimingDict != null?
                                                _buttonTimingBasedOnPreviousTimingDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value):
                                                buttonTimingBasedOnPreviousTimingDict == null? new Dictionary<string, string>(): buttonTimingBasedOnPreviousTimingDict;
        
        List<string> keys = buttonTimingBasedOnPreviousTimingDict.Keys.ToList();
        functionEnableList.Clear();
        foreach(string option in OptionsNowHierarchy){
            if (option == "None")
            {
                functionEnableList.Add(new bool[buttonFunctionsLs.Count]);
            }
            else
            {
                // string value = buttonTimingBasedOnPreviousTimingDicts[i][key];
                functionEnableList.Add(keys.Contains(option) ? certainButtonFunctionDefault[1] : certainButtonFunctionDefault[0]);
                keys.Remove(option);
            }
        }
        

        List<Transform> _optionLists = (from t in transform.GetComponentsInChildren<Transform>() where t.name == "Content" select t).ToList();
        if(_optionLists.Count == 0){return;}
        Transform optionList = _optionLists[0];
        List<Transform> options = (from Transform child in optionList where child.name.StartsWith("Item ") select child).ToList();
        for(int i = 0; i < options.Count; i ++){
            Transform option = options[i];
            List<ScrFunctionalButton> functionalButtons = option.GetComponentsInChildren<ScrFunctionalButton>(includeInactive:true).ToList();
            int[] functionalButtonsNow = (from func in functionalButtons select (func.gameObject.activeSelf? buttonFunctionsLs.IndexOf(func.Type): -1)).ToArray();
            for(int j = 0; j < buttonFunctionsLs.Count(); j++){
                if(functionEnableList[i][j] != functionalButtonsNow.Contains(j)){
                    functionalButtons.Where(fb => fb.Type == buttonFunctionsLs[j]).ToList()[0].gameObject.SetActive(functionEnableList[i][j]);
                    // Debug.Log(string.Join(",", (from fb in functionalButtons select (fb.name + fb.index.ToString() + fb.gameObject.activeSelf.ToString())).ToList()));

                }
            }
        }

    }

    public void UpdateCaptionText(){
        UpdateCaptionText(optionSelectedIncludeHigerHierarchy);
    }

    public void UpdateCaptionText(string captionText){
        if(captionText == null || captionText.Length == 0){return;}
        captionText = captionText[..Math.Min(12, captionText.Length)] + (captionText.Length > 12? "..": "");
        dropdown.captionText.text = captionText;
    }

    void Awake(){
        dropdown = GetComponent<Dropdown>();
    }

    void Start()
    {
        if(EnableOptionsFunction){
            Debug.Log("新建dorpdown start");
            if(ui_update != null){
                buttonFunctionsLs = ui_update.buttonFunctionsLs;
                optionSelectedIncludeHigerHierarchy = dropdown.options[dropdown.value].text;
            }
            if(transform.GetChild(2).gameObject.activeSelf){
                transform.GetChild(2).gameObject.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {   
        // IsShow = dropdown.IsShown;
        if(EnableOptionsFunction){
            // Debug.Log($"{name} ShowStatus: {isShow}; updated: {updated}");
            if(isShow && !updated){
                updated = true;
                // UpdateOptions();
                UpdateOptionsFunctionEnableStatus();
            }else if(! isShow && updated){
                updated = false;
                ui_update.ControlsParsePublic(name, dropdown.value, $"hide;{nowSubHierarchyIndex}");

            }
        }
        
    }

    void OnDestroy()
    {
        if(EnableOptionsFunction && ui_update != null){
            ui_update.ControlsParsePublic(name, dropdown.value, $"destroy;{nowSubHierarchyIndex}");
        }
    }
}
