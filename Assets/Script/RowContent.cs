using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class RowContent : MonoBehaviour
{
    public string content {get{return GetContent();} set{SetContent(value);}}
    public int level = -1;
    private string head;
    public string annotate = "";
    public bool isFocused = false;
    private TableDraw manager;
    private TMP_InputField inputField;
    private TMP_Text childText;
    private TMP_Text childAnnotate;
    private Transform secondTf;

    public string GetContent(){
        return childAnnotate.text +":" +inputField.text;
    }

    public string GetContent(bool all){
        if(!all){return GetContent();}
        return head+ ":" + GetContent();
    }

    public void SetContent(string _content, string color = "black"){
        inputField.text = _content;
        ColorUtility.TryParseHtmlString(color, out Color _color);
        childText.color = _color;
    }

    public void Init(TableDraw _manager, string _content, int _level, string _annotate, string _head, string _color = "black"){
        manager = _manager;
        content = _content;
        level = _level;
        annotate = _annotate;
        head = _head;
        childAnnotate.text = annotate;
        ChangeColor(_color);
    }

    public void ChangeColor(string color){
        ColorUtility.TryParseHtmlString(color, out Color _color);
        childText.color = _color;
    }

    void Awake(){
        inputField = gameObject.GetComponent<TMP_InputField>();
        secondTf = transform.GetChild(0).GetComponent<Transform>();
        for(int i = 0; i<secondTf.childCount; i++){
            GameObject go = secondTf.GetChild(i).gameObject;
            string temp_name = go.name;
            if(temp_name == "Text"){
                childText = go.GetComponent<TMP_Text>();
            }
            else if(temp_name == "Placeholder"){
                go.GetComponent<TMP_Text>().text = "";
            }
        }

        for(int i = 0; i<transform.childCount; i++){
            GameObject go = transform.GetChild(i).gameObject;
            string temp_name = go.name;
            if(temp_name == "Annotation"){
                childAnnotate = go.GetComponent<TMP_Text>();
            }
        } 
    }

    void Start()
    {
        if(level == 0){
            childText.fontStyle = TMPro.FontStyles.Bold;
            childText.fontSize += 2;
        }
    }

    // Update is called once per frame
    void Update()
    {
        isFocused = inputField.isFocused;
        if(isFocused){
            manager.CheckFocus(inputField);
        }
    }
}
