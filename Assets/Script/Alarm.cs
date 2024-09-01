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

    public void SetAlarm(int ind, int fixedFrames, string _name = ""){
        fixedFrames = Math.Max(fixedFrames, 1);
        alarm[ind] = fixedFrames;
        if(_name != "" && _name != alarmName[ind]){
            alarmName[ind] = _name;
            if(alarmNameIndDic.ContainsKey(_name)){
                alarmNameIndDic[_name] = ind;
            }else{
                alarmNameIndDic.Add(_name, ind);
            }
        }
    }

    public void SetAlarm(string _alarmName, int fixedFrames){//不确定时使用TrySetAlarm
        if(_alarmName != "" && alarmName.Contains(_alarmName)){
            SetAlarm(alarmNameIndDic[_alarmName], fixedFrames);
        }else{}
    }

    public bool TrySetAlarm(string _alarmName, float _sec, out int alarmInd){
        return TrySetAlarm(_alarmName, (int)(_sec / Time.fixedUnscaledDeltaTime), out alarmInd);
    }
    public bool TrySetAlarm(string _alarmName, int frames, out int alarmInd){
        if(!alarmName.Contains(_alarmName)){
            for(int i = 0; i < alarm.Count(); i++){
                if(alarmName[i] == ""){
                    alarmInd = i;
                    SetAlarm(i, frames, _alarmName);
                    return true;
                }
            }
            alarmInd = -1;
            return false;
        }else{
            SetAlarm(alarmNameIndDic[_alarmName], frames);
            alarmInd = alarmNameIndDic[_alarmName];
            return true;
        }
    }

    public int DeleteAlarm(string _alarmName, bool forceDelete = false){
        if(alarmName.Contains(_alarmName)){
            if(alarm[alarmName.IndexOf(_alarmName)] == -1){
                alarmNameIndDic.Remove(_alarmName);
                alarmName.Remove(_alarmName);
                return 1;
            }else{
                if(forceDelete){
                    alarmNameIndDic.Remove(_alarmName);
                    alarmName.Remove(_alarmName);
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
            }
        }
        return tempLs;
    }
    public void AlarmFixUpdate(){
        for (int i = 0; i < alarm.Length; i++){
            if (alarm[i] != -1){
                alarm[i]--;
            }
        }
    }
}
