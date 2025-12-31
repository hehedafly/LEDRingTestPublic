using System;
using System.IO;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Data.Common;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class Context_generate : MonoBehaviour
{
    
    public string context_config_path;
    public GameObject prefabRewardZone;
    public GameObject prefabTunnel;
    public GameObject landmarkDefault;
    public Material materialWallpaperDefault;
    public GameObject player;
    private Position_control position_control;
    private LinearTrackUIUpdate ui_update;
    private IniReader ini_Reader;
    private int context_repeat_num;
    private int[] context_zone;//start_pos, end_pos, start_pos, end_pos...
    private int[] reward_zone;//reward_pos, end_pos, reward_pos, end_pos...
    private int startMode = -1;
    Dictionary<string, string> dicMatPath = new Dictionary<string, string>(){{"bar", "Tex_bar.mat"}, {"point", "Tex_point.mat"}, {"point_sparse", "Tex_point_sparse.mat"}, {"custom", "Tex_custom.mat"}};
    Dictionary<string, string> dicPrefabPath = new Dictionary<string, string>(){{"tower", "tower.prefab"}, {"twin_tower", "twin_tower.prefab"}, {"tunnel", "tunnel.prefab"}, {"tunnel_no_ceil", "tunnel_no_ceil.prefab"}, {"tunnel_no_ceil_and_end", "tunnel_no_ceil_and_end.prefab"}};
    public Material materialMissing;
    public GameObject landmarkMissing;
    public GameObject gameObjectMissing;
    
    [DllImport("User32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
    public static extern int MessageBox(IntPtr handle, String message, String title, int type);
    
    private readonly struct Context{//作为值类型的struct存储在list中时，其参数不能通过 set 更改。后续可以改成record类型
        public Context(string _name,string _start,int _length, string _wallpaper, string _reward_zone_paper, int _reward_zone_length, int _reward_zone_start, string _landmark_name, int _landmark_start, int _lick_count_max, string _lick_threshold, string _end_mark, string _end_arg, bool _is_inf=false){
            if(_length>=999){
                _length=2000;
                _is_inf=true;
                //_lick_count_max=9999;
                //if(_reward_zone_length > _length - _reward_zone_start){_reward_zone_length = _length - _reward_zone_start;}
            }
            if(_reward_zone_length  > _length - _reward_zone_start){_reward_zone_length = _length-_reward_zone_start;}

            name                    = _name              ;
            start                   = _start             ;
            length                  = _length            ;
            wallpaper               = _wallpaper         ;
            wallpaper_reward_zone   = _reward_zone_paper ;
            reward_zone_length      = _reward_zone_length;
            reward_zone_start       = _reward_zone_start ;
            landmark_name           = _landmark_name     ;
            landmark_start          = _landmark_start    ;
            lick_count_max          = _lick_count_max    ;
            end_mark                = _end_mark          ;
            end_arg                 = _end_arg           ;
            is_inf                  = _is_inf            ;

            lick_threshold = new int[2];
            try{
                lick_threshold[0] = Convert.ToInt32(_lick_threshold.Split(",")[0]);
                lick_threshold[1] = Convert.ToInt32(_lick_threshold.Split(",")[1]);
            }
            catch(Exception e){Debug.Log(e.Message);}

        }
        public string   name                {get;}
        public string   start               {get;}//"" or "random0-5" or...
        public int      length              {get;}
        public string   wallpaper           {get;}
        public string   wallpaper_reward_zone   {get;}
        public int      reward_zone_length  {get;}
        public int      reward_zone_start   {get;} //reward_zone start postion
        public string   landmark_name       {get;}//"tower" or sth.
        public int      landmark_start      {get;}
        public int      lick_count_max      {get;}
        public int[]    lick_threshold      {get;}//[correct, incorrect]
        public string   end_mark            {get;}//"success_color, fail_color"
        public string   end_arg             {get;}
        public bool     is_inf              {get;}
        //加的时候记得改StartMenuDraw中对应项


        public void Generate_context(int context_start_position, GameObject tunnel, GameObject reward_zone, GameObject landmark, Material wallpaper, Material wallpaper_reward_zone){
            GameObject obj_tunnel= Instantiate(tunnel, new Vector3(context_start_position, 0f, 0.0f), Quaternion.identity);
            Transform tunnel_tf=obj_tunnel.GetComponent<Transform>();
            int generate_length=length + (is_inf? 50: 0);
            tunnel_tf.localScale=new Vector3((generate_length+1)*0.01f, 1, 1);
            tunnel_tf.position = new Vector3(tunnel_tf.position.x-1, tunnel_tf.position.y, tunnel_tf.position.z);

            if(reward_zone_start>=0 && wallpaper!=wallpaper_reward_zone){
                GameObject obj_reward_zone = Instantiate(reward_zone, new Vector3(context_start_position+reward_zone_start, -0.01f, 0.0f), Quaternion.identity);
                Transform obj_reward_zone_tf = obj_reward_zone.GetComponent<Transform>();
                obj_reward_zone_tf.localScale=new Vector3(Math.Min(reward_zone_length, length - reward_zone_start)*0.01f, 1.0f, 0.98f);

                for(int i=0; i<obj_reward_zone_tf.childCount; i++){
                    GameObject temp_go=obj_reward_zone_tf.GetChild(i).gameObject;
                    if (temp_go.name.Contains("left") || temp_go.name.Contains("right")){
                        Material instanceMaterial = new Material(wallpaper_reward_zone);
                        instanceMaterial.mainTextureScale = new Vector2(1, reward_zone_length);
                        temp_go.GetComponent<Renderer>().material = instanceMaterial;
                    }
                }
            }

            if(landmark_name!="none" && landmark_start>0){GameObject obj_land_mark = Instantiate(landmark, new Vector3(context_start_position+landmark_start, 0f, 0f), Quaternion.identity);}
            for(int i=0; i<tunnel_tf.childCount; i++){
                GameObject temp_go=tunnel_tf.GetChild(i).gameObject;
                if (temp_go.name.Contains("left") || temp_go.name.Contains("right")){
                    Material instanceMaterial = new Material(wallpaper);
                    instanceMaterial.mainTextureScale = new Vector2(1, generate_length+1);
                    temp_go.GetComponent<Renderer>().material = instanceMaterial;
                }
            }
            return;
        }
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

    public string GetConfigPath(){
        #if UNITY_EDITOR
            context_config_path="Assets/Resources/linear_track/config.ini";
        #else
            context_config_path=Application.dataPath+"/Resources/linear_track/config.ini";
            if(!System.IO.Directory.Exists(Application.dataPath+"/linear_track/Sprites")){System.IO.Directory.CreateDirectory(Application.dataPath+"/linear_track/Sprites");}
        #endif
        return context_config_path;
    }
    
    void Awake()
    {
        Debug.Log(GetType()+ "Display.displays.Length = " + Display.displays.Length);
        for (int i = 0; i < Math.Min(2, Display.displays.Length); i++)
        {
            Display.displays[i].Activate();
            Screen.SetResolution(Display.displays[i].renderingWidth, Display.displays[i].renderingHeight, true);
        }
         
        ini_Reader=new IniReader(GetConfigPath());
        position_control = player.GetComponent<Position_control>();
        ui_update = player.GetComponent<LinearTrackUIUpdate>();
        //TextAsset txt = Resources.Load(context_config) as TextAsset;
        //GameObject obj_reward_zone= Instantiate(prefab_reward_zone, new Vector3(10.0f, 0.0f, 0.49f), Quaternion.identity);
        try{
            string[] temp_contexts=ini_Reader.ReadIniContent("trail_content", "context_for_repeat", "context0").Split(',');
            context_repeat_num=Convert.ToInt32(ini_Reader.ReadIniContent("trail_content", "repeat_num", "5"));
            startMode = Convert.ToInt32(ini_Reader.ReadIniContent("trail_content", "start_mode", "0"));

            position_control.Set_trial_info(context_repeat_num);
            
            List<string> context_list_for_repeat=new List<string>();//context repeat list
            List<string> context_list=new List<string>();//context list removed repetition
            foreach(var name in temp_contexts){
                var context_name=name.IndexOf("context")==-1? "": "context"+name.Substring(name.IndexOf("context")+7);
                if (context_name!=""){
                    context_list_for_repeat.Add(context_name);
                    if(!context_list.Contains(context_name)){context_list.Add(context_name);}
                }
            }
            Dictionary<string, Context> context_struct_map= new Dictionary<string, Context>();
            foreach(var name in context_list){
                context_struct_map.Add(name, new Context(
                    name,
                    ini_Reader.ReadIniContent(                name,  "start",               "0"             ),
                    Convert.ToInt32(ini_Reader.ReadIniContent(name,  "length",              "10"           )),
                    ini_Reader.ReadIniContent(                name,  "wall_paper",          "point_sparse"  ),
                    ini_Reader.ReadIniContent(                name,  "reward_zone_paper",   "bar"           ),
                    Convert.ToInt32(ini_Reader.ReadIniContent(name,  "reward_zone_length",  "3"            )),
                    Convert.ToInt32(ini_Reader.ReadIniContent(name,  "reward_zone_start",   "1"            )),
                    ini_Reader.ReadIniContent(                name,  "landmark_name",       "none"          ),
                    Convert.ToInt32(ini_Reader.ReadIniContent(name,  "landmark_start",      "-1"           )),
                    Convert.ToInt32(ini_Reader.ReadIniContent(name,  "lick_count_max",      "8"            )),
                    ini_Reader.ReadIniContent(                name,  "lick_threshold",      "4,8"           ),
                    ini_Reader.ReadIniContent(                name,  "end_context",         "grey,black"    ),
                    ini_Reader.ReadIniContent(                name,  "end_arg",             "3"             )
                ));
            }
            int start_pos=0;
            context_zone=new int[context_list_for_repeat.Count*2];
            reward_zone=new int[context_list_for_repeat.Count*2];
            //from context list generate gameobject
            int temp_i=0;
            var temp_ls=position_control.context_info_ls;
            foreach(var context in context_list_for_repeat){
                // context_struct_map[context].Generate_context(start_pos, prefab_tunnel, prefab_reward_zone, GetPrefab(context_struct_map[context].landmark_name), GetMaterial(context_struct_map[context].wall_paper));
                context_struct_map[context].Generate_context(start_pos, prefabTunnel, GetPrefab("tunnel_no_ceil_and_end"), GetPrefab(context_struct_map[context].landmark_name, "landmark"), GetMaterial(context_struct_map[context].wallpaper), GetMaterial(context_struct_map[context].wallpaper_reward_zone));
                context_zone[temp_i]=start_pos;                                                context_zone[temp_i+1]=context_zone[temp_i]+context_struct_map[context].length;
                reward_zone[temp_i]=start_pos+context_struct_map[context].reward_zone_start;   reward_zone[temp_i+1]=reward_zone[temp_i]+context_struct_map[context].reward_zone_length;
                temp_ls.Add(new Position_control.Context_info(
                    context_struct_map[context].start,
                    context_struct_map[context].lick_count_max,
                    context_struct_map[context].end_mark.Split(',')[0], 
                    context_struct_map[context].end_mark.Split(',')[1],
                    Convert.ToSingle(context_struct_map[context].end_arg),
                    context_struct_map[context].lick_threshold,
                    context_repeat_num,
                    context_struct_map[context].is_inf
                    ));
                temp_i+=2;
                start_pos+=context_struct_map[context].length+1;
                if(context_struct_map[context].is_inf){break;}
            }
            position_control.contextZoneStartAndEnd=new int[temp_ls.Count()*2];
            position_control.rewardZoneStartAndEnd=new int[temp_ls.Count()*2];
            for(int i=0; i<temp_ls.Count()*2; i++){
                position_control.contextZoneStartAndEnd[i] = context_zone[i];
                position_control.rewardZoneStartAndEnd[i]  = reward_zone[i];
            }
        }
        catch(Exception e){
            MessageBox(IntPtr.Zero, e.Message+"\n"+e.StackTrace, "error", 0);
            Application.Quit();
        }    

    }

    void Start()
    {
        if(startMode != -1){
            ui_update.Controls_parse("ModeSelect", startMode);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
