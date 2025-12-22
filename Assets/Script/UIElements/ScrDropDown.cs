using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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

    public List<bool[]> functionEnableList = new List<bool[]> { };//bool[0]: delete, bool[1]:spread
    public int noneOptionCount = 0;
    /// <summary>
    /// 存储每一个定时的按键名称及其存储其定时方式
    /// </summary>
    public Dictionary<int, string> optionsNowHierarchy = new Dictionary<int, string>();
    List<int> optionsPerHierarchyKeys = new List<int>();
    public int nowSelectedTimingId = -1;
    public int nowSelectedSubHierarchy = -1;
    public int nowSubHierarchyIndex = -1;
    public string optionSelectedIncludeHigerHierarchy = "";
    public bool ignoreValueChange = false;
    int previousValue = -1;

    public void OnValueChanged(){

        if (ui_update != null && !ignoreValueChange){
            int value = dropdown.value;
            string option = dropdown.options[value].text;
            
            if (EnableOptionsFunction){//生成的子dropdown不需要更新captionText
                optionSelectedIncludeHigerHierarchy = dropdown.options[dropdown.value].text;
                nowSelectedTimingId = optionsNowHierarchy.Keys.ToList()[dropdown.value];
                if(nowSubHierarchyIndex == 0) {
                    UpdateCaptionText();
                }
            }
            int timingSucceed = ui_update.ControlsParsePublic(name, value, $"type_dropdown;{option};{nowSelectedTimingId}", ignoreTiming: false);
            if (timingSucceed != 0) {
                ignoreValueChange = true;
                dropdown.value = previousValue;
                dropdown.RefreshShownValue();
                ignoreValueChange = false;
            }
            else {
                previousValue = value;
            }
        }
    }

    public int UpdateOptions(Dictionary<int, string> timingMethods = null, List<int> enableList = null, int selectId = -2, string selectText = "") {//每次提供全部列表
        
        dropdown.Hide();
        optionsNowHierarchy = optionsNowHierarchy.Where(o => o.Key == -1).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        functionEnableList = functionEnableList.Take(optionsNowHierarchy.Count).ToList();
        if (timingMethods is not null) {
            optionsNowHierarchy.AddRange(timingMethods);
            optionsPerHierarchyKeys = optionsNowHierarchy.Keys.ToList();

            functionEnableList.AddRange(enableList.Select(v => certainButtonFunctionDefault[v]).ToList());
            if (functionEnableList.Count != optionsNowHierarchy.Count) {
                Debug.Log("timingMethods count not equal to enableList");
                functionEnableList = Enumerable.Repeat(certainButtonFunctionDefault[0], optionsNowHierarchy.Count).ToList();
            }
            if (nowSelectedTimingId != -1 && !timingMethods.Keys.Contains(nowSelectedTimingId) && nowSubHierarchyIndex == nowSelectedSubHierarchy) {
                optionSelectedIncludeHigerHierarchy = optionsNowHierarchy.ContainsKey(-1) ? optionsNowHierarchy[-1] : "None";
                nowSelectedTimingId = -1;
                nowSelectedSubHierarchy = -1;
                UpdateCaptionText();
            }
        }
        else {
            optionSelectedIncludeHigerHierarchy = optionsNowHierarchy.ContainsKey(-1) ? optionsNowHierarchy[-1] : "None";
            nowSelectedTimingId = -1;
            nowSelectedSubHierarchy = -1;
            UpdateCaptionText();
        }
        ignoreValueChange = true;
        dropdown.ClearOptions();
        dropdown.AddOptions(optionsNowHierarchy.Values.ToList());
        if (optionsPerHierarchyKeys.Contains(nowSelectedTimingId) || optionsPerHierarchyKeys.Contains(selectId)) {
            int ind = optionsPerHierarchyKeys.Contains(selectId)? optionsPerHierarchyKeys.IndexOf(selectId) : optionsPerHierarchyKeys.IndexOf(nowSelectedTimingId);
            dropdown.value = ind;
            optionSelectedIncludeHigerHierarchy = dropdown.options[dropdown.value].text;
        }
        dropdown.RefreshShownValue();
        ignoreValueChange = false;
        optionSelectedIncludeHigerHierarchy = selectText == "" ? optionSelectedIncludeHigerHierarchy : selectText;
        UpdateCaptionText();
        // Debug.Log($"UpdateOptions, timing methods: {timingMethods.Count()}, now dropdown options: {string.Join(";" ,optionsNowHierarchy.Values.ToList())}, nowSelectedTimingId: {nowSelectedTimingId}, nowSelectedSubHierarchy: {nowSelectedSubHierarchy}, nowSubHierarchyIndex: {nowSubHierarchyIndex}, optionSelectedIncludeHigerHierarchy: {optionSelectedIncludeHigerHierarchy}, ");
        if(name == "TimingBaseSelect") {
            // Debug.Log($"nowSelectTimingId: {nowSelectedTimingId}, selectText: {optionSelectedIncludeHigerHierarchy}");
        }
        // UpdateOptionsFunctionEnableStatus();
        return 1;
    }
    
    /// <summary>
    /// call this function with buttonFunctions given after dropdown options edited, functionEnableList created from TimingBasedOnPreviousTimingDictsbgiven
    /// </summary>
    public void UpdateOptionsFunctionEnableStatus(){

        List<Transform> _optionLists = (from t in transform.GetComponentsInChildren<Transform>() where t.name == "Content" select t).ToList();
        if(_optionLists.Count == 0){return;}
        Transform optionList = _optionLists[0];
        List<Transform> options = (from Transform child in optionList where child.name.StartsWith("Item ") select child).ToList();
        for(int i = 0; i < options.Count; i ++){
            Transform option = options[i];
            List<ScrFunctionalButton> functionalButtons = option.GetComponentsInChildren<ScrFunctionalButton>(includeInactive:true).ToList();
            int[] functionalButtonsNow = (from func in functionalButtons select (func.gameObject.activeSelf? buttonFunctionsLs.IndexOf(func.Type): -1)).ToArray();
            for(int j = 0; j < buttonFunctionsLs.Count(); j++){
                var fb = functionalButtons.Where(fb => fb.Type == buttonFunctionsLs[j]).ToList()[0].gameObject;
                fb.SetActive(functionEnableList[i][j]);
                fb.GetComponent<ScrFunctionalButton>().Id = optionsPerHierarchyKeys[i];
                // Debug.Log($"Set {fb.name} (from {option.name}) Id: {optionsPerHierarchyKeys[i]}, now {fb.GetComponent<ScrFunctionalButton>().Id}");
                // Debug.Log(string.Join(",", (from fb in functionalButtons select (fb.name + fb.index.ToString() + fb.gameObject.activeSelf.ToString())).ToList()));
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
        if (EnableOptionsFunction) {
            optionSelectedIncludeHigerHierarchy = dropdown.options[dropdown.value].text;
            if(optionSelectedIncludeHigerHierarchy == "None") {
                optionsNowHierarchy.Add(-1, "None");
                functionEnableList.Add(new bool[2]);
                optionsPerHierarchyKeys.Add(-1);
                nowSelectedSubHierarchy = 0;
                noneOptionCount = 1;
            }
        }
    }

    void Start()
    {
        if(EnableOptionsFunction){
            // Debug.Log("新建dorpdown start");
            if(ui_update != null){
                buttonFunctionsLs = ui_update.buttonFunctionsLs;
                
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
        if (EnableOptionsFunction) {
            // Debug.Log($"{name} ShowStatus: {isShow}; updated: {updated}");
            if (isShow && !updated) {
                updated = true;
                // UpdateOptions();
                UpdateOptionsFunctionEnableStatus();
            }
            else if (!isShow && updated) {
                updated = false;
                ui_update.ControlsParsePublic(name, dropdown.value, $"hide;{nowSubHierarchyIndex}");

            }
        }
        if (ignoreValueChange) { ignoreValueChange = false; }
        
    }

    void OnDestroy()
    {
        if(EnableOptionsFunction && ui_update != null){
            ui_update.ControlsParsePublic(name, dropdown.value, $"destroy;{nowSubHierarchyIndex}");
        }
    }
}
