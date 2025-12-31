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
    public ConfigBody(string _context_name,string _start,string _length, string _wallpaper, string _reward_zone_paper, string _reward_zone_length, string _reward_zone_start, string _landmark_name, string _landmark_start, string _lick_count_max, string _lick_threshold, string _end_context, string _end_arg){
        context_name         = _context_name        ;
        start                = _start               ;        
        length               = _length              ;
        wall_paper           = _wallpaper           ;
        reward_zone_paper    = _reward_zone_paper   ;
        reward_zone_length   = _reward_zone_length  ;
        reward_zone_start    = _reward_zone_start   ;
        landmark_name        = _landmark_name       ;
        landmark_start       = _landmark_start      ;
        lick_count_max       =  _lick_count_max     ;
        lick_threshold       =  _lick_threshold     ;
        end_context          = _end_context         ;
        end_arg              = _end_arg             ;
    }
    public string context_name;
    public string start;            
    public string length;           
    public string wall_paper;       
    public string reward_zone_paper;
    public string reward_zone_length;
    public string reward_zone_start;
    public string landmark_name;    
    public string landmark_start;   
    public string lick_count_max;   
    public string lick_threshold;   
    public string end_context;      
    public string end_arg; 

    public int ChangeContent(string newContent){
        string _content_head = newContent.Split(":")[0];
        string _content_body = newContent.Split(":")[1];

        //context_name       = _content_head == "context_name"       ? _content_body : context_name      ;
        start              = _content_head == "start"              ? _content_body : start             ;
        length             = _content_head == "length"             ? _content_body : length            ;
        wall_paper         = _content_head == "wall_paper"         ? _content_body : wall_paper        ;
        reward_zone_paper  = _content_head == "reward_zone_paper"  ? _content_body : reward_zone_paper ;
        reward_zone_length = _content_head == "reward_zone_length" ? _content_body : reward_zone_length;
        reward_zone_start  = _content_head == "reward_zone_start"  ? _content_body : reward_zone_start ;
        landmark_name      = _content_head == "landmark_name"      ? _content_body : landmark_name     ;
        landmark_start     = _content_head == "landmark_start"     ? _content_body : landmark_start    ;
        lick_count_max     = _content_head == "lick_count_max"     ? _content_body : lick_count_max    ;
        lick_threshold     = _content_head == "lick_threshold"     ? _content_body : lick_threshold    ;
        end_context        = _content_head == "end_context"        ? _content_body : end_context       ;
        end_arg            = _content_head == "end_arg"            ? _content_body : end_arg           ;
        return 1;
    }   

    public string PrintContent(){
        string result = //"context_name"       + ":" + context_name      +";"+
                        "start"              + ":" + start             +";"+
                        "length"             + ":" + length            +";"+
                        "wall_paper"         + ":" + wall_paper        +";"+
                        "reward_zone_paper"  + ":" + reward_zone_paper +";"+
                        "reward_zone_length" + ":" + reward_zone_length+";"+
                        "reward_zone_start"  + ":" + reward_zone_start +";"+
                        "landmark_name"      + ":" + landmark_name     +";"+
                        "landmark_start"     + ":" + landmark_start    +";"+
                        "lick_count_max"     + ":" + lick_count_max    +";"+
                        "lick_threshold"     + ":" + lick_threshold    +";"+
                        "end_context"        + ":" + end_context       +";"+
                        "end_arg"            + ":" + end_arg                  ;

        return result;
    }

    public List<string> ReturnContent(){
        List<string> result = new List<string>{
            //"context_name"       + ":" + context_name      ,
            "start"              + ":" + start             ,
            "length"             + ":" + length            ,
            "wall_paper"         + ":" + wall_paper        ,
            "reward_zone_paper"  + ":" + reward_zone_paper ,
            "reward_zone_length" + ":" + reward_zone_length,
            "reward_zone_start"  + ":" + reward_zone_start ,
            "landmark_name"      + ":" + landmark_name     ,
            "landmark_start"     + ":" + landmark_start    ,
            "lick_count_max"     + ":" + lick_count_max    ,
            "lick_threshold"     + ":" + lick_threshold    ,
            "end_context"        + ":" + end_context       ,
            "end_arg"            + ":" + end_arg           ,
        };

        return result;
    }      

    public int INIWriteContent(IniReader ini_reader){//写入ini的键名称和代码中变量名不同
        ini_reader.WriteIniContent(context_name, "start"              , start             .StartsWith("default:")? start             .Substring(8): start             );
        ini_reader.WriteIniContent(context_name, "length"             , length            .StartsWith("default:")? length            .Substring(8): length            );
        ini_reader.WriteIniContent(context_name, "wall_paper"         , wall_paper        .StartsWith("default:")? wall_paper        .Substring(8): wall_paper        );
        ini_reader.WriteIniContent(context_name, "reward_zone_paper"  , reward_zone_paper .StartsWith("default:")? reward_zone_paper .Substring(8): reward_zone_paper );
        ini_reader.WriteIniContent(context_name, "reward_zone_length" , reward_zone_length.StartsWith("default:")? reward_zone_length.Substring(8): reward_zone_length);
        ini_reader.WriteIniContent(context_name, "reward_zone_start"  , reward_zone_start .StartsWith("default:")? reward_zone_start .Substring(8): reward_zone_start );
        ini_reader.WriteIniContent(context_name, "landmark_name"      , landmark_name     .StartsWith("default:")? landmark_name     .Substring(8): landmark_name     );
        ini_reader.WriteIniContent(context_name, "landmark_start"     , landmark_start    .StartsWith("default:")? landmark_start    .Substring(8): landmark_start    );
        ini_reader.WriteIniContent(context_name, "lick_count_max"     , lick_count_max    .StartsWith("default:")? lick_count_max    .Substring(8): lick_count_max    );
        ini_reader.WriteIniContent(context_name, "lick_threshold"     , lick_threshold    .StartsWith("default:")? lick_threshold    .Substring(8): lick_threshold    );
        ini_reader.WriteIniContent(context_name, "end_context"        , end_context       .StartsWith("default:")? end_context       .Substring(8): end_context       );
        ini_reader.WriteIniContent(context_name, "end_arg"            , end_arg           .StartsWith("default:")? end_arg           .Substring(8): end_arg           );
        // if(!start             .StartsWith("default")){ini_reader.WriteIniContent(context_name, "start"              , start             );}
        // if(!length            .StartsWith("default")){ini_reader.WriteIniContent(context_name, "length"             , length            );}
        // if(!wall_paper        .StartsWith("default")){ini_reader.WriteIniContent(context_name, "wall_paper"         , wall_paper        );}
        // if(!reward_zone_paper .StartsWith("default")){ini_reader.WriteIniContent(context_name, "reward_zone_paper"  , reward_zone_paper );}
        // if(!reward_zone_length.StartsWith("default")){ini_reader.WriteIniContent(context_name, "reward_zone_length" , reward_zone_length);}
        // if(!reward_zone_start .StartsWith("default")){ini_reader.WriteIniContent(context_name, "reward_zone_start"  , reward_zone_start );}
        // if(!landmark_name     .StartsWith("default")){ini_reader.WriteIniContent(context_name, "landmark_name"      , landmark_name     );}
        // if(!landmark_start    .StartsWith("default")){ini_reader.WriteIniContent(context_name, "landmark_start"     , landmark_start    );}
        // if(!lick_count_max    .StartsWith("default")){ini_reader.WriteIniContent(context_name, "lick_count_max"     , lick_count_max    );}
        // if(!lick_threshold    .StartsWith("default")){ini_reader.WriteIniContent(context_name, "lick_threshold"     , lick_threshold    );}
        // if(!end_context       .StartsWith("default")){ini_reader.WriteIniContent(context_name, "end_context"        , end_context       );}
        // if(!end_arg           .StartsWith("default")){ini_reader.WriteIniContent(context_name, "end_arg"            , end_arg           );}

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

    public Config(string _config_path, string _config_name, string _exp_index, string _mouse_index, string _name, string _contexts_list_str, string _context_repeat_num, string _start_mode){
        //contexts_list = new string[_contexts_list_size];

        config_path = _config_path;
        config_name = _config_name;
        exp_index = _exp_index;
        mouse_index = _mouse_index;
        name = _name;
        //_contexts_list.CopyTo(contexts_list, 0);
        contexts_list_str = _contexts_list_str;
        context_repeat_num = _context_repeat_num;
        start_mode = _start_mode;
        configs = new List<ConfigBody>{};
    }
        
    public string config_path;
    public string config_name;
    public string exp_index;
    public string mouse_index;
    public string name;
    //string[] contexts_list;//temp_contexts，暂时不考虑重复
    public string contexts_list_str;
    public string context_repeat_num;
    public string start_mode;
    public List<ConfigBody> configs;

    public int ChangeContent(string newContent){
        string _content_head = newContent.Split(":")[0];
        string _content_body = newContent.Split(":")[1];

        contexts_list_str   = _content_head == "contexts_list_str"        ? _content_body : contexts_list_str       ;
        context_repeat_num  = _content_head == "context_repeat_num"       ? _content_body : context_repeat_num      ;
        start_mode          = _content_head == "start_mode"               ? _content_body : start_mode              ;
        return 1;
    }

    public string PrintContent(){
        string result = "contexts_list_str"  + ":" + contexts_list_str     +";"+
                        "context_repeat_num" + ":" + context_repeat_num    +";"+
                        "start_mode"         + ":" + start_mode                  ;

        return result;
    }

    public List<string> ReturnContent(){
        List<string> result = new List<string>{
            "contexts_list_str"  + ":" + contexts_list_str ,
            "context_repeat_num" + ":" + context_repeat_num,
            "start_mode"         + ":" + start_mode        ,
        };

        return result;
    }      

    public int INIWriteContent(IniReader ini_reader){//写入ini的键名称和代码中变量名不同
        // if(!contexts_list_str .StartsWith("default")){ini_reader.WriteIniContent("trail_content", "context_for_repeat"   , contexts_list_str            );}
        // if(!context_repeat_num.StartsWith("default")){ini_reader.WriteIniContent("trail_content", "repeat_num"           , context_repeat_num.ToString());}
        // if(!start_mode        .StartsWith("default")){ini_reader.WriteIniContent("trail_content", "start_mode"           , start_mode        .ToString());}
        ini_reader.WriteIniContent("trail_content", "context_for_repeat"  , contexts_list_str .StartsWith("default:")? contexts_list_str .Substring(8): contexts_list_str );
        ini_reader.WriteIniContent("trail_content", "repeat_num"          , context_repeat_num.StartsWith("default:")? context_repeat_num.Substring(8): context_repeat_num);
        ini_reader.WriteIniContent("trail_content", "start_mode"          , start_mode        .StartsWith("default:")? start_mode        .Substring(8): start_mode        );

        return 1;
    }

    public int INIWriteAll(IniReader ini_reader){
        INIWriteContent(ini_reader);
        foreach(ConfigBody configBody in configs){
            configBody.INIWriteContent(ini_reader);
        }

        return 1;
    }
}

#endregion

public class StartMenuDraw : MonoBehaviour
{
    // Start is called before the first frame update
    #if UNITY_EDITOR
        private string context_config_path="Assets/Resources/linear_track";//+"/config.ini" or sth.
    #else
        private string context_config_path;
    #endif

    private string metaConfigName = "/metaConfig.ini";
    private string openINIFileName;

    public GameObject Rows;
    public List<TMP_InputField> inputFields = new List<TMP_InputField>{};
    public List<Button> buttons = new List<Button>();
    private TMP_InputField IFfocused;
    private IniReader ini_reader; 
    private TableDraw tableDraw;

    public Material materialMissing;
    public GameObject landmarkMissing;
    public GameObject gameObjectMissing;
    public string readConfigJson = "";
    Config config;
    Dictionary<string, string> dicMatPath = new Dictionary<string, string>(){{"bar", "Tex_bar.mat"}, {"point", "Tex_point.mat"}, {"point_sparse", "Tex_point_sparse.mat"}, {"custom", "Tex_custom.mat"}};
    Dictionary<string, string> dicPrefabPath = new Dictionary<string, string>(){{"tower", "tower.prefab"}, {"twin_tower", "twin_tower.prefab"}, {"tunnel", "tunnel.prefab"}, {"tunnel_no_ceil", "tunnel_no_ceil.prefab"}, {"tunnel_no_ceil_and_end", "tunnel_no_ceil_and_end.prefab"}};

    [DllImport("User32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    public static extern int MessageBox(IntPtr handle, String message, String title, int type);

    enum MessageBoxReturnValueType{
        Button_VOID   ,
        Button_OK     ,
        Button_CANCEL ,
        Button_ABORT  ,
        Button_RETRY  ,
        Button_IGNORE ,
        Button_YES    ,
        Button_NO     ,
    }

    enum MessageBoxType{
        OK              ,
        OKCANCEL        ,
        ABORTRETRYIGNORE,
        YESNOCANCEL     ,
        YESNO           ,
        RETRYCANCEL     ,
    }

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
                    string[] _context_ls = config.contexts_list_str.Split(",");
                    if(_rowcontent_head == "trail_content"){
                        config.ChangeContent(_content_body[(_content_body.IndexOf(":")+1)..]);
                        break;
                    }
                    else{
                        for(int i = 0; i < _context_ls.Length; i++){
                            if(_rowcontent_head == _context_ls[i]){
                                config.configs[i].ChangeContent(_content_body[(_content_body.IndexOf(":")+1)..]);
                                break;
                            }
                            if(i == _context_ls.Length - 1){
                                Debug.Log("error in parse: wrong section");
                            }
                        }
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

    void Awake()
    {   
        tableDraw = Rows.GetComponent<TableDraw>();
        #if !UNITY_EDITOR
            context_config_path = Application.dataPath+"/Resources";
            //context_config_path = Application.dataPath+"/Resources/config.ini";
            ini_reader = new IniReader(context_config_path+metaConfigName);
            string nowConfigFileName = ini_reader.ReadIniContent("configInfo", "preference", "config.ini");
            context_config_path += "/" +nowConfigFileName;
            
            if(!System.IO.File.Exists(context_config_path)){
                MessageBox(IntPtr.Zero, "No config file!", "Error", (int)MessageBoxType.OK);
                Application.Quit();//临时
            }
        #else
            ini_reader = new IniReader(context_config_path+metaConfigName);
            string nowConfigFileName = ini_reader.ReadIniContent("configInfo", "preference", "config.ini");
            context_config_path += "/"+nowConfigFileName;
            if(!System.IO.File.Exists(context_config_path)){
                MessageBox(IntPtr.Zero, "No config file!", "Error", (int)MessageBoxType.OK);
                UnityEditor.EditorApplication.isPlaying = false;
            }
        #endif
        openINIFileName = nowConfigFileName;

        ini_reader = null;

        ini_reader=new IniReader(context_config_path);
        string temp_contexts_str = ini_reader.ReadIniContent("trail_content", "context_for_repeat" , "context0", true);
        if(temp_contexts_str == ""){
            MessageBox(IntPtr.Zero, "content not avaliable, read in default", "No avaliable value!", 0);
        }

        string[] temp_contexts = temp_contexts_str.Split(',');
        int context_repeat_num  =   Convert.ToInt32(ini_reader.ReadIniContent("trail_content", "repeat_num"         , "5"       , true));
        int startMode           =   Convert.ToInt32(ini_reader.ReadIniContent("trail_content", "start_mode"         , "0"       , true));

        config = new Config(context_config_path, "config", "exp0", "#", "config", temp_contexts_str, context_repeat_num.ToString(), startMode.ToString());
        foreach(string context in temp_contexts){
            ConfigBody temp_configBody = new ConfigBody(
                context,
                ini_reader.ReadIniContent(context,  "start",               "default:"              ),
                ini_reader.ReadIniContent(context,  "length",              "default:10"            ),
                ini_reader.ReadIniContent(context,  "wall_paper",          "default:point_sparse"  ),
                ini_reader.ReadIniContent(context,  "reward_zone_paper",   "default:bar"           ),
                ini_reader.ReadIniContent(context,  "reward_zone_length",  "default:3"             ),
                ini_reader.ReadIniContent(context,  "reward_zone_start",   "default:1"             ),
                ini_reader.ReadIniContent(context,  "landmark_name",       "default:none"          ),
                ini_reader.ReadIniContent(context,  "landmark_start",      "default:-1"            ),
                ini_reader.ReadIniContent(context,  "lick_count_max",      "default:8"             ),
                ini_reader.ReadIniContent(context,  "lick_threshold",      "default:4,8"           ),
                ini_reader.ReadIniContent(context,  "end_context",         "default:grey,black"    ),
                ini_reader.ReadIniContent(context,  "end_arg",             "default:3"             )
            );//真正写入config时不写入"default"
            config.configs.Add(temp_configBody);
        }

        readConfigJson = JsonUtility.ToJson(config);
    }

    void Start()
    {
        AddRowHead("trail_content", "");
        foreach(string content in config.ReturnContent()){
            AddRowBody(content, "trail_content");
        }
        foreach(ConfigBody configBody in config.configs){
            AddRowHead(configBody.context_name, configBody.context_name);
            foreach(string content in configBody.ReturnContent()){
                AddRowBody(content, configBody.context_name);
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
                if(MessageBox(IntPtr.Zero, "Save Changes?", "Config", (int)MessageBoxType.OKCANCEL) == (int)MessageBoxReturnValueType.Button_OK){
                    config.INIWriteAll(ini_reader);
                }
            }
        }
    }
}
