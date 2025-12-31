using System.Collections;
using System.Collections.Generic;
//using UnityEditor.UIElements;
using UnityEngine;

using System.IO;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class TableDraw : MonoBehaviour
{
    public GameObject prefabRow;
    private float width = 480;
    private float[] rowXOffset = new float[]{0, 120, 150, 180, 210};
    private float rowHeight = 30;
    private float nowRowY = 0;
    private TMP_InputField IFFocused;
    //private string ingoreHead = "#";
    private RectTransform rect_tf;
    private List<int> rowLevelList = new List<int>();
    private List<GameObject> rowGOList= new List<GameObject>();
    public int AddRow(int level, string content, string head, string addColor = "green"){
        GameObject tempGO = Instantiate(prefabRow);

        tempGO.transform.SetParent(transform);
        tempGO.GetComponent<RectTransform>().sizeDelta = new Vector2(width - rowXOffset[level], rowHeight);
        tempGO.transform.localPosition = new Vector3(rowXOffset[level], nowRowY*-1, 0);
        //tempGO.transform.SetParent(transform.parent);

        string annotate = "";
        string _color = "black";
        if(content.Contains(":")){
            annotate = content.Substring(0, content.IndexOf(":"));
            content  = content.Substring(content.IndexOf(":")+1);
            if(content.StartsWith("default")){
                _color = addColor;
            }
        }
        tempGO.GetComponent<RowContent>().Init(this, content, level, annotate, head, _color);

        rowLevelList.Add(level);
        rowGOList.Add(tempGO);
        nowRowY += rowHeight;
        if(nowRowY >= rect_tf.rect.height){
            rect_tf.sizeDelta = new Vector2(rect_tf.rect.width, rect_tf.rect.height+rowHeight);
        }
        return 1;
    }
    public int InsertRow(int insertPos, int level, string content, string head){
        if(insertPos < 0){return -1;}
        if(insertPos >= rowLevelList.Count){return AddRow(level, content, head);}

        for(int i = insertPos; i < rowLevelList.Count; i++){
            rowGOList[i].GetComponent<Transform>().position += new Vector3(0, rowHeight, 0);
        }

        GameObject tempGO = Instantiate(prefabRow);

        tempGO.transform.SetParent(transform);
        tempGO.GetComponent<RectTransform>().sizeDelta = new Vector2(width - rowXOffset[level], rowHeight);
        tempGO.transform.localPosition = new Vector3(rowXOffset[level], nowRowY*-1, 0);
        //tempGO.transform.SetParent(transform.parent);

        string annotate = "";
        if(content.Contains(":")){
            annotate = content.Substring(0, content.IndexOf(":"));
            content  = content.Substring(content.IndexOf(":")+1);
        }
        tempGO.GetComponent<RowContent>().Init(this, content, level, annotate, head);

        rowLevelList.Insert(insertPos, level);
        rowGOList.Insert(insertPos, tempGO);
        nowRowY += rowHeight;
        if(nowRowY >= rect_tf.rect.height){
            rect_tf.sizeDelta = new Vector2(rect_tf.rect.width, rect_tf.rect.height+rowHeight);
        }
        return 1;
    }

    public int DeleteRow(int deletePos){
        if(deletePos<=0 || deletePos > rowLevelList.Count){return -1;}
        if(deletePos == rowLevelList.Count-1){
            
        }

        int tempDeletedNum = 0;
        int deleteLevel = rowLevelList[deletePos];
        for(int i = deletePos; i < rowLevelList.Count; i++){
            if(rowLevelList[i] < deleteLevel){
                tempDeletedNum++;
                Destroy(rowGOList[i]);
            }else{
                break;
            }
        }
        rowLevelList.RemoveRange(deletePos, tempDeletedNum);
        rowGOList.RemoveRange(deletePos, tempDeletedNum);
        return 1;
    }

    public TMP_InputField CheckFocus(TMP_InputField inputField = null){
        if(inputField == null){
            TMP_InputField result = IFFocused;
            IFFocused = null;
            return result;
        }
        else{
            IFFocused = inputField;
        }
        return null;
    }

    public string ReturnContent(TMP_InputField inputField){//head:annotate:content
        if(inputField.GetComponent<RowContent>() != null){
            return inputField.GetComponent<RowContent>().GetContent(all:true);
        }
        return "";
    }

    // public int AddTest(int level){
    //     return AddRow(level, $"test level{level}, now at{nowRowY}", );
    // }

    void Awake()
    {
        rect_tf = GetComponent<RectTransform>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
