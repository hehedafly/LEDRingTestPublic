using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

#region class for save and load
[System.Serializable]
class ConfigBody{
    public ConfigBody(string _sectionContent, IniReader defaultINIReader){
        List<string> _sectionContentLs = _sectionContent.Split('\n').ToList();
        SectionName = _sectionContentLs[0];
        _sectionContentLs.RemoveAt(0);

        foreach(string _content in _sectionContentLs){
            string _head = _content.Split("=")[0];
            string _body = _content.Split("=")[1];
            configContent.Add(_head, _body);
            configDefaultValues.Add(_head, defaultINIReader.ReadIniContent(SectionName, _head));
        }
    }

    public string SectionName;
    IniReader iniReader;
    Dictionary<string, string> configContent = new Dictionary<string, string>();
    Dictionary<string, string> configDefaultValues = new Dictionary<string, string>();

    public bool isBlank(){
        return configContent.Count > 0;
    }
    public int ChangeContent(string newContent){
        string _content_head = newContent.Split(":")[0];
        string _content_body = newContent.Split(":")[1];
        if(configContent.ContainsKey(_content_head)){
            configContent[_content_head] = _content_body;
            return 1;
        }else{
            return 0;
        }

    }   

    public string PrintContent(){
        string result = "";//"context_name"       + ":" + context_name      +";"+
        foreach(string key in configContent.Keys){
            result += key + ":" + configContent[key]+";";
        }
        return result;
    }

    public List<string> ReturnContent(){
        List<string> result = new List<string>();
        foreach(string key in configContent.Keys){
            result.Add(key + ":" + configContent[key]);
        }
        return result;
    }      

    public int INIWriteContent(IniReader iniReader){//写入ini的键名称和代码中变量名不同
        foreach(string key in configContent.Keys){
            iniReader.WriteIniContent(SectionName, key, configContent[key] != "" ? configContent[key]: configDefaultValues[key]);
        }
        
        return 1;
    }

    // private string ignoreDefaultLable(string value){
    //     if(value.StartsWith("default:")){
    //         return value[(value.IndexOf("default:") + 1)..];
    //     }
    //     else{return value;}
    // }
}

[System.Serializable]
class Config{

    public Config(){
        configs = new List<ConfigBody>{};
    }

    public string configPathRoot;
    public string config_name;
    public string exp_index;
    public string mouse_index;
    public string name;
        
    public List<ConfigBody> configs;

    public int ChangeContent(string newContent){
        string _content_head = newContent.Split(":")[0];
        string _content_body = newContent.Split(":")[1];

        //contexts_list_str   = _content_head == "contexts_list_str"        ? _content_body : contexts_list_str       ;
        return 1;
    }

    public string PrintContent(){
        string result = "";//"contexts_list_str"  + ":" + contexts_list_str     +";"+
        return result;
    }

    public List<string> ReturnContent(){
        List<string> result = new List<string>{
            //"contexts_list_str"  + ":" + contexts_list_str ,
        };

        return result;
    }      

    public int INIWriteContent(IniReader iniReader){//写入ini的键名称和代码中变量名不同
        // iniReader.WriteIniContent("trail_content", "context_for_repeat"  , contexts_list_str .StartsWith("default:")? contexts_list_str .Substring(8): contexts_list_str );
        // configPathRoot
        // config_name
        // exp_index
        // mouse_index
        // name
        return 1;
    }

    public int INIContentSplit(string _text){

        return 1;
    }

    public int INIWriteAll(IniReader iniReader){
        INIWriteContent(iniReader);
        foreach(ConfigBody configBody in configs){
            configBody.INIWriteContent(iniReader);
        }

        return 1;
    }
}

#endregion

public class StartMenuDraw : MonoBehaviour
{
    // Start is called before the first frame update
    #if UNITY_EDITOR
        private string configPathRoot="Assets/Resources/";//+"config.ini" or sth.
    #else
        private string configPathRoot;
    #endif

    private string metaConfigName = "metaConfig.ini";
    private string defaultConfigName = "defaultConfig.ini";

    public GameObject Rows;
    public List<TMP_InputField> inputFields = new List<TMP_InputField>{};
    public List<Button> buttons = new List<Button>();
    private TMP_InputField IFfocused;
    private IniReader metaIniReader; 
    private IniReader iniReader; 
    private TableDraw tableDraw;

    public Material materialMissing;
    public GameObject landmarkMissing;
    public GameObject gameObjectMissing;
    public string readConfigJson = "";
    Config config;
    Dictionary<string, string> dicMatPath = new Dictionary<string, string>(){{"bar", "Tex_bar.mat"}, {"point", "Tex_point.mat"}, {"point_sparse", "Tex_point_sparse.mat"}, {"custom", "Tex_custom.mat"}};
    Dictionary<string, string> dicPrefabPath = new Dictionary<string, string>(){{"tower", "tower.prefab"}, {"twin_tower", "twin_tower.prefab"}, {"tunnel", "tunnel.prefab"}, {"tunnel_no_ceil", "tunnel_no_ceil.prefab"}, {"tunnel_no_ceil_and_end", "tunnel_no_ceil_and_end.prefab"}};



    Material GetMaterial(string material_name){
        Material temp_material;
        string  temp_name;
        if(dicMatPath.TryGetValue(material_name, out temp_name)){
            string temp_path="";
            #if UNITY_EDITOR
                temp_path="Assets/Resources/Materials/Linear_track"+$"/{temp_name}";
                temp_material = AssetDatabase.LoadAssetAtPath<Material>(temp_path);
            #else
                temp_material = Resources.Load<Material>("Materials/Linear_track"+$"/{temp_name.Split('.')[0]}");
            #endif
            if(temp_name == "custom" && System.IO.File.Exists(Application.dataPath+"/Sprites/custom.png")){
                //创建文件读取流
                FileStream fileStream = new FileStream(temp_path == ""? Application.dataPath+"/Sprites/custom.png" : "Assets/Sprites/WallPapers/spr_custom.png", FileMode.Open, FileAccess.Read);
                fileStream.Seek(0, SeekOrigin.Begin);
                //创建文件长度缓冲区
                byte[] bytes = new byte[fileStream.Length]; 
                //读取文件
                fileStream.Read(bytes, 0, (int)fileStream.Length);
                //释放文件读取流
                fileStream.Close();
                fileStream.Dispose();
                fileStream = null;

                Texture2D texture = new Texture2D(100, 100);
                texture.LoadImage(bytes);
                temp_material.SetTexture("_MainTex", texture);
            }
            return temp_material;
        }

        return materialMissing;
    }

    GameObject GetPrefab(string Prefab_name, string _prefab_type=""){
        GameObject temp_gameobject;
        string  temp_name;
        if(dicPrefabPath.TryGetValue(Prefab_name, out temp_name)){
            #if UNITY_EDITOR
                string temp_path="Assets/Resources/Prefabs/Linear_track"+$"/{temp_name}";
                temp_gameobject = AssetDatabase.LoadAssetAtPath<GameObject>(temp_path);
            #else
                temp_gameobject = Resources.Load<GameObject>("Prefabs/Linear_track"+$"/{temp_name.Split('.')[0]}");
            #endif
            return temp_gameobject;
        }

        if(_prefab_type=="landmark"){return landmarkMissing;}
        else{
            return gameObjectMissing;
        }
    }
    
    void AddRowHead(string content, string head, int level = 0){
        tableDraw.AddRow(level, content, head);
    }
    void AddRowBody(string content, string head, int level = 1, string addColor = "green"){
        tableDraw.AddRow(level, content, head, addColor);
    }

    public void SetIFFocus(TMP_InputField inputField){
        IFfocused = inputField;
    }
    void IFContentParse(string content){//if_name:value
        if(content == ""){return;}
        else{
            string _content_head = content[0..content.IndexOf(":")];
            string _content_body = content[(content.IndexOf(":")+1)..];

            switch(_content_head){
                case "ConfigContent":{
                    string _rowcontent_head = _content_body[0.._content_body.IndexOf(":")];//head:annotate:content
                    if(_rowcontent_head == ""){
                        config.ChangeContent(_content_body[(_content_body.IndexOf(":")+1)..]);
                        break;
                    }
                    break;
                }
                case "ConfigName":{
                    config.config_name = _content_body;
                    break;
                }
                case "MouseIndex":{
                    config.mouse_index = _content_body;
                    break;
                }
                case "ExpIndex":{
                    config.exp_index = _content_body;
                    break;
                }
                default:{break;}
            }
        }
    }

    string SectionComplete(List<string> contentLs, int start = 0, int end = -1){//包括end所在行
        if(contentLs.Count > 0){
            if(end == -1 || end >= contentLs.Count){end = contentLs.Count - 1;}
            if(start >= end || start < 0){return "";}

            bool _headExist = false;
            bool _bodyExist = false;
            List<string> RealContentLs = new List<string>();
            for(int i = start; i < end; i++){
                if(!_headExist){
                    if(contentLs[i].Contains("[") && contentLs[i].Contains("]")){
                        RealContentLs.Add(contentLs[i]);
                        _headExist = true;
                    }
                }else{//直到添加过head后再添加body
                    if(contentLs[i].Trim('=').Split("=").Length == 2){
                        RealContentLs.Add(contentLs[i]);
                        _bodyExist = true;
                    }
                }
            }
            if(_headExist && _bodyExist){
                return string.Join("\n", RealContentLs);
            }else{
                return "";
            }
        }else{
            return "";
        }
    }

    void Awake()
    {   
        tableDraw = Rows.GetComponent<TableDraw>();

        string configPath, defaultConfigPath;

        #if !UNITY_EDITOR
            configPathRoot = Application.dataPath+"/Resources";
            //configPathRoot = Application.dataPath+"/Resources/config.ini";
            metaIniReader = new IniReader(configPathRoot + "/" + metaConfigName);
            if(!metaIniReader.Exists() || !System.IO.File.Exists(configPathRoot)){
                MessageBoxForUnity.Ensure("No config file!", "Error");
                Application.Quit();//临时
            }

        #else
            metaIniReader = new IniReader(configPathRoot + "/" + metaConfigName);
            if(!metaIniReader.Exists() || !System.IO.File.Exists(configPathRoot)){
                MessageBoxForUnity.Ensure("No config file!", "Error");
                UnityEditor.EditorApplication.isPlaying = false;
            }

        #endif
        
        string nowConfigFileName = metaIniReader.ReadIniContent("configInfo", "preference", "config.ini");
        configPath = configPathRoot + "/" + nowConfigFileName;
        defaultConfigPath = configPathRoot + "/" + defaultConfigName;
        iniReader = new IniReader(configPath);
        IniReader _defaultINIReader=new IniReader(defaultConfigPath);

        config = new Config();

        string _allConfigContent = File.ReadAllText(configPath);
        List<string> _allConfigContentLs = _allConfigContent.Replace(" ", "").Split("\n").ToList();
        _allConfigContentLs.RemoveAll(line => line == "");
        int _sectionInd = -1;
        for(int _i = 0; _i < _allConfigContentLs.Count(); _i++){
            if(_allConfigContentLs[_i].Contains("[") && _allConfigContentLs[_i].Contains("]")){
                if(_sectionInd >= 0){
                    _sectionInd = _i;
                }else{
                    string tempContent = SectionComplete(_allConfigContentLs, _sectionInd, _i - 1);
                    if(tempContent.Length > 0){
                        config.configs.Add(new ConfigBody(tempContent, _defaultINIReader));
                    }
                }
            }
        }
        // readConfigJson = JsonUtility.ToJson(config);
    }

    void Start()
    {
        // AddRowHead("trail_content", "");
        // foreach(string content in config.ReturnContent()){
        //     AddRowBody(content, "trail_content");
        // }
        foreach(ConfigBody configBody in config.configs){
            AddRowHead(configBody.SectionName, "section");
            foreach(string content in configBody.ReturnContent()){
                AddRowBody(content.Split(":")[1], content.Split(":")[0]);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Return)){
            IFfocused = tableDraw.CheckFocus();
            if(IFfocused == null){
                foreach(TMP_InputField inputField in inputFields){
                    if(inputField.isFocused){IFfocused = inputField;}
                }
            }

            if(IFfocused != null){
                string _changed_content = tableDraw.ReturnContent(IFfocused);
                if(_changed_content != ""){
                    _changed_content = "ConfigContent:" + _changed_content;
                }else{
                    _changed_content = IFfocused.text;
                }

                IFContentParse(_changed_content);
                IFfocused.GetComponent<RowContent>().ChangeColor("red");
                IFfocused = null;
                //update config content
            }

            IFfocused = null;
        }

        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)){//保存按钮
            string nowConfigJson = JsonUtility.ToJson(config);
            Debug.Log(nowConfigJson);
            if(nowConfigJson != readConfigJson){
                if(MessageBoxForUnity.EnsureAndCancel("Save Changes?", "Config") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_OK){
                    config.INIWriteAll(iniReader);
                }
            }
        }
    }
}
