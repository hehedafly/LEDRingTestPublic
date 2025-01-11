using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedMMF;
using System.Linq;
using System;

public class IPCClient : MonoBehaviour
{
    // Start is called before the first frame update
    Sharedmm sharedmm;
    Moving moving;
    UIUpdate uiUpdate;
    bool activited = false;      
    public bool Activated{get {return activited;} set{activited = value;} }
    public bool Silent = true;
    // int frameInd = -1;
    List<float> sceneInfo = new List<float>();//centerx, centery, radius, initDir
    int[] pos = new int[]{-1, -1, -1};//[x,y,frameInd]
    List<int[]> selectedAreas = new List<int[]>();
    double pythonTimeOffset = 0;//差值不可能为0

    // void Quit(){
    //     #if UNITY_EDITOR
    //         UnityEditor.EditorApplication.isPlaying = false;
    //     #else
    //         Application.Quit();
    //     #endif
    // }

    /// <summary>
    /// [x,y,frameInd]
    /// </summary>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public int[] GetPos(){
        return pos;
    }

    /// <summary>
    /// #type: 0-rectange, 1-circle ; lu/centerx ; lb/centery ; ru/rad ; rb/- ; soft(0-255)
    /// </summary>
    /// <returns></returns>
    public List<int[]> GetselectedArea(){
        return selectedAreas;
    }

    int CreateSharedmm(){
        sharedmm = new Sharedmm("UnityProject", "server");
        try{
            sharedmm.Init("UnityShareMemoryTest", 32+5*16*1024);
        }
        catch(System.Exception e){
            sharedmm = null;
            Debug.Log(e.Message);
            activited = false;
            Silent = true;
            // Quit();
            return -1;
        }
        return 1;
    }

    void Awake()
    {
        uiUpdate = GetComponent<UIUpdate>();
        moving = GetComponent<Moving>();
    }

    void Start()
    {
        
    }
    
    public void CloseSharedmm(){
        if(sharedmm != null) {sharedmm.CloseSharedmm(manually:true);}
        activited = false;
        Silent = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {   
        if(!Silent && sharedmm == null){
            if(CreateSharedmm() < 0){
                Silent = true;
                Debug.LogWarning("failed to create sharedmm");
            }
        }

        if(!Silent && sharedmm != null && sharedmm.CheckServerOnlineStatus()){

            if(activited){
                // string tempStr = $"From Unity-- Now Time:{Time.time}";
                if(sharedmm != null){
                    // sharedmm.WriteContent(tempStr, true);
                    List<string> readMsgs = sharedmm.ReadMsg(0, "new");
                    readMsgs.Reverse();
                    bool posUpdated = false;
                    foreach(string msg in readMsgs){
                        string msgHead = msg.Split(':')[0];
                        switch(msgHead){
                            case "pos":{
                                if(!posUpdated && msg.Split(";").Length == 3){
                                    string tempmsg = msg.Split(":")[1];
                                    List<int> temppos =  new List<int>((from num in tempmsg.Split(';') select int.Parse(num)).ToList());
                                    temppos.CopyTo(pos);
                                    posUpdated = true;
                                }
                                break;
                            }
                            case "select":{
                                // Debug.Log("from Python:" + msg);
                                if(msg.Split(";").Length == 6){
                                    int[] tempselectedArea = new int[]{-1, -1, -1, -1, -1, -1};
                                    string tempmsg = msg.Split(":")[1];
                                    List<int> tempArea =  new List<int>((from num in tempmsg.Split(';') select int.Parse(num)).ToList());
                                    tempArea.CopyTo(tempselectedArea);
                                    List<int[]> tempCopy = selectedAreas.Select(area => area).ToList();
                                    bool existing = false;
                                    foreach(int[] selectArea in tempCopy){
                                        if(tempselectedArea[0] < 0){
                                            if(Math.Abs(tempselectedArea[0] + 1) == selectArea[0] && selectArea[1..].Equals(tempselectedArea[1..])){
                                                selectedAreas.Remove(selectArea);
                                                break;
                                            }
                                            Debug.Log("no match selection");
                                        }
                                        else{
                                            if(tempselectedArea[0] == selectArea[0]){
                                                if(!selectArea[1..].SequenceEqual(tempselectedArea[1..])){
                                                    selectedAreas.Remove(selectArea);
                                                    selectedAreas.Add(tempselectedArea);
                                                    Debug.Log("added changed selection");
                                                }else{
                                                    existing = true;
                                                }
                                            }
                                        }
                                    }
                                    if (!existing){          
                                        selectedAreas.Add(tempselectedArea);
                                    }
                                }
                                break;
                            }
                            default:break;
                        }
                    }
                }
            }else{
                if(sharedmm.WriteContent($"time:{Time.fixedUnscaledTime}") != -1){//被读了
                    int failTimes = 0;
                    while(!activited && failTimes < 99){
                        sharedmm.WriteContent($"time:{Time.fixedUnscaledTime}");
                        List<string> timeMsgs = sharedmm.ReadMsg(0, "all");

                        timeMsgs.Reverse();
                        bool timeUpdated = false;
                        foreach(string msg in timeMsgs){
                            if(msg.StartsWith("time:") && !timeUpdated){
                                if(double.TryParse(msg[5..], out double time)){
                                    pythonTimeOffset = Time.fixedUnscaledTime - time;
                                    timeUpdated = true;
                                }
                                // break;
                            }else if(msg.StartsWith("scene:")){
                            // Debug.Log("msg from pyhon: "+ msg);
                                if(pythonTimeOffset != 0){
                                    sceneInfo = msg[6..].Split(";").ToList().Select(v => Convert.ToSingle(v)).ToList();
                                    activited = true;
                                    moving.WriteInfo(sceneInfo);
                                    uiUpdate.MessageUpdate($"time synchronized: offset: {pythonTimeOffset}");
                                    uiUpdate.MessageUpdate("scene info:" + string.Join(";", sceneInfo));
                                    break;
                                }
                            }
                        }
                        failTimes ++;
                    }
                    if(failTimes >= 99){
                        uiUpdate.MessageUpdate($"failed to sync");
                        // Debug.Log("failed to sync");
                        if(sharedmm != null) {sharedmm.CloseSharedmm(manually:true);}
                        sharedmm = null;
                        Silent = true;
                    }
                }else{
                    Activated = false;
                    // Debug.Log("no reading");
                    Array.Fill(pos, -1);
                }
                // Quit();
            }
        }else{
            if(activited){
                Activated = false;
                Array.Fill(pos, -1);
                uiUpdate.MessageUpdate($"lost connection");

            }
            // Debug.Log("no one online or not activated");
            if(sharedmm != null){
                sharedmm.CloseSharedmm(manually:true);
                sharedmm = null;
            }
        }
    }
}
