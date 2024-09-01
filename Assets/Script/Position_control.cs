using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class Position_control : MonoBehaviour
{
    public List<Context_info> context_info_ls = new List<Context_info>();
    private int now_trial=1; public int Now_trial{get{return now_trial;}}
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
    
    private Transform tf;
    private Rigidbody m_rb;
    private Moving moving;
    private UIUpdate ui_update;

    //private void Trial_args_init(int now_context, bool update=true ,bool init_all=false){
    public void Trial_args_init(bool update=true ,bool init_all=false){//清理位置信息，更新位置以及context相关参数
        if(update){

        }

        moving.DataSend("init");

        if(init_all){

        }
    }

    private int Trial_context_sync(int context_id){
        List<string> variables = new List<string>(){"p_enter_reward_context"};
        List<int> values = new List<int>(){-1};
        variables.Add("p_trial");
        values.Add(now_trial);

        int sync_result = moving.Context_verify(variables, values);
        if(sync_result==-2){
            ui_update.MessageUpdate("serial port not open!\n");
        }else{
            int sync_max_time=100;
            while(sync_result!=1 && sync_max_time>0){
                sync_result = moving.Context_verify(variables, values);
                if(sync_result!=1){
                    Debug.LogError("context info sync failed");
                }
                else{break;}
                sync_max_time--;
            }
        }
        
        variables.Clear();
        values.Clear();

        if(sync_result!=1){
            Debug.LogError("context info sync failed");
            return -1;
        }
        //ui_update.Message_update("information of next context synchronized\n");

        return 1;
    }

    public float[] GetContextInfo(){
        //  0           1           2           3           4           5       6,7,8
        //context, pos<relative>, length, reward start, reward end, is_inf, lick_count_rec
        float[] results = new float[9];
        // results[0] = now_context;
        // results[1] = X - contextZoneStartAndEnd[now_context*2];
        // results[2] = contextZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
        // results[3] = rewardZoneStartAndEnd[now_context*2] - contextZoneStartAndEnd[now_context*2];
        // results[4] = rewardZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
        // results[5] = context_info_ls[now_context].is_inf? 1: 0;
        // Array.ConstrainedCopy(lick_count_rec, 0, results, 6, 3);
        //results[1] = 
        return results;
    }

    //private Transform tf_self;
    // Start is called before the first frame update
    void Awake() {
        m_rb = GetComponent<Rigidbody>();
        tf = GetComponent<Transform>();
        moving = GetComponent<Moving>();
        ui_update = GetComponent<UIUpdate>();

        // temp_quad=Instantiate(prefab_quad, new Vector3(X-0.4f, 0, 0), prefab_quad.transform.rotation);
        // temp_quad.GetComponent<FullscreenColorQuad>().obj_main = gameObject;
        // temp_quad.SetActive(false);
    }

    void Start()
    {
        if(context_info_ls.Count()>0){

        }
        else{//退出
            #if UNITY_EDITOR
            Debug.Log("No context loaded!");
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
    }

    void Update(){
        float now_pos=tf.position[0];
        // temp_quad.SetActive(true);
        
    }

    void FixedUpdate()
    {

    }
}
