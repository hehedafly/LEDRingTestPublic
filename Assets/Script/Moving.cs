﻿using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;
using System.Text;
using System.Linq;
using System.IO;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Runtime.Remoting;
using TMPro;
using UnityEngine.Experimental.GlobalIllumination;

[System.Serializable]
class ContextInfo{

    public ContextInfo(string _start_method, string _available_pos_array, string _pump_pos_array, string _lick_pos_array, int _maxTrial, int _backgroundLight, float _barDelayTime, float _barLastingTime, float _waitFromLastLick, float _soundLength, string _trialTriggerDelay, string _trialInterval, float _s_wait_sec, float _f_wait_sec, float _trialExpireTime, int _trialStartType, string _assigned_pos = "", int _seed = -1){
        startMethod = _start_method;
        seed = _seed == -1? (int)DateTime.Now.ToBinary(): _seed;
        maxTrial = _maxTrial;
        backgroundLight = _backgroundLight;
        barDelayTime = _barDelayTime;
        barLastingTime = _barLastingTime;
        waitFromLastLick = _waitFromLastLick;
        soundLength = _soundLength;
        sWaitSec = _s_wait_sec;
        fWaitSec = _f_wait_sec;
        trialExpireTime = _trialExpireTime;
        trialTriggerMode = _trialStartType;
        barPosLs = new List<int>();

        avaliablePosArray = new List<int>();
        foreach(string availablePos in _available_pos_array.Split(',')){
            int temp_pos = Convert.ToInt16(availablePos) % 360;
            if(!avaliablePosArray.Contains(temp_pos))avaliablePosArray.Add(temp_pos);
        }
        avaliablePosArray.Sort();

        pumpPosLs = new List<int>();
        var _strPumpPos = _pump_pos_array.Split(",");
        if(_strPumpPos.Count() > 0 && _strPumpPos.Count() >= avaliablePosArray.Count()){
            foreach(string pos in _strPumpPos){
                //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                    pumpPosLs.Add(Convert.ToInt16(pos));
                //}
            }
        }else{
            foreach(int _ in avaliablePosArray){
                pumpPosLs.Add(pumpPosLs.Count());
            }
        }

        lickPosLs = new List<int>();
        var _strLickPos = _lick_pos_array.Split(",");
        if(_strLickPos.Count() > 0 && _strLickPos.Count() >= avaliablePosArray.Count()){
            foreach(string pos in _strLickPos){
                //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                    lickPosLs.Add(Convert.ToInt16(pos));
                //}
            }
        }else{
            foreach(int _ in avaliablePosArray){
                lickPosLs.Add(lickPosLs.Count());
            }
        }
        
        if(_start_method.StartsWith("random")){
            if(_maxTrial != -1){
                List<int> ints = new List<int>();
                for (int i = 0; i < avaliablePosArray.Count; i++){ints.Add(i % avaliablePosArray.Count);}
                for (int i=0; i<_maxTrial; i++){
                    if(i % ints.Count == 0){
                        Shuffle(ints);
                    }
                    barPosLs.Add(avaliablePosArray[ints[i % ints.Count]]);
                }
            }
        }else{
            foreach(string pos in _assigned_pos.Replace("..", "").Replace(" ", "").Split(',')){//form like 0,1,2,1 ...... or 0,1,0,2,1,1..  ...... or 0*100,1*100,0*50,1*50..
                try{
                    int _pos = -1;
                    int multiple = 1;
                    if(pos.Contains("*")){
                        _pos = Convert.ToInt16(pos.Substring(0, pos.IndexOf("*")));
                        multiple = Convert.ToInt16(pos.Substring(pos.IndexOf("*") + 1));
                    }else{
                        _pos = Convert.ToInt16(pos);
                    }

                    if(avaliablePosArray.Contains(Convert.ToInt16(_pos))){
                        for(int i = 0; i < multiple; i++){
                            barPosLs.Add(_pos % 360);
                        }
                    }
                }catch(Exception e){ 
                    if(pos.Contains("..")){
                        barPosLs.Add(Convert.ToInt16(pos.Replace("..", "")));
                    }else{
                        Debug.Log(e.Message);
                    }
                }
            }

            for(int i=barPosLs.Count(); i<_maxTrial; i++){
                if(_assigned_pos.EndsWith("..")){
                    barPosLs.Add(barPosLs[barPosLs.Count - 1]);
                }else{
                    barPosLs.Add(0);
                }
                //barPosLs.Add(avaiblePosArray[UnityEngine.Random.Range(0, avaiblePosArray.Count)]);
            }
        }

        if(_trialInterval.StartsWith("random")){
            string[] temp_ls=_trialInterval[6..].Split("~");
            if(temp_ls.Length==2){
                try{
                    trialInterval = new List<float>{-1, -1};
                    trialInterval[0] = Convert.ToSingle(temp_ls[0]);
                    trialInterval[1] = Convert.ToSingle(temp_ls[1]);
                    if(trialInterval[0] >= trialInterval[1]){
                        trialInterval[1] = trialInterval[0];
                    }
                }catch{
                    Debug.Log($"error in _trialWaitSec parse, invalid input: {temp_ls[0]}, {temp_ls[1]}");
                    trialInterval = new List<float>{15, 45};
                }
            }else{
                Debug.Log("error in _trialWaitSec parse");
                trialInterval = new List<float>{15, 45};
            }
        }else{
            trialInterval = new List<float>{-1, -1};
            if(float.TryParse(_trialInterval, out float _interval)){
                trialInterval[0] = _interval;
                trialInterval[1] = _interval;
            }
        }

        if(_trialTriggerDelay.StartsWith("random")){
            string[] temp_ls=_trialTriggerDelay[6..].Split("~");
            if(temp_ls.Length==2){
                try{
                    trialTriggerDelay = new List<float>{-1, -1};
                    trialTriggerDelay[0] = Convert.ToSingle(temp_ls[0]);
                    trialTriggerDelay[1] = Convert.ToSingle(temp_ls[1]);
                    if(trialTriggerDelay[0] >= trialTriggerDelay[1]){
                        trialTriggerDelay[1] = trialTriggerDelay[0];
                    }
                }catch{
                    Debug.Log($"error in _trialWaitSec parse, invalid input: {temp_ls[0]}, {temp_ls[1]}");
                    trialTriggerDelay = new List<float>{1.5f, 2};
                }
            }else{
                Debug.Log("error in _trialWaitSec parse");
                trialTriggerDelay = new List<float>{2, 3};
            }
        }else{
            trialTriggerDelay = new List<float>{-1, -1};
            if(float.TryParse(_trialTriggerDelay, out float _interval)){
                trialTriggerDelay[0] = _interval;
                trialTriggerDelay[1] = _interval;
            }
        }

    }
    //public string start_method;
    public string       startMethod     {get;}
    public List<int>    avaliablePosArray {get;}//最多8个，从角度0开始对位置顺时针编号0-7
    

    public List<int>    lickPosLs       {get;}//lick, pump等物理位置自己标定（顺时针或其他方式），按avaliable
    public List<int>    pumpPosLs       {get;}
    public int          maxTrial        {get;}
    public int          seed            {get;}
    public float        barDelayTime    {get;}
    public float        barLastingTime  {get;}
    public float        waitFromLastLick{get;}
    public float        soundLength     {get;}
    public int          backgroundLight {get;}
    public List<float>  trialInterval   {get;}
    public float        sWaitSec        {get;}
    public float        fWaitSec        {get;}
    public int          trialTriggerMode{get;}
    public List<float>  trialTriggerDelay{get;}
    public float        trialExpireTime {get;}

    [JsonIgnore]
    List<int>    barPosLs        {get;}
    [JsonIgnore]

    public float soundCueLeadTime   {get;set;}

    void Shuffle(List<int> _ints){
        for (int i = 0; i < _ints.Count; i++){
            int rand = UnityEngine.Random.Range(0, _ints.Count - i);
            (_ints[_ints.Count - 1 - i], _ints[rand]) = (_ints[rand], _ints[_ints.Count - 1 - i]);
        }
    }

    public float GetDeg(int pos){
        if(pos < 0 || pos > avaliablePosArray.Count){return -1;}
        return avaliablePosArray[pos];
    }

    public int GetRightLickPosIndInTrial(int trial){
        if(trial < 0 || trial > barPosLs.Count){return -1;}
        return lickPosLs[avaliablePosArray.IndexOf(barPosLs[trial])];
    }

    public int GetPumpPosInTrial(int trial){
        if(trial < 0 || trial > barPosLs.Count){return -1;}
        return pumpPosLs[GetRightLickPosIndInTrial(trial)];
    }

    public float GetDegInTrial(int trial){
        if(trial < 0 || trial > barPosLs.Count){return -1;}
        return barPosLs[trial];
    }

    public bool verify(int lickInd, int trial, out int realLickInd){//传入的lickInd为RawInd,需要经过LickPos转换
        if(lickInd < 0 || lickInd > lickPosLs.Count()){
            realLickInd = -1;
            return false;
        }
        int rightBarPosInd = GetRightLickPosIndInTrial(trial);
        realLickInd = lickPosLs[lickInd];

        if(lickInd >= avaliablePosArray.Count){return false;}
        return lickInd == lickPosLs[rightBarPosInd];
    }
}

public class Moving : MonoBehaviour
{
    bool InApp = false;
    public GameObject background;
    public GameObject barPrefab;
    public GameObject circleBarPrefab;
    public GameObject centerShaftPrefab;
    public UnityEngine.UI.Slider slider;
    public Material materialMissing;
    int displayPixels;
    bool isRing;
    GameObject bar;
    GameObject barChild;
    GameObject barChild2;
    GameObject centerShaft;
    int barWidth;
    float trialInitTime = 0;    public float TrialInitTime {get{return trialInitTime;}}
    float trialStartTime = -1;
    float waitSecRec = -1;
    float waitSec = -1;
    bool waiting = true;
    bool forceWaiting = true;
    public bool ForceWaiting { get { return forceWaiting; } set { forceWaiting = value; } }
    int trialMode = 0x00;// 0x?0, 0x?1 : 0: 舔到对的进入下一个trial，无论其他; 1: 只能舔对的，舔到错的
                         // 0x0?, 0x1? : 0:trial开始就给水; 1:舔了才给水
                        public int TrialMode { get { return trialMode; } }
                                List<int> trialModes = new List<int>(){0x00, 0x01, 0x10, 0x11};
                        public  List<int> TrialModes { get { return trialModes; } }

    int trialStartTriggerMode = 0;//0:定时, 1:红外, 2:压杆
    public int TrialStartTriggerMode {get{return trialStartTriggerMode;}}
    List<string> trialStartTriggerModeLs = new List<string>(){"延时", "红外", "压杆"};
    bool trialStartReady = false;
    List<int> trialResult = new List<int>();//1:success 0:fail -1:manully skip -2:manually successful skip -3:unimportant fail(in mode 0x00 and 0x01)
    List<List<int>> trialResultPerLickPort = new List<List<int>>();//0, 2, 4, 6,...success/fail, 1, 3, 5, 7,...:miss
    
    List<List<int>> lickCount = new List<List<int>>();
    AudioSource audioSource;
    float[] audioPlayTime = new float[3];
    UIUpdate ui_update;
    Alarm alarm;

    #region communicating
    List<string> port_black_list = new List<string>();
    SerialPort sp = null;
    CommandConverter commandConverter;
    List<string> ls_types = new List<string>(){"lick", "entrance", "press", "context_info", "log", "echo", "value_change", "command"};
    List<byte[]> serial_read_content_ls = new List<byte[]>();//仅在串口线程中改变
    int serialReadContentLsMark = -1;
    float commandVerifyExpireTime = 2;//2s
    ManualResetEvent manualResetEventVerify = new ManualResetEvent(true);
    //readonly object lockObject_command = new object();
    ConcurrentQueue<byte[]> commandQueue = new ConcurrentQueue<byte[]>();
    public ConcurrentDictionary<float, string> commandVerifyDict = new ConcurrentDictionary<float, string>();
    List<string> Arduino_var_list =  "p_lick_mode, p_trial, p_trial_set, p_now_pos, p_lick_rec_pos, p_water_flush".Replace(" ", "").Split(',').ToList();
    List<string> Arduino_ArrayTypeVar_list =  "p_waterServeMills, p_lick_count".Replace(" ", "").Split(',').ToList();
    Dictionary<string, string> Arduino_var_map =  new Dictionary<string, string>{};//{"p_...", "0"}, {"p_...", "1"}...
    Dictionary<string, string> Arduino_ArrayTypeVar_map =  new Dictionary<string, string>{};
    #endregion communicating end

    #region  context generate
    #if UNITY_EDITOR
        private string config_path="Assets/Resources/config.ini";
    #else
        private string config_path;
    #endif
    IniReader iniReader;

    int nowTrial = 0; public int NowTrial{get{return nowTrial;}}
    ContextInfo contextInfo;

    public float DegToPos(float deg){
        if(displayPixels <= 0){return -1;}
        float temp_value = deg % 360 /360;
        return (float)((temp_value - 0.5)*(displayPixels/10));
    }

    public void SetBarPos(float actual_pos){//0-1，角度输入时需要配合DegToPos
        bar.transform.localPosition = new Vector3(actual_pos, bar.transform.localPosition.y, bar.transform.localPosition.z);
    }

    void SetBarMaterial(bool isDriftGrating, float _speed = 1, float _frequency = 5, int _direction = 1, int _horizontal = 0, string _mat = "#000000", float _backgroundLight = -1, GameObject otherBar = null){
        GameObject tempBar = otherBar == null? bar: otherBar;
        Debug.Log(tempBar.name);
        Debug.Log(bar.GetComponent<MeshRenderer>().material.shader.name);
        if(isDriftGrating){
            // var tempMaterial = new Material(Shader.Find("Unlit/Color"))//会出错，暂时不用
            // {
            //     shader = Shader.Find("ShaDriftGrating")
            // };
            var tempMaterial = tempBar.GetComponent<MeshRenderer>().material;
            tempMaterial.SetFloat("_speed", _speed);
            tempMaterial.SetFloat("_Frequency", _frequency);
            tempMaterial.SetFloat("_Direction", _direction);
            tempMaterial.SetFloat("_Horizontal", _horizontal);
            if(_backgroundLight >= 0){
            tempMaterial.SetFloat("_BackgroundLight", _backgroundLight);
            tempMaterial.SetFloat("_Frequency", _frequency * 2);
            }
            tempBar.GetComponent<MeshRenderer>().material = tempMaterial;
            if(isRing && otherBar == null){
                barChild.GetComponent<MeshRenderer>().material = tempMaterial;
                barChild2.GetComponent<MeshRenderer>().material = tempMaterial;
            }

        }else{
            Material tempMaterial = materialMissing;
            if(_mat.StartsWith("#")){
                Color color;
                if(!ColorUtility.TryParseHtmlString(_mat, out color)){
                    return;
                }
                tempMaterial = null;
                tempMaterial = new Material(Shader.Find("Unlit/Color")){color = color};
            }else{
                string tempPath = InApp? Application.dataPath+$"/Resources/{_mat}.png" : $"Assets/Resources/{_mat}.png";

                if(System.IO.File.Exists(tempPath)){
                    //创建文件读取流
                    FileStream fileStream = new FileStream(tempPath, FileMode.Open, FileAccess.Read);
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
                    tempMaterial.SetTexture("_MainTex", texture);
                    tempMaterial.mainTextureScale = new Vector2(barWidth/400, 5);
                }
            }

            tempBar.GetComponent<MeshRenderer>().material = tempMaterial;
            if(isRing && otherBar == null){
                barChild.GetComponent<MeshRenderer>().material = tempMaterial;
                barChild2.GetComponent<MeshRenderer>().material = tempMaterial;
            }
        }
    }

    void ActivateBar(int pos = -1, int trial = -1){
        Debug.Log("bar activited");
        if(pos != -1){
            SetBarPos(DegToPos(contextInfo.GetDeg(pos)));
        }else if(trial != -1){
            SetBarPos(DegToPos(contextInfo.GetDegInTrial(trial)));
        }else{
            return;
        }
        bar.SetActive(true);
        if(barChild != null && barChild2 != null){
            barChild.SetActive(true);
            barChild2.SetActive(true);
        }
    }

    void DeactivateBar(){
        bar.SetActive(false);
        if(barChild != null && barChild2 != null){
            barChild.SetActive(false);
            barChild2.SetActive(false);
        }
    }

    int InitContext(bool isDriftGrating, float _speed, float _frequency, int _direction, int _horizontal, string _matName, bool _isCircleBar = false, bool _centerShaft = false, float _centerShaftPos = 180, string _centerShaftMat = "#000000"){
        float displayLength = displayPixels / 18;
        GameObject tempPrefab = _isCircleBar? circleBarPrefab: barPrefab;
        bar = Instantiate(tempPrefab);
        if(!_isCircleBar){
            bar.transform.localScale = new Vector3(displayLength*(float)barWidth/displayPixels *0.1f, 1, bar.transform.localScale.z);
        }else{
            bar.transform.localScale = new Vector3(displayLength*(float)barWidth/displayPixels *0.1f, 1, displayLength*(float)barWidth/displayPixels *0.1f);
        }
        
        if(isRing){
            barChild = Instantiate(tempPrefab);
            barChild.transform.localScale = new Vector3(displayLength*(barWidth/displayPixels) *0.1f, 1, barChild.transform.localScale.z);
            //barChild.transform.SetParent(transform.parent.transform);
            barChild.transform.SetParent(bar.transform);
            bar.transform.localPosition = new Vector3(0, 0, -0.01f);
            barChild.transform.localPosition = new Vector3(-1*displayLength, 0, 0);

            barChild2 = Instantiate(tempPrefab);
            barChild2.transform.localScale = new Vector3(displayLength*(barWidth/displayPixels) *0.1f, 1, barChild2.transform.localScale.z);
            //barChild2.transform.SetParent(transform.parent.transform);
            barChild2.transform.SetParent(bar.transform);
            barChild2.transform.localPosition = new Vector3(displayLength, 0, 0f);
        }
        
        SetBarMaterial(isDriftGrating, _speed, _frequency, _direction, _horizontal, _matName, (float)Math.Clamp((float)contextInfo.backgroundLight / 255, 0, 0.8f));
        if(_centerShaft){
            centerShaft = Instantiate(centerShaftPrefab);
            SetBarMaterial(false, 0, 0, 0, _horizontal, _centerShaftMat, otherBar: centerShaft);
            centerShaft.transform.localPosition = new Vector3(DegToPos(_centerShaftPos), bar.transform.localPosition.y, bar.transform.localPosition.z);
        }

        float lightStrength = (float)Math.Clamp((float)contextInfo.backgroundLight / 255, 0, 0.8);
        background.GetComponent<Renderer>().material.color = new Color(lightStrength, lightStrength, lightStrength);
        
        DeactivateBar();

        return 1;
    }

    int ContextInitSync(){
        List<string> names = new List<string>(){"p_lick_mode", "p_trial"};
        List<int> values = new List<int>(){trialMode % 0x10, 0};
        DataSend("init");
        int res = Context_verify(names, values);
        //DataSend("p_trial_set=1", true);
        return res;
    }

    int ContextStartSync(){
        List<string> names = new List<string>(){"p_trial", "p_now_pos"};
        List<int> values = new List<int>(){nowTrial, contextInfo.GetPumpPosInTrial(nowTrial)};
        int res = Context_verify(names, values);
        DataSend("_");
        DataSend("p_trial_set=1", true, true);
        Debug.Log("sent :p_trial_set=1");
        return res;
    }

    int ContextEndSync(){
        DataSend("p_trial_set=0", true);
        DataSend("_");
        return Context_verify("p_trial_set", 0);
    }

    public int SetTrial(bool manual, bool waitSoundCue, float _waitSec = -1){//延时触发仅在最初调用，其他主动触发调用此方法进行startTrial
        if(manual){
            if(trialStartTriggerMode != 0){
                trialStartReady = true;
                ui_update.MessageUpdate("Ready");
            }else{

            }
            InitTrial();
        }
        
        if(trialStartTriggerMode == 0 && manual || (!manual && trialStartReady == true)){
            if(waitSoundCue && audioPlayTime[0] > 0){
                // waiting = true;
                // float _waitSec = contextInfo.soundCueLeadTime + 0.5f;
                // waitSec = _waitSec;
                // waitSecRec = Time.fixedUnscaledTime;
                audioSource.Play();
                audioPlayTime[1] = Time.fixedUnscaledTime;
                audioPlayTime[2] = Time.fixedUnscaledTime + audioPlayTime[0];
                alarm.TrySetAlarm("StartTrialWaitSoundCue", _waitSec == -1? audioPlayTime[0]: _waitSec, out _);
                trialStartReady = false;//无论声音，无论mode，到这步直接设false
                WriteInfo(recType: 7);
                return 0;
            }else{
                StartTrial();
                trialStartReady = false;//无论声音，无论mode，到这步直接设false
                return 0;
            }
        }else{
            return -1;
        }
    }

    int InitTrial(){
        nowTrial = -1;
        ContextInitSync();
        forceWaiting = false;
        waitSec = -1;
        waitSecRec = -1;
        lickCount.Clear();
        trialResult.Clear();
        trialResultPerLickPort.Clear();
        trialResultPerLickPort = new List<List<int>>(){};
        foreach(int _ in contextInfo.avaliablePosArray){
            trialResultPerLickPort.Add(new List<int>());
            trialResultPerLickPort.Add(new List<int>());
        }
        audioPlayTime = new float[]{audioPlayTime[0], -1, -1};
        audioSource.Stop();
        trialInitTime = Time.fixedUnscaledTime;
        ui_update.MessageUpdate("Trial initialized");
        return 0;
    }

    int StartTrial(bool isInit = false){//根据soundCueLeadTime在alarm中设置waiting
        // if(isInit){
        //     InitTrial();
        // }else{
        // }
        nowTrial++;
        trialStartTime = Time.fixedUnscaledTime;
        ContextStartSync();

        //waiting = false;
        alarm.SetAlarm(1, (int)(contextInfo.barDelayTime/Time.fixedDeltaTime), "SetWaitingToFalseAtTrialStart");
        ActivateBar(trial: nowTrial);

        int nowBarPos = contextInfo.GetRightLickPosIndInTrial(nowTrial);
        string _tempMsg = $"Trial {nowTrial} started at {nowBarPos}, pos {contextInfo.GetRightLickPosIndInTrial(nowTrial)}, start type {trialStartTriggerMode}";
        if(nowTrial > 0){
            string strLickCount = "";
            foreach(int count in lickCount[nowTrial-1]){
                strLickCount += count.ToString() + "\t";
            }
            _tempMsg += $", lickCount before: {strLickCount}";
        }
        ui_update.MessageUpdate(_tempMsg);
        WriteInfo(recType: 1);
        List<int> ints = new List<int>();
        for(int i = 0; i < contextInfo.avaliablePosArray.Count; i++){
            ints.Add(0);
        }
        lickCount.Add(ints);
        ui_update.MessageUpdate();
        return 1;
    }

    int EndTrial(bool isInit = false, bool trialSuccess = false, int rightLickPort = -1, float trialReadyWaitSec = -1){
        ContextEndSync();
        if(isInit || !trialSuccess){DeactivateBar();}
        else{alarm.SetAlarm(0, (int)(contextInfo.barLastingTime/Time.fixedDeltaTime), "DeactiveBar");}

        waiting = true;
        WriteInfo(recType: isInit? 3: 2, _lickPos: rightLickPort);
        //Debug.Log("rightLickPort" + rightLickPort);

        if(!isInit){
            waitSecRec = Time.fixedUnscaledTime;
            //lickCount.Clear();
            if(contextInfo.trialInterval[0] > 0){
                float _temp_waitSec;
                if(trialStartTriggerMode == 0){
                    _temp_waitSec = UnityEngine.Random.Range(contextInfo.trialInterval[0], contextInfo.trialInterval[1]);
                }else{//其他主动触发模式，用于intervalCheck
                    //_temp_waitSec = contextInfo.soundLength + contextInfo.soundCueLeadTime + (trialSuccess? contextInfo.barDelayTime: 0);
                    _temp_waitSec = trialSuccess? contextInfo.barDelayTime: 0;
                }

                if(_temp_waitSec > 0){
                    ui_update.MessageUpdate($"wait sec: {_temp_waitSec}");
                    Debug.Log($"wait sec: {_temp_waitSec}");
                }
                waitSec = _temp_waitSec;
            }else{
                if(trialStartTriggerMode == 0){
                    waitSec = trialResult[nowTrial] == 1? contextInfo.sWaitSec : contextInfo.fWaitSec;
                }else{//其他主动触发模式
                    //waitSec = contextInfo.soundLength + contextInfo.soundCueLeadTime + (trialSuccess? contextInfo.barDelayTime: 0);
                    // ui_update.MessageUpdate($"wait sec: {waitSec}");
                    // Debug.Log($"wait sec: {waitSec}");
                }
            }
            if(trialReadyWaitSec <= 0) {trialStartReady = true;}
            else{
                alarm.TrySetAlarm("SetTrialReadyToTrue", trialReadyWaitSec, out _);
            }
        }
        
        ui_update.MessageUpdate();
        return 1;
    }

    int IntervalCheck(){//-2:完全处于空闲时期，0：已经可以开始下一个trial，1/-1：已经可以播放声音
        //waitSecRec以及waitSec每个trial不初始化，可以持续使用
        float soundCueLeadTime = contextInfo.soundCueLeadTime;
        float _lasttime = waitSec - (Time.fixedUnscaledTime - waitSecRec);
        if(waitSec == -1 || waitSecRec == -1){
            return -2;
        }

        if(Time.fixedUnscaledTime - waitSecRec >= waitSec){
            return 0;
        }
        else if(Math.Abs(_lasttime - (audioPlayTime[0] + soundCueLeadTime)) <= Time.fixedUnscaledDeltaTime * 0.5){
            return soundCueLeadTime > 0? 1: -1;
        }
        else{
            return -2;
        }
    }

    public int ChangeMode(int _mode){
        if(_mode == trialMode){
            return 0;
        }else{
            if(_mode % 0x10 < 2){
                trialResult.Clear();
                trialResultPerLickPort.Clear();
                EndTrial(isInit: true);
                trialMode = _mode;
                nowTrial = -1;
                forceWaiting = true;
                int temp_sync_result = ContextInitSync();
                trialInitTime = 0;
                return temp_sync_result;
            }else{
                return -1;
            }
        }
    }

    public int ChangeTriggerMode(int _triggerMode){
        if(_triggerMode == trialStartTriggerMode){
            return 0;
        }else{

            if(_triggerMode < 3 && IntervalCheck() == -2){
                trialStartTriggerMode = _triggerMode;
                waitSec = -1;
                waitSecRec = -1;
                //不清理当前正在进行的trial

                if(trialStartTriggerMode == 0){ui_update.MessageUpdate($"interval: {contextInfo.trialInterval[0]} ~ {contextInfo.trialInterval[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
                else{                          ui_update.MessageUpdate($"interval: {contextInfo.trialTriggerDelay[0]} ~ {contextInfo.trialTriggerDelay[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
                return 0;
            }else{
                return -1;
            }
        }
    }

    int lickCountGetSet(string getOrSet, int lickInd, int lickTrial){
        if(lickInd < 0){return -1;}

        if(getOrSet == "set"){
            if(lickCount.Count <= lickTrial){
                List<int> ints = new List<int>();
                for(int i = 0; i < contextInfo.avaliablePosArray.Count; i++){
                    ints.Add(0);
                }
                ints[lickInd] ++;
                lickCount.Add(ints);
            }else{
                //Debug.Log(string.Join(",", lickCount[nowTrial]));
                lickCount[nowTrial][lickInd]++;
            }
        }else if(getOrSet == "get"){
            if(lickTrial >= lickCount.Count){
                Debug.Log($"index out of lickCountLs range: lickInd {lickInd}, list length {lickCount.Count}");
            }
        }else{
            return lickCount[lickTrial][lickInd];
        }
        return 1;
    }

    int LickResultAdd(int result, int trial, int lickPort, int rightLickPort, bool force = false){
        if(trialResult.Count() == trial){
            trialResult.Add(result);
            if(lickPort >= 0){
                trialResultPerLickPort[lickPort*2].Add(result);
                if(result == 0){
                    trialResultPerLickPort[rightLickPort*2+1].Add(1);
                }
            }
        }else{
            if(trialResult.Count() > trial){
                if(force){trialResult[trial] = result;}
                else{return 0;}
            }else{
                return -1;
            }
        }
        return 1;
    }
    public int LickResultCheckPubic(int lickInd){
        return LickResultCheck(lickInd, nowTrial);
    }
    int LickResultCheck(int lickInd, int lickTrial){//check后判断是否结束当前trial, lickInd = -1: 超时; -2: 手动成功进入下一个trial
        int rightLickInd = contextInfo.GetRightLickPosIndInTrial(lickTrial);
        int realLickInd;
        if(nowTrial == -1){
            return 0;
        }
        if(lickTrial != nowTrial){
            Debug.LogWarning("trial not sync");
            lickTrial = nowTrial;
        }
        if(lickInd >= contextInfo.avaliablePosArray.Count){
            Debug.LogWarning($"invalid Lick Port: {lickInd}");
            return -1;
        }

        lickCountGetSet("set", lickInd, lickTrial);

        if(!waiting){
            WriteInfo(_lickPos: lickInd);
        }

        if(!waiting){//waiting期间的舔不进一步进入判断，仅做记录
            bool result = contextInfo.verify(lickInd, nowTrial, out realLickInd);
            // if(trialResult.Count == nowTrial+1){
            //     return -2;//错误trial
            // }

            if(trialMode < 0x20){
                if(trialMode < 0x10){//只要舔到对的就进入下一个，不管错没错，待endtrial结束进入下一个trial
                    if(result || lickInd == -2){
                        if(lickInd >= 0){
                            LickResultAdd(1, nowTrial, lickInd, rightLickInd);
                            //trialResult.Add(1);
                            if(trialMode == 0x01){Context_verify("p_trial_set", 2);}
                            ui_update.MessageUpdate($"Trial completed at pos {realLickInd}");
                        }else{//手动跳过或结束trial
                            LickResultAdd(lickInd == -2? 1 : 0, nowTrial, lickInd, rightLickInd);
                            //trialResult.Add(lickInd == -2? 1 : 0);
                            if(lickInd == -1){
                                ui_update.MessageUpdate($"Trial expired at pos {realLickInd}");
                            }else if(lickInd == -2){
                                ui_update.MessageUpdate($"Trial completed manually at pos {realLickInd}");
                            }
                        }
                        EndTrial(trialSuccess: true, rightLickPort: rightLickInd, trialReadyWaitSec: contextInfo.barLastingTime);
                    }else{
                        LickResultAdd(lickInd == -1? -1: -3, nowTrial, lickInd, rightLickInd);
                        if(lickInd == -1){
                            EndTrial(trialSuccess:false, rightLickPort: rightLickInd, trialReadyWaitSec: contextInfo.barLastingTime);
                        }
                    }
                }else{//只能舔对的
                    result = result || lickInd == -2;
                    LickResultAdd(result? 1: 0, nowTrial, lickInd, rightLickInd);
                    if(result && trialMode == 0x11){Context_verify("p_trial_set", 2);}
                    if(lickInd < 0){
                        ui_update.MessageUpdate($"Trial {(result? "skipped manually": "expired")} at pos {realLickInd}, right place: {rightLickInd}");
                    }else{
                        ui_update.MessageUpdate($"Trial {(result? "success": "failed")} at pos {realLickInd}, right place: {rightLickInd}");
                    }
                    EndTrial(trialSuccess: result, rightLickPort: rightLickInd, trialReadyWaitSec: result? contextInfo.barLastingTime : 0);
                }
                ui_update.MessageUpdate();

            }else{
                //mode错误
                //trialResult.Add(-1);
            }
            return 1;//正常判断完成
        }else{
            ui_update.MessageUpdate();
            //alarm.SetAlarm("StartTrialWaitSoundCue", (int)alarm.GetAlarm("StartTrialWaitSoundCue")+1);
            // alarm.DeleteAlarm("StartTrialWaitSoundCue", forceDelete:true);
            // WriteInfo(recType:6);//计expire
            //暂时不需要在trial间对lick做进一步处理
            //waitSecRec = Time.fixedUnscaledTime;
            //Debug.Log($"still licking");
            return -3;//不需要判断，已返回给commandParse做延时处理
        }
    }

    public Dictionary<string, int> GetTrialInfo(){
        int showLickPortNum = contextInfo.avaliablePosArray.Count;
        /*
        "NowTrial"    
        "IsPausing"   
        "NowPos"      
        "lickPosCount"
        "waitSec"
        "lickCountArrayLength"
        "lickCount"0,1,2...
        "TrialSuccessNum"0,1,2...
        "TrialFailNum"0,1,2...
        "LickPortTotalTrial"0,1,2...
        */
        Dictionary<string, int> trialInfo = new Dictionary<string, int>
        {
            {"NowTrial"             , nowTrial},
            {"IsPausing"            , forceWaiting? 1: 0},
            {"NowPos"               , contextInfo.GetRightLickPosIndInTrial(nowTrial)},
            {"lickPosCount"         , showLickPortNum},
            {"waitSec"              , Convert.ToInt16(waitSec)},
            {"lickCountArrayLength" , lickCount.Count},
            {"TrialSuccessNum"  , trialResult.FindAll(value => value == 1).Count},
            {"TrialFailNum"     , trialResult.FindAll(value => value <= 0).Count},
        };
        if(lickCount.Count > 0){
            if(lickCount.Count > nowTrial){
                for(int i = 0; i < showLickPortNum; i++){
                    trialInfo.Add($"lickCount{i}", lickCount[nowTrial][i]);
                }
            }
            else{// start
                for(int i = 0; i < showLickPortNum; i++){
                    trialInfo.Add($"lickCount{i}", 0);
                }
            }
        }
        if(trialResultPerLickPort.Count > 0){
            for(int i = 0; i < showLickPortNum; i++){
                trialInfo.Add($"TrialSuccessNum{i}"     , trialResultPerLickPort[i * 2].FindAll(value => value == 1).Count);
                trialInfo.Add($"TrialFailNum{i}"        , trialResultPerLickPort[i * 2].FindAll(value => value == 0).Count);
                trialInfo.Add($"TrialMissNum{i}"        , trialResultPerLickPort[i * 2+1].Count);
                trialInfo.Add($"LickPortTotalTrial{i}"  , trialResultPerLickPort[i * 2].Count);
            }
        }else{
            for(int i = 0; i < showLickPortNum; i++){
                trialInfo.Add($"TrialSuccessNum{i}", 0);
                trialInfo.Add($"TrialFailNum{i}", 0);
                trialInfo.Add($"TrialMissNum{i}", 0);
                trialInfo.Add($"LickPortTotalTrial{i}", 0);
            }
        }

        return trialInfo;
    }

    #endregion context generate end

    #region  file writing
    StreamWriter streamWriter;
    string filePath = "";
    List<string> logList = new List<string>();  public List<string> LogList { get { return logList; } }
    Queue<string> writeQueue = new Queue<string>();
    const int BUFFER_SIZE = 256;
    const int BUFFER_THRESHOLD = 32;
    float[] time_rec_for_log = new float[2]{0, 0};
    #endregion file writing end
    
    #region methods of communicating
    string[] ScanPorts_API(){
        string[] portList = SerialPort.GetPortNames();
        return portList;
    }

    public void CommandParsePublic(string limitedCommand){//仅接收舔、红外、压杆信号模拟
        string tempHead = limitedCommand.Split(":")[0];
        switch(tempHead){
            case "lick":{
                break;
            }
            case "entrance":{
                break;
            }
            case "press":{
                break;
            }
            default:{
                return;
            }
        }
        CommandParse(commandConverter.ProcessSerialPortBytes(commandConverter.ConvertToByteArray(limitedCommand)));
    }
    void CommandParse(byte[] _command){//在主线程调用时内容不能有锁,目前全部在主线程调用
        //看注释！
        //看注释！
        //看注释！
        //"lick", "entrance", "press", "context_info", "log", "echo", "value_change", "command"
        //int startInd = -1;
        int temp_type = commandConverter.GetCommandType(_command, out _);
        string command = commandConverter.ConvertToString(_command);
        //Debug.Log(command);
        switch(temp_type){
            case 0:{//lick, format: lick:lickInd:TrialMark
                int lickInd = Convert.ToInt16(command.Split(":")[1]);
                int lickTrialMark = Convert.ToInt16(command.Split(":")[2]);
                float waitFromLastLick = contextInfo.waitFromLastLick;
                float soundCueLeadTime = contextInfo.soundCueLeadTime;

                if(LickResultCheck(lickInd, lickTrialMark) == -3 && waitFromLastLick > 0){
                    //Debug.Log(waitSec - (Time.fixedUnscaledTime - waitSecRec));
                    if(trialStartTriggerMode == 0){
                        float _lasttime = waitSec - (Time.fixedUnscaledTime - waitSecRec);
                        if(waitSec != -1 && _lasttime <= waitFromLastLick && (trialStartTriggerMode != 0 || _lasttime > (audioPlayTime[0] + soundCueLeadTime))){//目前主动触发时声音立即发生，舔的时候需要延迟bar出现时间。
                            waitSecRec = Time.fixedUnscaledTime - waitSec + waitFromLastLick;//声音出现之前，trial开始前舔则延迟trial开始
                        }
                    }else{
                        float _lasttime = alarm.GetAlarm("StartTrialWaitSoundCue") * Time.fixedUnscaledDeltaTime;
                        if(_lasttime > 0){
                            alarm.SetAlarm("StartTrialWaitSoundCue", (int)(Math.Max(_lasttime, contextInfo.waitFromLastLick) / Time.fixedUnscaledDeltaTime));
                        }
                    }
                }
                break;
            }
            case 1:{//entrance
                //Debug.Log("enter");
                bool inOrLeave = command.EndsWith("In");
                if(trialStartTriggerMode == 1){
                    if(inOrLeave){
                        if(contextInfo.soundLength > 0 && contextInfo.trialTriggerDelay[0] > 0){
                            //alarm.TrySetAlarm("SetTrialInfraRedLightDelay", (int)(contextInfo.trialTriggerDelay/Time.fixedUnscaledDeltaTime), out _);
                            contextInfo.soundCueLeadTime = UnityEngine.Random.Range(contextInfo.trialTriggerDelay[0], contextInfo.trialTriggerDelay[1]);
                            SetTrial(manual:false, waitSoundCue: true, _waitSec: contextInfo.soundCueLeadTime  + audioPlayTime[0]);
                        }else{
                            SetTrial(manual:false, waitSoundCue: false);
                        }
                    }
                    WriteInfo(recType:4, _lickPos:inOrLeave? 1: 0);
                    //ui_update.MessageUpdate("enter");
                }
                break;
            }
            case 2:{//press
                Debug.Log("Lever Pressed");
                if(trialStartTriggerMode == 2){
                    if(contextInfo.trialTriggerDelay[0] > 0){
                        //alarm.TrySetAlarm("SetTrialPressDelay", (int)(UnityEngine.Random.Range(contextInfo.trialTriggerDelay[0], contextInfo.trialTriggerDelay[1])/Time.fixedUnscaledDeltaTime), out _);
                        contextInfo.soundCueLeadTime = UnityEngine.Random.Range(contextInfo.trialTriggerDelay[0], contextInfo.trialTriggerDelay[1]);
                        SetTrial(manual:false, waitSoundCue: true, _waitSec: contextInfo.soundCueLeadTime  + audioPlayTime[0]);
                    }else{
                        SetTrial(manual:false, waitSoundCue: true);
                    }
                    WriteInfo(recType:5);
                    ui_update.MessageUpdate("Lever Pressed");
                }
                break;
            }
            case 3:{//cotext_info

                break;
            }
            case 4:{//log
                //if(!command.Contains("received")){ui_update.MessageUpdate(command+"\n");}
                //ui_update.MessageUpdate(command);
                Debug.Log($"received :\"{command}\" at {Time.unscaledTime}");
                break;
            }
            case 5:{//echo
                command = command["echo:".Length..];
                if(command.Contains(":echo")){
                    List<float> temp_keys = commandVerifyDict.Keys.ToList();
                    temp_keys.Sort();
                    foreach(float time in temp_keys){
                        if(commandVerifyDict.ContainsKey(time) && commandVerifyDict[time].CompareTo(command[..command.IndexOf(":echo")]) == 0){
                            //Debug.Log("verified: "+commandVerifyDict[time]);
                            commandVerifyDict.Remove(time, out _);
                        }
                    }
                }
                //Debug.Log($"received :\"{command}\" at {Time.unscaledTime}");
                break;
            }
            default: break;
        }
    }

    void DataReceived(){
        while (true){
            manualResetEventVerify.WaitOne();
            if (sp!= null && sp.IsOpen){
                int count = sp.BytesToRead;
                if (count > 0){
                    byte[] readBuffer = new byte[count];
                    try{
                        sp.Read(readBuffer, 0, count);
                        //Debug.Log("received in second tread"+string.Join(",", readBuffer));
                    }
                    catch (Exception ex){
                        Debug.Log(ex.Message);
                        continue;
                    }
                    serial_read_content_ls.Add(readBuffer);
                    if(commandConverter.FindMarkOfMessage(true, readBuffer, 0)!=-1){
                        serialReadContentLsMark=serial_read_content_ls.Count()-1;
                    }
                    int temp_end=-1;
                    if(serialReadContentLsMark!=-1){
                        temp_end=commandConverter.FindMarkOfMessage(false, readBuffer, 0);
                        //if(temp_end==-1){serial_read_content_ls.Add(readBuffer);}
                    }

                    if(serialReadContentLsMark!=-1 && temp_end!=-1){
                        byte[] temp_complete_msg;
                        temp_complete_msg = commandConverter.ProcessSerialPortBytes(commandConverter.Read_buffer_concat(serial_read_content_ls, serialReadContentLsMark, -1));
                        //Debug.Log("process: "+string.Join(",", temp_complete_msg));
                        if(temp_complete_msg.Length>0){
                            // if (command_Converter.GetCommandType(temp_complete_msg, out _) ==0 ){
                            //     //Debug.Log(string.Join(",", temp_complete_msg));
                            //     Command_parse(temp_complete_msg);
                            // }
                            // else{commandQueue.Enqueue(temp_complete_msg);}
                            commandQueue.Enqueue(temp_complete_msg);
                        }

                        serial_read_content_ls.Clear();
                        if(readBuffer.Length-temp_end>0){
                            byte[] temp_readBuffer=new byte[readBuffer.Length-temp_end];
                            Array.Copy(readBuffer, temp_end ,temp_readBuffer, 0, temp_readBuffer.Length);
                            if(commandConverter.FindMarkOfMessage(true, temp_readBuffer, 0)!=-1){
                                serial_read_content_ls.Add(temp_readBuffer);
                                serialReadContentLsMark=0;
                            }
                        }
                    }
                }
                else{
                    Thread.Sleep(1);
                }
            }
            else{break;}
        }
    }

    public int DataSend(string message, bool variable_change = false, bool inVerifyOrVerifyNeedless=false){
        if(sp!= null && sp.IsOpen){
            if(variable_change){//form: p_.... = 1
                // if(simple_mode){
                //     //byte[] temp_msg = new byte[]{0xAA, 0xBB, 0xCC, 0xDD};
                //     byte[] temp_msg = new byte[]{0xAA, 0xBB, 0xCC, 0xDD};
                //     sp.Write(temp_msg, 0, temp_msg.Length);
                //     Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                // }
                //else{
                    string temp_var_name = message.Split('=')[0];
                    temp_var_name = temp_var_name.Replace("/","");
                    
                    if(Arduino_var_map.ContainsKey(temp_var_name) || Arduino_ArrayTypeVar_map.ContainsKey(temp_var_name[..temp_var_name.IndexOf('[')])){//从p_xxx转为int=int
                        string temp_command;
                        if(temp_var_name.Contains('[')){
                            temp_command = Arduino_ArrayTypeVar_map[temp_var_name[..temp_var_name.IndexOf('[')]] + temp_var_name[temp_var_name.IndexOf('[')..] +"="+message.Split('=')[1];
                        }
                        else{temp_command = Arduino_var_map[temp_var_name]+"="+message.Split('=')[1];}

                        if(!inVerifyOrVerifyNeedless){
                            commandVerifyDict.TryAdd(Time.fixedUnscaledTime, temp_command);
                        }
                        byte[] temp_msg = commandConverter.ConvertToByteArray("value_change:"+temp_command);
                        sp.Write(temp_msg, 0, temp_msg.Length);
                        Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                        return 2;
                    }
                    else{
                        if(Int16.TryParse(temp_var_name, out short temp_id) && temp_id<255){//重发int=int
                            string temp_command=temp_var_name+"="+message.Split('=')[1];
                            byte[] temp_msg = commandConverter.ConvertToByteArray("value_change:"+temp_command);
                            sp.Write(temp_msg, 0, temp_msg.Length);
                            //Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                            return 2;
                        }
                        return 0;
                    }
                //}
            }else{
                byte[] temp_msg = commandConverter.ConvertToByteArray("command:"+message);
                sp.Write(temp_msg, 0, temp_msg.Length);
                //Debug.Log("Data sent: "+message);
            }
            return 1;
        }
        else{
            Debug.LogError("port not open");
            return -2;
        }
    }
    
    public int Context_verify(List<string> messages, List<int> values){
        manualResetEventVerify.Reset();
        sp.ReadTimeout = 200;
        try{
            int temp_i = 0;//记录已经同步完成的内容
            for(int i=temp_i; i<messages.Count; i++){
                string temp_echo = "error";
                DataSend("test", inVerifyOrVerifyNeedless:true); 
                DataSend(messages[i]+"="+values[i].ToString(), true, inVerifyOrVerifyNeedless:true);
                while(true){
                    temp_echo = sp.ReadLine();
                    //Debug.Log("echo received: "+temp_echo);
                    if(temp_echo.StartsWith("echo:")){
                        temp_echo=temp_echo[5..temp_echo.IndexOf(":echo")];
                        temp_i = i+1;
                        break;
                    }
                }
                string temp_aim = Arduino_var_list.FindIndex(str => str==messages[i]).ToString() + "=" + values[i].ToString();
                if(temp_echo.Replace(" ", "")==temp_aim){
                    Debug.Log("verified:"+temp_aim);
                    //ui_update.Message_update("verified:"+temp_aim+"\n");
                    continue;
                }
                manualResetEventVerify.Set();
                return -1;
            }
        }
        catch(Exception e){
            Debug.Log(e.Message);
            manualResetEventVerify.Set();
            if(e.Message.Contains("not open")){
                return -2;
            }
            return -1;
        }
        finally{
            manualResetEventVerify.Set();
        }
        return 1;
    }

    public int Context_verify(string message, int value){
        List<string> variables = new List<string>(){message};
        List<int> values = new List<int>(){value};
        return Context_verify(variables, values);
    }

    public int Context_verify(string message, int value, string message2, int value2){
        List<string> variables = new List<string>(){message, message2};
        List<int> values = new List<int>(){value, value2};
        return Context_verify(variables, values);
    }

    #endregion methods of communicating end

    #region methods of file write
    private void InitializeStreamWriter(){
        try{
            #if UNITY_EDITOR
                if(!Directory.Exists("Assets/Resources/Logs/")){Directory.CreateDirectory("Assets/Resources/Logs/");}
                filePath ="Assets/Resources/Logs/"+DateTime.Now.ToString("yyyy_MM_dd_HH_mm") + "_rec.txt";
            #else
                if(!Directory.Exists(Application.dataPath+"/Logs")){Directory.CreateDirectory(Application.dataPath+"/Logs");}
                filePath=Application.dataPath+"/Logs/"+DateTime.Now.ToString("yyyy_MM_dd_HH_mm") + "_rec.txt";
            #endif
            FileStream fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
            streamWriter = new StreamWriter(fileStream);
        }
        catch (Exception e){
            Debug.LogError($"Error initializing StreamWriter: {e.Message}");
        }
    }

    private void ProcessWriteQueue(bool writeAll = false)//txt文件写入，位于主进程
    {
        while (writeQueue.Count > 0 && streamWriter !=  null){
            string chunk = writeQueue.Peek();
            streamWriter.WriteLine(chunk);

            if (writeAll || streamWriter.BaseStream.Position >=  streamWriter.BaseStream.Length - BUFFER_THRESHOLD){
                streamWriter.Flush();
            }

            writeQueue.Dequeue();
        }
    }

    private void CleanupStreamWriter()
    {
        if (streamWriter !=  null)
        {
            streamWriter.Close();
            streamWriter.Dispose();
            streamWriter = null;
        }
    }

    public string WriteInfo(bool returnTypeHead = false, int recType = 0, int _lickPos = -1, string enqueueMsg = ""){
        if(! returnTypeHead && nowTrial == -1){return "";}

        List<string> recTypeLs = new List<string>(){
            // 0        1       2     3         4          5           6           7
            "lick", "start", "end", "init", "entrance", "press", "lickExpire", "trigger"
        };
        if(enqueueMsg != ""){
            writeQueue.Enqueue(enqueueMsg);
            ProcessWriteQueue();
        }
        else if(!returnTypeHead){
            time_rec_for_log[1] = Time.fixedUnscaledTime;
            // float[] temp_context_info=position_control.GetcontextInfo();
            // // results[0] = now_context;
            // // results[1] = X - contextZoneStartAndEnd[now_context*2];
            // // results[2] = contextZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
            // // results[3] = rewardZoneStartAndEnd[now_context*2] - contextZoneStartAndEnd[now_context*2];
            // // results[4] = rewardZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
            // // results[5] = context_info_ls[now_context].is_inf? 1: 0;
            // //string temp_time = (time_rec_for_log[1]-time_rec_for_log[0]).ToString(".00");
            // string strLickCount = "";
            // if(recType == 1 && nowTrial > 0){
            //     foreach(int count in lickCount[nowTrial -1]){
            //         strLickCount += count.ToString() + "\t";
            //     }
            // }
                try{
                string data_write =   $@"{recTypeLs[recType]}"
                                        +$"\t{time_rec_for_log[1]-time_rec_for_log[0]}"
                                        +$"\t0x{trialMode:X2}"
                                        +$"\t{nowTrial}"
                                        +$"\t{_lickPos}"
                                        +$"\t"
                                        //+$"\t{(recType == 1? strLickCount : "")}"
                                        ;
                string resultORAddInfo = null;
                switch(recType){
                    case 2:{//end
                        resultORAddInfo = trialResult[nowTrial].ToString();
                        break;
                    }
                    case 4:{//entrance
                        //resultORAddInfo = _lickPos.ToString();
                        break;
                    }
                    // case 5:{
                    //     resultORAddInfo = _lickPos.ToString();
                    //     break;
                    // }
                    default:{
                        break;
                    }
                }
                data_write += resultORAddInfo;

                if(recType == 3){
                    writeQueue.Enqueue($"Mode 0x{trialMode:X2}, Start at {DateTime.Now.ToString("HH:mm:ss ")}");
                }
                writeQueue.Enqueue(data_write);
                ProcessWriteQueue();
            }
            finally{
                
            }
        }
        return "type\tdelta time\tmode\ttrial\tlickPos\tresult";
    }

    #endregion methods of file write end

    void Awake(){
        for (int i = 0; i < Math.Min(3, Display.displays.Length); i++)
        {
            Display.displays[i].Activate();
            //Screen.SetResolution(Display.displays[i].renderingWidth, Display.displays[i].renderingHeight, true);
        }
        #if !UNITY_EDITOR
            InApp = true;
            config_path=Application.dataPath+"/Resources/config.ini";
            if(!System.IO.Directory.Exists(Application.dataPath+"/Sprites")){System.IO.Directory.CreateDirectory(Application.dataPath+"/Sprites");}
        #endif

        time_rec_for_log[0] = Time.fixedUnscaledTime;
        commandConverter = new CommandConverter(ls_types);
        alarm = new Alarm();
        iniReader=new IniReader(config_path);
        if(!iniReader.Exists()){
            Debug.LogError("file not exist");
            return;
        }
        ui_update = GetComponent<UIUpdate>();
        audioSource = GetComponent<AudioSource>();
        
        string _strMode = iniReader.ReadIniContent(  "settings", "start_mode", "0x00");
        trialMode = Convert.ToInt16(_strMode[(_strMode.IndexOf("0x")+2)..], 16);
        barWidth = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "bar_width", "400"));
        displayPixels = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "displayPixels", "2160"));
        isRing = iniReader.ReadIniContent(  "displaySettings", "isRing", "false") == "true";

        contextInfo = new ContextInfo(
            iniReader.ReadIniContent(                   "settings", "start_method"      ,   "assign"                ),                 // string _start_method
            iniReader.ReadIniContent(                   "settings", "availabe_pos"      ,   "0, 90, 180, 270, 360"  ),                 // string _available_pos_array
            iniReader.ReadIniContent(                   "settings", "pump_pos"          ,   "0,1,2,3"               ),                 // string _pump_pos_array
            iniReader.ReadIniContent(                   "settings", "lick_pos"          ,   "0,1,2,3"               ),                 // string _lick_pos_array
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "max_trial"         ,   "10000"                  )),               // int _maxTrial
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "backgroundLight"   ,   "0"                     )),                // int _backgroundLight
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "barDelayTime"      ,   "1"                     )),                // float _barLastTime
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "barLastingTime"    ,   "1"                     )),                // float 
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "waitFromLastLick"  ,   "3"                     )),                // float 
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "soundLength"       ,   "0.2"                   )),                // float 
            iniReader.ReadIniContent(                   "settings", "triggerModeDelay"  ,   "0"                     ),                 // float triggerDelay        
            iniReader.ReadIniContent(                   "settings", "trialWaitSec"      ,   "random5~10"            ),                 // string
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "success_wait_sec"  ,   "3"                     )),                // float _s_wait_sec
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "fail_wait_sec"     ,   "6"                     )),                // float _f_wait_sec
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "trialExpireTime"   ,   "9999"                  )),                // float trialExpireTime
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "triggerMode"       ,   "0"                     )),                // int triggerMode
            iniReader.ReadIniContent(                   "settings", "assign_pos"        ,   "0, 90, 180, 270, 360"  ),                 // string _assigned_pos
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "seed"              ,   "-1"                     ))                 // int _seed
        );                

        audioPlayTime[0] = contextInfo.soundLength;
        trialStartTriggerMode = contextInfo.trialTriggerMode;

        InitContext(
            iniReader.ReadIniContent("barSettings", "isDriftgrating", "true") == "true",
            Convert.ToSingle(iniReader.ReadIniContent("barSettings", "speed", "1")),
            Convert.ToSingle(iniReader.ReadIniContent("barSettings", "frequency", "5")),
            iniReader.ReadIniContent("barSettings", "direction", "right") == "right" ? 1 : -1,
            Convert.ToInt16(iniReader.ReadIniContent("barSettings", "horizontal", "0")),
            iniReader.ReadIniContent("barSettings", "barMaterial", "#000000"),
            iniReader.ReadIniContent("barSettings", "isCircleBar", "false") == "true",
            iniReader.ReadIniContent("barSettings", "centerShaft", "false") == "true",
            Convert.ToSingle(iniReader.ReadIniContent("barSettings", "centerShaftPos", "180")),
            iniReader.ReadIniContent("barSettings", "centerShaftMat", "#000000")
        );

        for(int i = 0; i<Arduino_var_list.Count; i++){
            Arduino_var_map.Add(Arduino_var_list[i], i.ToString());
        }
        for(int i = 0; i<Arduino_ArrayTypeVar_list.Count; i++){
            Arduino_ArrayTypeVar_map.Add(Arduino_ArrayTypeVar_list[i], i.ToString());
        }

        foreach(string com in iniReader.ReadIniContent("serialSettings", "blackList", "").Split(",")){
            if(!port_black_list.Contains(com)){port_black_list.Add(com);}
        }
        foreach(string port in ScanPorts_API()){
            if(port.Contains("COM") && !port_black_list.Contains(port)){
                try{
                    //Debug.Log(sp.IsOpen);
                    //if (!sp.IsOpen){
                        Debug.Log("try normal");
                        sp = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                        sp.Open();
                        Debug.Log("COM avaible: "+port);

                        sp.ReadTimeout = 1000;
                        while(true){
                            string temp_readline=sp.ReadLine();
                            //Debug.Log(temp_readline);
                            if(temp_readline=="initialed"){
                                break;
                            }
                            else{continue;}
                        }
                    //}
                }
                catch (Exception e){
                    sp = new SerialPort();
                    Debug.Log(e);
                    ui_update.MessageUpdate(e.Message+"\n");
                    sp.Close();
                    if(e.Message.Contains("拒绝访问")){
                        #if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying = false;
                        #else
                            Application.Quit();
                        #endif
                    }
                }
                finally{
                    Thread thread = new Thread(new ThreadStart(DataReceived));
                    thread.Start();
                    Debug.Log("thread started");
                }
            }
            Debug.Log(port);
        }

        if(sp!= null){
            InitializeStreamWriter();
            string data_write = WriteInfo(returnTypeHead: true);
            writeQueue.Enqueue(data_write);
        }

    }

    void Start(){
        ui_update.ControlsParse("ModeSelect", trialMode, "passive");
        ui_update.ControlsParse("TriggerModeSelect", trialStartTriggerMode, "passive");
        if(trialStartTriggerMode == 0){ui_update.MessageUpdate($"interval: {contextInfo.trialInterval[0]} ~ {contextInfo.trialInterval[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
        else{                          ui_update.MessageUpdate($"interval: {contextInfo.trialTriggerDelay[0]} ~ {contextInfo.trialTriggerDelay[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
    }

    void Update(){

        if(waiting){
            if(!forceWaiting){
                //if(Time.fixedUnscaledTime - waitSecRec >= (trialResult[nowTrial] == 1? contextInfo.sWaitSec : contextInfo.fWaitSec)){

                if(waitSec != -1 && IntervalCheck() == 0){
                    if(trialStartTriggerMode == 0){
                        StartTrial(isInit:false);
                        waitSec = -1;
                        if(nowTrial >= contextInfo.maxTrial){
                            DeactivateBar();
                            forceWaiting = true;
                            Debug.Log($"{contextInfo.maxTrial} trials finished");
                            return; 
                        }
                    }else{
                        //trialStartReady = true;
                        //其他模式等待拉杆
                    }
                }else{
                    if(IntervalCheck() == -2){
                        //完全空闲
                    }
                    if(waitSec != -1 && Time.fixedUnscaledTime - trialStartTime >= contextInfo.trialExpireTime){
                        LickResultCheck(-1, nowTrial);
                    }
                }
            }else{
                if(trialInitTime != 0){
                    waitSec += Time.unscaledDeltaTime;
                }
            }
        }else{
            if(nowTrial >= contextInfo.maxTrial){
                return;
            }
        }
    }

    void FixedUpdate(){
        ui_update.MessageUpdate(UpdateFreq: 1);
        List<string> tempFInishedLs = alarm.GetAlarmFinish();
        foreach (string alarmFinished in tempFInishedLs){
            switch(alarmFinished){
                case "StartTrialWaitSoundCue":{
                    StartTrial(false);
                    break;
                }
                case "DeactiveBar":{
                    DeactivateBar();
                    Debug.Log("bar deactivited");
                    break ;
                }
                case "SetWaitingToFalseAtTrialStart":{
                    waiting = false;
                    Debug.Log("waiting set to false");
                    break ;
                }
                case "SetWaitingToFalse":{
                    waiting = false;
                    Debug.Log("waiting set to false");
                    break ;
                }
                case "SetTrialInfraRedLightDelay":{
                    SetTrial(manual:false, waitSoundCue: true);
                    break;
                }
                case "SetTrialPressDelay":{
                    SetTrial(manual:false, waitSoundCue: true);
                    break;
                }
                case "SetTrialReadyToTrue":{
                    trialStartReady = true;
                    break;
                }
                default:{
                    break;
                }
            }
        }
        alarm.AlarmFixUpdate();

        if(waitSec != -1 && trialStartTriggerMode == 0){
            if(IntervalCheck() == 1){
                if(!audioSource.isPlaying){
                    audioSource.Play();
                    Debug.Log("sound played");
                    audioPlayTime[1] = Time.fixedUnscaledTime;
                    audioPlayTime[2] = Time.fixedUnscaledTime + audioPlayTime[0];
                }
            }
        }

        if(audioPlayTime[1] != 0 && Time.fixedUnscaledTime >= audioPlayTime[2]){
            audioSource.Stop();
            audioPlayTime[1] = 0;
        }
        // if(alarm.GetAlarm("DeactiveBar") == 0){
        //     DeactivateBar();
        //     Debug.Log("bar deactivited");
        // }
        // if(alarm.GetAlarm("SetWaitingToFalseAtTrialStart") == 0){
        //     waiting = false;
        //     Debug.Log("waiting set to false");
        // }
        // if(alarm.GetAlarm("SetWaitingToFalse") == 0){
        //     waiting = false;
        //     Debug.Log("waiting set to false");
        // }
        //WriteInfo();

        while(commandQueue.Count()>0){//解析保存的command
            commandQueue.TryDequeue(out byte[] _command);
            CommandParse(_command);
        }
        if(commandVerifyDict.Count>0){//重新发送之前未能同步成功的内容
            List<float> temp_keys = commandVerifyDict.Keys.ToList();
            temp_keys.Sort();
            foreach(float time in temp_keys){
                if(Time.fixedUnscaledTime-time>=commandVerifyExpireTime){
                    Debug.LogWarning($"verify failed at {time}: {commandVerifyDict[time]}");
                    commandVerifyDict.Remove(time, out string removeValue);
                }else{
                    DataSend(commandVerifyDict[time], false);
                }
            }
        }

        if (Input.GetKey(KeyCode.Escape)){
            try{
                writeQueue.Enqueue(JsonConvert.SerializeObject(contextInfo));
                logList.Add(ui_update.logMessage.text);
                logList.Add(ui_update.TexContextInfo.text);
                foreach(string logs in logList){
                    writeQueue.Enqueue(logs);
                }
                ProcessWriteQueue(true);
                CleanupStreamWriter();
                if(sp!= null){
                    sp.Close();
                    Debug.Log("serial closed");
                }
            }
            catch{}
            finally{
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
        }
    }

    void OnDestroy()
    {
        if(sp!= null && sp.IsOpen){
            //sp.WriteLine("init");
            sp.Close();
        }
        CleanupStreamWriter();
    }
}