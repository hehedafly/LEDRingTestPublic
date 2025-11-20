using System;
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
using Unity.VisualScripting;
// using System.Drawing;

[System.Serializable]
class ContextInfo{

    public ContextInfo(string _start_method, string _available_pos_array, string _assigned_pos,string _matStart_method, string _matAvailable_array, string _matAssigned, string _pump_pos_array, string _lick_pos_array, string _trackMark_array, int _maxTrial, int _backgroundLight, int _backgroundRedMode, float _barDelayTime, string _waitFromStart, float _barLastingTime, float _waitFromLastLick, string _trialTriggerDelay, string _trialInterval, string _s_wait_sec, string _f_wait_sec, string _barShiftLs, float _trialExpireTime, int _trialStartType, int _seed = -1){
        startMethod = _start_method;
        startMethodStr = _assigned_pos;
        matStartMethod = _matStart_method;
        seed = _seed == -1? (int)DateTime.Now.ToBinary(): _seed;
        maxTrial = Math.Max(1, _maxTrial);
        backgroundLight = _backgroundLight;
        backgroundRedMode = _backgroundRedMode;
        barDelayTime = _barDelayTime;
        barLastingTime = _barLastingTime;
        waitFromLastLick = _waitFromLastLick;
        trialExpireTime = _trialExpireTime;
        trialTriggerMode = _trialStartType;
        barPosLs = new List<int>();
        barmatLs = new List<string>();
        materialsInfo = new List<string>();
        
        UnityEngine.Random.InitState(seed);
        string errorMessage = "";
        try{
            errorMessage = "avaliablePosArray";
            avaliablePosDict = new Dictionary<int, int>();
            foreach (string availablePos in _available_pos_array.Split(',')){
                int temp_pos = Convert.ToInt16(availablePos) % 360;
                // if(!avaliablePosDict.Contains(temp_pos)){avaliablePosDict.Add(temp_pos);}
                avaliablePosDict.Add(avaliablePosDict.Count(), temp_pos);
            }

            errorMessage = "avaliableMatArray";
            matAvaliableArray = new List<string>();
            foreach (string availableMat in _matAvailable_array.Split(',')){
                if (!matAvaliableArray.Contains(availableMat)) { matAvaliableArray.Add(availableMat); }
            }

            errorMessage = "pumpPosLs";
            pumpPosLs = new List<int>();
            var _strPumpPos = _pump_pos_array.Split(",");
            if (_strPumpPos.Count() > 0 && _strPumpPos.Count() >= avaliablePosDict.Count()){
                foreach (string pos in _strPumpPos){
                    //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                    pumpPosLs.Add(Convert.ToInt16(pos));
                    //}
                }
            }
            else{
                foreach (int _ in avaliablePosDict.Keys){
                    pumpPosLs.Add(pumpPosLs.Count());
                }
            }

            errorMessage = "lickPosLs";
            lickPosLs = new List<int>();
            var _strLickPos = _lick_pos_array.Split(",");
            if (_strLickPos.Count() > 0 && _strLickPos.Count() >= avaliablePosDict.Count()){
                foreach (string pos in _strLickPos){
                    //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                    lickPosLs.Add(Convert.ToInt16(pos));
                    //}
                }
            }
            else{
                foreach (int _ in avaliablePosDict.Keys){
                    lickPosLs.Add(lickPosLs.Count());
                }
            }

            errorMessage = "trackMarkLs";
            trackMarkLs = new List<int>();
            var _strtrackMark = _trackMark_array.Split(",");
            if (_strtrackMark.Count() > 0 && _strtrackMark.Count() >= avaliablePosDict.Count()){
                foreach (string pos in _strtrackMark){
                    //if(Convert.ToInt16(pos) < avaliablePosArray.Count()){
                    trackMarkLs.Add(Convert.ToInt16(pos));
                    //}
                }
            }
            else{
                foreach (int _ in avaliablePosDict.Keys){
                    trackMarkLs.Add(trackMarkLs.Count());
                }
            }

            errorMessage = "assign or random pos parse";
            List<int> posLs = new List<int>();
            List<int> availablePosArray = avaliablePosDict.Keys.ToList();

            if (startMethod.StartsWith("random")){//format like random0,90,180
                string content = startMethod[6..].Replace(" ", "");
                string[] temp = content.Length > 0 ? content.Split(",") : new string[] { };
                // Debug.Log(temp.Length > 0);
                // try{
                if (temp.Length > 0) { posLs = temp.Select(str => availablePosArray.Contains(Convert.ToInt16(str)) ? Convert.ToInt16(str) : -1).ToList(); }
                else { posLs = availablePosArray; }
                if (posLs.Contains(-1)) { throw new Exception(""); }

                List<int> ints = new List<int>();
                for (int i = 0; i < posLs.Count * 3; i++) { ints.Add(i % posLs.Count); }
                while (barPosLs.Count < _maxTrial){
                    Shuffle(ints);
                    foreach (int j in ints) { barPosLs.Add(posLs[j]); }
                }
                // }
                // catch(FormatException e){
                //     MessageBoxForUnity.Ensure(e.Message, "error in random start method parse");

                // }
                // catch(Exception e){
                //     MessageBoxForUnity.Ensure(e.Message, "error in random start method parse");
                // }


            }
            else if (startMethod.StartsWith("assign")){
                posLs = availablePosArray;
                string lastUnit = "";
                foreach (string pos in _assigned_pos.Replace("..", "").Replace(" ", "").Split(',')){//form like 0,1,2,1 ...... or 0,1,0,2,1,1..  ...... or 0*0,1*100,0*50,1*50.. or(0-1)*50,(2-3)*50 or (0-1)..
                    List<int> _pos = new List<int>();
                    ProcessUnit(pos);
                    lastUnit = pos;
                }

                for (int i = barPosLs.Count(); i < _maxTrial; i++){
                    if (_assigned_pos.EndsWith("..")){
                        while (barPosLs.Count() < maxTrial){
                            ProcessUnit(lastUnit);
                        }
                    }
                    else{
                        barPosLs.Add(UnityEngine.Random.Range(0, avaliablePosDict.Count));
                    }
                    //barPosLs.Add(avaiblePosArray[UnityEngine.Random.Range(0, avaiblePosArray.Count)]);
                }
            }
            else{
                errorMessage = $"incorrect mode:{startMethod}, should be assign or random";
                throw new Exception("");
            }

            errorMessage = "assign or random mat parse";
            if (matStartMethod.StartsWith("random")){
                List<string> matLs = new List<string>();
                string content = matStartMethod[6..].Replace(" ", "");
                string[] temp = content.Length > 0 ? content.Split(",") : new string[] { };
                if (temp.Length > 0) { matLs = temp.ToList(); }
                else { matLs = matAvaliableArray; }

                bool posMatMatch = true;
                if (matLs.Count != posLs.Count){
                    if (MessageBoxForUnity.YesOrNo("materials random setting does not match the pos settings, ignore?", "bar settings") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_NO){
                        Quit();
                        return;
                    }
                    posMatMatch = false;
                }

                if (posMatMatch && MessageBoxForUnity.YesOrNo("Align to bar Pos? (recommend)", "bar settings") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                    errorMessage += "\nBar pos count does not match to mat count, please fill the mat types to the same count as bar pos settting ";
                    for (int i = 0; i < _maxTrial; i++){
                        barmatLs.Add(matAvaliableArray[barPosLs[i]]);
                    }
                }
                else{

                    List<int> ints = new List<int>();
                    for (int i = 0; i < matLs.Count; i++) { ints.Add(i % matLs.Count); }
                    for (int i = 0; i < _maxTrial; i++){
                        if (i % ints.Count == 0){
                            Shuffle(ints);
                        }
                        barmatLs.Add(matLs[ints[i % ints.Count]]);
                    }
                }

            }
            else if (matStartMethod.StartsWith("assign")){//有多种barmat时才考虑align
                if (matAvaliableArray.Count > 1 && MessageBoxForUnity.YesOrNo("Align to bar Pos? (recommend)", "bar settings") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                    errorMessage += "\nBar pos count does not match to mat count, please fill the mat types to the same count as bar pos settting ";
                    for (int i = 0; i < _maxTrial; i++){
                        barmatLs.Add(matAvaliableArray[barPosLs[i]]);
                    }
                }
                else{
                    string lastUnit = "";
                    foreach (string mat in _matAssigned.Replace("..", "").Replace(" ", "").Split(',')){//form like 0,1,2,1 ...... or 0,1,0,2,1,1..  ...... or 0*100,1*100,0*50,1*50.. or(0-1)*50,(2-3)*50 or (0-1)..
                        ProcessMatUnit(mat);
                        lastUnit = mat;
                    }

                    for (int i = barmatLs.Count(); i < _maxTrial; i++){
                        if (_matAssigned.EndsWith("..")){
                            while (barmatLs.Count() < maxTrial){
                                ProcessMatUnit(lastUnit);
                            }
                        }
                        else{
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
            trialInterval = new List<float> { };
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
            barShiftedLs = new List<int> { };
            try{
                RandomParse(_barShiftLs, barShiftLs);
                foreach (var _ in barPosLs){
                    barShiftedLs.Add(GetRandom(barShiftLs));
                }
            }
            catch (Exception e) when (e.Message.Equals("invalid input")){//format like random-20~20*30,0.. or random-20~20*30,0*30,-20~20*30 or random0*40,-20~30*20...
                barShiftedLs.Clear();
                string lastUnit = "";
                foreach (string _unit in _barShiftLs[6..].Replace("..", "").Split(",")){
                    ProcessUnit(_unit, barShiftedLs);
                    lastUnit = _unit;
                }

                for (int i = barShiftedLs.Count(); i < _maxTrial; i++){
                    if (_barShiftLs.EndsWith("..")){
                        while (barShiftedLs.Count() < maxTrial){
                            ProcessUnit(lastUnit, barShiftedLs);
                        }
                    }
                    else{
                        barShiftedLs.Add(0);
                    }
                    //barPosLs.Add(avaiblePosArray[UnityEngine.Random.Range(0, avaiblePosArray.Count)]);
                }
            }
        }
        catch (Exception e){
            MessageBoxForUnity.Ensure($"Value Error in Config: {errorMessage}, detail:{e.Message}", "Config Parse Failed");
            Quit();
            return;
        }
        finally{

        }

    }

    /// <summary>
    /// OGTtiggerMethod, MSTriggerMethod中，若[start]和[end]条件相同，[start]优先结算
    /// </summary>
    public void ContextInfoAdd(float _soundLength, float _cueVolume, int _barOffset, bool _destAreaFollow, float _standingSecInTrigger, float _standingSecInDest, string _OGTriggerMethod, string _MSTriggerMethod, bool _countAfterLeave){
        soundLength = _soundLength;
        cueVolume = _cueVolume;
        barOffset = _barOffset;      
        destAreaFollow = _destAreaFollow;
        standingSecInTrigger = _standingSecInTrigger;
        standingSecInDest = _standingSecInDest;
        countAfterLeave = _countAfterLeave;
        manuplateMethods = "OG: "+_OGTriggerMethod + ";\nMS: " + _MSTriggerMethod;
        DeviceTriggerMethodLs = new List<string>() {"certainTrialStart", "everyTrialStart", "certainTrialEnd", "everyTrialEnd", "certainTrialInTarget", "everyTrialInTarget", "nextTrialStart", "nextTrialEnd", "nextTrialInTarget"};
        DeviceTriggerMethodLsSorted = new List<List<string>>();
        while(DeviceTriggerMethodLsSorted.Count() < DeviceTriggerMethodLs.Count()/2){DeviceTriggerMethodLsSorted.Add(new List<string>());}
        foreach(var triggerMethod in DeviceTriggerMethodLs){
            if(triggerMethod.Contains("TrialStart")){
                DeviceTriggerMethodLsSorted[0].Add(triggerMethod);
            }else if(triggerMethod.Contains("TrialEnd")){
                DeviceTriggerMethodLsSorted[1].Add(triggerMethod);
            }else if(triggerMethod.Contains("TrialInTarget")){
                DeviceTriggerMethodLsSorted[2].Add(triggerMethod);
            }
        }
        
        OGTriggerMethodLs = new List<Dictionary<string, int[]>>();
        MSTriggerMethodLs = new List<Dictionary<string, int[]>>();

        //format: [start]{"certainTrialStart":<trial indexes int array>|...};[end]{"everyTrialStart:<ignore value>"|...}
        OGTriggerMethodLs.Add(new Dictionary<string, int[]>());
        OGTriggerMethodLs.Add(new Dictionary<string, int[]>());
        if(_OGTriggerMethod.Length > 0){
            foreach(string section in _OGTriggerMethod.Split(';')){
                if((section.Contains("[start]") || section.Contains("[end]")) && section[section.IndexOf(']') + 1].Equals('{') && section.EndsWith("}")){}
                else{throw new Exception("OGTriggerMethod malform: " + section);}
                string _section = section[(section.IndexOf(']') + 2)..(section.Length - 1)];
                // if(method.StartsWith("[start]")){
                foreach(string _method in _section.Split('|')){
                    string _type = _method[.._method.IndexOf(':')];
                    string _content = _method[(_method.IndexOf(':') + 1)..];
                    int[] _values = new int[]{-1};
                    if(_content.Contains("n")){
                        List<int> _lsvalues = new List<int>(){section.StartsWith("[start]")? 1: 0};
                        
                        List<int> _nmultiple = ExtractFactors(_content, "*n");
                        List<int> _n1multiple = ExtractFactors(_content, "*(n+1)");
                        List<int> allFactors = _nmultiple.Concat(_n1multiple).ToList();

                        int _length = ExtractFactor(_content, "~", 1);
                        int _offset = ExtractFactor(_content, "+", 0) - ExtractFactor(_content, "-", 0);

                        foreach (int factor in allFactors){
                            for (int i = factor == _n1multiple.FirstOrDefault(x => x == factor) ? factor : 0; i < maxTrial; i += factor){
                                for (int j = 0; j < _length; j++){
                                    int _i = i + j + _offset;
                                    if(_i > 0 && _i < maxTrial){
                                        _lsvalues.Add(_i);
                                    }
                                }
                            }
                        }
                        _values = _lsvalues.ToArray();
                    }else{
                        _values = new int[]{section.StartsWith("[start]")? 1: 0}.Concat(_method[(_method.IndexOf(':') + 1)..].Split(',').Select(x => Convert.ToInt32(x))).ToArray();
                    }

                    if(DeviceTriggerMethodLs.Contains(_type)){
                        OGTriggerMethodLs[section.StartsWith("[start]")? 0: 1].Add(_type, _values);
                    }else{
                        throw new Exception("wrong trigger method in OGTriggerMethod parse:" + _type);
                    }
                }
            }
        }

        MSTriggerMethodLs.Add(new Dictionary<string, int[]>());
        MSTriggerMethodLs.Add(new Dictionary<string, int[]>());
        if(_MSTriggerMethod.Length > 0){
            foreach(string section in _MSTriggerMethod.Split(';')){
                if((section.Contains("[start]") || section.Contains("[end]")) && section[section.IndexOf(']') + 1].Equals('{') && section.EndsWith("}")){}
                else{throw new Exception("MSTriggerMethod malform: " + section);}
                string _section = section[(section.IndexOf(']') + 2)..(section.Length - 1)];
                // if(method.StartsWith("[start]")){
                foreach(string _method in _section.Split('|')){
                    string _type = _method[.._method.IndexOf(':')];
                    string _content = _method[(_method.IndexOf(':') + 1)..];
                    int[] _values = new int[]{-1};
                    if(_content.Contains("n")){
                        List<int> _lsvalues = new List<int>(){section.StartsWith("[start]")? 1: 0};
                        List<int> _nmultiple = ExtractFactors(_content, "*n");
                        List<int> _n1multiple = ExtractFactors(_content, "*(n+1)");
                        int _length = ExtractFactor(_content, "~", 1);
                        int _offset = ExtractFactor(_content, "+", 0) - ExtractFactor(_content, "-", 0);

                        List<int> allFactors = _nmultiple.Concat(_n1multiple).ToList();

                        foreach (int factor in allFactors){
                            for (int i = factor == _n1multiple.FirstOrDefault(x => x == factor) ? factor : 0; i < maxTrial; i += factor){
                                for (int j = 0; j < _length; j++){
                                    int _i = i + j + _offset;
                                    if(_i > 0 && _i < maxTrial){
                                        _lsvalues.Add(_i);
                                    }
                                }
                            }
                        }
                        _values = _lsvalues.ToArray();
                    }else{
                        _values = new int[]{section.StartsWith("[start]")? 1: 0}.Concat(_method[(_method.IndexOf(':') + 1)..].Split(',').Select(x => Convert.ToInt32(x))).ToArray();
                    }
                                        
                    if(DeviceTriggerMethodLs.Contains(_type)){
                        MSTriggerMethodLs[section.StartsWith("[start]")? 0: 1].Add(_type, _values);
                    }else{
                        throw new Exception("wrong trigger method in MSTriggerMethod parse:" + _type);
                    }
                }
            }
        }
        
        OGTriggerSortedInType = new List<Dictionary<string, int[]>>();
        MSTriggerSortedInType = new List<Dictionary<string, int[]>>();
        for(int i = 0; i < DeviceTriggerMethodLsSorted.Count(); i++){
            OGTriggerSortedInType.Add(
                                    OGTriggerMethodLs
                                    .SelectMany(list => list.Where(kvp => DeviceTriggerMethodLsSorted[i].Contains(kvp.Key)))
                                    .GroupBy(kvp => kvp.Key)  // 按原始Key分组
                                    .SelectMany(group => 
                                        group.Select((item, index) =>  // 改用item代替kvp避免作用域冲突
                                            index == 0 
                                                ? item  // 第一个保留原键
                                                : new KeyValuePair<string, int[]>(
                                                    $"{item.Key}_{index}",  // 后续添加后缀
                                                    item.Value
                                                )
                                        )
                                    )
                                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            MSTriggerSortedInType.Add(
                                    MSTriggerMethodLs.SelectMany(list => list.Where(kvp => DeviceTriggerMethodLsSorted[i].Contains(kvp.Key)))
                                    .GroupBy(kvp => kvp.Key)  // 按原始Key分组
                                    .SelectMany(group => 
                                        group.Select((item, index) =>  // 改用item代替kvp避免作用域冲突
                                            index == 0 
                                                ? item  // 第一个保留原键
                                                : new KeyValuePair<string, int[]>(
                                                    $"{item.Key}_{index}",  // 后续添加后缀
                                                    item.Value
                                                )
                                        )
                                    )
                                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            // OGTriggerSortedInType.Add(
            //                         OGTriggerMethodLs[0].Where(kvp => DeviceTriggerMethodLsSorted[i].Contains(kvp.Key)).Concat(
            //                         OGTriggerMethodLs[1].Where(kvp => DeviceTriggerMethodLsSorted[i].Contains(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            // MSTriggerSortedInType.Add(
            //                         MSTriggerMethodLs[0].Where(kvp => DeviceTriggerMethodLsSorted[i].Contains(kvp.Key)).Concat(
            //                         MSTriggerMethodLs[1].Where(kvp => DeviceTriggerMethodLsSorted[i].Contains(kvp.Key))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }
    }

    /// <summary>
    /// return possible positive numbers before the assigned pattern, like 20,30 will be returned when content:20*n..30*n and pattern:*n were given
    /// </summary>
    /// <param name="content"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    List<int> ExtractFactors(string content, string pattern){
        List<int> indices = content.AllIndexesOf(pattern).ToList();
        return indices.Select(i => {
            // Try parsing from (i-2)..i first, then (i-1)..i
            int _i = 1;
            int parseValue = -1;
            int parsedValue = -1;
            while(int.TryParse(content.Substring(Math.Max(0, i - _i), _i), out parseValue)){
                _i++;
                parsedValue = parseValue;
            }
            return parsedValue;
        })
        .Where(x => x > 0) // Filter out non-positive values
        .ToList();
    }

    /// <summary>
    /// return the first positive integer before the assigned pattern, like 20 will be returned when content:20*n..30*n and pattern:*n were given
    /// </summary>
    /// <returns></returns>
    int ExtractFactor(string content, string pattern, int _default){
        List<int> _res = ExtractFactors(content, pattern);
        if(_res.Count() > 0){
            return _res[0];
        }else{
            return _default;
        }
    }
    //public string start_method;
    public string       startMethod     {get;}
    public string       startMethodStr     {get;}
    public Dictionary<int, int>    avaliablePosDict {get;}//最多8个，从角度0开始对位置顺时针编号0-7
    public string       matStartMethod     {get;}
    public List<string> matAvaliableArray {get;}
    public List<int>    lickPosLs       {get;}//lick, pump等物理位置自己标定（顺时针或其他方式），按avaliable
    public List<int>    pumpPosLs       {get;}
    public List<int>    trackMarkLs     {get;}
    public List<int>    barShiftLs      {get;}
    public int          barOffset       {get;set;}
    public bool         destAreaFollow  {get;set;}
    public float        standingSecInTrigger    {get;set;}
    public float        standingSecInDest       {get;set;}
    public int          maxTrial        {get;}
    public int          seed            {get;}
    public float        barDelayTime    {get;}//主动触发的trial间最短间隔
    public float        barLastingTime  {get;}
    public List<float>  waitFromStart   {get;}
    public float        waitFromLastLick{get;}
    public int          backgroundLight {get;}
    public int          backgroundRedMode{get;}
    public List<float>  trialInterval   {get;}
    public List<float>  sWaitSec        {get;}
    public List<float>  fWaitSec        {get;}
    public int          trialTriggerMode{get;}
    public List<float>  trialTriggerDelay{get;}
    public float        trialExpireTime {get;}
    public List<string> materialsInfo   {get;set;}
    public float        soundLength     {get; set;}
    public float        cueVolume {get; set;}
    public string       manuplateMethods {get; set;}
    public bool         countAfterLeave   {get;set; }

    /// <summary>
    /// resore template keys of trigger Dicts:
    ///keys:{"certainTrialStart", "everyTrialStart", "certainTrialEnd", "everyTrialEnd"};
    /// values: trial indexes,      possibility(x100)        same             same
    /// </summary> 
    [JsonIgnore]
    public List<string> DeviceTriggerMethodLs {get; set;}
    
    /// <summary>
    /// 0:trialStart, 1:trialEnd
    /// </summary>
    [JsonIgnore]
    public List<List<string>> DeviceTriggerMethodLsSorted {get; set;}

    /// <summary>
    /// 0-start, 1-end
    /// </summary>
    [JsonIgnore]
    public List<Dictionary<string, int[]>> OGTriggerMethodLs {get; set;}//均包含开始和结束两个DIct，Dict Key为DeviceTriggerMethod内容，value为float
    
    /// <summary>
    /// 0-start, 1-end
    /// </summary>
    [JsonIgnore]
    public List<Dictionary<string, int[]>> MSTriggerMethodLs {get; set;}//均包含开始和结束两个DIct，Dict Key为DeviceTriggerMethod内容，value为float
    [JsonIgnore]
    public List<Dictionary<string, int[]>> OGTriggerSortedInType    {get; set;}
    [JsonIgnore]
    public List<Dictionary<string, int[]>> MSTriggerSortedInType    {get; set;}
    
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
                    throw new Exception("invalid input");
                }
            }else{
                Debug.Log($"error in parse, invalid input: {randomArg}");
                throw new Exception("invalid input");
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

    void ProcessUnit(string pos, List<int> targetLs = null){
        if(targetLs==null){targetLs = barPosLs;}

        List<int> _pos = new List<int>{};
        int multiple = 0;
        if(pos.Contains("*")){
            if(pos.Contains("+")){//format like (20+30+20)*n, means 20,30,20, 20,30,20,...
                foreach(string posUnit in pos[..pos.LastIndexOf("*")].Replace("(", "").Replace(")", "").Split('+')){
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
                string posContent = pos[..pos.IndexOf("*")];
                try{
                    multiple = Convert.ToInt16(pos[(pos.IndexOf("*") + 1)..]);
                    _pos.Add(Convert.ToInt16(posContent));
                }
                catch(FormatException) when(multiple > 0){
                    if(posContent.Contains("~")){
                        List<int> tempRange = new List<int>();
                        RandomParse("random" + posContent, tempRange);
                        for (int i = 0; i < multiple; i++){
                            targetLs.Add(GetRandom(tempRange));
                        }
                        multiple = 0;
                    }
                    else{
                        throw;
                    }
                }
            }
        }else{
            multiple = 1;
            _pos.Add(Convert.ToInt16(pos));
        }

        
        for(int i = 0; i < multiple; i++){
            foreach(int posUnit in _pos){
                if(posUnit >= avaliablePosDict.Count() && targetLs == barPosLs){
                    throw new Exception($"wrong pos id: pos {posUnit} not in available_pos");
                }
                else{
                    targetLs.Add(posUnit);
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

    public int GetBarOffset(){
        return barOffset;
    }

    public int GetFinalBarPos(int trial){
        return (GetDegInTrial(trial, raw:false, withoffset:true) + 360)  % 360;
        // return (GetBarPos(trial) + GetBarShift(trial) + GetBarOffset()) % 360;
    }

    public int GetRightLickPosIndInTrial(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return lickPosLs[barPosLs[trial]];
    }

    public int GetPumpPosInTrial(int trial){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return pumpPosLs[barPosLs[trial]];
    }

    public int GetDegInTrial(int trial, bool raw = false, bool withoffset = true){
        if(trial < 0 || trial >= barPosLs.Count){return -1;}
        return (avaliablePosDict[barPosLs[trial]] + (raw? 0: barShiftedLs[trial]) + (withoffset? barOffset: 0)) % 360;
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
    public Camera CameraMonitor;
    public Camera MainCamera;
    public Camera SecondCamera;
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
    float standingSecNowInTrigger = -1;
    // float[] standingPosInTrial = new float[2];
    float standingSecNowInDest = -1;
    bool waiting = true;
    bool forceWaiting = true;
    /// <summary>
    /// -1：初始未开始，-2：forcewaiting, -3:not start but record，0：waiting， 1：started，2：finished but not end
    /// </summary>
    public  int trialStatus = -2;
    int[] trialDeviceTriggerStatus = new int[3];
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

    /// <summary>
    /// key: soundMode
    /// </summary>
    Dictionary<int, float[]> audioPlayTimes = new Dictionary<int, float[]>{};
    public List<string> TrialSoundPlayModeExplain = new List<string>{"Off", "BeforeTrial", "NearStart", "BeforeGoCue", "BeforeLickCue", "InPos", "EnableReward", "AtFail"};
    public List<Material> Backgrounds = new List<Material>();
    float cueVolume;

    // float[] cueSoundPlayTime    {get{return audioPlayTimes.Count > 0? audioPlayTimes[TrialSoundPlayModeCorresponding["BeforeTrial"]]: new float[3];} set{if(audioPlayTimes.Count > 0){audioPlayTimes[TrialSoundPlayModeCorresponding["BeforeTrial"]] = value;}}}
    float alarmPlayTimeInterval;
    bool alarmPlayReady = false;//如果其他情况设false，使用alarm.DeleteAlarm("SetAlarmReadyToTrue", forceDelete:true);防止alarmPlayReady在播放间隔后恢复
    float alarmLickDelaySec = 2;
    List<string> recTypeLs = new List<string>(){
        // 0        1       2     3         4          5           6           7        8          9            10          11          12
        "lick", "start", "end", "init", "entrance", "press", "lickExpire", "trigger", "stay", "soundplay", "OGManuplate", "sync", "miniscopeRecord"
    };
    List<string> recTypeAddtion = new List<string>(){"skip", "complete_manually", "null"};
    UIUpdate ui_update;
    Alarm alarm;    public Alarm alarmPublic{get{return alarm;}}
    // public SoundConfig soundConfig;

    #region communicating
    IPCClient ipcclient;    public IPCClient Ipcclient { get { return ipcclient; } }
    List<string> portBlackList = new List<string>();
    SerialPort sp = null;
    volatile bool StopSerialThread = false;
    int serialSpeed = -1;
    List<string> compatibleVersion = new List<string>(){"V2.1"};
    Thread serialThread;
    // Thread serialSyncThread;
    CommandConverter commandConverter;
    /// <summary>
    /// 0-lick, 1-entrance, 2-press, 3-context_info, 4-log, 5-echo, 6-value_change, 7-command, 8-debugLog, 9-stay, 10-syncInfo, 11-miniscopeRecord
    /// </summary>
    /// <typeparam name="string"></typeparam>
    /// <returns></returns>
    List<string> lsTypes = new List<string>(){
        // "lick", "entrance", "press", "context_info", "log", "echo", "value_change", "command", "debugLog", "stay", "syncInfo", "miniscopeRecord"
           "li",      "en",       "pr",     "ci",       "log", "echo", "vc",           "cmd",     "debugLog", "st",    "si",       "ms"
    };//虚拟command从其他脚本实现
    public List<string> LsTypes {get {return lsTypes;}}
    List<byte[]> serial_read_content_ls = new List<byte[]>();//仅在串口线程中改变
    int serialReadContentLsMark = -1;
    float commandVerifyExpireTime = 2;//2s
    ManualResetEvent manualResetEventVerify = new ManualResetEvent(true);
    //readonly object lockObject_command = new object();
    ConcurrentQueue<byte[]> commandQueue = new ConcurrentQueue<byte[]>();
    ConcurrentQueue<string> buildinCommandQueue = new ConcurrentQueue<string>();
    public ConcurrentDictionary<float, string> commandVerifyDict = new ConcurrentDictionary<float, string>();
    /// <summary>
    /// 0-p_lick_mode, 1-p_trial, 2-p_trial_set, 3-p_now_pos, 4-p_lick_rec_pos, 5-p_INDEBUGMODE, 6-p_OGActiveMills, 7-p_miniscopeRecord
    /// </summary>
    List<string> Arduino_var_list =  "p_lick_mode, p_trial, p_trial_set, p_now_pos, p_lick_rec_pos, p_INDEBUGMODE, p_OGActiveMills, p_miniscopeRecord".Replace(" ", "").Split(',').ToList(); public List<string> ArduinoVarList { get { return Arduino_var_list; }}
    List<string> Arduino_ArrayTypeVar_list =  "p_waterServeMicros, p_lick_count, p_water_flush".Replace(" ", "").Split(',').ToList();
    Dictionary<string, string> Arduino_var_map =  new Dictionary<string, string>{};//{"p_...", "0"}, {"p_...", "1"}...
    Dictionary<string, string> Arduino_ArrayTypeVar_map =  new Dictionary<string, string>{};
    bool debugMode = false; public bool DebugMode { get { return debugMode;} set{debugMode = value;}}
    public GameObject refseg;
    Material refSegementMat;
    public Dictionary<string, bool> DeviceEnableDict = new Dictionary<string, bool>{};
    public Dictionary<string, int> ButtonTriggerDict = new Dictionary<string, int>{};
    /// <summary>
    /// 0:Optogenetics, 1:Miniscope, 2:PythonScript
    /// </summary>
    bool[] DeviceCloseOptionBeforeExits = new bool[3] { true, true, false };
    


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
    string IniReadContent;

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
                if(!UnityEngine.ColorUtility.TryParseHtmlString(_mat, out color)){
                    return this;
                }
                if(name == "backgroundMat"){
                    color = new Color(Math.Max(backgroundLight, color.r)/255, Math.Max(BackgroundLightRedMode? 0: backgroundLight, color.g)/255, Math.Max(BackgroundLightRedMode? 0: backgroundLight, color.b)/255);
                }
                material = new Material(Shader.Find("Unlit/Color")){color = color};
                material.name = name == "backgroundMat"? "Main background": material.name;

            }else{
                #if UNITY_EDITOR
                string tempPath = $"Assets/Resources/{_mat}.png";
                #else
                string tempPath = Application.dataPath + $"/Resources/{_mat}.png";
                #endif
                if(System.IO.File.Exists(tempPath)){
                    LoadMaterialFromPath(tempPath, ref material);
                    material.mainTextureScale = new Vector2(width/400, 1);
                    if(name == "backgroundMat"){material.name = "Main background";}
                }else{
                    Debug.LogWarning($"No such Material named {_mat}.png");
                }
            }

            return this;
        }

        public void SetMaterialToObject(GameObject gameObject){
            gameObject.GetComponent<MeshRenderer>().material = material;
        }

        public Material GetMaterial(){
            return material;
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

    /// <summary>
    /// deg: float 0~360
    /// </summary>
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

    public static void LoadMaterialFromPath(string path, ref Material material){
            FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            fileStream.Seek(0, SeekOrigin.Begin);
            //创建文件长度缓冲区
            byte[] bytes = new byte[fileStream.Length]; 
            //读取文件
            fileStream.Read(bytes, 0, (int)fileStream.Length);
            //释放文件读取流
            fileStream.Close();
            fileStream.Dispose();

            Texture2D texture = new Texture2D(100, 100);
            texture.LoadImage(bytes);
            material.SetTexture("_MainTex", texture);
        }
    
    void SetBarMaterial(MaterialStruct _mat, GameObject otherBar = null){
        GameObject tempBar = otherBar == null? bar: otherBar;
        if(tempBar.GetComponent<MeshRenderer>().material.name == _mat.Name+" (Instance)"){return;}
        // Debug.Log(tempBar.name);
        // Debug.Log(bar.GetComponent<MeshRenderer>().material.shader.name);
        _mat.SetMaterialToObject(tempBar);
        if(isRing && otherBar == null){
            _mat.SetMaterialToObject(barChild);
            _mat.SetMaterialToObject(barChild2);
        }
    }

    public void SetBackgroundMaterial(Material _mat){
        background.GetComponent<MeshRenderer>().material = _mat;
    }
    
    MaterialStruct GetMaterialStruct(string matName){
        if(MaterialDict.ContainsKey(matName)){
            return MaterialDict[matName];
        }else{
            return MaterialDict["default"];
        }

    }

    int ActivateBar(int pos = -1, int trial = -1){
        float tempPos = -1;
        if(pos != -1){
            tempPos = contextInfo.GetDeg(pos);
            SetBarPos(DegToPos(tempPos));
        }else if(trial != -1){
            // tempPos = contextInfo.GetDegInTrial(trial);
            tempPos = contextInfo.GetFinalBarPos(trial);
            SetBarPos(DegToPos(tempPos));
        }else{
            return (int)tempPos;
        }

        bar.SetActive(true);
        if(barChild != null && barChild2 != null){
            barChild.SetActive(true);
            barChild2.SetActive(true);
        }
        if(refSegementMat != null){refSegementMat.color = Color.white;}
        Debug.Log("bar activited");
        return (int)tempPos;
    }

    void DeactivateBar(){//后续和endtrial分离?
        bar.SetActive(false);
        if(barChild != null && barChild2 != null){
            barChild.SetActive(false);
            barChild2.SetActive(false);
        }
        if(refSegementMat != null){refSegementMat.color = Color.black;}

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
        backgroundMat.SetMaterialToObject(background);
        Backgrounds.Insert(0, backgroundMat.GetMaterial());
        // MaterialDict["backgroundMat"].SetMaterial(background);
        DeactivateBar();

        return 1;
    }

    float GetSoundPitch(float _lastTime, float _totalTime){//后续改
        return Math.Max(1, Math.Min(5, 0.5f + 0.5f * _totalTime/_lastTime));
    }
    
    /// <summary>
    /// addorremove: 0:remove, 1:add, 1+:change
    /// </summary>
    /// <param name="_playMode"></param>
    /// <param name="addOrRemove"></param>
    /// <param name="soundName"></param>
    /// <param name="clearAll"></param>
    /// <param name="clearOtherAll"></param>
    /// <returns></returns>
    public int ChangeSoundPlayMode(int _playMode, int addOrRemove, string soundName, bool clearAll = false, bool clearOtherAll = false){// addOrRemove = false时, clearAll为true则清除其他模式
        if(addOrRemove > 0){
            if(addOrRemove == 1 && !audioPlayModeNow.Contains(_playMode) && audioClips.ContainsKey(soundName)){
                audioPlayModeNow.Add(_playMode);
                audioSources[_playMode].clip = audioClips[soundName];
                audioSources[_playMode].name = soundName;
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

        public int PlaySoundPublic(int soundMode, string addInfo = "", bool manual = false){
            return PlaySound(soundMode, addInfo, manual);
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
    int PlaySound(AudioSource audioSource, int trackBackMode = -1, string addInfo = "", bool manual = false){
        if(!audioSources.ContainsValue(audioSource)){ return -1;}
        // if(audioSources[soundName].isPlaying){return 0;}


        if(audioSource.clip.name == "alarm"){
            if(alarmPlayReady || manual){
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
    int PlaySound(int soundMode, string addInfo = "", bool manual = false){
        if(audioSources.ContainsKey(soundMode) || manual == true){
            int playResult = PlaySound(audioSources[soundMode], soundMode, addInfo:addInfo, manual:manual);
            Debug.Log($"playResult: {playResult} {(manual? "manually":"")}");
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
        foreach(int mode in audioPlayTimes.Keys){
            if(TrialSoundPlayModeExplain[mode] == "InPos"){//InPos不用TryStop
                continue;
            }
            float[] tempTimes = audioPlayTimes[mode];
            if(tempTimes[0] > 0 && tempTimes[2] > 0){
                if(Time.fixedUnscaledTime >= tempTimes[2]){
                    StopSound(mode);
                }
            }
        }

    }

    int CreateSerialConnection(out SerialPort sp, ref List<string> portInfo){
        string[] portLs = ScanPorts_API();
        bool connected = false;
        if (portLs.Length == 0) { portInfo.Add("No Port Found!"); }
        foreach (string port in portLs){
            if (!connected && port.Contains("COM") && !portBlackList.Contains(port)){
                try{
                    sp = new SerialPort(port, serialSpeed, Parity.None, 8, StopBits.One);
                    sp.RtsEnable = true;
                    sp.DtrEnable = true;
                    sp.Open();
                    Debug.Log("COM avaible: " + port);

                    sp.ReadTimeout = 1000;
                    int fail_count = 10;
                    while (fail_count > 0){
                        fail_count--;
                        string temp_readline = sp.ReadLine();
                        //Debug.Log(temp_readline);
                        if (temp_readline.StartsWith("initialed")){
                            if (temp_readline.Length > 10 && temp_readline[9..].StartsWith(":")){
                                string tempInfo = temp_readline[10..];
                                if (compatibleVersion.Count() > 0 && !compatibleVersion.Contains(tempInfo)){
                                    portInfo.Add($"Incompatible version in {port}: {tempInfo}, required version: {string.Join(", ", compatibleVersion)}");
                                    break;
                                }else{
                                    connected = true;
                                    return 1;
                                }
                            }
                            break;
                        }
                        else{
                            if (fail_count == 0){
                                throw new Exception($"Arduino not initialed or version doesn't match, init info:{temp_readline}");
                            }
                            continue;
                        }
                    }

                    //}
                }
                catch (Exception e){
                    sp = new SerialPort();
                    Debug.Log(e);
                    // ui_update.MessageUpdate(e.Message+"\n");
                    sp.Close();
                    sp = null;
                    if (e.Message.Contains("拒绝访问")){
                        string strPortLs = string.Join(", ", portLs);
                        portInfo.Add($"port {port} accssion Denied");
                        // MessageBoxForUnity.Ensure($"Accssion Denied, please try another port or free {port} frist.\nserial speed: {serialSpeed}; now port: {port};  all ports:{strPortLs}", "Serial Error");
                        // Quit();
                    }
                    else{
                        string strPortLs = string.Join(", ", portLs);
                        portInfo.Add($"Can not connect to port {port} because: {e.Message}");
                        // MessageBoxForUnity.Ensure($"Can not connect to Arduino, please try another port or use Arduino IDE to Reopen The Serial Communicator.\nserial speed: {serialSpeed} now port: {port}; all ports: {strPortLs}", "Serial Error");
                        // Quit();
                    }
                }
                finally{

                }
            }
            Debug.Log(port);
        }
        sp = null;
        return -1;
    }

    void RecreateSerialConnection(bool inMainThread = true){
        if(sp != null){
            sp.Close();
            sp = null;
        }
        List<string> portInfo = new List<string>();
        
        if (CreateSerialConnection(out sp, ref portInfo) > 0){
            Debug.Log("serial reconnected in trial start");
        }else{
            Debug.LogError("No Connection to Arduino! ports' info as follow:\n" + string.Join("\n", portInfo));
            // Quit();
            if(MessageBoxForUnity.YesOrNo("Fatal error occured, failed connecting to Arduino, Stay?", "Serial Error") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_NO){
                if(inMainThread){
                    Quit();
                }else{
                    StopSerialThread = true;
                    buildinCommandQueue.Enqueue("exit");
                }
            }
        }
    }

    int ContextInitSync(){
        List<string> names = new List<string>(){Arduino_var_list[0], Arduino_var_list[1]};
        List<int> values = new List<int>(){trialMode % 0x10, Math.Max(0, nowTrial)};
        DataSend("forceinit");
        int res = CommandVerify(names, values);
        //DataSend("p_trial_set=1", true);
        return res;
    }

    int ContextStartSync(){
        List<string> names = new List<string>(){Arduino_var_list[1], Arduino_var_list[3], Arduino_var_list[2]};
        List<int> values = new List<int>(){nowTrial, contextInfo.GetPumpPosInTrial(nowTrial), 1};
        int res = CommandVerify(names, values);
        // DataSend("_");
        // DataSend("p_trial_set=1", true, true);
        // Debug.Log("sent :p_trial_set=1");
        return res;
    }

    int ContextEndSync(){//给水判定在lick中，暂时没用

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
            InitTrial(isFristInit:manual);
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

    int InitTrial(bool isFristInit = false){
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
        if(isFristInit){
            if(trialStartTriggerMode == 3 && ipcclient.Activated){
                ipcclient.SetCurrentTriggerArea(0);
                ipcclient.MDDrawTemp(ipcclient.GetCurrentSelectArea());
            }
        }
        StopSound();
        DeactivateBar();
        trialInitTime = Time.fixedUnscaledTime;
        WriteInfo(recType:3);
        ui_update.MessageUpdate("Trial initialized");
        return 0;
    }

    int ServeWaterInTrial(){
        int fail = 0;
        while(CommandVerify(Arduino_var_list[2], 2) == -1){
            fail += 1;
            if (fail > 10){
                Debug.Log("ServeWaterInTrial failed for 10 times");
                return -10;
            }
        };
        // Debug.Log($"ServeWaterInTrial {(fail == 0? "succes": $"failed for {fail} time")}");
        return 0 - fail;
    }

    int StartTrial(){//根据soundCueLeadTime在alarm中设置waiting
 
        nowTrial++;
        trialStatus = 1;
        trialStartTime = Time.fixedUnscaledTime;
        while (!DebugWithoutArduino && ContextStartSync() < 0){
            RecreateSerialConnection();
            ContextInitSync();
        }
        string tempMatName = contextInfo.GetBarMaterialInTrial(nowTrial);
        MaterialStruct tempMs = GetMaterialStruct(tempMatName);
        // Debug.Log(tempMs.PrintArgs());
        SetBarMaterial(tempMs);
        alarmPlayReady = true;//为waiting的delay允许alarm
        if(ipcclient.Activated){
            ipcclient.MDClearTemp();

            int markCountPerType = 32;
            int rightMark = contextInfo.GetTrackMarkInTrial(nowTrial);
            List<int[]>DestinationAreas = ipcclient.GetselectedArea().Where(area => area[0] / markCountPerType == 1).ToList();
            List<int[]>TriggerAreas = ipcclient.GetselectedArea().Where(area => area[0] / markCountPerType == 0).ToList();

            if(rightMark >= 0 && contextInfo.destAreaFollow){
                int[] CertainAreaNowTrial = DestinationAreas.Find(area => area[0] % markCountPerType == rightMark);
                if(CertainAreaNowTrial != null){//ipcclient绘制部分
                    ipcclient.SetCurrentDestArea(contextInfo.GetDegInTrial(nowTrial, withoffset:false), circle:CertainAreaNowTrial[1] == 0);
                    List<int[]> areas = ipcclient.GetCurrentSelectArea();
                    if(trialStartTriggerMode == 3){
                        foreach(int[] tarea in TriggerAreas){areas.Add(tarea);}
                    }
                    ipcclient.MDDrawTemp(areas, new List<Vector2Int[]>{ipcclient.GetCircledRotatedRectange(contextInfo.GetFinalBarPos(nowTrial))});//矩形绘制bar所在位置
                }else{
                    Debug.Log("No selected area match the pos now");
                }
            }else if(!contextInfo.destAreaFollow){
                int[] CertainAreaNowTrial = DestinationAreas.Find(area => area[0] % markCountPerType == rightMark);
                if(CertainAreaNowTrial != null){//ipcclient绘制部分
                    ipcclient.SetCurrentDestArea(rightMark + 32);
                    List<int[]> areas = ipcclient.GetCurrentSelectArea();
                    if(trialStartTriggerMode == 3){
                        foreach(int[] tarea in TriggerAreas){areas.Add(tarea);}
                    }
                    ipcclient.MDDrawTemp(areas, new List<Vector2Int[]>{ipcclient.GetCircledRotatedRectange(contextInfo.GetFinalBarPos(nowTrial))});
                }else{
                    Debug.Log("No selected area match the pos now");
                }
            }else{
                ui_update.MessageUpdate("No selected area match the pos now");
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
        DeviceTriggerExecute(0);

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
        WriteInfo(recType: 1, _lickPos:activitedPos, addInfo:$"{contextInfo.GetBarShift(nowTrial)}");

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
    int EndTrial(bool isInit = false, bool trialSuccess = false, int rightLickSpout = -1, float trialReadyWaitSec = -1, bool serveWater = false, bool ignoreBarLatstingTime = false){
        // Debug.Log($"End Trial {nowTrial}");
        ContextEndSync();
        trialStatus = 0;
        if(isInit || !trialSuccess || ignoreBarLatstingTime){DeactivateBar();}
        else{alarm.TrySetAlarm("DeactivateBar", contextInfo.barLastingTime, out _);}
        
        if(!isInit && !trialSuccess){PlaySound("AtFail");}

        waiting = true;
        alarmPlayReady = false;
        alarm.DeleteAlarm("SetAlarmReadyToTrue", forceDelete:true);
        alarm.TrySetAlarm("SetAlarmReadyToTrueAfterTrianEnd", alarmLickDelaySec, out _);
        if(ipcclient.Activated && trialStartTriggerMode == 3){
            ipcclient.MDClearTemp();
            ipcclient.SetCurrentTriggerArea(0);
            ipcclient.MDDrawTemp(ipcclient.GetCurrentSelectArea());
        }
        
        WriteInfo(recType: isInit? 3: 2, _lickPos: rightLickSpout);
        //Debug.Log("rightLickSpout" + rightLickSpout);

        if(!isInit){
            DeviceTriggerExecute(1);
            if(serveWater){ServeWaterInTrial();}//20ms左右，如果放在DeviceTriggerExecute后能正常运行，说明串口影响了定时器中断
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
                    ui_update.MessageUpdate($"Interval: {_temp_waitSec}", attachToLastLine:true);
                    Debug.Log(Time.fixedUnscaledTime);
                }
                waitSec = _temp_waitSec;
            }else{
                if(trialStartTriggerMode == 0){
                    
                    waitSec = trialResult[nowTrial] == 1? GetRandom(contextInfo.sWaitSec) : GetRandom(contextInfo.fWaitSec);
                    ui_update.MessageUpdate($"Interval: {waitSec}", attachToLastLine:true);

                }else{//其他主动触发模式
                    waitSec = contextInfo.barDelayTime;
                    ui_update.MessageUpdate($"Interval: {waitSec}", attachToLastLine:true);
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
            Debug.Log($"Time.fixedUnscaledTime {Time.fixedUnscaledTime}, waitSecRec {waitSecRec}, waitSec {waitSec}, _lasttime {_lasttime}");
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
        // ui_update.SetButtonColor("IPCRefreshButton", res? Color.white : Color.grey);
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
        if(lickTrial < 0){return -1;}
        if(lickInd < 0){
            lickCount.Add(new int[8].ToList());
            return 0;
        }

        if(getOrSet == "set"){
            if(lickCount.Count <= lickTrial){
                while(lickCount.Count <= lickTrial){
                    if(lickCount.Count ==  + 1){
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

    int TrialResultAdd(int result, int trial, int LickSpout, int rightLickSpout, bool force = false){
        if(LickSpout < 0 || rightLickSpout < 0){return -2;}
        if(trialResult.Count() == trial){
            trialResult.Add(result);
            trialResultPerLickSpout[LickSpout*2].Add(result);
            if(result == 0){
                trialResultPerLickSpout[rightLickSpout*2+1].Add(1);
                return 1;
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

    public int LickingCheckPubic(int lickInd, int lickTypeMark = 1){
        return LickingCheck(lickInd, lickTypeMark);
    }

    /// <summary>
    /// check后判断是否结束当前trial, lickInd = -1: 超时; -2: 手动成功进入下一个trial, -3: 位置检测完成trial
    /// lick: 1:reach, 0:leave, default 
    /// return: 1: default lick, 0:default leave  -3:延时, -4:位置判定过
    /// </summary>
    /// <param name="lickInd"></param>
    /// <param name="lickTypeMark"></param>
    /// <returns></returns>
    int LickingCheck(int lickInd, int lickTypeMark = 1){
        // Debug.Log($"LickingCheck: lickInd {lickInd}, lickTypeMark {lickTypeMark} in trial {nowTrial}");
        int rightLickInd = contextInfo.GetRightLickPosIndInTrial(nowTrial);
        
        if(lickInd >= 0 && trialMode >> 4 == 3){
            lickInd = -4;
        }
        
        if(lickTypeMark == 1){
            lickCountGetSet("set", lickInd, nowTrial);
            
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
        }

        if(!waiting){//waiting期间的舔不进一步进入判断，仅做记录
            if(lickTypeMark == 0){
                WriteInfo(_lickPos: lickInd, addInfo:"leave");
                return 0;
            }
            
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
                            TrialResultAdd(lickInd == -2? 1 : 0, nowTrial, lickInd == -2? rightLickInd: lickInd, rightLickInd);
                            //trialResult.Add(lickInd == -2? 1 : 0);
                            if(lickInd == -2){
                                if(trialMode == 0x01){ServeWaterInTrial();}
                                ui_update.MessageUpdate($"Trial completed manually at pos {rightLickInd}");
                            }
                        }
                        EndTrial(trialSuccess: true, rightLickSpout: rightLickInd, trialReadyWaitSec: contextInfo.barLastingTime);
                    }else{
                        TrialResultAdd(lickInd == -1? -1: -3, nowTrial, lickInd == -1? rightLickInd: lickInd, rightLickInd);
                        if(lickInd == -1){
                            ui_update.MessageUpdate($"Trial expired at pos {rightLickInd}");
                            EndTrial(trialSuccess:false, rightLickSpout: rightLickInd, trialReadyWaitSec: contextInfo.barLastingTime);
                        }
                    }
                }else if(trialMode >> 4 == 1){
                    //只能舔对的
                    result = result || lickInd == -2;
                    TrialResultAdd(result? 1: 0, nowTrial, lickInd == -2? rightLickInd: lickInd, rightLickInd);
                    if(result && trialMode == 0x11){
                        ServeWaterInTrial();
                    }
                    else{CommandVerify(Arduino_var_list[2], 0);}
                    if(lickInd < 0){
                        ui_update.MessageUpdate($"Trial {(result? "skipped manually": "expired")} at pos {lickInd}, right place: {rightLickInd}");
                    }else if(lickInd >= 0){
                        ui_update.MessageUpdate($"Trial {(result? "success": "failed")} at pos {lickInd}, right place: {rightLickInd}");
                    }
                    EndTrial(trialSuccess: result, rightLickSpout: rightLickInd, trialReadyWaitSec: result? contextInfo.barLastingTime : 0);
                }else if(trialMode >> 4 == 2){//到特定地方
                    result = false;
                    if(lickInd < 0){
                        result = lickInd == -2 || lickInd == -3;
                    }

                    if(lickInd >= 0 && trialResult.Count > nowTrial){//完成任务后小鼠舔了
                        // if(trialMode % 0x10 == 2){ServeWaterInTrial();}
                        ui_update.MessageUpdate("Trial end");
                        EndTrial(trialSuccess:trialResult[nowTrial] == 1, serveWater:trialMode % 0x10 == 2? true: false, ignoreBarLatstingTime:true);
                    }else if(lickInd < 0){//小鼠完成了任务，或手动按下按键完成/跳过
                        TrialResultAdd(result? (lickInd == -2 ? -2: 1): 0, nowTrial, rightLickInd, rightLickInd);
                        if(result && trialStatus != 2){
                            if(trialMode % 0x10 == 1){ServeWaterInTrial();}
                            // DeactivateBar();
                            alarm.TrySetAlarm("DeactivateBar", contextInfo.barLastingTime, out _);
                            ui_update.MessageUpdate("Target arrived.");
                            DeviceTriggerExecute(2);
                            PlaySound("EnableReward");

                            trialStatus = 2;
                        }else if(!result){//手动跳过
                            ui_update.MessageUpdate("Trial skipped");
                            EndTrial(trialSuccess: false, ignoreBarLatstingTime:true);
                        }
                    }
                }
                ui_update.MessageUpdate();

            }else{
                
            }
            return 1;//正常判断完成
        }else{
            ui_update.MessageUpdate();
            if(lickTypeMark == 1 || contextInfo.countAfterLeave){
                return -3;//不需要判断，已返回给commandParse做延时处理
            }else{
                return 1;
            }
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
    /// rectype: 0-lick, 1-start, 2-end, 3-init, 4-entrance, 5-press, 6-lickExpire, 7-trigger, 8-stay, 9-soundplay, 10-OGManuplate, 11-sync, 12-miniscopeRecord
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

    /// <summary>
    /// use CommandVerify
    /// </summary>
    /// <param name="_mills"></param>
    /// <returns></returns>
    public bool OGSet(int _mills){//_mills: 0: 关闭, 1+: mills, -1：持续
        bool _on = _mills > 0 || _mills == -1;
        int res = 0;
        if(_mills < 30000){
            res = CommandVerify(Arduino_var_list[6], _mills);
            if(res == 1 || res == -3){
                WriteInfo(recType: 10, _lickPos: _mills);
                Debug.Log($"OG set {_mills}");
                ui_update.MessageUpdate($"OG {(_mills != 0? "on": "off")}{(_mills > 0 ? $" for {_mills} mills": "")}");
            }
            return res == 1;
        }

        if(alarm.GetAlarm("ogEnd") >= 0){
            alarm.TrySetAlarm("ogStart", 1, out _, addInfo:$"{_mills}");
            alarm.StartAlarmAfter("ogStart", "ogEnd");
            alarm.TrySetAlarm("ogEnd", 1, out _);
        }else{
            res = CommandVerify(Arduino_var_list[6], _on? -1: 0);
            if(res == 1 || res == -3){
                WriteInfo(recType:12, _lickPos:_mills);
                Debug.Log($"OG {(_on? "on": "off")}");
                ui_update.MessageUpdate($"OG {(_on? "on": "off")}{(_mills > 0 ? $" for {_mills/1000}s": "")}");
                if(_mills > 0){
                    alarm.TrySetAlarm("ogEnd", (float)_mills / 1000, out _);
                }
            }

        }
        
        return res == 1;
    }

    /// <summary>
    /// use CommandVerify, return res
    /// </summary>
    /// <param name="_on"></param>
    /// <returns></returns>
    public bool MSSet(int _sec = -1){
        bool _on = _sec > 0 || _sec == -1;
        int res = 0;
        if(alarm.GetAlarm("miniscopeEnd") >= 0){
            alarm.TrySetAlarm("miniscopeStart", 1, out _, addInfo:$"{_sec}");
            alarm.StartAlarmAfter("miniscopeStart", "miniscopeEnd");
            alarm.TrySetAlarm("miniscopeEnd", 1, out _);
        }else{
            res = CommandVerify(Arduino_var_list[7], (_sec > 0 || _sec == -1)? 1: 0);
            if(res == 1 || res == -3){
                WriteInfo(recType:12, _lickPos:_sec);
                Debug.Log($"MS {(_on? "on": "off")}");
                ui_update.MessageUpdate($"MS {(_on? "on": "off")}{(_sec > 0 ? $" for {_sec}s": "")}");
                if(_sec > 0){
                    alarm.TrySetAlarm("miniscopeEnd", (float)_sec, out _);
                }
            }

        }
        
        return res == 1;
    }
    
    void CloseDevices(){
        if(DeviceCloseOptionBeforeExits[0]){OGSet(0);}
        if(DeviceCloseOptionBeforeExits[1]){MSSet(0);}
        // serialSync = false;
        // serialSyncThread.Join();
    }

    bool CheckInRegion(long[] _pos, int[] selectedPos){//还没改好
        if(selectedPos[1] == 0){//圆形
            bool incircle = Math.Sqrt(Math.Pow(Math.Abs(_pos[0] - selectedPos[2]), 2) + Math.Pow(Math.Abs(_pos[1] - selectedPos[3]), 2)) < selectedPos[4];
            return incircle == (Math.Abs(selectedPos[5]) == 1);
        }else if(selectedPos[1] == 1){//矩形
            return (selectedPos[4] > _pos[0] &&  _pos[0] > selectedPos[2]) && (selectedPos[5] > _pos[1] &&  _pos[1] > selectedPos[3]);
        }else{
            return false;
        }
    }

    string CheckMouseStat(){//待加其他内容
        return "";
    }

    public bool GetContextInfoDestAreaFollow(){
        return contextInfo.destAreaFollow;
    }

    /// <summary>
    /// triggerType: 0, 1-trialStart/end, 2-finish  ，无论具体名称后缀              
    /// 
    /// keys:{"certainTrialStart", "everyTrialStart", "certainTrialEnd", "everyTrialEnd", "certainTrialFinish", "everyTrialFinish"};
    /// values: trial indexes,      possibility(x100)        same             same
    /// 
    /// </summary>
    /// <param name="triggerType"></param>
    /// <returns></returns>
    bool DeviceTriggerExecute(int triggerType){
        if(!new int[]{0, 1, 2}.Contains(triggerType)){return false;}
        // if (triggerType < 2){
        var OGTrigger = contextInfo.OGTriggerSortedInType[triggerType];
        var MSTrigger = contextInfo.MSTriggerSortedInType[triggerType];
        var ElementTrigger = ButtonTriggerDict.ContainsValue(nowTrial) ? ButtonTriggerDict.Keys.Where(x => ButtonTriggerDict[x] == nowTrial).ToList() : new List<string>();
        if(DeviceEnableDict.TryGetValue("OG", out bool _enable) && _enable){
            foreach(var _trigger in OGTrigger){
                int _mills = ui_update.TryGetDeviceSetTime("OGTime", out int _mills_temp)? _mills_temp: -1;
                if(_trigger.Key.StartsWith("certain")){
                    if(_trigger.Value[1..].Contains(nowTrial)){
                        OGSet(_trigger.Value[0] == 1? _mills: 0);
                        trialDeviceTriggerStatus[0] = nowTrial;
                        break;
                    }
                }else if(_trigger.Key.StartsWith("every")){
                    if(GetRandom(new List<int>{0, 100}) < _trigger.Value[1]){
                        OGSet(_trigger.Value[0] == 1? _mills: 0);
                        trialDeviceTriggerStatus[0] = nowTrial;
                        break;
                    }
                }else if(_trigger.Key.StartsWith("next")){
                    if(nowTrial == trialDeviceTriggerStatus[0] + 1){
                        OGSet(_trigger.Value[0] == 1? _mills: 0);
                    }
                }
            }
        }

        if(DeviceEnableDict.TryGetValue("MS", out _enable) && _enable){
            foreach(var _trigger in MSTrigger){
                int _sec = ui_update.TryGetDeviceSetTime("MSTime", out int _mills_temp)? _mills_temp: -1;
                if(_trigger.Key.StartsWith("certain")){
                    if(_trigger.Value[1..].Contains(nowTrial)){
                        MSSet(_trigger.Value[0] == 1? _sec: 0);
                        trialDeviceTriggerStatus[1] = nowTrial;

                        break;
                    }
                }else if(_trigger.Key.StartsWith("every")){
                    if(GetRandom(new List<int>{0, 100}) < _trigger.Value[1]){
                        MSSet(_trigger.Value[0] == 1? _sec: 0);
                        trialDeviceTriggerStatus[1] = nowTrial;

                        break;
                    }
                }else if(_trigger.Key.StartsWith("next")){
                    if(nowTrial == trialDeviceTriggerStatus[0] + 1){
                        MSSet(_trigger.Value[0] == 1? _sec: 0);
                    }
                }
            }
        }
        
        foreach(string elementName in ElementTrigger){
            //  $"Timing{_timing.name};{_timing.Id};{value}"
            var buttonNameSplit = elementName.Split(";");
            ui_update.SetTiming(string.Join(";", buttonNameSplit[..2]), int.Parse(buttonNameSplit[2]));
            ButtonTriggerDict.Remove(elementName);
        }

        return true;
    }

    #endregion context generate end

    #region  file writing
    StreamWriter logStreamWriter;
    StreamWriter posStreamWriter;
    // StreamWriter serialSyncStreamWriter;
    string filePath = "";
    List<string> logList = new List<string>();  public List<string> LogList { get { return logList; } }
    Queue<string> logWriteQueue = new Queue<string>();
    Queue<string> posWriteQueue = new Queue<string>();
    // ConcurrentQueue<float> syncWriteQueue = new ConcurrentQueue<float>();
    const int BUFFER_SIZE = 4096;
    const int BUFFER_THRESHOLD = 32;
    float[] time_rec_for_log = new float[2]{0, 0};
    #endregion file writing end
    
    #region methods of communicating

    string[] ScanPorts_API(){
        string[] portList = SerialPort.GetPortNames();
        return portList;
    }

    public void CommandParsePublic(string limitedCommand, bool urgent = false){//仅接收舔、红外、压杆信号模拟，视频检测移动到特定位置
        string tempHead = limitedCommand.Split(":")[0];
        //   "li",      "en",       "pr",     "ci",       "log", "echo", "vc",           "cmd",     "debugLog", "st",    "si",       "ms"
        string[] availableHead = new string[] { lsTypes[0], lsTypes[1], lsTypes[2], lsTypes[3], lsTypes[9] };
        if(!availableHead.Contains(tempHead)){return;}
        if(urgent){
            CommandParse(commandConverter.ProcessSerialPortBytes(commandConverter.ConvertToByteArray(limitedCommand)));
        }else{
            commandQueue.Enqueue(commandConverter.ProcessSerialPortBytes(commandConverter.ConvertToByteArray(limitedCommand)));
        }
    }

    /// <summary>
    /// 在主线程调用时内容不能有锁,目前全部在主线程调用
    /// </summary>
    void CommandParse(byte[] _command){
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
                try{
                    Convert.ToInt16(command.Split(":")[1]);
                    Convert.ToInt16(command.Split(":")[2]);
                }
                catch(Exception e){
                    Debug.Log(e.Message + "invalid command: " + command);
                    return;
                }
                int lickInd = Convert.ToInt16(command.Split(":")[1]);
                int lickTypeMark = Convert.ToInt16(command.Split(":")[2]);
                float soundCueLeadTime = contextInfo.soundCueLeadTime;
                float waitFromLastLick = Math.Max(soundCueLeadTime, contextInfo.waitFromLastLick);

                // if(LickResultCheck(lickInd, lickTrialMark) == -3 && waitFromLastLick > 0){
                if(LickingCheck(lickInd, lickTypeMark) == -3){//仍在waiting
                    if(trialStartTime < 0){//trial开始前指定时间舔了应延迟
                        if(waitFromLastLick > 0){
                            float _lasttime = waitSec - (Time.fixedUnscaledTime - waitSecRec);
                            // Debug.Log($"from lick parse : Time.fixedUnscaledTime {Time.fixedUnscaledTime}, waitSecRec {waitSecRec}, waitSec {waitSec}, _lasttime {_lasttime}");
                            if(_lasttime < waitFromLastLick){
                                float tempDelay = (soundCueLeadTime < 0 || soundCueLeadTime > waitFromLastLick)? waitFromLastLick: (_lasttime < soundCueLeadTime? soundCueLeadTime* 0.95f: _lasttime);
                                if(alarm.GetAlarm("StartTrialWaitSoundCue") > 0){alarm.TrySetAlarm("StartTrialWaitSoundCue", tempDelay, out _);}
                                waitSecRec = Time.fixedUnscaledTime - waitSec + tempDelay;//无论声音是否出现，trial开始前舔则延迟trial开始
                                // Debug.Log($"after lick parse : Time.fixedUnscaledTime {Time.fixedUnscaledTime}, waitSecRec {waitSecRec}, waitSec {waitSec}, _lasttime {_lasttime}");

                                // Debug.Log("waitSecRec" + waitSecRec);
                                // PlaySound("NearStart");
                            }
                        }
                        // Debug.Log("alarm of SetAlarmReadyToTrueAfterTrianEnd: " + alarm.GetAlarm("SetAlarmReadyToTrueAfterTrianEnd"));
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
                try{Convert.ToInt16(command.Split(":")[1]);}
                catch(Exception e){Debug.Log(e.Message + "invalid command: " + command);return;}
                int tempType = Convert.ToInt16(command.Split(":")[1]);
                if(tempType == 0){
                    TriggerRespond(true, 9);
                }else if(tempType == 1){
                    LickingCheckPubic(lickInd:-3);
                }
                break;
            }
            case 10:{
                break;
            }
            case 11:{//miniscope control
                break;
            }
            default: break;
        }
    }

    /// <summary>
    /// 仅处理Exit等直接固定信息，处理字符串形式模拟串口信息使用CommandParsePublic
    /// </summary>
    void CommandParse(string _buildinCmd){
        switch(_buildinCmd){
            case "exit":{
                Quit();
                break;
            }
            default:{
                break;
            }
        }
    }

    void SerialCommunicating(){
        while (!StopSerialThread){
            manualResetEventVerify.WaitOne();
            if (sp!= null && sp.IsOpen){
                try{
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
                        if (commandConverter.FindMarkOfMessage(true, readBuffer, 0) != -1){
                            serialReadContentLsMark = serial_read_content_ls.Count() - 1;
                        }
                        int temp_end = -1;
                        if (serialReadContentLsMark != -1){
                            temp_end = commandConverter.FindMarkOfMessage(false, readBuffer, 0);
                            //if(temp_end==-1){serial_read_content_ls.Add(readBuffer);}
                        }

                        if (serialReadContentLsMark != -1 && temp_end != -1){
                            byte[] temp_complete_msg;
                            temp_complete_msg = commandConverter.ProcessSerialPortBytes(commandConverter.Read_buffer_concat(serial_read_content_ls, serialReadContentLsMark, -1));
                            //Debug.Log("process: "+string.Join(",", temp_complete_msg));
                            if (temp_complete_msg.Length > 0){
                                if (commandConverter.GetCommandType(temp_complete_msg, out _) == lsTypes.IndexOf("syncInfo")){
                                    //Debug.Log(string.Join(",", temp_complete_msg));
                                    // syncWriteQueue.Enqueue(UnscaledfixedTime);
                                }else{
                                    commandQueue.Enqueue(temp_complete_msg);
                                }
                            }

                            serial_read_content_ls.Clear();
                            if (readBuffer.Length - temp_end > 0){
                                byte[] temp_readBuffer = new byte[readBuffer.Length - temp_end];
                                Array.Copy(readBuffer, temp_end, temp_readBuffer, 0, temp_readBuffer.Length);
                                if (commandConverter.FindMarkOfMessage(true, temp_readBuffer, 0) != -1){
                                    serial_read_content_ls.Add(temp_readBuffer);
                                    serialReadContentLsMark = 0;
                                }
                            }
                        }
                    }else{
                        Thread.Sleep(1);
                    }
                }catch(System.IO.IOException e){
                    Debug.Log(e.Message);
                    sp = null;
                }
            }else{
                RecreateSerialConnection(false);
            }
        }
    }

    public int DataSendRaw(string message){
        byte[] temp_msg = Encoding.UTF8.GetBytes(message);
        sp.Write(temp_msg, 0, temp_msg.Length);
        return 1;
    }
    public int DataSend(string message, bool needParse = false, bool inVerifyOrVerifyNeedless=false){
        // Debug.Log("Data sent: " + message);
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
                        byte[] temp_msg = commandConverter.ConvertToByteArray($"{lsTypes[6]}:{temp_command}");
                        sp.Write(temp_msg, 0, temp_msg.Length);
                        // Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                        return 2;
                    }
                    else{
                        if(Int16.TryParse(temp_var_name, out short temp_id) && temp_id<255){//重发int=int
                            string temp_command=temp_var_name+"="+message.Split('=')[1];
                            byte[] temp_msg = commandConverter.ConvertToByteArray($"{lsTypes[6]}:{temp_command}");
                            sp.Write(temp_msg, 0, temp_msg.Length);
                            //Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                            return 2;
                        }
                        return -1;
                    }
                //}
            }else{
                byte[] temp_msg = commandConverter.ConvertToByteArray($"{lsTypes[7]}:{message}");
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

    /// <summary>
    /// return: 1:success, -1:fail, -2:port not open, -3:no port
    /// </summary>
    public int CommandVerify(List<string> messages, List<int> values){
        if (sp == null) { return -3; }
        manualResetEventVerify.Reset();
        sp.ReadTimeout = 100;
        int fail_count = 0;
        bool succes = false;
        int fail_countMax = 10;
        int tempMsgInd = 0;//记录已经同步完成的内容

        // Debug.Log("verify start");
        while (!succes && fail_count < fail_countMax){
            try{
                for (int i = tempMsgInd; i < messages.Count; i++){
                    string temp_echo = "error";
                    DataSend("ping", inVerifyOrVerifyNeedless: true);
                    DataSend(messages[i] + "=" + values[i].ToString(), true, inVerifyOrVerifyNeedless: true);
                    while (true){
                        temp_echo = sp.ReadLine();

                        // Debug.Log($"echo received: {temp_echo}");
                        if (temp_echo.StartsWith("echo:")){
                            temp_echo = temp_echo[5..temp_echo.IndexOf(":echo")];
                            break;
                        }
                        else if(temp_echo.Length > 3){
                            serial_read_content_ls.Add(new byte[] { 0xAA }.Concat(Encoding.UTF8.GetBytes(temp_echo)[1..(temp_echo.Length - 1)]).Concat(new byte[] { 0xDD }).ToArray());
                        }
                    }
                    string temp_aim = Arduino_var_list.FindIndex(str => str == messages[i]).ToString() + "=" + values[i].ToString();
                    if (temp_echo.Replace(" ", "") == temp_aim){
                        // Debug.Log("verified:" + temp_aim);
                        tempMsgInd = i + 1;

                        if (tempMsgInd == messages.Count){
                            succes = true;
                            manualResetEventVerify.Set();
                            return 1;
                        }
                        //ui_update.Message_update("verified:"+temp_aim+"\n");
                        fail_count++;
                        continue;
                    }
                    fail_count++;

                    // manualResetEventVerify.Set();
                    // return -1;
                }
            }
            catch (Exception e){
                Debug.Log(e.Message);
                if (e.Message.Contains("not open")){
                    manualResetEventVerify.Set();
                    return -2;
                }
                fail_count++;
                // return -1;
            }
            finally{
                if (serial_read_content_ls.Count() > 0){
                    byte[] totalMsgInVerify = commandConverter.Read_buffer_concat(serial_read_content_ls, 0, -1);
                    int temp_end = commandConverter.FindMarkOfMessage(false, totalMsgInVerify, 0);
                    while (temp_end != -1){
                        byte[] tempCompleteMsgInVerify = commandConverter.ProcessSerialPortBytes(totalMsgInVerify);
                        // Debug.Log("process: " + string.Join(",", tempCompleteMsgInVerify));
                        if (tempCompleteMsgInVerify.Length > 0){
                            commandQueue.Enqueue(tempCompleteMsgInVerify);
                        }
                        else{
                            serial_read_content_ls.Clear();
                        }
                        totalMsgInVerify = totalMsgInVerify[(temp_end + 1)..].ToArray();
                        temp_end = commandConverter.FindMarkOfMessage(false, totalMsgInVerify, 0);

                    }
                }
                // manualResetEventVerify.Set();
            }
        }
        manualResetEventVerify.Set();
        return -1;
    }

    /// <summary>
    /// messages: p_... or other ;return:         p_trial_set:0-end, 1-start, 2-serve water and end
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
            FileStream logfileStream = new FileStream(filePath + "_rec.txt", FileMode.Create, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
            logStreamWriter = new StreamWriter(logfileStream);
            FileStream posfileStream = new FileStream(filePath + "_pos.txt", FileMode.Create, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
            posStreamWriter = new StreamWriter(posfileStream);
            posWriteQueue.Enqueue(string.Join("\t", new string[]{"x", "y", "syncFrameInd", "100*pythonTime", "frameInd", "TimeInUnitySecFromTrialStart"}));
            // FileStream serialSyncStream = new FileStream(filePath + "_sync.txt", FileMode.Create, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
            // serialSyncStreamWriter = new StreamWriter(serialSyncStream);

            // serialSyncThread = new Thread(new ThreadStart(ProcessSerialSyncWriteQueue));
            // serialSyncThread.Start();
        }
        catch (Exception e){
            Debug.LogError($"Error initializing StreamWriter: {e.Message}");
        }
    }

    private void ProcessWriteQueue(bool writeAll = false){//txt文件写入，位于主进程
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

    // void ProcessSerialSyncWriteQueue()
    // {
    //     while(serialSync){
    //         while (syncWriteQueue.Count > 0 && serialSyncStreamWriter !=  null){
    //             syncWriteQueue.TryPeek(out float chunk);
    //             serialSyncStreamWriter.Write($"{chunk}\t");

    //             if (serialSyncStreamWriter.BaseStream.Position >=  serialSyncStreamWriter.BaseStream.Length - BUFFER_THRESHOLD){
    //                 serialSyncStreamWriter.Flush();
    //             }

    //             syncWriteQueue.TryDequeue(out _);
    //         }
    //     }

    //     if (serialSyncStreamWriter !=  null)
    //     {
    //         serialSyncStreamWriter.Close();
    //         serialSyncStreamWriter.Dispose();
    //         serialSyncStreamWriter = null;
    //     }
    // }

    private void CleanupStreamWriter(){
        if (logStreamWriter !=  null){
            logStreamWriter.Close();
            logStreamWriter.Dispose();
            logStreamWriter = null;
        }

        if (posStreamWriter !=  null){
            posStreamWriter.Close();
            posStreamWriter.Dispose();
            posStreamWriter = null;
        }
        // serialSync = false;
    }

    /// <summary>
    /// rectype: 0-lick, 1-start, 2-end, 3-init, 4-entrance, 5-press, 6-lickExpire, 7-trigger, 8-stay, 9-soundplay, 10-OGManuplate, 11-sync, 12-miniscopeRecord
    /// if enqueMsg is not empty, it will enqueue the message and not write in normal format.
    /// mouse leave lick spout marked by addInfo.
    /// </summary>
    /// <param name="returnTypeHead"></param>
    /// <param name="recType"></param>
    /// <param name="_lickPos"></param>
    /// <param name="enqueueMsg"></param>
    /// <returns></returns>
    public string WriteInfo(bool returnTypeHead = false, int recType = 0, int _lickPos = -1, string enqueueMsg = "", string addInfo = ""){
        if(! returnTypeHead && nowTrial == -1){return "";}
        if(enqueueMsg != ""){
            logWriteQueue.Enqueue(enqueueMsg);
            ProcessWriteQueue();
        }
        else if(!returnTypeHead){
            time_rec_for_log[1] = Time.realtimeSinceStartup;
            try{
                string recTypeStr = (recType == 0 && _lickPos < 0)? recTypeAddtion[Math.Abs(_lickPos) - 1]: recTypeLs[recType];
                if(recTypeStr.Equals("null")){return "";}

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
    void WriteInfo(long[] posInfo, string addInfo = ""){
        string tempPosText = string.Join("\t", posInfo.Select(v => v.ToString("")).ToList());
        if(addInfo != "" && addInfo != null){
            tempPosText += "\t"+addInfo;
        }
        posWriteQueue.Enqueue(tempPosText);
        ProcessWriteQueue();
        return;
    }

    public void WriteInfo(List<float> sceneInfoEtc){
        string tempPosText = string.Join("\t", sceneInfoEtc.Select(v => v.ToString("")).ToList());
        posWriteQueue.Enqueue(tempPosText);
        ProcessWriteQueue();
        return;
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

        time_rec_for_log[0] = Time.realtimeSinceStartup;
        commandConverter = new CommandConverter(lsTypes);
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

        barWidth = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "barWidth", "100"));
        barHeight = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "barHeight", "1080"));
        bool disableMainDisplay = iniReader.ReadIniContent(  "displaySettings", "disableMainDisplay", "false") == "true";
        displayPixelsLength = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "displayPixelsLength", "1920"));
        displayPixelsHeight = Convert.ToInt16(iniReader.ReadIniContent(  "displaySettings", "displayPixelsHeight", "1080"));
        displayVerticalPos  = Convert.ToSingle(iniReader.ReadIniContent(  "displaySettings", "displayVerticalPos", "0.5"));
        isRing = iniReader.ReadIniContent(  "displaySettings", "isRing", "false") == "true";
        bool separate = iniReader.ReadIniContent(  "displaySettings", "separate", "false") == "true";//仅支持分离为双屏1920
        displayVerticalPos = Math.Clamp(displayVerticalPos, -1, 2);
        if(!float.TryParse(iniReader.ReadIniContent("settings", "refSegement", "-1"), out float refSegementDeg)){refSegementDeg = -1;}
        if(refseg != null){
            if(refSegementDeg >= 0){
                if(refseg.name == "refSegement"){
                    refSegementMat = refseg.GetComponent<MeshRenderer>().material;
                    refseg.transform.localPosition = new Vector3(DegToPos(Math.Clamp(refSegementDeg, 0, 360)), refseg.transform.localPosition.y, refseg.transform.localPosition.z);
                }
            }else{
                if(refseg.name == "refSegement"){refseg.gameObject.SetActive(false);}
            }
        }

        Display mainScreen = Display.displays[0];
        mainScreen.Activate();
        mainScreen.SetRenderingResolution(1366, 768);
        // mainScreen.Activate(1366, 768, new RefreshRate(){numerator = 60, denominator = 1});
        Screen.fullScreen = false;

        if(InApp){
            if(!disableMainDisplay){
                Display monitorDisplay = null;
                if(separate){
                    try{
                    
                        if (Display.displays.Length > 3){//无监视屏
                            monitorDisplay = Display.displays[1];
                        }
                        for (int i = (monitorDisplay == null? 1: 2); i < Math.Min(4, Display.displays.Length); i++){
                            Display.displays[i].Activate();
                            Screen.fullScreen = false;
                            Display.displays[i].Activate(1920, displayPixelsHeight, new RefreshRate(){numerator = 60, denominator = 1});
                        }
                        
                        SecondCamera.enabled = true;
                        MainCamera.targetDisplay = 2;
                        MainCamera.GetComponent<Transform>().position = new Vector3(-96, 0, -10);
                        SecondCamera.targetDisplay = 3;
                        SecondCamera.GetComponent<Transform>().position = new Vector3(96, 0, -10);
                    }
                    catch(Exception e){
                        Debug.LogError(e.Message);
                    }
                }else{
                    try{
                        if (Display.displays.Length > 2){//无监视屏
                            monitorDisplay = Display.displays[1];
                        }
                        Display LEDRing = monitorDisplay == null ? Display.displays[1] : Display.displays[2];
                        Screen.fullScreen = false;
                        LEDRing.Activate(displayPixelsLength, displayPixelsHeight, new RefreshRate(){numerator = 60, denominator = 1});
                        SecondCamera.enabled = false;
                    }
                    catch (Exception e){
                        Debug.LogWarning("no third screen connected for moniter:" + e.Message);
                    }
                }
                
                if (monitorDisplay != null){
                    CameraMonitor.targetDisplay = 1;
                    monitorDisplay.Activate(1440, 1080, new RefreshRate() { numerator = 60, denominator = 1 });
                    Canvas tempChildTs = CameraMonitor.GetComponent<Transform>().GetChild(0).GetComponent<Canvas>();
                    tempChildTs.targetDisplay = 1;
                }
            }else{
                MainCamera.enabled = false;
                SecondCamera.enabled = false;
            }
        }
            else{
                if (!disableMainDisplay){
                    if (separate){
                        try{
                            SecondCamera.enabled = true;
                            MainCamera.targetDisplay = 2;
                            MainCamera.GetComponent<Transform>().position = new Vector3(-96, 0, -10);
                            SecondCamera.targetDisplay = 3;
                            SecondCamera.GetComponent<Transform>().position = new Vector3(96, 0, -10);
                        }
                        catch (Exception e){
                            Debug.LogError(e.Message);
                        }
                    }
                    else{
                        try{
                            SecondCamera.enabled = false;
                        }
                        catch (Exception e){
                            Debug.LogError(e.Message);
                        }
                    }
                }
                else{
                    MainCamera.enabled = false;
                    SecondCamera.enabled = false;
                }
                CameraMonitor.targetDisplay = 1;
                Canvas tempChildTs = CameraMonitor.GetComponent<Transform>().GetChild(0).GetComponent<Canvas>();
                tempChildTs.targetDisplay = 1;
            }

        string _strMode = iniReader.ReadIniContent(  "settings", "start_mode", "0x00");
        trialMode = Convert.ToInt16(_strMode[(_strMode.IndexOf("0x")+2)..], 16);
        try{
            contextInfo = new ContextInfo(
                iniReader.ReadIniContent(                   "settings", "start_method"      ,   "assign"                ),                 // string _start_method
                iniReader.ReadIniContent(                   "settings", "available_pos"     ,   "0, 90, 180, 270"  ),                 // string _available_pos_array
                iniReader.ReadIniContent(                   "settings", "assign_pos"        ,   "(0+1+2+3)*100.."  ),                 // string _assigned_pos
                iniReader.ReadIniContent(                   "settings", "MatStartMethod"    ,   "assign"                ),
                iniReader.ReadIniContent(                   "settings", "MatAvailable"      ,   "default"               ),               
                iniReader.ReadIniContent(                   "settings", "MatAssign"         ,   "default.."             ),              
                iniReader.ReadIniContent(                   "settings", "pump_pos"          ,   "0,1,2,3"               ),                 // string _pump_pos_array
                iniReader.ReadIniContent(                   "settings", "lick_pos"          ,   "0,1,2,3"               ),                 // string _lick_pos_array
                iniReader.ReadIniContent(                   "settings", "TrackPosMark"      ,   ""                      ),                 // string _lick_pos_array
                Convert.ToInt16(iniReader.ReadIniContent(   "settings", "max_trial"         ,   "10000"                 )),               // int _maxTrial
                Convert.ToInt16(iniReader.ReadIniContent(   "settings", "backgroundLight"   ,   "0"                     )),                // int _backgroundLight
                Convert.ToInt16(iniReader.ReadIniContent(   "settings", "backgroundLightRed",   "-1"                    )),                // int _backgroundLightRed
                Convert.ToSingle(iniReader.ReadIniContent(  "settings", "barDelayTime"      ,   "1"                     )),                // float _barLastTime
                iniReader.ReadIniContent(                   "settings", "waitFromStart"     ,  "random2~5"             ),                // float go cue
                Convert.ToSingle(iniReader.ReadIniContent(  "settings", "barLastingTime"    ,   "1"                     )),                // float 
                Convert.ToSingle(iniReader.ReadIniContent(  "settings", "waitFromLastLick"  ,   "3"                     )),                // float 
                iniReader.ReadIniContent(                   "settings", "triggerModeDelay"  ,   "0"                     ),                 // float triggerDelay        
                iniReader.ReadIniContent(                   "settings", "trialInterval"     ,   "random5~10"            ),                 // string
                iniReader.ReadIniContent(                   "settings", "success_wait_sec"  ,   "3"                     ),                // string _s_wait_sec
                iniReader.ReadIniContent(                   "settings", "fail_wait_sec"     ,   "6"                     ),                // string _f_wait_sec
                iniReader.ReadIniContent(                   "settings", "barShiftLs"        ,   "0"                     ),                // string _f_wait_sec
                Convert.ToSingle(iniReader.ReadIniContent(  "settings", "trialExpireTime"   ,   "9999"                  )),                // float trialExpireTime
                Convert.ToInt16(iniReader.ReadIniContent(   "settings", "triggerMode"       ,   "0"                     )),                // int triggerMode
                Convert.ToInt32(iniReader.ReadIniContent(   "settings", "seed"              ,   "-1"                     ))                 // int _seed
            );

            contextInfo.ContextInfoAdd(
                Convert.ToSingle(iniReader.ReadIniContent(  "soundSettings" , "soundLength"             ,   "0.2"                   )),                // float 
                Convert.ToSingle(iniReader.ReadIniContent(  "soundSettings" , "cueVolume"               ,   "0.5"                   )),                // float 
                Convert.ToInt16(iniReader.ReadIniContent(   "settings"      , "barOffset"               ,   "0"                     )),
                iniReader.ReadIniContent(                   "settings"      , "destAreaFollow"          ,   "true"                  ) == "true",
                Convert.ToSingle(iniReader.ReadIniContent(  "settings"      , "standingSecInTrigger"    ,   "0.5" )),
                Convert.ToSingle(iniReader.ReadIniContent(  "settings"      , "standingSecInTrialInDest",   "0.5" )),
                iniReader.ReadIniContent(                   "settings"      , "OGTriggerMethod"          ,   ""                  ),
                iniReader.ReadIniContent(                   "settings"      , "MSTriggerMethod"          ,   ""                  ),
                iniReader.ReadIniContent(                   "settings"      , "countAfterLeave"          ,   "true"              ) == "true"
            );

        }
        catch(Exception e){
            MessageBoxForUnity.Ensure("wrong arguments in config file: "+e.Message, "error");
            Debug.LogError(e.Message);
            Quit();
            return;
        }

        ExeLauncher exeLauncher= new ExeLauncher();
        if(iniReader.ReadIniContent("settings", "openLogEvent", "false") == "true"){
            string _path = exeLauncher.Start(iniReader.ReadIniContent("settings", "logEventPath", ""), "LogEvent");
            if(File.Exists(_path)){
                iniReader.WriteIniContent("settings", "logEventPath", _path);
            }
        }
        if(iniReader.ReadIniContent("settings", "openPythonScript", "false") == "true"){
            string _command = iniReader.ReadIniContent("settings", "PythonScriptCommand", "");
            List<string> options = exeLauncher.CommandParser(_command);
            exeLauncher.LaunchPython(
                options[0], options[1], options[2], options[3]
            );
            DeviceCloseOptionBeforeExits[2] = iniReader.ReadIniContent("settings", "closePythonScriptBeforeExit", "false") == "true";
        }

        lickPosLsCopy = contextInfo.lickPosLs;

        trialStartTriggerMode = contextInfo.trialTriggerMode;
        foreach(var _ in TrialSoundPlayModeExplain){
            audioPlayTimes.Add(audioPlayTimes.Count, new float[]{
                contextInfo.soundLength,
                -1,
                -1
            });
        }

        cueVolume = contextInfo.cueVolume;
        
        foreach(AudioClip _clip in Resources.LoadAll("Audios", typeof(AudioClip))){
            if(!audioClips.ContainsKey(_clip.name)){
                audioClips.Add(_clip.name, _clip);
            }
        }

        while(audioSources.Count < TrialSoundPlayModeExplain.Count){
            GameObject gameObject = Instantiate(audioSourceSketchObject);
            AudioSource _tempAudioSource = gameObject.GetComponent<AudioSource>();
            _tempAudioSource.volume = 1;
            _tempAudioSource.clip = audioClips.Values.First();
            audioSources.Add(audioSources.Count + 1, _tempAudioSource);
        }
                
        foreach(string _option in iniReader.ReadIniContent("soundSettings" , "TrialSoundPlayMode", "").Split(";")){
            List<string> _optionList = _option.Split(":").Where(x => x!= "").ToList();
            if(_optionList.Count>0){
                try{
                    int mode = Convert.ToInt16(_optionList[0]);
                    string _audioName =  _optionList.Count > 1 ? _optionList[1]: audioClips.Keys.First();
                    ChangeSoundPlayMode(mode, audioPlayModeNow.Contains(mode)? 2: 1, _audioName);
                }
                catch(Exception e){
                    Debug.LogError(e.Message);
                }
            }
        }

        ui_update.IFContentLoaded = iniReader.ReadIniContent("defaultOptionSettings" , "InputfieldContent", "");

        alarmPlayTimeInterval = contextInfo.soundLength > 0? Convert.ToSingle(iniReader.ReadIniContent("soundSettings", "alarmPlayTimeInterval",  "1.5")) : 0;

        foreach(Texture2D _background in Resources.LoadAll("Backgrounds")){
            Material material = new Material(materialMissing);
            material.SetTexture("_MainTex", _background);
            material.name = _background.name;
            Backgrounds.Add(material);
        }
        // #if UNITY_EDITOR
        // string tempPath = $"Assets/Resources/Backgrounds";
        // #else
        // string tempPath = Application.dataPath + $"/Resources/Backgrounds";
        // #endif
        if(InApp){
            string tempPath = Application.dataPath + "/Resources/Backgrounds";
            if(Directory.Exists(tempPath)){
                List<string> availableBackgroundPicExtensions = new List<string>{".jpg", ".png", ".jpeg"};
                foreach(FileInfo _backgroundFile in new DirectoryInfo(tempPath).GetFiles()){
                        if(availableBackgroundPicExtensions.Contains(_backgroundFile.Extension)){
                        Material material = new Material(materialMissing);
                        LoadMaterialFromPath(_backgroundFile.FullName, ref material);
                        material.name = _backgroundFile.Name.Split('.')[0];
                        Backgrounds.Add(material);
                    }
                }
            }
        }
        
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
        foreach(string matchVersion in iniReader.ReadIniContent("serialSettings", "compatibleVersion", "").Split(",")){
            if(matchVersion.Length > 0){
                compatibleVersion.Add(matchVersion);
            }
        }
        
        List<List<string>> tempIniReadContent = iniReader.GetReadContent();
        IniReadContent = "default:\t\t\t\tothers:\n";
        for(int i = 0; i < Math.Max(tempIniReadContent[0].Count, tempIniReadContent[1].Count); i++){
            if(i < tempIniReadContent[0].Count){
                IniReadContent += tempIniReadContent[0][i] + "\t\t\t";
            }else{IniReadContent += "\t\t\t\t\t\t\t";}

            if(i < tempIniReadContent[1].Count){
                IniReadContent += tempIniReadContent[1][i] + "\n";
            }else{IniReadContent += "\n";}
        }
            if(iniReader.ReadIniContent("settings", "checkConfigContent", "false") == "true"){
            if(MessageBoxForUnity.YesOrNo("Please check the following Configs:\n" + IniReadContent, "iniReader") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){}
            else{
                Quit();
                return;
            }
        }
        
        int[] availableSerialSpeed = new int[]{115200, 230400, 250000, 460800, 500000, 921600};
        if(!int.TryParse(iniReader.ReadIniContent("serialSettings", "serialSpeed", "115200"), out serialSpeed) || !availableSerialSpeed.Contains(serialSpeed)){
            serialSpeed = 115200;
        }

        List<string> portInfo = new List<string>();
        CreateSerialConnection(out sp, ref portInfo);

        if (sp != null){
            InitializeStreamWriter();
            string data_write = WriteInfo(returnTypeHead: true);
            logWriteQueue.Enqueue(data_write);

            serialThread = new Thread(new ThreadStart(SerialCommunicating));
            serialThread.Start();
            Debug.Log(" serial thread started");
        }else{
            Debug.LogWarning("No Connection to Arduino! ports' info as follow:\n" + string.Join("\n", portInfo));
            if (MessageBoxForUnity.YesOrNo($"No Connection to Arduino! ports' info as follow:\n" + string.Join("\n", portInfo) + "\nContinue without connection to Arduino?", "Serial Error") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                DebugWithoutArduino = true;
                InitializeStreamWriter();
                string data_write = WriteInfo(returnTypeHead: true);
                logWriteQueue.Enqueue(data_write);

            }
            else{
                Quit();
            }
        }

    }

    void Start(){
        ui_update.ControlsParsePublic("ModeSelect", trialMode, "passive");
        ui_update.ControlsParsePublic("TriggerModeSelect", trialStartTriggerMode, "passive");
        IsIPCInNeed();
        foreach(int mode in audioPlayModeNow){
            ui_update.ControlsParsePublic("sound", mode, $"passive;add;{audioSources[mode].name}");
        }
        if(trialStartTriggerMode == 0){ui_update.MessageUpdate($"interval: {contextInfo.trialInterval[0]} ~ {contextInfo.trialInterval[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
        else{                          ui_update.MessageUpdate($"interval: {contextInfo.trialTriggerDelay[0]} ~ {contextInfo.trialTriggerDelay[1]}, {trialStartTriggerModeLs[trialStartTriggerMode]}触发", UpdateFreq: -1);}
    }

    void Update(){
        // Debug.LogWarning(Display.displays[0].systemHeight);
        
    }

    void FixedUpdate(){
        // UnscaledfixedTime = Time.realtimeSinceStartup;
        ui_update.MessageUpdate(UpdateFreq: 1);
        List<string> tempFInishedLs = alarm.GetAlarmFinish();
        foreach (string alarmFinished in tempFInishedLs){
            switch(alarmFinished){
                case "StartTrialWaitSoundCue":{
                    StartTrial();
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
                case "miniscopeStart":{
                    if(float.TryParse(alarm.GetAlarmAddInfo("miniscopeStart"), out float _secInAlarm)){
                        if(_secInAlarm > 0){MSSet((int)_secInAlarm);}
                    }else{Debug.Log("wrong argument in time set");}
                    break;
                }
                case "miniscopeEnd":{
                    MSSet(0);
                    break;
                }
                case "ogStart":{
                    if(float.TryParse(alarm.GetAlarmAddInfo("miniscopeStart"), out float _secInAlarm)){
                        if(_secInAlarm > 0){OGSet((int)_secInAlarm);}
                    }else{Debug.Log("wrong argument in time set");}
                    break;
                }
                case "ogEnd":{
                    OGSet(0);
                    break;
                }
                case "ClosePythonScript":{
                    ipcclient.ClosePythonScript();
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
        while(buildinCommandQueue.Count()>0){
            buildinCommandQueue.TryDequeue(out string _command);
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

            if(trialStatus != -2 && !pos.SequenceEqual(new long[]{-1, -1, -1, -1, -1})){
                WriteInfo(pos, $"{Time.realtimeSinceStartup - time_rec_for_log[0]}");
                if(trialStartTriggerMode == 3 && (trialStatus == -1 || trialStatus == 0)){//trigger
                    pos[0..2].CopyTo(standingPos, 0);
                    bool InTriggerArea = false;
                    // foreach (int[] selectedArea in TriggerAreas){
                    List<int[]> areas = ipcclient.GetCurrentSelectArea();
                    if(areas.Count>0){
                        if(CheckInRegion(pos, areas[1])){
                            InTriggerArea = true;
                        }
                    }
                    // }
                    if(InTriggerArea){
                        standingSecNowInTrigger = standingSecNowInTrigger == -1? Time.fixedUnscaledDeltaTime: standingSecNowInTrigger + Time.fixedUnscaledDeltaTime;
                        float speedUpScale = GetSoundPitch(contextInfo.standingSecInTrigger - standingSecNowInTrigger, contextInfo.standingSecInTrigger);
                        PlaySound("InPos", addInfo:$"pitch:{speedUpScale}");
                        if(contextInfo.standingSecInTrigger > 0 && standingSecNowInTrigger >= contextInfo.standingSecInTrigger){
                            standingSecNowInTrigger = -1;
                            WriteInfo(recType:8);
                            CommandParsePublic($"{lsTypes[9]}:0", urgent:true);
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
                        if(TrialResultCheck(nowTrial) == -4 && ShiftedCertainAreaNowTrial[0] >= markCountPerType){
                            if(CheckInRegion(pos, ShiftedCertainAreaNowTrial)){
                                // PlaySound("InPos");
                                standingSecNowInDest = standingSecNowInDest == -1? Time.fixedUnscaledDeltaTime: standingSecNowInDest + Time.fixedUnscaledDeltaTime;
                                float speedUpScale = GetSoundPitch(contextInfo.standingSecInDest - standingSecNowInDest, contextInfo.standingSecInDest);
                                PlaySound("InPos", addInfo:$"pitch:{speedUpScale}");
                                if(contextInfo.standingSecInDest > 0 && standingSecNowInDest >= contextInfo.standingSecInDest){
                                    standingSecNowInDest = -1;
                                    pos[0..2].CopyTo(standingPos, 0);
                                    WriteInfo(recType:8);
                                    CommandParsePublic($"{lsTypes[9]}:1", urgent:true);
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
        }

        if(waiting){//延时模式下下一个trial开始相关计算
            if(!forceWaiting){
                //if(Time.fixedUnscaledTime - waitSecRec >= (trialResult[nowTrial] == 1? contextInfo.sWaitSec : contextInfo.fWaitSec)){

                if(waitSec != -1 && IntervalCheck() == 0){
                    if(trialStartTriggerMode == 0){
                        Debug.Log(Time.fixedUnscaledTime);
                        StartTrial();
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
                LickingCheck(-1);
            }

        }
    }

    public void PreExit(bool timing = true){
        if(DeviceCloseOptionBeforeExits[2]){
            if(timing){alarm.TrySetAlarm("ClosePythonScript", 0.1f, out _, 10);}
            else{ipcclient.ClosePythonScript();}
        }
    }

    public void Exit(){
        try{
            StopSerialThread = true;
            logList.Add(ui_update.MessageUpdate(returnAllMsg:true));
            foreach(string logs in logList){
                logWriteQueue.Enqueue(logs);
            }
            logWriteQueue.Enqueue(IniReadContent);
            logWriteQueue.Enqueue(JsonConvert.SerializeObject(contextInfo));
            logWriteQueue.Enqueue(JsonConvert.SerializeObject(audioPlayModeNow));
            logWriteQueue.Enqueue(JsonConvert.SerializeObject(audioSources.Select(
                                                                                kvp => new { kvp.Key, kvp.Value.clip.name })
                                                                                ));
            ProcessWriteQueue(true);
            CleanupStreamWriter();
            
            if(ipcclient.Activated){
                ipcclient.CloseSharedmm();
            }
            if(sp!= null){
                CloseDevices();
                if (serialThread != null && serialThread.IsAlive) { serialThread.Join(100); }
                sp.Close();
                sp = null;
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
        // Exit();从ui_update中已调用
        CleanupStreamWriter();
    }
}