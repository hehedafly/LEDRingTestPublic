using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class Alarm
{
    // Start is called before the first frame update
    long[] alarm = new long[20];
    long[] alarmRec = new long[20];
    int[] alarmExecuteTimes = new int[20];
    Dictionary<string, int> alarmNameIndDic = new Dictionary<string, int>();
    List<string> alarmName = new List<string>();
    public Alarm(int initValue = -1, int size = 20){
        if(alarm.Count() != size){
            alarm = new long[size];
        }
        Array.Fill(alarm, initValue);
        for (int i = 0; i < alarm.Length; i++){
            alarmName.Add("");
        }
    }

    public void SetAlarm(int ind, long fixedFrames, string _name = "", int executeCount = 0){
        if(ind < 0 || ind >= 20){return;}

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

    public void SetAlarm(string _alarmName, long fixedFrames, int executeCount = 0){//不确定时使用TrySetAlarm
        if(_alarmName != "" && alarmName.Contains(_alarmName)){
            SetAlarm(alarmNameIndDic[_alarmName], fixedFrames, executeCount:executeCount);
        }else{}
    }

    public bool TrySetAlarm(string _alarmName, long frames, out int alarmInd, int executeCount = 0){
        if(!alarmName.Contains(_alarmName)){
            for(int i = 0; i < alarm.Count(); i++){
                if(alarmName[i] == ""){
                    alarmInd = i;
                    SetAlarm(i, frames, _alarmName, executeCount);
                    return true;
                }
            }
            alarmInd = -1;
            return false;
        }else{
            SetAlarm(alarmNameIndDic[_alarmName], frames, executeCount:executeCount);
            alarmInd = alarmNameIndDic[_alarmName];
            return true;
        }
    }

    public bool TrySetAlarm(string _alarmName, float _sec, out int alarmInd, int executeCount = 0){
        return TrySetAlarm(_alarmName, (int)(_sec / Time.fixedUnscaledDeltaTime), out alarmInd, executeCount);
    }

/// <summary>
/// return 0:未删除成功，正在进行
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
                alarmName.Remove(_alarmName);
                alarmRec[ind] = -1;
                alarmExecuteTimes[ind] = 0;
                return 1;
            }else{
                if(forceDelete){
                    alarmNameIndDic.Remove(_alarmName);
                    alarmName.Remove(_alarmName);
                    alarmRec[ind] = -1;
                    alarmExecuteTimes[ind] = 0;
                    return 2;
                }else{
                    return 0;
                }
            }
        }else{
            return -1;
        }
    }

    public long GetAlarm(int ind){
        if(ind < 0 || ind >= alarm.Count()){
            return -2;
        }
        return alarm[ind];
    }

    public long GetAlarm(string indName){
        if(alarmNameIndDic.TryGetValue(indName, out int ind)){
            return alarm[ind];
        }
        else{
            return -2;
        }
        //return -1;
    }

    public List<string> GetAlarmFinish(){
        List<string> tempLs = new List<string>();
        for(int i = 0; i < alarm.Count();i++){
            if(alarm[i] == 0){
                alarm[i] = -1;
                tempLs.Add(alarmName[i]);
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
            if (alarm[i] != -1){
                alarm[i]--;
                if(alarm[i] == -1){
                //同样的内容发生在 GetAlarmFinish 中，基本不会再这里发生
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
