using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
//using UnityEditor.PackageManager;
using UnityEngine;

public class Position_control : MonoBehaviour
{
    // public GameObject prefab_quad;
    public int[] contextZoneStartAndEnd;//start_pos, end_pos, start_pos, end_pos...
    public int[] rewardZoneStartAndEnd;//reward_pos, end_pos, reward_pos, end_pos...
    public List<Context_info> context_info_ls = new List<Context_info>();
    public bool now_context_success;
    int now_trial=1; public int Now_trial{get{return now_trial;}}
    public int trial_per_section=0;
    public int serve_water_mode=0;
    public int lick_count_correct=0;
    public int lick_count_max=0;
    public int waterServedCount = 0;//每个trial刷新
    public int[] lick_count_rec=new int[]{0, 0, 0};//before reward_zone, in_reward_zone, after_reward_zone
    public int[] lick_count_succes_threshold=new int[]{4, 8};
    public struct Context_info{
        public Context_info(string _start_method, int _lick_count_max, string _s_color, string _f_color, float _wait_sec, int[] _lick_threshold, int trial_count, bool _is_inf=false):this(){
            start_pos = new float[trial_count];
            if(_start_method.StartsWith("random")){
                string[] temp_ls=_start_method[_start_method.IndexOf("random")..].Split("~");
                if(temp_ls.Length==2){
                    for(int i=0; i<trial_count; i++){
                        start_pos[i] = UnityEngine.Random.Range(Convert.ToSingle(temp_ls[0]), Convert.ToSingle(temp_ls[1]));
                    }
                }
            }else{
                for(int i=0; i<trial_count; i++){start_pos[i] = Convert.ToSingle(_start_method);}
            }
            lick_count_max = _lick_count_max;
            if (ColorUtility.TryParseHtmlString(_s_color, out Color temp_color)){
                succes_color = temp_color;
            }
            if (ColorUtility.TryParseHtmlString(_f_color, out temp_color)){
                fail_color=temp_color;
            }
            wait_sec = _wait_sec;
            is_inf = _is_inf;
            lick_threshold = new int[2];
            _lick_threshold.CopyTo(lick_threshold, 0);
        }
        //public string start_method;
        public float[]  start_pos       {get;}//relative to start of context
        public int      lick_count_max  {get;}
        public Color    succes_color    {get;}
        public Color    fail_color      {get;}
        public float    wait_sec        {get;}
        public int[]    lick_threshold  {get;}
        public bool     is_inf          {get;}
    }
    //public Dictionary<int, Context_end> dic_context_end=new Dictionary<string, Context_end>();
    
    float X {get{ return transform.position.x;}set{ transform.position = new Vector3(value, transform.position.y, transform.position.z);}}
    public float RelativeX {get{ return X- contextZoneStartAndEnd[now_context*2];}}
    int now_context=0;
    public int NowContext{get{return now_context;}}
    bool context_available=false;
    bool trial_syncing=false;   public bool Trial_syncing{get{return trial_syncing;}}
    float counter=-999f;//context结束计时
    int waiting=-1;//0:false, 1:true, 2:waiting for sync, -1:inital sync
    float pre_pos=-1f;
    float[] lengthRec = new float[]{-1, 0, 0};//{indicator, begin<可正可负>, length passed}
    public bool LengthRecClear  {set{ if(value){lengthRec = new float[]{-1, 0, 0}; LengthRecClear = false;}}}
    Transform tf;
    Rigidbody m_rb;
    LinearTrackMoving moving;
    LinearTrackUIUpdate ui_update;
    public GameObject fullScreenColor;
    Dictionary<string, float> dicWaterServingSpdVariables = new Dictionary<string, float>(){
        {"speed_threshold", 0.2f}, {"lasting_time_threshold", 0.4f}, {"running_time_threshold", 2}, {"time_serve_interval", 2}, {"time_serve_interval_when_runing", 1}, {"time_run_begin", 0}, {"time_served", 0}, {"random_delayed", 0}
    };   
    Dictionary<string, float> dicWaterServingLengthVariables = new Dictionary<string, float>(){
        {"length_threshold", 1f}, {"running_threshold", 10f}, {"time_serve_interval", 2}, {"time_serve_interval_when_runing", 1}, {"time_served", 0}, {"random_delayed", 0}
    };    
    public float dic_water_serving_speed_threshold{set{dicWaterServingSpdVariables["speed_threshold"]=value;}}
    public float dic_water_serving_runbegin{set{dicWaterServingSpdVariables["time_run_begin"]=value;}}
    Vector3 rec_pos;

    //void Trial_args_init(int now_context, bool update=true ,bool init_all=false){
    public void Trial_args_init(bool update=true ,bool init_all=false){//清理位置信息，更新位置以及context相关参数
        if(update){
            if(now_context==context_info_ls.Count-1){now_trial++;}
            now_context = (now_context+ 1) % context_info_ls.Count;
            context_available=false;
            float temp_length_diff = X - lengthRec[1];
            X = context_info_ls[now_context].start_pos[now_trial]+contextZoneStartAndEnd[now_context*2];
            lengthRec[1] = X - temp_length_diff;
            rec_pos=tf.position;
        }
        waterServedCount = 0;
        lick_count_correct=0;
        now_context_success=false;
        lick_count_rec=new int[]{0, 0, 0};
        moving.DataSend("init");

        if(init_all){
            now_context=0;
            counter=0f;
            waiting=0;
            pre_pos=-1f;
            //rec_pos=new Vector3(0, tf.position.y, 0);
        }
    }

    int Trial_context_sync(int context_id){
        //"p_enter_reward_context, p_in_reward_context, p_lick_time_accu, p_lick_count, p_start_water, p_lick_mode, p_trial, p_lick_count_max, p_lick_mode0_delay, p_lick_mode1_delay"
        trial_syncing=true;
        lick_count_max=context_info_ls[context_id].lick_count_max;
        context_info_ls[context_id].lick_threshold.CopyTo(lick_count_succes_threshold, 0);
        List<string> variables = new List<string>(){"p_enter_reward_context", "p_lick_count_max"};
        List<int> values = new List<int>(){-1, lick_count_max};
        variables.Add("p_trial");
        values.Add(now_trial);

        int sync_result = moving.Context_verify(variables, values);
        if(sync_result==-2){
            ui_update.MessageUpdate("serial port not open!\n");
        }else{
            int sync_max_time=100;
            while(sync_result!=1 && sync_max_time>0){
                sync_result = moving.Context_verify(variables, values);
                if(sync_result!=1 && sync_result != -3){
                    Debug.LogError("context info sync failed");
                }
                else{break;}
                sync_max_time--;
            }
        }
        
        variables.Clear();
        values.Clear();

        if(sync_result!=1 && sync_result != -3){
            Debug.LogError("context info sync failed");
            trial_syncing=false; 
            return -1;
        }

        context_available=true;
        trial_syncing=false;
        //ui_update.Message_update("information of next context synchronized\n");

        return 1;
    }

    public void Set_trial_info(int _trial_per_section){
        trial_per_section=_trial_per_section;
    }

    public float[] GetContextInfo(){
        //  0           1           2           3           4           5       6,7,8
        //context, pos<relative>, length, reward start, reward end, is_inf, lick_count_rec
        float[] results = new float[9];
        results[0] = now_context;
        results[1] = X - contextZoneStartAndEnd[now_context*2];
        results[2] = contextZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
        results[3] = rewardZoneStartAndEnd[now_context*2] - contextZoneStartAndEnd[now_context*2];
        results[4] = rewardZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
        results[5] = context_info_ls[now_context].is_inf? 1: 0;
        Array.ConstrainedCopy(lick_count_rec, 0, results, 6, 3);
        //results[1] = 
        return results;
    }

    public void Lick_rec(int add_num, bool is_correct){
        if (X >= contextZoneStartAndEnd[now_context*2] && X <= contextZoneStartAndEnd[now_context*2+1]){
            if (X >= rewardZoneStartAndEnd[now_context*2]){
                if (X < rewardZoneStartAndEnd[now_context*2+1]){
                    if(!is_correct){
                        lick_count_rec[1] += add_num;
                    }else{
                        lick_count_correct += add_num;
                    }
                }else{lick_count_rec[2] += add_num;}
            }else{lick_count_rec[0] += add_num;}
        }
    }

    public float[] Get_set_dic_water_serving(string key, int mode){//get value
        float[] array=new float[]{-1, -1};//length, aim value
        if(mode == 1){
            if(dicWaterServingSpdVariables.ContainsKey(key)){
                array[0]=0;
                array[1]=dicWaterServingSpdVariables[key];
            }
            return array;
        }
        else if(mode == 2){
            if(dicWaterServingLengthVariables.ContainsKey(key)){
                array[0]=0;
                array[1]=dicWaterServingLengthVariables[key];
            }
            return array;
        }
        else{
            return array;
        }
    }
    public void Get_set_dic_water_serving(string key, float value, int mode){//set value
        if(mode ==1){
            if(dicWaterServingSpdVariables.ContainsKey(key)){
                dicWaterServingSpdVariables[key]=value;
            }
            return;
        }
        else if(mode == 2){
            if(dicWaterServingLengthVariables.ContainsKey(key)){
                dicWaterServingLengthVariables[key]=value;
            }
            return;
        }
    }
    // public float[] Get_set_dic_water_serving(int mode, out string key_text, float value=-1, int index=-1){//后面改成return keys
    public float[] Get_set_dic_water_serving(int mode, out string key_text, int index=-1){//后面改成return keys
        float[] array=new float[]{-1, -1};//length, aim value
        if(mode==1){
            List<string> temp_list = new List<string>(dicWaterServingSpdVariables.Keys);
            array[0]=temp_list.Count;

            string temp_text=index==-1? "": temp_list[index];
            key_text=temp_text;
            if(dicWaterServingSpdVariables.ContainsKey(temp_text)){
                array[1]=dicWaterServingSpdVariables[temp_text];
            }
            return array;
        }
        else if(mode==2){
            List<string> temp_list = new List<string>(dicWaterServingLengthVariables.Keys);
            array[0]=temp_list.Count;

            string temp_text = index == -1? "": temp_list[index];
            key_text=temp_text;
            if(dicWaterServingLengthVariables.ContainsKey(temp_text)){
                array[1]=dicWaterServingLengthVariables[temp_text];
            }
            return array;
        }
        else{
            key_text = "";
            return array;
        }
    }

    void ServeWaterSpeedDepend(float spd, int random_delay_sec=0, bool IsInRewardZone=true, bool RewardZoneNeeded=false){//random_delay_sec: 延迟随机x秒后给水
        //speed_threshold, lasting_time_threshold,      running_time_threshold,             time_serve_interval,        time_serve_interval_when_runing, time_run_begin, time_served
        //                  ↖短暂跑动的最低时间要求，约0.5s?     ↖判定持续跑动的最低时间要求，约2s?      ↖间断跑给水均服从第一个间隔  ↖持续跑给水均服从第二个间隔   
        if(spd>dicWaterServingSpdVariables["speed_threshold"]){//在跑了
            float now_time=Time.fixedUnscaledTime;
            if(dicWaterServingSpdVariables["time_run_begin"]==0){dicWaterServingSpdVariables["time_run_begin"]=now_time;}//开始跑时记录开始时间，每次跑动结束后归零
            else{//正在跑动
                if(now_time-dicWaterServingSpdVariables["time_run_begin"]>dicWaterServingSpdVariables["lasting_time_threshold"]){//至少达到最低时间要求了
                    //Debug.Log("at least trying or already running");
                    if(now_time-dicWaterServingSpdVariables["time_run_begin"]>dicWaterServingSpdVariables["running_time_threshold"]){
                    //开始持续跑了
                        //Debug.Log("running");
                        if(now_time-dicWaterServingSpdVariables["time_served"]>dicWaterServingSpdVariables["time_serve_interval_when_runing"]){//持续跑动过程中随机给水
                            if(IsInRewardZone || !RewardZoneNeeded){    
                                if(dicWaterServingSpdVariables["random_delayed"]==1){//上次已经delay过
                                    Debug.Log("water served in running");    
                                    string temp_str="p_lick_mode1_delay=-2";
                                    if(moving.DataSend(temp_str, true, true)==-1){Debug.LogError("missing variable name: "+temp_str);}

                                    dicWaterServingSpdVariables["random_delayed"]=0;
                                    dicWaterServingSpdVariables["time_served"]=now_time;
                                }else{//随机delay
                                    dicWaterServingSpdVariables["time_served"]+=UnityEngine.Random.Range(0, random_delay_sec);
                                    dicWaterServingSpdVariables["random_delayed"]=1;
                                }
                            }
                        }
                    }else{
                    //间断短途跑动
                        if(now_time-dicWaterServingSpdVariables["time_served"]>dicWaterServingSpdVariables["time_serve_interval"]){
                            if(IsInRewardZone || !RewardZoneNeeded){
                                Debug.Log("water served in trying");
                                string temp_str="p_lick_mode1_delay=-2";
                                if(moving.DataSend(temp_str, true, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
                                dicWaterServingSpdVariables["time_served"]=now_time;
                            }
                        }
                    }
                }
            }
        }else{
            //没在跑
        }
    }

    // void ServeWaterLengthDepend(float length, int random_delay_sec=0, bool IsInRewardZone=true, bool RewardZoneNeeded=false){//random_delay_sec: 延迟随机x秒后给水
    void ServeWaterLengthDepend(float length, bool IsInRewardZone=true, bool RewardZoneNeeded=false){//random_delay_sec: 延迟随机x秒后给水
        //speed_threshold, lasting_time_threshold,      running_time_threshold,             time_serve_interval,        time_serve_interval_when_runing, time_run_begin, time_served
        //                  ↖短暂跑动的最低时间要求，约0.5s?     ↖判定持续跑动的最低时间要求，约2s?      ↖间断跑给水均服从第一个间隔  ↖持续跑给水均服从第二个间隔   
        if(length>dicWaterServingLengthVariables["length_threshold"]){//在跑了
            float now_time=Time.fixedUnscaledTime;
            // if(length>dicWaterServingLengthVariables["running_threshold"]){
            // //开始持续跑了
            //     if(now_time-dicWaterServingLengthVariables["time_served"]>dicWaterServingLengthVariables["time_serve_interval_when_runing"]){//持续跑动过程中随机给水
            //         if(IsInRewardZone || !RewardZoneNeeded){    
            //             if(dicWaterServingLengthVariables["random_delayed"]==1){//上次已经delay过
            //                 Debug.Log("water served in running");    
            //                 string temp_str="p_lick_mode1_delay=-2";
            //                 if(moving.DataSend(temp_str, true)==-1){Debug.LogError("missing variable name: "+temp_str);}

            //                 dicWaterServingLengthVariables["random_delayed"]=0;
            //                 dicWaterServingLengthVariables["time_served"]=now_time;
            //             }else{//随机delay
            //                 dicWaterServingLengthVariables["time_served"]+=UnityEngine.Random.Range(0, random_delay_sec);
            //                 dicWaterServingLengthVariables["random_delayed"]=1;
            //             }
            //         }
            //     }
            // }else{
            //间断短途跑动
                if(now_time-dicWaterServingLengthVariables["time_served"]>dicWaterServingLengthVariables["time_serve_interval"]){
                    if(IsInRewardZone || !RewardZoneNeeded){
                        Debug.Log("water served in trying");
                        string temp_str="p_lick_mode1_delay=-2";
                        if(moving.DataSend(temp_str, true, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
                        dicWaterServingLengthVariables["time_served"]=now_time;
                        LengthRecClear = true;
                    }
                }
            //}
        }
    }

    void ActivateFullSCreenColor(Color color){
        fullScreenColor.SetActive(true);
        fullScreenColor.GetComponent<Camera>().backgroundColor = color;
    }

    void DeactivateFillScreenColor(){
        fullScreenColor.SetActive(false);
    }

    //Transform tf_self;
    // Start is called before the first frame update
    void Awake() {
        m_rb = GetComponent<Rigidbody>();
        tf = GetComponent<Transform>();
        moving = GetComponent<LinearTrackMoving>();
        ui_update = GetComponent<LinearTrackUIUpdate>();
        rec_pos = new Vector3(X, transform.position.y, transform.position.z);
        DeactivateFillScreenColor();
        // fullScreenColor=Instantiate(prefab_quad, new Vector3(X-0.4f, 0, 0), prefab_quad.transform.rotation);
        // fullScreenColor.GetComponent<FullscreenColorQuad>().obj_main = gameObject;
        // fullScreenColor.SetActive(false);
    }

    void Start()
    {
        if(context_info_ls.Count()>0){
            lick_count_max=context_info_ls[0].lick_count_max;
        }
        else{//退出
            #if UNITY_EDITOR
            // Debug.Log("No context loaded!");
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    void Update(){
        float now_pos=tf.position[0];
        
        if(waiting!=0){//context结束结算
            
            moving.PauseMoving();
            ui_update.MessageUpdate(_pauseChange:true, _pauseMoving: true);
            if(! context_available){//trial init之后设为false,即结算后再同步
                if(!trial_syncing){
                    Trial_context_sync(now_context);
                }
                if(waiting!=-1){waiting=2;}
            }else{
                waiting=1;
            }

            tf.position=rec_pos;
            if(waiting==1 && Time.unscaledTime-counter>context_info_ls[now_context].wait_sec){//已同步完成，正常进入下一context
                waiting = 0;
                counter = 0;
                DeactivateFillScreenColor();
                // fullScreenColor.SetActive(false);
                //Destroy(temp_quad);
                pre_pos=tf.position[0];
                moving.ContinueMoving();
                ui_update.PositionIndicateUpdate(GetContextInfo());
                ui_update.MessageUpdate(_pauseChange:true, _pauseMoving: false);
                //保证黑屏时不能给水
                if(context_info_ls[now_context].start_pos[now_trial]+contextZoneStartAndEnd[now_context*2] >= rewardZoneStartAndEnd[now_context*2] && context_info_ls[now_context].start_pos[now_trial]+contextZoneStartAndEnd[now_context*2] < rewardZoneStartAndEnd[now_context*2+1]){
                    moving.Context_verify("p_enter_reward_context", 1);
                }

                if(now_trial>trial_per_section){
                    counter = Time.unscaledTime+10000;
                    ActivateFullSCreenColor(Color.green);
                    DeactivateFillScreenColor();
                    // fullScreenColor.GetComponent<FullscreenColorQuad>().quadColor = Color.green;
                    waiting = 1;
                        //pause or do something
                }
            }
        }
        else{//移动中

            int i_context_zone=now_context*2;
            bool IsInRewardZone = transform.position[0]>rewardZoneStartAndEnd[i_context_zone] && transform.position[0]<rewardZoneStartAndEnd[i_context_zone+1];

            ui_update.PositionIndicateUpdate(X - contextZoneStartAndEnd[now_context*2]);
            if (pre_pos<=contextZoneStartAndEnd[i_context_zone] && now_pos>contextZoneStartAndEnd[i_context_zone]){//进入context
                // now_context=Convert.ToInt32(i_context_zone*0.5);
            }
            else if (pre_pos<contextZoneStartAndEnd[i_context_zone+1] && now_pos>=contextZoneStartAndEnd[i_context_zone+1]){//离开context
                moving.Context_verify("p_enter_reward_context", -1);//保证黑屏时舔水不能给水
                if(context_info_ls[now_context].is_inf){
                    float temp_length_diff = X - lengthRec[1];
                    X = contextZoneStartAndEnd[i_context_zone];
                    lengthRec[1] = X - temp_length_diff;
                    rec_pos=tf.position;
                }
                else{
                    //IsInRewardZone = false;
                    waiting = 1;
                    float temp_length_diff = X - lengthRec[1];
                    X = contextZoneStartAndEnd[i_context_zone+1];//不超过context end
                    lengthRec[1] = X - temp_length_diff;
                    counter=Time.unscaledTime;
                    if(context_info_ls[now_context].wait_sec>0){
                        now_context_success=lick_count_rec[1]>=lick_count_succes_threshold[0] && lick_count_rec[1]<=lick_count_succes_threshold[1];
                        //temp_quad=Instantiate(prefab_quad, new Vector3(x, 0, 0), prefab_quad.transform.rotation);
                        ActivateFullSCreenColor(now_context_success? context_info_ls[now_context].succes_color: context_info_ls[now_context].fail_color);
                        // fullScreenColor.SetActive(true);
                        // //temp_quad.transform.position=new Vector3(x, 0, 0);
                        // fullScreenColor.GetComponent<FullscreenColorQuad>().quadColor=now_context_success? context_info_ls[now_context].succes_color: context_info_ls[now_context].fail_color;
                    }
                    ui_update.MessageUpdate($"now trial: {now_trial}, now_context: {now_context}, lick status: correct:{lick_count_correct}, rec: before {lick_count_rec[0]} in {lick_count_rec[1]} after {lick_count_rec[2]}\n");
                    Trial_args_init();//所有参数更新为下一个context，并开始等待
                }
            }//进入或离开context

            if (pre_pos<=rewardZoneStartAndEnd[i_context_zone] && now_pos>rewardZoneStartAndEnd[i_context_zone]){//进入奖励区
                //IsInRewardZone = true;
                string temp_str="p_enter_reward_context=1";
                if(moving.DataSend(temp_str, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
            }
            else if(pre_pos<rewardZoneStartAndEnd[i_context_zone+1] && now_pos>rewardZoneStartAndEnd[i_context_zone+1]){//离开奖励区
                //IsInRewardZone = false;
                string temp_str="p_enter_reward_context=-1";
                if(moving.DataSend(temp_str, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
            }//进入或离开reward_zone
 
            pre_pos=tf.position[0];

            if(serve_water_mode==1){ServeWaterSpeedDepend(m_rb.velocity.magnitude, 2, IsInRewardZone, true);}
            else if(serve_water_mode==2){
                if(lengthRec[0] != -1){
                    lengthRec[2] = X - lengthRec[1];
                }
                else{
                    lengthRec = new float[]{0, X, 0};
                }
                // ServeWaterLengthDepend(lengthRec[2], 2, IsInRewardZone, true);
                ServeWaterLengthDepend(lengthRec[2], IsInRewardZone, true);
            }
        }
    }

    void FixedUpdate()
    {

    }
}
