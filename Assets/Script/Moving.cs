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
using MatrixClaculator;

[System.Serializable]
class ContextInfo{

    public ContextInfo(string _start_method, string _available_pos_array, string _assigned_pos,string _matStart_method, string _matAvailable_array, string _matAssigned, string _pump_pos_array, string _lick_pos_array, string _trackMark_array, int _maxTrial, int _backgroundLight, int _backgroundRedMode, float _barDelayTime, string _waitFromStart, float _barLastingTime, float _waitFromLastLick, float _soundLength, string _trialTriggerDelay, string _trialInterval, string _s_wait_sec, string _f_wait_sec, string _barShiftLs, float _trialExpireTime, int _trialStartType, int _seed = -1){
        startMethod = _start_method;
        matStartMethod = _matStart_method;
        seed = _seed == -1? (int)DateTime.Now.ToBinary(): _seed;
        maxTrial = Math.Max(1, _maxTrial);
        backgroundLight = _backgroundLight;
        backgroundRedMode = _backgroundRedMode;
        barDelayTime = _barDelayTime;
        barLastingTime = _barLastingTime;
        waitFromLastLick = _waitFromLastLick;
        soundLength = _soundLength;
        trialExpireTime = _trialExpireTime;
        trialTriggerMode = _trialStartType;
        barPosLs = new List<int>();
        barmatLs = new List<string>();
        materialsInfo = new List<string>();

        string errorMessage = "";
        try{
            errorMessage = "avaliablePosArray";
            avaliablePosDict = new Dictionary<int, int>();
            foreach(string availablePos in _available_pos_array.Split(',')){
                int temp_pos = Convert.ToInt16(availablePos) % 360;
                // if(!avaliablePosDict.Contains(temp_pos)){avaliablePosDict.Add(temp_pos);}
                avaliablePosDict.Add(avaliablePosDict.Count(), temp_pos);
            }

            errorMessage = "avaliableMatArray";
            matAvaliableArray = new List<string>();
            foreach(string availableMat in _matAvailable_array.Split(',')){
                if(!matAvaliableArray.Contains(availableMat)){matAvaliableArray.Add(availableMat);}
            }
            
            errorMessage = "pumpPosLs";
            pumpPosLs = new List<int>();
            var _strPumpPos = _pump_pos_array.Split(",");
            if(_strPumpPos.Count() > 0 && _strPumpPos.Count() >= avaliablePosDict.Count()){
                foreach(string pos in _strPumpPos){
                    //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                        pumpPosLs.Add(Convert.ToInt16(pos));
                    //}
                }
            }else{
                foreach(int _ in avaliablePosDict.Keys){
                    pumpPosLs.Add(pumpPosLs.Count());
                }
            }

            errorMessage = "lickPosLs";
            lickPosLs = new List<int>();
            var _strLickPos = _lick_pos_array.Split(",");
            if(_strLickPos.Count() > 0 && _strLickPos.Count() >= avaliablePosDict.Count()){
                foreach(string pos in _strLickPos){
                    //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                        lickPosLs.Add(Convert.ToInt16(pos));
                    //}
                }
            }else{
                foreach(int _ in avaliablePosDict.Keys){
                    lickPosLs.Add(lickPosLs.Count());
                }
            }

            errorMessage = "trackMarkLs";
            trackMarkLs = new List<int>();
            var _strtrackMark = _trackMark_array.Split(",");
            if(_strtrackMark.Count() > 0 && _strtrackMark.Count() >= avaliablePosDict.Count()){
                foreach(string pos in _strtrackMark){
                    //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                        trackMarkLs.Add(Convert.ToInt16(pos));
                    //}
                }
            }else{
                foreach(int _ in avaliablePosDict.Keys){
                    trackMarkLs.Add(trackMarkLs.Count());
                }
            }
            
            errorMessage = "assign or random port parse";
            List<int> posLs = new List<int>();
            List<int> availablePosArray = avaliablePosDict.Keys.ToList();

            if(startMethod.StartsWith("random")){
                string content = startMethod[6..].Replace(" ", "");
                string[] temp = content.Length > 0? content.Split(",") : new string[]{};
                // Debug.Log(temp.Length > 0);
                if(temp.Length > 0){posLs = temp.Select(str => availablePosArray.Contains(Convert.ToInt32(str))? Convert.ToInt32(str): -1).ToList();}
                else{posLs = availablePosArray;}
                if(posLs.Contains(-1)){throw new Exception("");}
                
                List<int> ints = new List<int>();
                for (int i = 0; i < posLs.Count * 3; i++){ints.Add(i % posLs.Count);}
                while(barPosLs.Count < _maxTrial){
                    Shuffle(ints);
                    foreach(int j in ints){barPosLs.Add(posLs[j]);}
                }

                
            }else if(startMethod.StartsWith("assign")){
                posLs = availablePosArray;
                string lastUnit = "";
                foreach(string pos in _assigned_pos.Replace("..", "").Replace(" ", "").Split(',')){//form like 0,1,2,1 ...... or 0,1,0,2,1,1..  ...... or 0*0,1*100,0*50,1*50.. or(0-1)*50,(2-3)*50 or (0-1)..
                    List<int> _pos = new List<int>();
                    ProcessUnit(pos);
                    lastUnit = pos;
                }

                for(int i=barPosLs.Count(); i<_maxTrial; i++){
                    if(_assigned_pos.EndsWith("..")){
                        while(barPosLs.Count() < maxTrial){
                            ProcessUnit(lastUnit);
                        }
                    }else{
                        barPosLs.Add(avaliablePosDict[UnityEngine.Random.Range(0, avaliablePosDict.Count)]);
                    }
                    //barPosLs.Add(avaiblePosArray[UnityEngine.Random.Range(0, avaiblePosArray.Count)]);
                }
            }
            else{
                errorMessage = $"incorrect mode:{startMethod}, should be assign or random";
                throw new Exception("");
            }

            errorMessage = "assign or random mat parse";
            if(matStartMethod.StartsWith("random")){
                List<string> matLs = new List<string>();
                string content = matStartMethod[6..].Replace(" ", "");
                string[] temp = content.Length > 0? content.Split(",") : new string[]{};
                if(temp.Length > 0){matLs = temp.ToList();}
                else{matLs = matAvaliableArray;}

                bool posMatMatch = true;
                if(matLs.Count != posLs.Count){
                    if(MessageBoxForUnity.YesOrNo("materials random setting does not match the pos settings, ignore?", "bar settings") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_NO){
                        Quit();
                    }
                    posMatMatch = false;
                }

                if(posMatMatch && MessageBoxForUnity.YesOrNo("Align to bar Pos? (recommend)", "bar settings") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                    errorMessage += "\nBar pos count does not match to mat count, please fill the mat types to the same count as bar pos settting ";
                    for (int i=0; i<_maxTrial; i++){
                        barmatLs.Add(matAvaliableArray[barPosLs[i]]);
                    }
                }else{

                    List<int> ints = new List<int>();
                    for (int i = 0; i < matLs.Count; i++){ints.Add(i % matLs.Count);}
                    for (int i=0; i<_maxTrial; i++){
                        if(i % ints.Count == 0){
                            Shuffle(ints);
                        }
                        barmatLs.Add(matLs[ints[i % ints.Count]]);
                    }
                }
                
            }else if(matStartMethod.StartsWith("assign")){//有多种barmat时才考虑align
                if(matAvaliableArray.Count > 1 && MessageBoxForUnity.YesOrNo("Align to bar Pos? (recommend)", "bar settings") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                    errorMessage += "\nBar pos count does not match to mat count, please fill the mat types to the same count as bar pos settting ";
                    for (int i=0; i<_maxTrial; i++){
                        barmatLs.Add(matAvaliableArray[barPosLs[i]]);
                    }
                }else{
                    string lastUnit = "";
                    foreach(string mat in _matAssigned.Replace("..", "").Replace(" ", "").Split(',')){//form like 0,1,2,1 ...... or 0,1,0,2,1,1..  ...... or 0*100,1*100,0*50,1*50.. or(0-1)*50,(2-3)*50 or (0-1)..
                        ProcessMatUnit(mat);
                        lastUnit = mat;
                    }

                    for(int i=barmatLs.Count(); i<_maxTrial; i++){
                        if(_matAssigned.EndsWith("..")){
                            while(barmatLs.Count() < maxTrial){
                                ProcessMatUnit(lastUnit);
                            }
                        }else{
                            barmatLs.Add(matAvaliableArray[UnityEngine.Random.Range(0, matAvaliableArray.Count)]);
                        }
                        //barPosLs.Add(avaiblePosArray[UnityEngine.Random.Range(0, avaiblePosArray.Count)]);
                    }
                }
            }
            else{
                errorMessage = $"incorrect mode:{matStartMethod}, should be assign or random";
                throw new Exception("");
            }

            errorMessage = "trialInterval";
            trialInterval = new List<float>{};
            RandomParse(_trialInterval, trialInterval);

            errorMessage = "interval when success or fail";
            sWaitSec = new List<float>();
            fWaitSec = new List<float>();
            RandomParse(_s_wait_sec, sWaitSec);
            RandomParse(_f_wait_sec, fWaitSec);

            errorMessage = "trialTriggerDelay";
            trialTriggerDelay = new List<float>();
            RandomParse(_trialTriggerDelay, trialTriggerDelay);


            errorMessage = "waitFromStart";
            waitFromStart = new List<float>();
            RandomParse(_waitFromStart, waitFromStart);

            errorMessage = "barShiftLs";
            barShiftLs = new List<int>();
            barShiftedLs = new List<int>{};
            RandomParse(_barShiftLs, barShiftLs);
            
            foreach( var _ in barPosLs){
                barShiftedLs.Add(GetRandom(barShiftLs));
            }
            
        }
        catch{
            MessageBoxForUnity.Ensure($"Value Error in Config: {errorMessage}", "Config Parse Failed");
            Quit();
        }
        finally{}

    }
    //public string start_method;
    public string       startMethod     {get;}
    public Dictionary<int, int>    avaliablePosDict {get;}//最多8个，从角度0开始对位置顺时针编号0-7
    public string       matStartMethod     {get;}
    public List<string> matAvaliableArray {get;}
    public List<int>    lickPosLs       {get;}//lick, pump等物理位置自己标定（顺时针或其他方式），按avaliable
    public List<int>    pumpPosLs       {get;}
    public List<int>    trackMarkLs     {get;}
    public List<int>    barShiftLs      {get;}
    public int          maxTrial        {get;}
    public int          seed            {get;}
    public float        barDelayTime    {get;}//主动触发的trial间最短间隔
    public float        barLastingTime  {get;}
    public List<float>  waitFromStart   {get;}
    public float        waitFromLastLick{get;}
    public float        soundLength     {get;}
    public int          backgroundLight {get;}
    public int          backgroundRedMode{get;}
    public List<float>  trialInterval   {get;}
    public List<float>  sWaitSec        {get;}
    public List<float>  fWaitSec        {get;}
    public int          trialTriggerMode{get;}
    public List<float>  trialTriggerDelay{get;}
    public float        trialExpireTime {get;}
    public List<string> materialsInfo   {get;set;}
    
    //addon info:
    public string userName;
    public string mouseInd;

    [JsonIgnore]
    List<int>    barPosLs        {get;}
    [JsonIgnore]
    List<int>    barShiftedLs    {get;}
    [JsonIgnore]
    List<string> barmatLs        {get;}
    [JsonIgnore]
    public float soundCueLeadTime   {get;set;}//仅在延时模式下trial开始时使用，每次使用调用GetRandom(contextInfo.trialTriggerDelay)
    [JsonIgnore]
    public float GoCueLeadTime   {get;set;}//trial开始后随即一定时间给go cue，调用GetRandom(contextInfo.waitFromStart)

    void RandomParse<T>(string randomArg, List<T> ls){
        if(randomArg.StartsWith("random")){
            string[] temp_ls=randomArg[6..].Split("~");
            if(temp_ls.Length==2){
                try{
                    ls.Clear();
                    ls.Add((T)Convert.ChangeType(temp_ls[0], typeof(T)));
                    ls.Add((T)Convert.ChangeType(temp_ls[1], typeof(T)));
                    ls.Sort();
                }catch{
                    Debug.Log($"error in parse, invalid input: {randomArg}");
                    throw new Exception("");
                }
            }else{
                Debug.Log("error in parse, invalid input: {randomArg}");
                throw new Exception("");
            }
        }else{
            ls.Clear();
            ls.Add((T)Convert.ChangeType(randomArg, typeof(T)));
            ls.Add((T)Convert.ChangeType(randomArg, typeof(T)));
        }
    }

    T GetRandom<T>(List<T> _range){
        return (T)Convert.ChangeType(UnityEngine.Random.Range(Convert.ToSingle(_range[0]), Convert.ToSingle(_range[1])), typeof(T));
    }

    void ProcessUnit(string pos){
        List<int> _pos = new List<int>{};
        int multiple = 1;
        if(pos.Contains("*")){
            if(pos.Contains("-")){
                foreach(string posUnit in pos[..pos.LastIndexOf("*")].Replace("(", "").Replace(")", "").Split('-')){
                    if(posUnit.Contains("*")){
                        int tempMultiple = Convert.ToInt16(posUnit[(posUnit.IndexOf("*")+1)..]);
                        for(int i = 0; i < tempMultiple; i++){_pos.Add(Convert.ToInt16(posUnit[..posUnit.IndexOf("*")]));}
                    }else{
                        _pos.Add(Convert.ToInt16(posUnit));
                    }
                }
                multiple = Convert.ToInt16(pos[(pos.LastIndexOf("*") + 1)..]);
            }
            else{
                _pos.Add(Convert.ToInt16(pos[..pos.IndexOf("*")]));
                multiple = Convert.ToInt16(pos[(pos.IndexOf("*") + 1)..]);
            }
        }else{
            _pos.Add(Convert.ToInt16(pos));
        }

        
        for(int i = 0; i < multiple; i++){
            foreach(int posUnit in _pos){
                if(avaliablePosDict.ContainsValue(Convert.ToInt16(posUnit))){
                    barPosLs.Add(posUnit % 360);
                }
                else{
                    throw new Exception("");
                }
            }
        }
    }

    void ProcessMatUnit(string mat){
        List<string> _mat = new List<string>();
        int multiple = 1;
        if(mat.Contains("*")){
            if(mat.Contains("-")){
                foreach(string matUnit in mat[..mat.IndexOf("*")].Replace("(", "").Replace(")", "").Split('-')){
                    if(matUnit.Contains("*")){
                        int tempMultiple = Convert.ToInt16(matUnit[(matUnit.IndexOf("*")+1)..]);
                        for(int i = 0; i < tempMultiple; i++){_mat.Add(matUnit[..matUnit.IndexOf("*")]);}
                    }else{
                        _mat.Add(matUnit);
                    }
                }
                multiple = Convert.ToInt16(mat[(mat.IndexOf("*") + 1)..]);
            }
            else{
                _mat.Add(mat[..mat.IndexOf("*")]);
                multiple = Convert.ToInt16(mat[(mat.IndexOf("*") + 1)..]);
            }
        }else{
            _mat.Add(mat);
        }

        
        for(int i = 0; i < multiple; i++){
            foreach(string matUnit in _mat){
                if(matAvaliableArray.Contains(matUnit)){
                    barmatLs.Add(matUnit);
                }
                else{
                    throw new Exception("");
                }
            }
        }

    }

    void Quit(){
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void Shuffle(List<int> _ints){
        for (int i = 0; i < _ints.Count; i++){
            int rand = UnityEngine.Random.Range(0, _ints.Count - i);
            (_ints[_ints.Count - 1 - i], _ints[rand]) = (_ints[rand], _ints[_ints.Count - 1 - i]);
        }
    }

    public float GetDeg(int pos){
        if(pos < 0 || pos >= avaliablePosDict.Count){return -1;}
        return avaliablePosDict[pos];
    }

    public int GetBarInd(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return barPosLs[trial];
    }

    public int GetBarPos(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return avaliablePosDict[barPosLs[trial]];
    }

    public int GetBarShift(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return barShiftedLs[trial];
    }

    public int GetRightLickPosIndInTrial(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return lickPosLs[barPosLs[trial]];
    }

    public int GetPumpPosInTrial(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return pumpPosLs[barPosLs[trial]];
    }

    public float GetDegInTrial(int trial, bool raw = false){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return (avaliablePosDict[barPosLs[trial]] + (raw? 0: barShiftedLs[trial])) % 360;
    }

    public int GetTrackMarkInTrial(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return trackMarkLs[barPosLs[trial]];
    }

    public bool verify(int lickInd, int trial){//传入的lickInd为RawInd,需要经过LickPos转换
        if(lickInd < 0){
            return false;
        }
        int rightBarPosInd = GetRightLickPosIndInTrial(trial);

        // if(lickInd >= avaliablePosArray.Count){return false;}
        return lickInd == rightBarPosInd;
    }

    public string GetBarMaterialInTrial(int trial){
        if(trial < 0 || trial >= barmatLs.Count){return "";}
        return barmatLs[trial];
    }
}
public class Moving : MonoBehaviour
{
    public bool InApp = false;
    bool DebugWithoutArduino = false;
    public GameObject background;
    public GameObject backgroundCover;
    public GameObject barPrefab;
    public GameObject circleBarPrefab;
    public GameObject centerShaftPrefab;
    public UnityEngine.UI.Slider slider;
    public Material materialMissing;
    public Material driftGratingBase;
    int barWidth;
    int barHeight;
    int displayPixelsLength;
    int displayPixelsHeight;
    float displayVerticalPos = 0.5f;
    bool isRing;
    GameObject bar;
    GameObject barChild;
    GameObject barChild2;
    GameObject centerShaft;
    float trialInitTime = 0;    public float TrialInitTime {get{return trialInitTime;}}
    float trialStartTime = -1;
    /// <summary>
    /// 设置为上一个tiral的end trial时间，不清零
    /// </summary>
    // float WaitSecRec = -1; float waitSecRec {get{return WaitSecRec;} set{WaitSecRec = value; Debug.Log("waitSecRecChanged: "+value);}}
    float WaitSecRec = -1; float waitSecRec {get{return WaitSecRec;} set{WaitSecRec = value;}}
    float waitSec = -1;
    float[] standingPos = new float[2];
    float standingSecInTrigger = -1;
    float standingSecNowInTrigger = -1;
    // float[] standingPosInTrial = new float[2];
    float standingSecInDest = -1;
    float standingSecNowInDest = -1;
    bool waiting = true;
    bool forceWaiting = true;
    /// <summary>
    /// -1：初始未开始，-2：forcewaiting，0：waiting， 1：started，2：finished but not end
    /// </summary>
    int trialStatus = -1;
    public bool ForceWaiting { get { return forceWaiting; } set { forceWaiting = value; } }
    /// <summary>
    /// 0x?0, 0x?1 : 0: 舔到对的进入下一个trial，无论其他; 1: 只能舔对的 2:在对应位置待到时间  |||  0x0?, 0x1? : 0:trial开始就给水; 1:符合条件才给水
    /// </summary>
    int trialMode = 0x00;// 0x?0, 0x?1 : 0: 舔到对的进入下一个trial，无论其他; 1: 只能舔对的 2:在对应位置待到时间
                         // 0x0?, 0x1? : 0:trial开始就给水; 1:符合条件才给水
                        public int TrialMode { get { return trialMode; } }
                                List<int> trialModes = new List<int>(){0x00, 0x01, 0x10, 0x11, 0x21, 0x22};
                        public  List<int> TrialModes { get { return trialModes; } }

    /// <summary>
    /// //0:定时, 1:红外, 2:压杆, 3：视频检测位置, 4: trial结束后便开始
    /// </summary> <summary>
    /// 
    /// </summary>
    int trialStartTriggerMode = 0;

    public int TrialStartTriggerMode {get{return trialStartTriggerMode;}}
    List<string> trialStartTriggerModeLs = new List<string>(){"延时", "红外", "压杆", "位置检测", "结束"};
    // public List<int> TrialSoundPlayMode = new List<int>{};
    // public List<string> TrialSoundPlayModeExplain = new List<string>{"Off", "BeforeTrial", "NearStart", "BeforeGoCue", "BeforeLickCue", "InPos", "EnableReward", "AtFail"};

    // /// <summary>
    // /// {"Off", 0}, {"BeforeTrial", 1}, {"NearStart", 2}, {"BeforeGoCue", 3}, {"BeforeLickCue", 4}, {"InPos", 5}, {"EnableReward", 6}, {"AtFail", 7}
    // /// </summary>
    // /// <typeparam name="string"></typeparam>
    // /// <typeparam name="int"></typeparam>
    // /// <returns></returns>
    // public Dictionary<string, int> TrialSoundPlayModeCorresponding = new Dictionary<string, int>(){};//0 无声音、1 trial开始前、2将要开始trial但延迟、3 可以舔之前、4 开始后可以舔时、5 trial中、6 可获取奖励、7 失败
    // public Dictionary<int, string> TrialSoundPlayModeAudio = new Dictionary<int, string>{};//int(key): TrialSoundPlayModeCorresponding.values; value: "6000hz", "alarm"
    bool trialStartReady = false;
    List<int> trialResult = new List<int>();//1:success 0:fail -1:manully skip -2:manually successful skip -3:unimportant fail(in mode 0x00 and 0x01)
    List<List<int>> trialResultPerLickSpout = new List<List<int>>();//0, 2, 4, 6,...success/fail, 1, 3, 5, 7,...:miss
    List<int> lickPosLsCopy;
    
    List<List<int>> lickCount = new List<List<int>>();
    public GameObject audioSourceSketchObject;
    public Dictionary<int, AudioSource> audioSources = new Dictionary<int, AudioSource>{};
    public List<int> audioPlayModeNow = new List<int>();
    // AudioSource audioSourceCueSketch;
    // AudioSource audioSourceAlarmSketch;
    public Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
    // public Dictionary<string, int> audioSourceInds = new Dictionary<string, int>();
    Dictionary<int, float[]> audioPlayTimes = new Dictionary<int, float[]>{};
    public List<string> TrialSoundPlayModeExplain = new List<string>{"Off", "BeforeTrial", "NearStart", "BeforeGoCue", "BeforeLickCue", "InPos", "EnableReward", "AtFail"};
    float cueVolume;

    // float[] cueSoundPlayTime    {get{return audioPlayTimes.Count > 0? audioPlayTimes[TrialSoundPlayModeCorresponding["BeforeTrial"]]: new float[3];} set{if(audioPlayTimes.Count > 0){audioPlayTimes[TrialSoundPlayModeCorresponding["BeforeTrial"]] = value;}}}
    float alarmPlayTimeInterval;
    bool alarmPlayReady = false;//如果其他情况设false，使用alarm.DeleteAlarm("SetAlarmReadyToTrue", forceDelete:true);防止alarmPlayReady在播放间隔后恢复
    float alarmLickDelaySec = 2;
    UIUpdate ui_update;
    Alarm alarm;    public Alarm alarmPublic{get{return alarm;}}
    // public SoundConfig soundConfig;

    #region communicating
    IPCClient ipcclient;    public IPCClient Ipcclient { get { return ipcclient; } }
    List<string> portBlackList = new List<string>();
    SerialPort sp = null;
    CommandConverter commandConverter;
    List<string> ls_types = new List<string>(){"lick", "entrance", "press", "context_info", "log", "echo", "value_change", "command", "debugLog", "stay"};//stay为虚拟command，从另一脚本实现
    List<byte[]> serial_read_content_ls = new List<byte[]>();//仅在串口线程中改变
    int serialReadContentLsMark = -1;
    float commandVerifyExpireTime = 2;//2s
    ManualResetEvent manualResetEventVerify = new ManualResetEvent(true);
    //readonly object lockObject_command = new object();
    ConcurrentQueue<byte[]> commandQueue = new ConcurrentQueue<byte[]>();
    public ConcurrentDictionary<float, string> commandVerifyDict = new ConcurrentDictionary<float, string>();
    List<string> Arduino_var_list =  "p_lick_mode, p_trial, p_trial_set, p_now_pos, p_lick_rec_pos, p_water_flush, p_INDEBUGMODE".Replace(" ", "").Split(',').ToList();
    List<string> Arduino_ArrayTypeVar_list =  "p_waterServeMicros, p_lick_count".Replace(" ", "").Split(',').ToList();
    Dictionary<string, string> Arduino_var_map =  new Dictionary<string, string>{};//{"p_...", "0"}, {"p_...", "1"}...
    Dictionary<string, string> Arduino_ArrayTypeVar_map =  new Dictionary<string, string>{};
    bool debugMode = false; public bool DebugMode { get { return debugMode;} set{
                                                                                    debugMode = value;
                                                                                    //backgroundCover.SetActive(debugMode);
                                                                                }}
    #endregion communicating end

    #region  context generate
    #if UNITY_EDITOR
        string config_path="Assets/Resources/config.ini";
        // string loadPath = $"Assets/Resources/";
    #else
        string config_path;
        // string loadPath = Application.dataPath + "/";
    #endif
    IniReader iniReader;

    int nowTrial = 0; public int NowTrial{get{return nowTrial;}}
    ContextInfo contextInfo;
    Dictionary<string, MaterialStruct> MaterialDict = new Dictionary<string, MaterialStruct>();
    // KalmanFilter kalmanFilter = null;
    // List<List<float>> selectRegions = new List<List<float>>();

    struct MaterialStruct{
        string name;            public string Name          { get { return name;}}
        bool isDriftGrating;    public bool IsDriftGrating  { get { return isDriftGrating;}}
        bool isCircleBar;       public bool IsCircleBar     { get { return isCircleBar;}}
        Material material;
        int width;
        float speed;
        float frequency;
        float direction;
        float horizontal;
        float backgroundLight;
        public MaterialStruct Init(string _name, Material _driftGratingBase, bool _isCircleBar, int _width, float _speed, float _frequency, float _direction, float _horizontal, float _backgroundLight){
            name               = _name;
            isDriftGrating     = true;
            isCircleBar        = _isCircleBar;
            material           = new Material(_driftGratingBase);
            width              = _width;
            speed              = _speed;
            frequency          = _frequency;
            direction          = _direction;
            horizontal         = _horizontal;
            backgroundLight    = _backgroundLight;

            material.SetFloat("_Speed", speed);
            material.SetFloat("_Frequency", frequency);
            material.SetFloat("_Direction", direction);
            material.SetFloat("_Horizontal", horizontal);
            if(_backgroundLight >= 0){
                material.SetFloat("_BackgroundLight", backgroundLight);
            }
            material.name = name;

            return this;
        }

        public MaterialStruct Init(string _name, string _mat, Material materialMissing, int _width = 400,  float _backgroundLight = 0, int backgroundLightRedModeValue = 0){
            name               = _name;
            isDriftGrating     = false;
            width              = _width;
            backgroundLight    = Math.Max(_backgroundLight, backgroundLightRedModeValue);
            speed              = -1;
            frequency          = -1;
            direction          = -1;
            horizontal         = -1;
            
            bool BackgroundLightRedMode  = backgroundLightRedModeValue > 0;
            
            material = new Material(materialMissing);
            if(_mat == ""){
                
            }
            else if(_mat.StartsWith("#")){
                Color color;
                if(!ColorUtility.TryParseHtmlString(_mat, out color)){
                    return this;
                }
                if(name.Contains("background")){
                    color = new Color(Math.Max(backgroundLight, color.r)/255, Math.Max(BackgroundLightRedMode? 0: backgroundLight, color.g)/255, Math.Max(BackgroundLightRedMode? 0: backgroundLight, color.b)/255);
                }
                material = new Material(Shader.Find("Unlit/Color")){color = color};
            }else{
                #if UNITY_EDITOR
                string tempPath = $"Assets/Resources/{_mat}.png";
                #else
                string tempPath = Application.dataPath + $"/Resources/{_mat}.png";
                #endif
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
                    material.SetTexture("_MainTex", texture);
                    material.mainTextureScale = new Vector2(width/400, 1);
                }else{
                    Debug.LogWarning($"No such Material named {_mat}.png");
                }
            }

            return this;

        }
        public void SetMaterial(GameObject gameObject){
            gameObject.GetComponent<MeshRenderer>().material = material;
        }

        public string PrintArgs(){
            return $"name:{name}, "+
                    $"isDriftGrating {isDriftGrating }"+
                    $"width          {width          }"+
                    $"backgroundLight{backgroundLight}"+
                    $"speed          {speed          }"+
                    $"frequency      {frequency      }"+
                    $"direction      {direction      }"+
                    $"horizontal     {horizontal     }";
        }
    }

    void Quit(){
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    T GetRandom<T>(List<T> _range){
        return (T)Convert.ChangeType(UnityEngine.Random.Range(Convert.ToSingle(_range[0]), Convert.ToSingle(_range[1])), typeof(T));
    }

    public float DegToPos(float deg){
        if(displayPixelsLength <= 0){return -1;}
        float temp_value = deg % 360 /360;
        return (float)((temp_value - 0.5)*(displayPixelsLength/10));
    }

    public void SetBarPos(float actual_pos){//0-1，角度输入时需要配合DegToPos
        bar.transform.localPosition = new Vector3(actual_pos, bar.transform.localPosition.y, bar.transform.localPosition.z);
        // ui_update.MessageUpdate($"bar Pos in float: {actual_pos}");
    }

    public List<int> GetAvaiableBarPos(){
        List<int> keys = contextInfo.avaliablePosDict.Keys.ToList();
        keys.Sort();
        List<int> values = new List<int>();
        foreach(int key in keys){
            values.Add(contextInfo.avaliablePosDict[key]);
        }
        return values;
    }

    // void SetMaterial(List<GameObject> goList, string _mat){
    // void SetMaterial(List<GameObject> goList, MaterialStruct _mat){
        
    //     foreach(GameObject go in goList){
    //         // go.GetComponent<MeshRenderer>().material = tempMaterial;
    //         _mat.SetMaterial(go);
    //     }
    // }

    // // void SetMaterial(GameObject go, string _mat){
    // void SetMaterial(GameObject go, MaterialStruct _mat){
    //     SetMaterial(new List<GameObject>(){go}, _mat);
    // }

    // void SetBarMaterial(bool isDriftGrating, float _speed = 1, float _frequency = 5, int _direction = 1, int _horizontal = 0, string _mat = "#000000", float _backgroundLight = 0, GameObject otherBar = null){
    void SetBarMaterial(MaterialStruct _mat, GameObject otherBar = null){
        GameObject tempBar = otherBar == null? bar: otherBar;
        if(tempBar.GetComponent<MeshRenderer>().material.name == _mat.Name+" (Instance)"){return;}
        // Debug.Log(tempBar.name);
        // Debug.Log(bar.GetComponent<MeshRenderer>().material.shader.name);
        _mat.SetMaterial(tempBar);
        if(isRing && otherBar == null){
            _mat.SetMaterial(barChild);
            _mat.SetMaterial(barChild2);
        }
    }
    
    MaterialStruct GetMaterialStruct(string matName){
        if(MaterialDict.ContainsKey(matName)){
            return MaterialDict[matName];
        }else{
            return MaterialDict["default"];
        }

    }

    int ActivateBar(int pos = -1, int trial = -1){
        Debug.Log("bar activited");
        float tempPos = -1;
        if(pos != -1){
            tempPos = contextInfo.GetDeg(pos);
            SetBarPos(DegToPos(tempPos));
        }else if(trial != -1){
            tempPos = contextInfo.GetDegInTrial(trial);
            SetBarPos(DegToPos(tempPos));
        }else{
            return (int)tempPos;
        }

        bar.SetActive(true);
        if(barChild != null && barChild2 != null){
            barChild.SetActive(true);
            barChild2.SetActive(true);
        }
        return (int)tempPos;
    }

    void DeactivateBar(){//后续和endtrial分离?
        bar.SetActive(false);
        if(barChild != null && barChild2 != null){
            barChild.SetActive(false);
            barChild2.SetActive(false);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="barMat"></param>
    /// <param name="centerShaftMat"></param>
    /// <param name="backgroundMat"></param>
    /// <param name="_centerShaft"></param>
    /// <param name="_centerShaftPos"></param>
    /// <returns></returns>
    /// // int InitContext(bool isDriftGrating, float _speed, float _frequency, int _direction, int _horizontal, string _barMatName, bool _isCircleBar = false, bool _centerShaft = false, float _centerShaftPos = 180, string _centerShaftMat = "#000000", string _backgroundMatName = "#000000"){
    int InitContext(MaterialStruct barMat, MaterialStruct centerShaftMat, MaterialStruct backgroundMat, bool _centerShaft = false, float _centerShaftPos = 180){

        // MaterialStruct barMat = new MaterialStruct();
        // if(isDriftGrating){
        //     barMat.Init("drifGrating", driftGratingBase, _speed, _frequency, _direction, _horizontal, lightStrength);
        // }else{
        //     barMat.Init("picBar", _barMatName, materialMissing);
        // }
        // MaterialDict.Add("bar", barMat);

        // MaterialStruct centerShaftMat = new MaterialStruct();
        // centerShaftMat.Init("centerShaft", _centerShaftMat, materialMissing);
        // MaterialDict.Add("centerShaft", centerShaftMat);

        // MaterialStruct backgroundMat = new MaterialStruct();
        // backgroundMat.Init("background", _backgroundMatName, materialMissing);
        // MaterialDict.Add("background", backgroundMat);
        bool _isCircleBar = barMat.IsCircleBar;

        float displayLength = (float)displayPixelsLength / 100;
        float displayHeight = (float)barHeight / 100;
        float barWidthScale = (float)(displayLength * barWidth) / displayPixelsLength;
        GameObject tempPrefab = _isCircleBar? circleBarPrefab: barPrefab;
        bar = Instantiate(tempPrefab);
        if(!_isCircleBar){
            bar.transform.localScale = new Vector3(barWidthScale, 1f, displayHeight);
        }else{
            bar.transform.localScale = new Vector3(barWidthScale, 1f, displayHeight);
        }
        
        bar.transform.localPosition = new Vector3(0, displayPixelsHeight * 0.1f * (displayVerticalPos - 0.5f), -0.01f);
        if(isRing){
            barChild = Instantiate(tempPrefab);
            barChild.transform.SetParent(bar.transform);
            barChild.transform.localScale = new Vector3(1, 1, 1);
            barChild.transform.localPosition = new Vector3(displayLength / barWidthScale* 10f, 0, 0);
            SetBarMaterial(barMat, barChild);


            barChild2 = Instantiate(tempPrefab);
            barChild2.transform.SetParent(bar.transform);
            barChild2.transform.localScale = new Vector3(1, 1, 1);
            barChild2.transform.localPosition = new Vector3(displayLength / barWidthScale * -10f, 0, 0f);
            SetBarMaterial(barMat, barChild2);
        }
        
        // SetBarMaterial(isDriftGrating, _speed, _frequency, _direction, _horizontal, _barMatName, (float)Math.Clamp((float)contextInfo.backgroundLight / 255, 0, 0.8f));
        SetBarMaterial(barMat, bar);
        if(_centerShaft){
            centerShaft = Instantiate(centerShaftPrefab);
            // SetBarMaterial(false, 0, 0, 0, _horizontal, _centerShaftMat, otherBar: centerShaft);
            SetBarMaterial(centerShaftMat, otherBar:centerShaft);
            centerShaft.transform.localPosition = new Vector3(DegToPos(_centerShaftPos), bar.transform.localPosition.y, bar.transform.localPosition.z);
        }

        // if(_backgroundMatName.StartsWith("#")){
        //     background.GetComponent<Renderer>().material.color = new Color(contextInfo.backgroundRedMode == -1? lightStrength: (float)contextInfo.backgroundRedMode / 255, lightStrength, lightStrength);
        // }else{
        //     SetMaterial(background, _backgroundMatName);
        // }
        backgroundMat.SetMaterial(background);
        // MaterialDict["backgroundMat"].SetMaterial(background);
        DeactivateBar();

        return 1;
    }

    float GetSoundPitch(float _lastTime, float _totalTime){//后续改
        return Math.Max(1, Math.Min(5, 0.5f + 0.5f * _totalTime/_lastTime));
    }
    public int ChangeSoundPlayMode(int _playMode, int addOrRemove, string soundName, bool clearAll = false, bool clearOtherAll = false){// addOrRemove = false时, clearAll为true则清除其他模式
        if(addOrRemove > 0){
            if(addOrRemove == 1 && !audioPlayModeNow.Contains(_playMode) && audioClips.ContainsKey(soundName)){
                audioPlayModeNow.Add(_playMode);
                audioSources[_playMode].clip = audioClips[soundName];
                if(soundName == "alarm"){
                    audioSources[_playMode].loop = false;
                    audioSources[_playMode].volume = 1;
                }else{
                    audioSources[_playMode].loop = true;
                    audioSources[_playMode].volume = cueVolume;
                }
                return 0;
            }else{//仅改变，不添加
                if(audioClips.ContainsKey(soundName)){
                    audioSources[_playMode].clip = audioClips[soundName];
                    if(soundName == "alarm"){
                        audioSources[_playMode].loop = false;
                        audioSources[_playMode].volume = 1;
                    }else{
                        audioSources[_playMode].loop = true;
                        audioSources[_playMode].volume = cueVolume;
                    }
                    return 0;
                }
                return -1;
            }
        }else if(addOrRemove == 0){
            if(clearAll){
                audioPlayModeNow.Clear();
                return 1;
            }else if(clearOtherAll){
                audioPlayModeNow.Clear();
                audioPlayModeNow.Add(_playMode);
                return 1;
            }
            else if(audioPlayModeNow.Contains(_playMode)){
                audioPlayModeNow.Remove(_playMode);
                return 1;
            }else{
                return -2;
            }
        }else{
            return -3;
        }
    }

    public bool AudioPlayModeNowContains(string soundModeName){
        if(TrialSoundPlayModeExplain.Contains(soundModeName)){
            return audioPlayModeNow.Contains(TrialSoundPlayModeExplain.IndexOf(soundModeName));
        }else{
            return false;
        }
    }

    /// <summary>
    /// audioSources.ContainsKey(soundName) -> audioSources[soundName].Play(), return 1: played, 0: alarm not ready
    /// </summary>
    /// <param name="soundName"></param>
    /// <param name="isAlarm"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="soundName"></param>
    /// <param name="isAlarm"></param>
    /// <returns></returns>
    int PlaySound(AudioSource audioSource, int trackBackMode = -1, string addInfo = ""){
        if(!audioSources.ContainsValue(audioSource)){ return -1;}
        // if(audioSources[soundName].isPlaying){return 0;}


        if(audioSource.clip.name == "alarm"){
            if(alarmPlayReady){
                Debug.Log("sound played: " + audioSource.clip.name);
                audioSource.Play();
                WriteInfo(recType:9, _lickPos:trackBackMode, addInfo:addInfo);
                alarmPlayReady = false;
                alarm.TrySetAlarm("SetAlarmReadyToTrue", alarmPlayTimeInterval, out _);
            }
            else{
                return 0;
            }
        }else{
            if(!audioSource.isPlaying || addInfo.StartsWith("pitch:")){
                if(addInfo.StartsWith("pitch:")){
                    addInfo = addInfo.Substring(6);
                    if(float.TryParse(addInfo, out float _pitch)){
                        audioSource.pitch = _pitch;
                    }
                }
                if(!audioSource.isPlaying){
                    audioSource.Play();
                    Debug.Log("sound played: " + audioSource.clip.name + "add Info: " + addInfo);
                }
                WriteInfo(recType:9, _lickPos:trackBackMode, addInfo:addInfo);
            }else{
                return -1;
            }
        }
        return 1;
    }

    /// <summary>
    /// Look for TrialSoundPlayModeCorresponding.keys, 可随意设置，仅在soundMode包括对应模式时才会真正播放声音
    /// </summary>
    /// <param name="soundMode"></param>
    /// <param name="isAlarm"></param>
    /// <returns></returns>
    int PlaySound(string soundModeName, string addInfo = ""){
        // Debug.Log($"Try play sound at condition: {soundModeName}, addInfo: {addInfo}");
        int soundMode = TrialSoundPlayModeExplain.IndexOf(soundModeName);
        if(soundMode >= 0 && audioPlayModeNow.Contains(soundMode)){
            return PlaySound(soundMode, addInfo);
        }else{
            return -1;
        }
    }
    int PlaySound(int soundMode, string addInfo = ""){
        if(audioSources.ContainsKey(soundMode)){
            int playResult = PlaySound(audioSources[soundMode], soundMode, addInfo:addInfo);
            Debug.Log($"playResult"+playResult);
            if(playResult == 1){
                float[] _audioPlayTime = audioPlayTimes[soundMode];
                _audioPlayTime[1] = Time.fixedUnscaledTime;
                _audioPlayTime[2] = Time.fixedUnscaledTime + (audioSources[soundMode].clip.name == "alarm"? alarmPlayTimeInterval:  _audioPlayTime[0]);
                // Debug.Log(string.Join(", ", _audioPlayTime));
            }
            return playResult;
        }
        return -2;

    }

    int StopSound(string soundModeName = "", bool all = true){
        if(soundModeName == "\\"){return -2;}
        if(soundModeName != ""){all = false;}
        if(!TrialSoundPlayModeExplain.Contains(soundModeName)){return -1;}
        int soundMode = TrialSoundPlayModeExplain.IndexOf(soundModeName);

        foreach(int _soundMode in audioSources.Keys){
            if((_soundMode == soundMode || all) && audioSources[_soundMode].isPlaying){
                StopSound(audioSources[_soundMode]);
                audioSources[_soundMode].pitch = 1;
                Debug.Log("sound stopped in mode "+ _soundMode);
            }
        }
        
        return 1;
    }

    int StopSound(int soundMode){
        if(!audioPlayTimes.ContainsKey(soundMode)){return -2;}
        float[] tempTimes = audioPlayTimes[soundMode];
        if(tempTimes[2] > 0){
            audioPlayTimes[soundMode][1] = -1;
            audioPlayTimes[soundMode][2] = -1;
            return StopSound(audioSources[soundMode]);
        }else{
            return -1;
        }
    }

    int StopSound(AudioSource audioSource){
        if(audioSource.isPlaying){
            audioSource.Stop();
            return 0;
        }else{
            return -1;
        }
    }

    void TryStopSound(){
        foreach(int soundMode in audioPlayModeNow){
            if(TrialSoundPlayModeExplain[soundMode] == "InPos"){//InPos不用TryStop
                return;
            }
            float[] tempTimes = audioPlayTimes[soundMode];
            if(tempTimes[0] > 0 && tempTimes[2] > 0){
                if(Time.fixedUnscaledTime >= tempTimes[2]){
                    StopSound(soundMode);
                }
            }
        }

    }

    int ContextInitSync(){
        List<string> names = new List<string>(){"p_lick_mode", "p_trial"};
        List<int> values = new List<int>(){trialMode % 0x10, 0};
        DataSend("init");
        int res = CommandVerify(names, values);
        //DataSend("p_trial_set=1", true);
        return res;
    }

    int ContextStartSync(){
        List<string> names = new List<string>(){"p_trial", "p_now_pos", "p_trial_set"};
        List<int> values = new List<int>(){nowTrial, contextInfo.GetPumpPosInTrial(nowTrial), 1};
        int res = CommandVerify(names, values);
        // DataSend("_");
        // DataSend("p_trial_set=1", true, true);
        // Debug.Log("sent :p_trial_set=1");
        return res;
    }

    int ContextEndSync(){//给水判定在lick中，暂时没用
        // DataSend("p_trial_set=0", true);
        // DataSend("_");
        // return CommandVerify("p_trial_set", 0);
        return 0;
    }

    /// <summary>
    /// 延时触发以及trial结束后触发仅在最初调用，其他主动触发调用此方法进行startTrial
    /// </summary>
    /// <param name="manual"></param>
    /// <param name="waitSoundCue"></param>
    /// <param name="_waitSec"></param>
    /// <returns></returns>
    public int SetTrial(bool manual, bool waitSoundCue, float _waitSec = -1){
        if(forceWaiting && !manual){return -2;}
        if(manual){
            if(trialStartTriggerMode == 3 || trialMode >> 4 == 2){
                if(ipcclient.Activated == false){
                    ui_update.MessageUpdate("IPC not connected");
                    return -3;
                }
                else{
                    List<int[]> selectRegions = ipcclient.GetselectedArea();
                    if(selectRegions.Count > 0){
                        List<int> types = selectRegions.Select(x => x[0]).ToList();
                        if(trialStartTriggerMode == 3 && types.Select(x => (x >= 0 && x < 32)).Count() > 0){

                        }else if(trialMode >> 4 == 2 && types.Select(x => (x >= 32 && x < 64)).Count() > 0){
                            //后续再加细致判断
                        }else{
                            return -4;
                        }
                    }else{
                        return -4;
                    }
                    //检查选择区域
                }
            }

            forceWaiting = false;
            if(trialStartTriggerMode != 0 && trialStartTriggerMode != 4){
                trialStartReady = true;
                ui_update.MessageUpdate("Ready");
            }else{

            }
            InitTrial();
        }
        
        if((trialStartTriggerMode == 0 || trialStartTriggerMode == 4) && manual || (!manual && trialStartReady == true)){//延时触发以及trial结束后触发仅在最初通过manual调用
            trialStartReady = false;//无论声音，无论mode，到这步直接设false
            if(waitSec < 0){
                // contextInfo.soundCueLeadTime = UnityEngine.Random.Range(contextInfo.trialTriggerDelay[0], contextInfo.trialTriggerDelay[1]);
                contextInfo.soundCueLeadTime = GetRandom(contextInfo.trialTriggerDelay);
            }

            if(AudioPlayModeNowContains("BeforeTrial") && waitSoundCue){//需要根据是否有声音决定trial开始方式，不能省略contains
                PlaySound("BeforeTrial");
                waiting = true;
                alarm.TrySetAlarm("StartTrialWaitSoundCue", _waitSec < 0? contextInfo.soundLength + contextInfo.soundCueLeadTime: _waitSec, out _);
                WriteInfo(recType: 7);
                return 0;
            }else{
                StartTrial();
                return 0;
            }
        }else{
            return -1;
        }
    }

    int InitTrial(){
        nowTrial = -1;
        trialStatus = -1;
        ContextInitSync();
        forceWaiting = false;
        waiting = true;
        waitSec = -1;
        waitSecRec = -1;
        trialStartTime = -1;
        lickCount.Clear();
        trialResult.Clear();
        trialResultPerLickSpout.Clear();
        trialResultPerLickSpout = new List<List<int>>(){};
        // foreach(int _ in contextInfo.avaliablePosArray){
        // foreach(int _ in contextInfo.avaliablePosArray){
        for(int i = 0; i < 8; i++){//暂时固定为8
            trialResultPerLickSpout.Add(new List<int>());
            trialResultPerLickSpout.Add(new List<int>());
        }
        // cueSoundPlayTime = new float[]{cueSoundPlayTime[0], -1, -1};
        StopSound();
        DeactivateBar();
        trialInitTime = Time.fixedUnscaledTime;
        ui_update.MessageUpdate("Trial initialized");
        return 0;
    }

    int ServeWaterInTrial(){
        return CommandVerify("p_trial_set", 2);
    }

    int StartTrial(bool isInit = false){//根据soundCueLeadTime在alarm中设置waiting
 
        nowTrial++;
        trialStatus = 1;
        trialStartTime = Time.fixedUnscaledTime;
        ContextStartSync();
        string tempMatName = contextInfo.GetBarMaterialInTrial(nowTrial);
        MaterialStruct tempMs = GetMaterialStruct(tempMatName);
        // Debug.Log(tempMs.PrintArgs());
        SetBarMaterial(tempMs);
        alarmPlayReady = true;//为waiting的delay允许alarm
        if(ipcclient.Activated){
            int markCountPerType = 32;
            int rightMark = contextInfo.GetTrackMarkInTrial(nowTrial);
            if(rightMark >= 0){
                List<int[]>DestinationArea = ipcclient.GetselectedArea().Where(area => area[0] / markCountPerType == 1).ToList();
                int[] CertainAreaNowTrial = DestinationArea.Find(area => area[0] % markCountPerType == rightMark);
                if(CertainAreaNowTrial != null){//ipcclient绘制部分
                    ipcclient.SetCurrentSelectArea(new List<int[]>{CertainAreaNowTrial}, contextInfo.GetBarShift(nowTrial), contextInfo.GetBarPos(nowTrial));
                    ipcclient.MDDrawTemp(ipcclient.GetCurrentSelectArea(), new List<Vector2Int[]>{ipcclient.GetCircledRotatedRectange(contextInfo.GetBarShift(nowTrial) + contextInfo.GetBarPos(nowTrial))});
                }else{
                    Debug.Log("No selected area match the pos now");
                }
            }
        }

        //waiting = false;

        // float tempDelay = contextInfo.waitFromStart[0] > 0 ? UnityEngine.Random.Range(contextInfo.waitFromStart[0], contextInfo.waitFromStart[1]): 0;
        float tempDelay = contextInfo.waitFromStart[0] > 0 ? GetRandom(contextInfo.waitFromStart): 0;
        contextInfo.GoCueLeadTime = tempDelay;
        alarm.TrySetAlarm("PlayGoCueWhenSetWaitingToFalse", (int)(tempDelay/Time.fixedDeltaTime), out _);
        alarm.TrySetAlarm("SetWaitingToFalseAtTrialStart", 1, out _);
        alarm.StartAlarmAfter("SetWaitingToFalseAtTrialStart", "PlayGoCueWhenSetWaitingToFalse");
        alarm.DeleteAlarm("DeactivateBar", true);
        int activitedPos = ActivateBar(trial: nowTrial);

        if(trialStartTriggerMode == 0){
            // contextInfo.soundCueLeadTime = UnityEngine.Random.Range(contextInfo.trialTriggerDelay[0], contextInfo.trialTriggerDelay[1]);
            contextInfo.soundCueLeadTime = GetRandom(contextInfo.trialTriggerDelay);
        }

        string _tempMsg = $"Trial {nowTrial} started at {contextInfo.GetBarInd(nowTrial)}, pos {activitedPos},lick pos {contextInfo.GetRightLickPosIndInTrial(nowTrial)}, start type {trialStartTriggerMode}";
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
        // for(int i = 0; i < contextInfo.avaliablePosArray.Count; i++){
        for(int i = 0; i < 8; i++){//暂时固定为8
            ints.Add(0);
        }
        lickCount.Add(ints);
        ui_update.MessageUpdate();
        return 1;
    }

    /// <summary>
    /// use trialSuccess for deactivate bar instantly and playsound
    /// </summary>
    /// <param name="isInit"></param>
    /// <param name="trialSuccess"></param>
    /// <param name="rightLickSpout"></param>
    /// <param name="trialReadyWaitSec"></param>
    /// <returns></returns>
    int EndTrial(bool isInit = false, bool trialSuccess = false, int rightLickSpout = -1, float trialReadyWaitSec = -1){
        ContextEndSync();
        trialStatus = 0;
        if(isInit || !trialSuccess){DeactivateBar();}
        else{alarm.TrySetAlarm("DeactivateBar", contextInfo.barLastingTime, out _);}
        
        if(!isInit && !trialSuccess){PlaySound("AtFail");}

        waiting = true;
        alarmPlayReady = false;
        alarm.DeleteAlarm("SetAlarmReadyToTrue", forceDelete:true);
        alarm.TrySetAlarm("SetAlarmReadyToTrueAfterTrianEnd", alarmLickDelaySec, out _);
        if(ipcclient.Activated){
            ipcclient.MDClearTemp();
        }

        
        WriteInfo(recType: isInit? 3: 2, _lickPos: rightLickSpout);
        //Debug.Log("rightLickSpout" + rightLickSpout);

        if(!isInit){
            waitSecRec = Time.fixedUnscaledTime;
            
            //lickCount.Clear();
            if(contextInfo.trialInterval[0] > 0){
                float _temp_waitSec;
                if(trialStartTriggerMode == 0){
                    // _temp_waitSec = UnityEngine.Random.Range(contextInfo.trialInterval[0], contextInfo.trialInterval[1]);
                    _temp_waitSec = GetRandom(contextInfo.trialInterval);
                }else{//其他主动触发模式，用于intervalCheck
                    //_temp_waitSec = contextInfo.soundLength + contextInfo.soundCueLeadTime + (trialSuccess? contextInfo.barDelayTime: 0);
                    _temp_waitSec = contextInfo.barDelayTime;
                }

                if(_temp_waitSec > 0){
                    ui_update.MessageUpdate($"Interval: {_temp_waitSec}");
                    // Debug.Log($"Interval: {_temp_waitSec}");
                }
                waitSec = _temp_waitSec;
            }else{
                if(trialStartTriggerMode == 0){
                    
                    waitSec = trialResult[nowTrial] == 1? GetRandom(contextInfo.sWaitSec) : GetRandom(contextInfo.fWaitSec);
                    ui_update.MessageUpdate($"Interval: {waitSec}");

                }else{//其他主动触发模式
                    waitSec = contextInfo.barDelayTime;
                    ui_update.MessageUpdate($"Interval: {waitSec}");
                    //waitSec = contextInfo.soundLength + contextInfo.soundCueLeadTime + (trialSuccess? contextInfo.barDelayTime: 0);
                    // ui_update.MessageUpdate($"Interval: {waitSec}");
                    // Debug.Log($"Interval: {waitSec}");
                }
            }
            
            if(trialReadyWaitSec <= 0){
                trialStartReady = true;
            }
            else{
                alarm.TrySetAlarm("SetTrialReadyToTrue", trialReadyWaitSec, out _);
            }

            if(trialStartTriggerMode == 4){
                alarm.TrySetAlarm("StartTrialAfterReady", 1, out _);
                if(alarm.GetAlarm("SetTrialReadyToTrue") > -1){
                    alarm.StartAlarmAfter("StartTrialAfterReady", "SetTrialReadyToTrue");
                }
            }
        }
        trialStartTime = -1;
        ui_update.MessageUpdate();
        return 1;
    }

    /// <summary>
    /// 根据waitSecRec以及waitSec判断， -2:完全处于空闲时期，0：已经可以开始下一个trial，1/-1：已经可以播放声音
    /// </summary>
    /// <returns></returns>
    int IntervalCheck(){
        //waitSecRec以及waitSec每个trial不初始化，可以持续使用
        float soundCueLeadTime = contextInfo.soundCueLeadTime;
        float _lasttime = waitSec - (Time.fixedUnscaledTime - waitSecRec);
        // Debug.Log(_lasttime);
        if(waitSec < 0 || waitSecRec == -1){
            return -2;
        }

        if(Time.fixedUnscaledTime - waitSecRec >= waitSec){
            // Debug.Log($"Time.fixedUnscaledTime {Time.fixedUnscaledTime}, waitSecRec {waitSecRec}, waitSec {waitSec}, _lasttime {_lasttime}");
            return 0;
        }
        else if(AudioPlayModeNowContains("BeforeTrial") && Math.Abs(_lasttime - (contextInfo.soundLength + soundCueLeadTime)) <= Time.fixedUnscaledDeltaTime * 0.5){
            return soundCueLeadTime > 0? 1: -1;
        }
        else{
            return -2;
        }
    }

    public bool IsIPCInNeed(){
        bool res = trialMode >> 4 == 2 || trialStartTriggerMode == 3;
        ui_update.SetButtonColor("IPCRefreshButton", res? Color.white : Color.grey);
        return res;
    }

    public bool SetIPCAvtive(bool value){
        if(value){
            ipcclient.Silent = false;

        }else{
            ipcclient.Silent = true;
            ipcclient.Activated = false;
        }
        return true;
    }
    public int ChangeMode(int _mode){
        if(_mode == trialMode){
            return 0;
        }else{
            if(_mode < 0x30){
                trialResult.Clear();
                trialResultPerLickSpout.Clear();
                EndTrial(isInit: true);
                trialMode = _mode;

                SetIPCAvtive(IsIPCInNeed());
                // if(IPCInNeed()){
                //     ipcclient.Silent = false;
                // }else{
                //     ipcclient.Silent = true;
                //     ipcclient.Activated = false;
                // }
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

            if(_triggerMode < trialStartTriggerModeLs.Count() && IntervalCheck() == -2 || trialStartTriggerMode == 4){
                trialStartTriggerMode = _triggerMode;

                SetIPCAvtive(IsIPCInNeed());
                // if(IPCInNeed()){
                //     ipcclient.Silent = false;
                // }else{
                //     ipcclient.Silent = true;
                //     ipcclient.Activated = false;
                // }
                waitSec = -1;
                waitSecRec = -1;
                //不清理当前正在进行的trial
                //此处需要修改，判断用trialinterval还是s/f waitsec
                if(trialStartTriggerMode == 0){ui_update.MessageUpdate($"interval: {contextInfo.trialInterval[0]} ~ {contextInfo.trialInterval[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
                else{                          ui_update.MessageUpdate($"interval: {contextInfo.trialTriggerDelay[0]} ~ {contextInfo.trialTriggerDelay[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
                return 0;
            }else{
                return -1;
            }
        }
    }

    /// <summary>
    /// "userName:xx"， "mouseInd:xx"
    /// </summary>
    /// <param name="info"></param>
    public void SetMouseInfo(string info){
        contextInfo.userName = info.StartsWith("userName:")?   info.Split(":")[1] : contextInfo.userName;
        contextInfo.mouseInd = info.StartsWith("mouseInd:")?   info.Split(":")[1] : contextInfo.mouseInd;
    }

    int lickCountGetSet(string getOrSet, int lickInd, int lickTrial){
        if(lickInd < 0 || lickTrial < 1){return -1;}

        if(getOrSet == "set"){
            if(lickCount.Count <= lickTrial){
                while(lickCount.Count <= lickTrial){
                    if(lickCount.Count == lickTrial){
                        List<int> ints = new int[8].ToList();
                        ints[lickInd] ++;
                        lickCount.Add(ints);
                    }else{
                        lickCount.Add(new int[8].ToList());
                    }
                }
            }else{
                //Debug.Log(string.Join(",", lickCount[nowTrial]));
                lickCount[nowTrial][lickInd]++;
            }
            return 0;
        }else if(getOrSet == "get"){
            if(lickTrial >= lickCount.Count){
                Debug.Log($"index out of lickCountLs range: lickInd {lickInd}, list length {lickCount.Count}");
            }else{
                return lickCount[lickTrial][lickInd];
            }
        }else{
            Debug.Log("wrong command in lickCountGetSet");
            return -1;
        }
        return -1;
    }

    int TrialResultAdd(int result, int trial, int LickSpout = -1, int rightLickSpout = -1, bool force = false){
        if(trialResult.Count() == trial){
            trialResult.Add(result);
            if(LickSpout >= 0){
                trialResultPerLickSpout[LickSpout*2].Add(result);
                if(result == 0){
                    trialResultPerLickSpout[rightLickSpout*2+1].Add(1);
                    return 1;
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
        return -1;
    }

    int TrialResultCheck(int trial){
        if(trial >= 0 && trialResult.Count() > trial){
            return trialResult[trial];
        }else{
            return -4;
        }

    }

    public int LickingCheckPubic(int lickInd){
        return LickingCheck(lickInd, nowTrial);
    }

    /// <summary>
    /// check后判断是否结束当前trial, lickInd = -1: 超时; -2: 手动成功进入下一个trial, -3: 位置检测完成trial
    /// </summary>
    /// <param name="lickInd"></param>
    /// <param name="lickTrial"></param>
    /// <returns></returns>
    int LickingCheck(int lickInd, int lickTrial){
        if(lickTrial != nowTrial){
            Debug.LogWarning("trial not sync");
            lickTrial = nowTrial;
        }
        
        int rightLickInd = contextInfo.GetRightLickPosIndInTrial(lickTrial);
        
        if(lickInd >= 0 && trialMode >> 4 == 3){
            lickInd = -4;
        }

        lickCountGetSet("set", lickInd, lickTrial);

        if(!forceWaiting){
            if(lickInd <= -3){
                if(TrialResultCheck(nowTrial) > 0){//位置检测模式已判定过，
                    return -4;
                }else{
                    WriteInfo(_lickPos: lickInd);
                }
            }else{
                WriteInfo(_lickPos: lickInd);
            }
        }

        if(!waiting){//waiting期间的舔不进一步进入判断，仅做记录
            bool result = contextInfo.verify(lickInd, nowTrial);
            // if(trialResult.Count == nowTrial+1){
            //     return -2;//错误trial
            // }

            if(trialMode < 0x30){
                if(trialMode >> 4 == 0){
                    //只要舔到对的就进入下一个，不管错没错，待endtrial结束进入下一个trial
                    if(result || lickInd == -2){
                        if(lickInd >= 0){
                            TrialResultAdd(1, nowTrial, lickInd, rightLickInd);
                            //trialResult.Add(1);
                            if(trialMode == 0x01){ServeWaterInTrial();}
                            ui_update.MessageUpdate($"Trial completed at pos {lickInd}");
                        }else{//手动跳过或结束trial
                            TrialResultAdd(lickInd == -2? 1 : 0, nowTrial, lickInd, rightLickInd);
                            //trialResult.Add(lickInd == -2? 1 : 0);
                            if(lickInd == -1){
                                ui_update.MessageUpdate($"Trial expired at pos {rightLickInd}");
                            }else if(lickInd == -2){
                                if(trialMode == 0x01){ServeWaterInTrial();}
                                ui_update.MessageUpdate($"Trial completed manually at pos {rightLickInd}");
                            }
                        }
                        EndTrial(trialSuccess: true, rightLickSpout: rightLickInd, trialReadyWaitSec: contextInfo.barLastingTime);
                    }else{
                        TrialResultAdd(lickInd == -1? -1: -3, nowTrial, lickInd, rightLickInd);
                        if(lickInd == -1){
                            EndTrial(trialSuccess:false, rightLickSpout: rightLickInd, trialReadyWaitSec: contextInfo.barLastingTime);
                        }
                    }
                }else if(trialMode >> 4 == 1){
                    //只能舔对的
                    result = result || lickInd == -2;
                    TrialResultAdd(result? 1: 0, nowTrial, lickInd, rightLickInd);
                    if(result && trialMode == 0x11){ServeWaterInTrial();}
                    else{CommandVerify("p_trial_set", 0);}
                    if(lickInd < 0){
                        ui_update.MessageUpdate($"Trial {(result? "skipped manually": "expired")} at pos {lickInd}, right place: {rightLickInd}");
                    }else{
                        ui_update.MessageUpdate($"Trial {(result? "success": "failed")} at pos {lickInd}, right place: {rightLickInd}");
                    }
                    EndTrial(trialSuccess: result, rightLickSpout: rightLickInd, trialReadyWaitSec: result? contextInfo.barLastingTime : 0);
                }else if(trialMode >> 4 == 2){//到特定地方
                    result = false;
                    if(lickInd < 0){
                        result = lickInd == -2 || lickInd == -3;
                    }

                    if(lickInd >= 0 && trialResult.Count > nowTrial){//完成任务后小鼠舔了
                        if(trialMode % 0x10 == 2){ServeWaterInTrial();}
                        // else{
                        //     // CommandVerify("p_trial_set", 0);
                        // }//结束时已经给了水，只用结束trial
                        EndTrial(trialSuccess:trialResult[nowTrial] == 1);
                    }else if(lickInd < 0){//小鼠完成了任务，或手动按下按键完成/跳过
                        if(result){
                            if(trialMode % 0x10 == 1){ServeWaterInTrial();}
                            DeactivateBar();
                            ui_update.MessageUpdate("Trial finished");
                            TrialResultAdd(result? (lickInd == -2 ? -2: 1): 0, nowTrial);
                            PlaySound("EnableReward");

                            trialStatus = 2;
                        }else{//手动跳过
                            EndTrial(trialSuccess: false);
                            ui_update.MessageUpdate("Trial skipped");
                        }
                        TrialResultAdd(result? (lickInd == -2 ? -2: 1): 0, nowTrial, lickInd, -1);
                    }
                }
                ui_update.MessageUpdate();

            }else{
                
            }
            return 1;//正常判断完成
        }else{
            ui_update.MessageUpdate();
            return -3;//不需要判断，已返回给commandParse做延时处理
        }
    }

    public Dictionary<string, int> GetTrialInfo(){
        int showLickSpoutNum = contextInfo.avaliablePosDict.Count;
        /*
        "NowTrial"    
        "IsPausing"   
        "NowPos"      
        "lickPosCount"
        "waitSec"
        "lickCount"0,1,2...
        "TrialSuccessNum"0,1,2...
        "TrialFailNum"0,1,2...
        "LickSpoutTotalTrial"0,1,2...
        "LickSpout"0,1,2...
        */
        Dictionary<string, int> trialInfo = new Dictionary<string, int>
        {
            {"NowTrial"         , nowTrial},
            {"IsPausing"        , forceWaiting? 1: 0},
            {"NowPos"           , contextInfo.GetRightLickPosIndInTrial(nowTrial)},
            {"lickPosCount"     , showLickSpoutNum},
            {"waitSec"          , Convert.ToInt16(waitSec)},
            {"TrialSuccessNum"  , trialResult.FindAll(value => value == 1).Count},
            {"TrialFailNum"     , trialResult.FindAll(value => value <= 0).Count},
        };
        if(lickCount.Count > 0 && nowTrial >= 0){
            if(lickCount.Count > nowTrial){
                for(int i = 0; i < showLickSpoutNum; i++){
                    trialInfo.Add($"lickCount{i}", lickCount[nowTrial][lickPosLsCopy[i]]);
                }
            }
            else{// start
                for(int i = 0; i < showLickSpoutNum; i++){
                    trialInfo.Add($"lickCount{i}", 0);
                }
            }
        }

        if(trialResultPerLickSpout.Count > 0){
            for(int i = 0; i < showLickSpoutNum; i++){
                trialInfo.Add($"TrialSuccessNum{i}"     , trialResultPerLickSpout[lickPosLsCopy[i] * 2].FindAll(value => value == 1).Count);
                trialInfo.Add($"TrialFailNum{i}"        , trialResultPerLickSpout[lickPosLsCopy[i] * 2].FindAll(value => value == 0).Count);
                trialInfo.Add($"TrialMissNum{i}"        , trialResultPerLickSpout[lickPosLsCopy[i] * 2+1].Count);
                trialInfo.Add($"LickSpoutTotalTrial{i}"  , trialResultPerLickSpout[lickPosLsCopy[i] * 2].Count);
            }
        }else{
            for(int i = 0; i < showLickSpoutNum; i++){
                trialInfo.Add($"TrialSuccessNum{i}", 0);
                trialInfo.Add($"TrialFailNum{i}", 0);
                trialInfo.Add($"TrialMissNum{i}", 0);
                trialInfo.Add($"LickSpoutTotalTrial{i}", 0);
            }
        }

        for(int i = 0; i < showLickSpoutNum; i++){
            trialInfo.Add($"LickSpout{i}", lickPosLsCopy[i]);
        }


        return trialInfo;
    }

    /// <summary>
    /// recType: same as _recType in WriteInfo
    /// "lick", "start", "end", "init", "entrance", "press", "lickExpire", "trigger", "moving", "stay"
    /// </summary>
    /// <param name="inOrLeave"></param>
    /// <param name="_recType"></param> 
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="inOrLeave"></param>
    /// <param name="_recType"></param> 
    /// <returns></returns>
    int TriggerRespond(bool inOrLeave, int _recType){
        if(trialStartTriggerMode > 0){
            if(inOrLeave){
                if(AudioPlayModeNowContains("BeforeTrial") && contextInfo.soundLength > 0 && contextInfo.trialTriggerDelay[0] > 0){
                    //alarm.TrySetAlarm("SetTrialInfraRedLightDelay", (int)(contextInfo.trialTriggerDelay/Time.fixedUnscaledDeltaTime), out _);
                    SetTrial(manual:false, waitSoundCue: true);
                }else{
                    SetTrial(manual:false, waitSoundCue: false);
                }
            }
            WriteInfo(recType:_recType, _lickPos:inOrLeave? 1: 0);
            //ui_update.MessageUpdate("enter");
        }
        return 1;
    }

    // int[] GetShiftedSelectedCircle(int[] selectedPos, float shiftAngle){//selectPlace: list[int] = [-1, -1, -1, -1, -1, -1]#type: mark; type(check pos region), 0-rectange, 1-circle ; lu/centerx ; lb/centery ; ru/rad ; rb/inner
    //     float[] sceneInfo = ipcclient.GetSceneInfo();
    //     return GetShiftedSelectedCircle(selectedPos, new int[]{(int)sceneInfo[0], (int)sceneInfo[1]}, (int)sceneInfo[2], shiftAngle);
    // }

    // int[] GetShiftedSelectedCircle(int[] selectedPos, int[] center, float radius, float shiftAngle){//selectPlace: list[int] = [-1, -1, -1, -1, -1, -1]#type: mark; type(check pos region), 0-rectange, 1-circle ; lu/centerx ; lb/centery ; ru/rad ; rb/inner
    //     if(selectedPos.Length != 6 || selectedPos[1] != 1){Debug.Log("wrong arg of selectedPos"); return selectedPos;}
    //     int[] selectedCircle = new int[selectedPos.Length];
    //     selectedPos.CopyTo(selectedCircle, 0);
    //     double tempangle = Math.Atan2(selectedPos[2]-center[0], selectedPos[3]-center[1]);
    //     selectedCircle[2] = center[0] + (int)(radius * Math.Sin(shiftAngle * Math.PI / 180 + tempangle));
    //     selectedCircle[3] = center[1] + (int)(radius * Math.Cos(shiftAngle * Math.PI / 180 + tempangle));
    //     return selectedCircle;
    // }

    bool CheckInRegion(long[] _pos, int[] selectedPos){//还没改好
        if(selectedPos[1] == 0){//圆形
            bool incircle = Math.Sqrt(Math.Pow(Math.Abs(_pos[0] - selectedPos[2]), 2) + Math.Pow(Math.Abs(_pos[1] - selectedPos[3]), 2)) < selectedPos[4];
            return incircle == (Math.Abs(selectedPos[5]) == 1);
        }else if(selectedPos[1] == 1){//矩形
            return (selectedPos[2] > _pos[0] &&  _pos[0] > selectedPos[4]) && (selectedPos[3] > _pos[1] &&  _pos[1] > selectedPos[5]);
        }else{
            return false;
        }
    }

    string CheckMouseStat(){//待加其他内容
        return "";
    }

    #endregion context generate end

    #region  file writing
    StreamWriter logStreamWriter;
    StreamWriter posStreamWriter;
    string filePath = "";
    List<string> logList = new List<string>();  public List<string> LogList { get { return logList; } }
    Queue<string> logWriteQueue = new Queue<string>();
    Queue<string> posWriteQueue = new Queue<string>();
    const int BUFFER_SIZE = 256;
    const int BUFFER_THRESHOLD = 32;
    float[] time_rec_for_log = new float[2]{0, 0};
    #endregion file writing end
    
    #region methods of communicating

    string[] ScanPorts_API(){
        string[] portList = SerialPort.GetPortNames();
        return portList;
    }

    public void CommandParsePublic(string limitedCommand, bool urgent = false){//仅接收舔、红外、压杆信号模拟，外加视频检测移动到特定位置
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
            case "stay":{
                break ;
            }
            // case "sw":{
            //     return;
            // }
            default:{
                return;
            }
        }
        if(urgent){
            CommandParse(commandConverter.ProcessSerialPortBytes(commandConverter.ConvertToByteArray(limitedCommand)));
        }else{
            commandQueue.Enqueue(commandConverter.ProcessSerialPortBytes(commandConverter.ConvertToByteArray(limitedCommand)));
        }
    }
    void CommandParse(byte[] _command){//在主线程调用时内容不能有锁,目前全部在主线程调用
        //看注释！
        //看注释！
        //看注释！
        //"lick", "entrance", "press", "context_info", "log", "echo", "value_change", "command", "debugLog", "stay"
        //int startInd = -1;
        int temp_type = commandConverter.GetCommandType(_command, out _);
        string command = commandConverter.ConvertToString(_command);
        //Debug.Log(command);
        switch(temp_type){
            case 0:{//lick, format: lick:lickInd:TrialMark
                int lickInd = Convert.ToInt16(command.Split(":")[1]);
                int lickTrialMark = Convert.ToInt16(command.Split(":")[2]);
                float soundCueLeadTime = contextInfo.soundCueLeadTime;
                float waitFromLastLick = Math.Max(soundCueLeadTime, contextInfo.waitFromLastLick);

                // if(LickResultCheck(lickInd, lickTrialMark) == -3 && waitFromLastLick > 0){
                if(LickingCheck(lickInd, nowTrial) == -3){//仍在waiting
                    if(trialStartTime < 0){//trial开始前指定时间舔了应延迟
                        if(waitFromLastLick > 0){
                            float _lasttime = waitSec - (Time.fixedUnscaledTime - waitSecRec);
                            Debug.Log($"from lick parse : Time.fixedUnscaledTime {Time.fixedUnscaledTime}, waitSecRec {waitSecRec}, waitSec {waitSec}, _lasttime {_lasttime}");
                            if(_lasttime < waitFromLastLick){
                                float tempDelay = (soundCueLeadTime < 0 || soundCueLeadTime > waitFromLastLick)? waitFromLastLick: (_lasttime < soundCueLeadTime? soundCueLeadTime* 0.95f: _lasttime);
                                if(alarm.GetAlarm("StartTrialWaitSoundCue") > 0){alarm.TrySetAlarm("StartTrialWaitSoundCue", tempDelay, out _);}
                                waitSecRec = Time.fixedUnscaledTime - waitSec + tempDelay;//无论声音是否出现，trial开始前舔则延迟trial开始
                                Debug.Log($"after lick parse : Time.fixedUnscaledTime {Time.fixedUnscaledTime}, waitSecRec {waitSecRec}, waitSec {waitSec}, _lasttime {_lasttime}");

                                // Debug.Log("waitSecRec" + waitSecRec);
                                // PlaySound("NearStart");
                            }
                        }
                        Debug.Log("alarm of SetAlarmReadyToTrueAfterTrianEnd: " + alarm.GetAlarm("SetAlarmReadyToTrueAfterTrianEnd"));
                        if(alarm.GetAlarm("SetAlarmReadyToTrueAfterTrianEnd") < 0){
                            PlaySound("NearStart");//无论等待时间，间隔舔了都播放警报
                        }else{
                            alarm.TrySetAlarm("SetAlarmReadyToTrueAfterTrianEnd", alarmLickDelaySec, out _);
                        }
                    }else{//trial开始后在Go Cue之前舔了应延迟
                        float _lasttime = alarm.GetAlarm("PlayGoCueWhenSetWaitingToFalse") * Time.fixedUnscaledDeltaTime;//waitFromStart无设置时此alarm一直为-1
                        if(_lasttime > 0){//仍在等待Go Cue
                        
                            // alarm.TrySetAlarm("SetWaitingToFalseAtTrialStart", contextInfo.GoCueLeadTime, out _);//SetWaitingToFalseAtTrialStart将在playSound后开始，不必要延迟这个
                            alarm.TrySetAlarm("PlayGoCueWhenSetWaitingToFalse", contextInfo.GoCueLeadTime, out _);
                            PlaySound("BeforeGoCue");
                        }
                    }

                }
                break;
            }
            case 1:{//entrance
                //Debug.Log("enter");
                bool inOrLeave = command.EndsWith("In");
                TriggerRespond(inOrLeave, 4);
                break;
            }
            case 2:{//press
                //Debug.Log("Lever Pressed");
                TriggerRespond(true, 5);
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
            case 6:{break;}
            case 7:{break;}
            case 8:{//debugLog
                ui_update.MessageUpdate(command);
                break;
            }
            case 9:{//stay
                int tempType = Convert.ToInt16(command.Split(":")[1]);
                if(tempType == 0){
                    TriggerRespond(true, 9);
                }else if(tempType == 1){
                    LickingCheckPubic(-3);
                }
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

    public int DataSendRaw(string message){
        byte[] temp_msg = Encoding.UTF8.GetBytes(message);
        sp.Write(temp_msg, 0, temp_msg.Length);
        return 1;
    }
    public int DataSend(string message, bool needParse = false, bool inVerifyOrVerifyNeedless=false){
        if(sp!= null && sp.IsOpen){
            if(needParse){//form: p_.... = 1
                // if(simple_mode){
                //     //byte[] temp_msg = new byte[]{0xAA, 0xBB, 0xCC, 0xDD};
                //     byte[] temp_msg = new byte[]{0xAA, 0xBB, 0xCC, 0xDD};
                //     sp.Write(temp_msg, 0, temp_msg.Length);
                //     Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                // }
                //else{
                    string temp_var_name = message.Split('=')[0];
                    temp_var_name = temp_var_name.Replace("/","");
                    
                    if(Arduino_var_map.ContainsKey(temp_var_name) || Arduino_ArrayTypeVar_map.ContainsKey(temp_var_name[..(temp_var_name.IndexOf('[') >= 0? temp_var_name.IndexOf('['): 0)])){//从p_xxx转为int=int
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
                        return -1;
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
            if(!DebugWithoutArduino){Debug.LogError("port not open");}
            return -2;
        }
    }
    
    public int CommandVerify(List<string> messages, List<int> values){
        if(sp == null){return -3;}
        manualResetEventVerify.Reset();
        sp.ReadTimeout = 200;
        Debug.Log("verify start");
        try{
            int temp_i = 0;//记录已经同步完成的内容
            for(int i=temp_i; i<messages.Count; i++){
                string temp_echo = "error";
                DataSend("test", inVerifyOrVerifyNeedless:true); 
                DataSend(messages[i]+"="+values[i].ToString(), true, inVerifyOrVerifyNeedless:true);
                while(true){
                    temp_echo = sp.ReadLine();
                    
                    Debug.Log("echo received: "+temp_echo);
                    if(temp_echo.StartsWith("echo:")){
                        temp_echo=temp_echo[5..temp_echo.IndexOf(":echo")];
                        temp_i = i+1;
                        break;
                    }else{
                        serial_read_content_ls.Add(new byte[]{0xAA}.Concat(Encoding.UTF8.GetBytes(temp_echo)).Concat(new byte[]{0xDD}).ToArray());
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
            if(serial_read_content_ls.Count() > 0){
                byte[] totalMsgInVerify = commandConverter.Read_buffer_concat(serial_read_content_ls, 0, -1);
                int temp_end=commandConverter.FindMarkOfMessage(false, totalMsgInVerify, 0);
                while(temp_end != -1){
                    byte[] tempCompleteMsgInVerify = commandConverter.ProcessSerialPortBytes(totalMsgInVerify);
                    Debug.Log("process: "+string.Join(",", tempCompleteMsgInVerify));
                    if(tempCompleteMsgInVerify.Length>0){
                        commandQueue.Enqueue(tempCompleteMsgInVerify);
                    }
                    totalMsgInVerify = totalMsgInVerify[(temp_end+1)..].ToArray();
                    temp_end=commandConverter.FindMarkOfMessage(false, totalMsgInVerify, 0);

                }
            }
            manualResetEventVerify.Set();
        }
        return 1;
    }

    /// <summary>
    /// p_trial_set:0-end, 1-start, 2-serve water and end
    /// </summary>
    /// <param name="message"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public int CommandVerify(string message, int value){
        List<string> variables = new List<string>(){message};
        List<int> values = new List<int>(){value};
        return CommandVerify(variables, values);
    }

    public int CommandVerify(string message, int value, string message2, int value2){
        List<string> variables = new List<string>(){message, message2};
        List<int> values = new List<int>(){value, value2};
        return CommandVerify(variables, values);
    }

    #endregion methods of communicating end

    #region methods of file write
    private void InitializeStreamWriter(){
        try{
            #if UNITY_EDITOR
                if(!Directory.Exists("Assets/Resources/Logs/")){Directory.CreateDirectory("Assets/Resources/Logs/");}
                filePath ="Assets/Resources/Logs/"+DateTime.Now.ToString("yyyy_MM_dd_HH_mm");
            #else
                if(!Directory.Exists(Application.dataPath+"/Logs")){Directory.CreateDirectory(Application.dataPath+"/Logs");}
                filePath=Application.dataPath+"/Logs/"+DateTime.Now.ToString("yyyy_MM_dd_HH_mm");
            #endif
            FileStream logfileStream = new FileStream(filePath + "_rec.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
            logStreamWriter = new StreamWriter(logfileStream);
            FileStream posfileStream = new FileStream(filePath + "_pos.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
            posStreamWriter = new StreamWriter(posfileStream);
            posWriteQueue.Enqueue(string.Join("\t", new string[]{"x", "y", "frameInd", "TimeInUnitySecFromTrialStart"}));
            
        }
        catch (Exception e){
            Debug.LogError($"Error initializing StreamWriter: {e.Message}");
        }
    }

    private void ProcessWriteQueue(bool writeAll = false)//txt文件写入，位于主进程
    {
        while (logWriteQueue.Count > 0 && logStreamWriter !=  null){
            string chunk = logWriteQueue.Peek();
            logStreamWriter.WriteLine(chunk);

            if (writeAll || logStreamWriter.BaseStream.Position >=  logStreamWriter.BaseStream.Length - BUFFER_THRESHOLD){
                logStreamWriter.Flush();
            }

            logWriteQueue.Dequeue();
        }

        while (posWriteQueue.Count > 0 && posStreamWriter !=  null){
            string chunk = posWriteQueue.Peek();
            posStreamWriter.WriteLine(chunk);

            if (writeAll || posStreamWriter.BaseStream.Position >=  posStreamWriter.BaseStream.Length - BUFFER_THRESHOLD){
                posStreamWriter.Flush();
            }

            posWriteQueue.Dequeue();
        }
    }

    private void CleanupStreamWriter()
    {
        if (logStreamWriter !=  null)
        {
            logStreamWriter.Close();
            logStreamWriter.Dispose();
            logStreamWriter = null;
        }

        if (posStreamWriter !=  null)
        {
            posStreamWriter.Close();
            posStreamWriter.Dispose();
            posStreamWriter = null;
        }
    }

    /// <summary>
    /// rectype: 
    /// </summary>
    /// <param name="returnTypeHead"></param>
    /// <param name="recType"></param>
    /// <param name="_lickPos"></param>
    /// <param name="enqueueMsg"></param>
    /// <returns></returns>
    public string WriteInfo(bool returnTypeHead = false, int recType = 0, int _lickPos = -1, string enqueueMsg = "", string addInfo = ""){
        if(! returnTypeHead && nowTrial == -1){return "";}

        List<string> recTypeLs = new List<string>(){
            // 0        1       2     3         4          5           6           7        8          9
            "lick", "start", "end", "init", "entrance", "press", "lickExpire", "trigger", "stay", "soundplay"
        };
        if(enqueueMsg != ""){
            logWriteQueue.Enqueue(enqueueMsg);
            ProcessWriteQueue();
        }
        else if(!returnTypeHead){
            time_rec_for_log[1] = Time.fixedUnscaledTime;
            try{
                string data_write =   $@"{recTypeLs[recType]}"
                                        +$"\t{time_rec_for_log[1]-time_rec_for_log[0]}"
                                        +$"\t0x{trialMode:X2}"
                                        +$"\t{nowTrial}"
                                        +$"\t{_lickPos}"
                                        +$"\t"
                                        //+$"\t{(recType == 1? strLickCount : "")}"
                                        ;
                switch(recType){
                    case 2:{//end
                        addInfo = trialResult[nowTrial].ToString();
                        break;
                    }
                    case 4:{//entrance
                        //resultORAddInfo = _lickPos.ToString();
                        break;
                    }
                    case 8:{
                        addInfo = string.Join(";", standingPos);
                        break;
                    }
                    case 9:{
                        break;
                    }
                    default:{
                        break;
                    }
                }
                data_write += addInfo;

                if(recType == 3){
                    logWriteQueue.Enqueue($"Mode 0x{trialMode:X2}, Start at {DateTime.Now.ToString("HH:mm:ss ")}");
                }
                logWriteQueue.Enqueue(data_write);
                ProcessWriteQueue();
            }
            finally{
                
            }
        }
        return "type\tdelta time\tmode\ttrial\tlickPos\tresult";
    }

    // public string WriteInfo(int posx, int posy, int ind, string addInfo = ""){
    public string WriteInfo(long[] posInfo, string addInfo = ""){
        string tempPosText = string.Join("\t", posInfo.Select(v => v.ToString("")).ToList());
        if(addInfo != "" && addInfo != null){
            tempPosText += "\t"+addInfo;
        }
        posWriteQueue.Enqueue(tempPosText);
        ProcessWriteQueue();
        return "";
    }

    public string WriteInfo(List<float> sceneInfo){
        string tempPosText = string.Join("\t", sceneInfo.Select(v => v.ToString("")).ToList());
        posWriteQueue.Enqueue(tempPosText);
        ProcessWriteQueue();
        return "";
    }

    #endregion methods of file write end

    void Awake(){

        // for (int i = 0; i < Math.Min(3, Display.displays.Length); i++)
        // {
        //     Display.displays[i].Activate();
        //     Screen.fullScreen = false;
        //     //Screen.SetResolution(Display.displays[i].renderingWidth, Display.displays[i].renderingHeight, true);
        // }

        #if !UNITY_EDITOR
            InApp = true;
            config_path=Application.dataPath+"/Resources/config.ini";
            // if(!System.IO.Directory.Exists(Application.dataPath+"/Sprites")){System.IO.Directory.CreateDirectory(Application.dataPath+"/Sprites");}
        #endif

        time_rec_for_log[0] = Time.fixedUnscaledTime;
        commandConverter = new CommandConverter(ls_types);
        alarm = new Alarm();
        string errorMessage = $"no config file: {config_path}";
        try{
            iniReader=new IniReader(config_path);
            if(!iniReader.Exists()){
                errorMessage = $"Failed to Create iniReader of config {config_path}";
            }
        }
        catch(Exception ){
            MessageBoxForUnity.Ensure(errorMessage, "error");
            Quit();
        }
        ui_update = GetComponent<UIUpdate>();
        ipcclient = GetComponent<IPCClient>();

        string _strMode = iniReader.ReadIniContent(  "settings", "start_mode", "0x00");
        trialMode = Convert.ToInt16(_strMode[(_strMode.IndexOf("0x")+2)..], 16);
        barWidth = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "barWidth", "100"));
        barHeight = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "barHeight", "1080"));
        displayPixelsLength = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "displayPixelsLength", "1920"));
        displayPixelsHeight = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "displayPixelsHeight", "1080"));
        displayVerticalPos  = Convert.ToSingle(iniReader.ReadIniContent(  "displaySettings", "displayVerticalPos", "0.5"));
        isRing = iniReader.ReadIniContent(  "displaySettings", "isRing", "false") == "true";
        displayVerticalPos = Math.Clamp(displayVerticalPos, -1, 2);

        Display mainScreen = Display.displays[0];
        mainScreen.Activate(1366, 768, new RefreshRate(){numerator = 60, denominator = 1});
        Screen.fullScreen = false;

        if(InApp){
            for (int i = 1; i < Math.Min(4, Display.displays.Length); i++){
                Display.displays[i].Activate();
                Screen.fullScreen = false;
                Display.displays[i].Activate(displayPixelsLength, displayPixelsHeight, new RefreshRate(){numerator = 60, denominator = 1});
                //Screen.SetResolution(Display.displays[i].renderingWidth, Display.displays[i].renderingHeight, true);
            }
        }

        contextInfo = new ContextInfo(
            iniReader.ReadIniContent(                   "settings", "start_method"      ,   "assign"                ),                 // string _start_method
            iniReader.ReadIniContent(                   "settings", "available_pos"     ,   "0, 90, 180, 270, 360"  ),                 // string _available_pos_array
            iniReader.ReadIniContent(                   "settings", "assign_pos"        ,   "0, 90, 180, 270, 360"  ),                 // string _assigned_pos
            iniReader.ReadIniContent(                   "settings", "MatStartMethod"    ,   "assign"                ),
            iniReader.ReadIniContent(                   "settings", "MatAvailable"      ,   "default"               ),               
            iniReader.ReadIniContent(                   "settings", "MatAssign"         ,   "default.."             ),              
            iniReader.ReadIniContent(                   "settings", "pump_pos"          ,   "0,1,2,3"               ),                 // string _pump_pos_array
            iniReader.ReadIniContent(                   "settings", "lick_pos"          ,   "0,1,2,3"               ),                 // string _lick_pos_array
            iniReader.ReadIniContent(                   "settings", "TrackPosMark"          ,  ""                      ),                 // string _lick_pos_array
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "max_trial"         ,   "10000"                 )),               // int _maxTrial
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "backgroundLight"   ,   "0"                     )),                // int _backgroundLight
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "backgroundLightRed",   "-1"                    )),                // int _backgroundLightRed
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "barDelayTime"      ,   "1"                     )),                // float _barLastTime
            iniReader.ReadIniContent(                   "settings", "waitFromStart"      ,  "random2~5"             ),                // float go cue
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "barLastingTime"    ,   "1"                     )),                // float 
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "waitFromLastLick"  ,   "3"                     )),                // float 
            Convert.ToSingle(iniReader.ReadIniContent(  "soundSettings", "soundLength"       ,   "0.2"                   )),                // float 
            iniReader.ReadIniContent(                   "settings", "triggerModeDelay"  ,   "0"                     ),                 // float triggerDelay        
            iniReader.ReadIniContent(                   "settings", "trialInterval"     ,   "random5~10"            ),                 // string
            iniReader.ReadIniContent(                   "settings", "success_wait_sec"  ,   "3"                     ),                // string _s_wait_sec
            iniReader.ReadIniContent(                   "settings", "fail_wait_sec"     ,   "6"                     ),                // string _f_wait_sec
            iniReader.ReadIniContent(                   "settings", "barShiftLs"        ,   "0"                     ),                // string _f_wait_sec
            Convert.ToSingle(iniReader.ReadIniContent(  "settings", "trialExpireTime"   ,   "9999"                  )),                // float trialExpireTime
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "triggerMode"       ,   "0"                     )),                // int triggerMode
            Convert.ToInt16(iniReader.ReadIniContent(   "settings", "seed"              ,   "-1"                     ))                 // int _seed
        );                
        standingSecInTrigger = Convert.ToSingle(iniReader.ReadIniContent(  "settings", "standingSecInTrigger",   "0.5" ));
        standingSecInDest = Convert.ToSingle(iniReader.ReadIniContent(  "settings", "standingSecInTrialInDest",   "0.5" ));
        lickPosLsCopy = contextInfo.lickPosLs;

        trialStartTriggerMode = contextInfo.trialTriggerMode;
        foreach(var _ in TrialSoundPlayModeExplain){
            audioPlayTimes.Add(audioPlayTimes.Count, new float[]{
                contextInfo.soundLength,
                -1,
                -1
            });
        }
        if (float.TryParse(iniReader.ReadIniContent("soundSettings", "cueVolume", "0.5"), out cueVolume)){
            cueVolume = Math.Clamp(cueVolume, 0, 1);
        }else{cueVolume = 0.5f;}

        while(audioSources.Count < TrialSoundPlayModeExplain.Count){
            GameObject gameObject = Instantiate(audioSourceSketchObject);
            audioSources.Add(audioSources.Count + 1, gameObject.GetComponent<AudioSource>());
        }

        foreach(AudioClip _clip in Resources.LoadAll("Audios", typeof(AudioClip))){
            if(!audioClips.ContainsKey(_clip.name)){
                audioClips.Add(_clip.name, _clip);
            }
        }

        alarmPlayTimeInterval = contextInfo.soundLength > 0? Convert.ToSingle(iniReader.ReadIniContent("soundSettings", "alarmPlayTimeInterval",  "1.5")) : 0;

        MaterialStruct defaultMat = new MaterialStruct();
        MaterialDict.Add("default", defaultMat.Init("default", "", materialMissing));
        
        List<string> MaterialStructKeyLs = iniReader.ReadIniContent("matSettings", "matList", "default,barMat,centerShaftMat,backgroundMat").Split(",").ToList();
        foreach(string matName in MaterialStructKeyLs){
            if(MaterialDict.ContainsKey(matName)){
                continue;
            }
            bool isDriftGrating = iniReader.ReadIniContent(matName, "isDriftGrating", "default") == "true";
            MaterialStruct tempMat = new MaterialStruct();
            if(isDriftGrating){
                tempMat.Init(matName, driftGratingBase,
                    iniReader.ReadIniContent(                   matName, "isCircleBar", "false") == "true",
                    Convert.ToInt16(iniReader.ReadIniContent(   matName, "width", "400")),
                    Convert.ToSingle(iniReader.ReadIniContent(  matName, "speed", "1")),
                    Convert.ToSingle(iniReader.ReadIniContent(  matName, "frequency", "5")),
                    iniReader.ReadIniContent(                   matName, "direction", "right") == "right" ? -1 : 1,
                    Convert.ToSingle(iniReader.ReadIniContent(  matName, "horizontal", "0")),
                    (float)Math.Clamp((float)contextInfo.backgroundLight / 255, 0, 0.8)
                );
            }else{
                tempMat.Init(
                    matName,
                    iniReader.ReadIniContent(matName, "mat", "#000000"),
                    materialMissing, 
                    Convert.ToInt16(iniReader.ReadIniContent(   matName, "width", "400")),
                    contextInfo.backgroundLight,
                    contextInfo.backgroundRedMode
                );
            }
            MaterialDict.Add(matName, tempMat);
            contextInfo.materialsInfo.Add(tempMat.PrintArgs());
        }
        
        InitContext(
            GetMaterialStruct("default"),
            GetMaterialStruct("centerShaftMat"),
            GetMaterialStruct("backgroundMat"),
            iniReader.ReadIniContent("matSettings", "centerShaft", "false") == "true",
            Convert.ToInt16(iniReader.ReadIniContent("centerShaft", "centerShaftPos", "0"))
        );

        for(int i = 0; i<Arduino_var_list.Count; i++){
            Arduino_var_map.Add(Arduino_var_list[i], i.ToString());
        }
        for(int i = 0; i<Arduino_ArrayTypeVar_list.Count; i++){
            Arduino_ArrayTypeVar_map.Add(Arduino_ArrayTypeVar_list[i], i.ToString());
        }

        foreach(string com in iniReader.ReadIniContent("serialSettings", "blackList", "").Split(",")){
            if(!portBlackList.Contains(com)){portBlackList.Add(com);}
        }
        
        List<List<string>> tempIniReadContent = iniReader.GetReadContent();
        string tempIniReadContentStr = "default:\t\t\t\tothers:\n";
        for(int i = 0; i < Math.Max(tempIniReadContent[0].Count, tempIniReadContent[1].Count); i++){
            if(i < tempIniReadContent[0].Count){
                tempIniReadContentStr += tempIniReadContent[0][i] + "\t\t\t";
            }else{tempIniReadContentStr += "\t\t\t\t\t\t\t";}

            if(i < tempIniReadContent[1].Count){
                tempIniReadContentStr += tempIniReadContent[1][i] + "\n";
            }else{tempIniReadContentStr += "\n";}
        }
        if(MessageBoxForUnity.YesOrNo("Please check the following Configs:\n" + tempIniReadContentStr, "iniReader") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){

        }else{
            Quit();
        }

        string[] portLs = ScanPorts_API();
        foreach(string port in portLs){
            if(port.Contains("COM") && !portBlackList.Contains(port)){
                try{
                    //Debug.Log(sp.IsOpen);
                    //if (!sp.IsOpen){
                        Debug.Log("try normal");
                        sp = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                        sp.RtsEnable = true;
                        sp.DtrEnable = true;
                        sp.Open();
                        Debug.Log("COM avaible: "+port);

                        sp.ReadTimeout = 1000;
                        while(true){
                            string temp_readline=sp.ReadLine();
                            //Debug.Log(temp_readline);
                            if(temp_readline=="initialed"){
                                break;
                            }
                            else{
                                continue;
                            }
                        }
                    //}
                }
                catch (Exception e){
                    sp = new SerialPort();
                    Debug.Log(e);
                    ui_update.MessageUpdate(e.Message+"\n");
                    sp.Close();
                    if(e.Message.Contains("拒绝访问")){
                        string strPortLs = string.Join(", ", portLs);
                        MessageBoxForUnity.Ensure($"Accssion Denied, please try another port or free {port} frist.\n all ports:{strPortLs}", "Serial Error");
                        Quit();
                    }else{
                        string strPortLs = string.Join(", ", portLs);
                        MessageBoxForUnity.Ensure($"Can not connect to Arduino, please try another port or use Arduino IDE to Reopen The Serial Communicator.\nall ports: {strPortLs}", "Serial Error");
                        Quit();
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
            logWriteQueue.Enqueue(data_write);
        }else{
            MessageBoxForUnity.Ensure("No Connection to Arduino!", "Serial Error");
            if(MessageBoxForUnity.YesOrNo("Continue without connection to Arduino?", "Serial Error") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                DebugWithoutArduino = true;
                InitializeStreamWriter();
                string data_write = WriteInfo(returnTypeHead: true);
                logWriteQueue.Enqueue(data_write);
            }else{
                Quit();
            }
        }

    }

    void Start(){
        ui_update.ControlsParse("ModeSelect", trialMode, "passive");
        ui_update.ControlsParse("TriggerModeSelect", trialStartTriggerMode, "passive");
        IsIPCInNeed();
        foreach(int mode in audioPlayModeNow){
            ui_update.ControlsParse("sound", mode, "passive;add");
        }
        if(trialStartTriggerMode == 0){ui_update.MessageUpdate($"interval: {contextInfo.trialInterval[0]} ~ {contextInfo.trialInterval[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
        else{                          ui_update.MessageUpdate($"interval: {contextInfo.trialTriggerDelay[0]} ~ {contextInfo.trialTriggerDelay[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
    }

    void Update(){
        // Debug.LogWarning(Display.displays[0].systemHeight);
        
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
                case "DeactivateBar":{
                    DeactivateBar();
                    Debug.Log("bar deactivited");
                    break ;
                }
                case "SetWaitingToFalseAtTrialStart":{
                    waiting = false;
                    alarmPlayReady = true;//这个trial内已经不会触发alarm了
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
                case "PlayGoCueWhenSetWaitingToFalse":{
                    PlaySound("BeforeLickCue", addInfo:"go cue");
                    break;
                }
                case "SetAlarmReadyToTrue":{
                    alarmPlayReady = true;
                    break ;
                }
                case "SetAlarmReadyToTrueAfterTrianEnd":{
                    alarmPlayReady = true;
                    break ;
                }
                case "StartTrialAfterReady":{
                    StartTrial();
                    break;
                }
                default:{
                    break;
                }
            }
            if(alarmFinished.StartsWith("sw")){
                DataSend(alarmFinished, false);
            }
        }
        alarm.AlarmFixUpdate();

        if(waitSec != -1 && trialStartTriggerMode == 0 && AudioPlayModeNowContains("BeforeTrial")){//延时模式到时间播放声音，但trial开始还要看小鼠是否继续舔
            if(IntervalCheck() == 1){
                PlaySound("BeforeTrial");
            }
        }

        // if(cueSoundPlayTime[1] > 0 && Time.fixedUnscaledTime >= cueSoundPlayTime[2]){
        //     StopSound(TrialSoundPlayModeCorresponding["BeforeTrial");
        // }
        TryStopSound();

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

        if(trialStartTriggerMode == 3 || trialMode >> 4 == 2){
            int markCountPerType = 32;
            long[] pos = ipcclient.GetPos();//x, y, frameInd, pythonTime, rawVideoFrame
            List<int[]> selectedAreas = ipcclient.GetselectedArea();
            // Debug.Log(JsonConvert.SerializeObject(selectedAreas));
            // if(kalmanFilter != null){
                
            // }

            if(trialStatus != -1 && !pos.SequenceEqual(new long[]{-1, -1, -1, -1, -1})){
                WriteInfo(pos, $"{Time.fixedUnscaledTime - time_rec_for_log[0]}");
                // if(kalmanFilter == null){
                //     kalmanFilter = new KalmanFilter(pos[0], pos[1]);
                // }
                
                List<int[]> TriggerAreas = selectedAreas.Where(area => area[0] / markCountPerType == 0).ToList();

                // if(selectedArea[..(selectedArea.Length - 2)].Contains(-1)){break;}
                if(trialStartTriggerMode == 3){//trigger，目前所有trigger区域合并处理
                    pos[0..2].CopyTo(standingPos, 0);
                    bool InTriggerArea = false;
                    foreach (int[] selectedArea in TriggerAreas){
                        if(CheckInRegion(pos, selectedArea)){
                            InTriggerArea = true;
                        }
                    }
                    if(InTriggerArea){
                        standingSecNowInTrigger = standingSecNowInTrigger == -1? Time.fixedUnscaledDeltaTime: standingSecNowInTrigger + Time.fixedUnscaledDeltaTime;
                        float speedUpScale = GetSoundPitch(standingSecInTrigger - standingSecNowInTrigger, standingSecInTrigger);
                        PlaySound("InPos", addInfo:$"pitch:{speedUpScale}");
                        if(standingSecInTrigger > 0 && standingSecNowInTrigger >= standingSecInTrigger){
                            standingSecNowInTrigger = -1;
                            WriteInfo(recType:8);
                            CommandParsePublic("stay:0", urgent:true);
                        }
                    }else{
                        StopSound("InPos");
                        standingSecNowInTrigger = -1;
                    }
                }

                if(trialMode >> 4 == 2){//destination
                    List<int[]> ShiftedCertainAreasNowTrial = ipcclient.GetCurrentSelectArea();
                    if(ShiftedCertainAreasNowTrial.Count() > 0){
                        int[] ShiftedCertainAreaNowTrial = ShiftedCertainAreasNowTrial[0];
                        if(TrialResultCheck(nowTrial) == -4 && ShiftedCertainAreaNowTrial.Length == 6 && CheckInRegion(pos, ShiftedCertainAreaNowTrial)){
                            // PlaySound("InPos");
                            standingSecNowInDest = standingSecNowInDest == -1? Time.fixedUnscaledDeltaTime: standingSecNowInDest + Time.fixedUnscaledDeltaTime;
                            float speedUpScale = GetSoundPitch(standingSecInDest - standingSecNowInDest, standingSecInDest);
                            PlaySound("InPos", addInfo:$"pitch:{speedUpScale}");
                            if(standingSecInDest > 0 && standingSecNowInDest >= standingSecInDest){
                                standingSecNowInDest = -1;
                                WriteInfo(recType:8);
                                CommandParsePublic("stay:1", urgent:true);
                                // PlaySound("EnableReward");//已挪至lickingCheck
                            }
                        }
                        else{
                            StopSound("InPos");
                            standingSecNowInDest = -1;
                        }
                    } 
                }
            }
        }

        if(waiting){//延时模式下下一个trial开始相关计算
            if(!forceWaiting){
                //if(Time.fixedUnscaledTime - waitSecRec >= (trialResult[nowTrial] == 1? contextInfo.sWaitSec : contextInfo.fWaitSec)){

                if(waitSec != -1 && IntervalCheck() == 0){
                    if(trialStartTriggerMode == 0){
                        StartTrial(isInit:false);
                        contextInfo.soundCueLeadTime = GetRandom(contextInfo.trialTriggerDelay);
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
                }
            }else{
                if(trialInitTime != 0){
                    waitSec += Time.unscaledDeltaTime;
                }
            }
        }else{//其他主动触发模式下相关计算
            if(nowTrial >= contextInfo.maxTrial){
                forceWaiting = true;
                return;
            }
            if(trialStartTime != -1 && Time.fixedUnscaledTime - trialStartTime >= contextInfo.trialExpireTime && trialResult.Count <= nowTrial){//超时进入下一个trial，因track模式下小鼠完成任务和结束分离，加入result判断
                LickingCheck(-1, nowTrial);
            }

        }
    }

    public void Exit(){
        try{
            logWriteQueue.Enqueue(JsonConvert.SerializeObject(contextInfo));
            logWriteQueue.Enqueue(JsonConvert.SerializeObject(audioPlayModeNow));
            logWriteQueue.Enqueue(JsonConvert.SerializeObject(audioSources.Select(
                                                                                kvp => new { kvp.Key, kvp.Value.clip.name })
                                                                                ));
            logList.Add(ui_update.MessageUpdate(returnAllMsg:true));
            foreach(string logs in logList){
                logWriteQueue.Enqueue(logs);
            }
            ProcessWriteQueue(true);
            CleanupStreamWriter();
            
            if(ipcclient.Activated){
                ipcclient.CloseSharedmm();
            }
            if(sp!= null){
                sp.Close();
                Debug.Log("serial closed");
            }
        }
        catch{}
        finally{
            Quit();
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