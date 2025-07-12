using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class Alarm
{
    // Start is called before the first frame update
    long[] alarm = new long[40];
    long[] alarmRec = new long[40];
    int[] alarmPause = new int[40];
    int[] alarmStartAfter = new int[40];
    int[] alarmExecuteTimes = new int[40];
    Dictionary<string, int> alarmNameIndDic = new Dictionary<string, int>();
    List<string> alarmName = new List<string>();
    string[] alarmAddInfo = new string[40];
    public Alarm(int initValue = -1, int size = 20){
        if(alarm.Count() != size){
            alarm = new long[size];
        }
        Array.Fill(alarm, initValue);
        Array.Fill(alarmStartAfter, -1);
        for (int i = 0; i < alarm.Length; i++){
            alarmName.Add("");
        }
    }

    void SetAlarm(int ind, long fixedFrames, string _name = "", int executeCount = 0, bool force = false){//提供ind分配对应alarm，由TrySetAlarm调用
        if(ind < 0 || ind >= 20){return;}
        if(alarmPause[ind] == 1 && !force){return;}

        fixedFrames = Math.Max(fixedFrames, 1);
        alarm[ind] = fixedFrames;
        alarmRec[ind] = fixedFrames;
        alarmExecuteTimes[ind] = alarmExecuteTimes[ind] == 0? executeCount: alarmExecuteTimes[ind];
        if(_name != "" && _name != alarmName[ind]){
            alarmName[ind] = _name;
            if(alarmNameIndDic.ContainsKey(_name)){
                alarmNameIndDic[_name] = ind;
            }else{
                alarmNameIndDic.Add(_name, ind);
            }
        }
    }

    void SetAlarm(string _alarmName, long fixedFrames, int executeCount = 0){//使用TrySetAlarm
        if(_alarmName != "" && alarmName.Contains(_alarmName)){
            SetAlarm(alarmNameIndDic[_alarmName], fixedFrames, executeCount:executeCount);
        }else{}
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="_alarmName"></param>
    /// <param name="frames"></param>
    /// <param name="alarmInd"></param>
    /// <param name="executeCount"></param>
    /// <param name="addInfo"></param> -1 in default
    /// <returns></returns>
    public bool TrySetAlarm(string _alarmName, long frames, out int alarmInd, int executeCount = 0, string addInfo = "", bool force = false){
        // Debug.Log($"{_alarmName} set for {frames} frames");
        if(!alarmName.Contains(_alarmName)){
            for(int i = 0; i < alarm.Count(); i++){
                if(alarmName[i] == ""){
                    alarmInd = i;
                    SetAlarm(i, frames, _alarmName, executeCount);
                    alarmAddInfo[i] = addInfo;
                    return true;
                }
            }
            alarmInd = -1;
            return false;
        }else{
            SetAlarm(alarmNameIndDic[_alarmName], frames, executeCount:executeCount, force:force);
            alarmAddInfo[alarmNameIndDic[_alarmName]] = addInfo;
            alarmInd = alarmNameIndDic[_alarmName];
            return true;
        }
    }

    public bool TrySetAlarm(string _alarmName, float _sec, out int alarmInd, int executeCount = 0, string addInfo = "", bool force = false){
        return TrySetAlarm(_alarmName, (int)(_sec / Time.fixedUnscaledDeltaTime), out alarmInd, executeCount, addInfo, force);
    }

    public int PauseAlarm(int ind){
        if(ind < 0 || ind >= 20){return 0;}

        alarmPause[ind] = 1;
        return 1;
    }
    public int PauseAlarm(string _alarmName){
        if(alarmNameIndDic.TryGetValue(_alarmName, out int ind)){
            return PauseAlarm(ind);
        }else{return -1;}
    }

    public int StartAlarm(int ind){
        if(ind < 0 || ind >= 20){return 0;}

        alarmPause[ind] = 0;
        return 1;
    }

     public int StartAlarm(string _alarmName){
        if(alarmNameIndDic.TryGetValue(_alarmName, out int ind)){
            return StartAlarm(ind);
        }else{return -1;}
    }

    /// <summary>
    /// subsequent:当前需要等待 nextTo 触发后再立即触发的alarm
    /// </summary>
    public int StartAlarmAfter(int subsequent, int nextTo){//一个alarm只能跟在另一个后触发，但多个alarm都可以跟在同一个alarm后触发
        if(subsequent < 0 || subsequent >= 20 || nextTo < 0 || nextTo >= 20){return 0;}

        PauseAlarm(subsequent);
        alarmStartAfter[subsequent] = nextTo;
        return 1;
    }

    /// <summary>
    /// subsequent:当前需要等待 nextTo 触发后再立即触发的alarm
    /// </summary>
    public int StartAlarmAfter(string _alarmName, string _alarmNameAfter){
        if(alarmNameIndDic.TryGetValue(_alarmName, out int _ind) && alarmNameIndDic.TryGetValue(_alarmNameAfter, out int _indAfter)){
            return StartAlarmAfter(_ind, _indAfter);
        }else{
            return -1;
        }
    }

/// <summary>
/// return 0:未删除成功，正在进行, 1:正常删除， 2：强制删除
/// </summary>
/// <param name="_alarmName"></param>
/// <param name="forceDelete"></param>
/// <returns></returns> <summary>
/// 
/// </summary>
/// <param name="_alarmName"></param>
/// <param name="forceDelete"></param>
/// <returns></returns>
    public int DeleteAlarm(string _alarmName, bool forceDelete = false){
        if(alarmName.Contains(_alarmName)){
            int ind = alarmName.IndexOf(_alarmName);
            if(alarm[ind] == -1){
                alarmNameIndDic.Remove(_alarmName);
                alarmName[ind] = "";
                alarmRec[ind] = -1;
                alarmExecuteTimes[ind] = 0;
                alarmStartAfter[ind] = -1;
                return 1;
            }else{
                if(forceDelete){
                    alarmNameIndDic.Remove(_alarmName);
                    alarmName[ind] = "";
                    alarmRec[ind] = -1;
                    alarmExecuteTimes[ind] = 0;
                    alarmStartAfter[ind] = -1;
                    return 2;
                }else{
                    return 0;
                }
            }
        }else{
            return -1;
        }
    }

    long GetAlarm(int ind){
        if(ind < 0 || ind >= alarm.Count()){
            return -2;
        }
        return alarm[ind];
    }

    /// <summary>
    /// return -2 if invalid name
    /// </summary>
    /// <param name="indName"></param>
    /// <returns></returns>
    public long GetAlarm(string indName){
        if(alarmNameIndDic.TryGetValue(indName, out int ind)){
            return alarm[ind];
        }
        else{
            return -2;
        }
        //return -1;
    }

    public string GetAlarmAddInfo(int ind){
        if(ind < 0 || ind >= alarm.Count()){
            return "//invalid ind";
        }
        return alarmAddInfo[ind];
    }

    public string GetAlarmAddInfo(string indName){
        if(alarmNameIndDic.TryGetValue(indName, out int ind)){
            return alarmAddInfo[ind];
        }
        else{
            return "//invalid name";
        }
    }

    public List<string> GetAlarmFinish(){
        List<string> tempLs = new List<string>();
        for(int i = 0; i < alarm.Count();i++){
            if(alarm[i] == 0){
                alarm[i] = -1;
                tempLs.Add(alarmName[i]);
                while(alarmStartAfter.Contains(i)){
                    int tempInd = Array.IndexOf(alarmStartAfter, i);
                    StartAlarm(tempInd);
                    alarmStartAfter[tempInd] = -1;
                }
                if(alarmExecuteTimes[i] > 0 && alarmRec[i] > 0){
                    //Debug.Log(alarmExecuteTimes[i]);
                    SetAlarm(i, alarmRec[i]);
                    alarmExecuteTimes[i] -= 1;
                }else{
                    alarmRec[i] = -1;
                }
            }
        }
        return tempLs;
    }
    public void AlarmFixUpdate(){
        for (int i = 0; i < alarm.Length; i++){
            if (alarm[i] != -1 && alarmPause[i] == 0){
                alarm[i]--;
                if(alarm[i] == -1){
                    //同样的内容发生在 GetAlarmFinish 中，基本不会在这里发生
                    while(alarmStartAfter.Contains(i)){
                        int tempInd = Array.IndexOf(alarmStartAfter, i);
                        StartAlarm(tempInd);
                        alarmStartAfter[tempInd] = -1;
                    }
                    if(alarmExecuteTimes[i] > 0){
                        SetAlarm(i, alarmRec[i]);
                        alarmExecuteTimes[i] -= 1;
                    }else{
                        alarmRec[i] = -1;
                    }
                }
            }
        }
    }
}
