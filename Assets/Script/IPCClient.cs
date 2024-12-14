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
    bool activited = true;      public bool Activated{get {return activited;}}
    int frameInd = -1;
    int[] pos;//[x,y,frameInd]
    int[] selectedArea;

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
    public int[] GetselectedArea(){
        return selectedArea;
    }

    void Awake()
    {
        sharedmm = new Sharedmm("unity", "");
        try{
            sharedmm.Init("UnityShareMemoryTest", 32+5*16*1024);
        }
        catch(System.Exception e){
            sharedmm = null;
            Debug.Log(e.Message);
            activited = false;
            // Quit();
        }
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {   
        if(activited && sharedmm.CheckServerOnlineStatus()){
            string tempStr = $"From Unity-- Now Time:{Time.time}";
            if(sharedmm != null){
                sharedmm.WriteContent(tempStr, true);
                foreach(string msg in sharedmm.ReadMsg(0, "all")){
                    if(msg.StartsWith("pos:") && msg.Split(";").Length == 3){
                        string tempmsg = msg.Split(":")[1];
                        List<int> temppos =  new List<int>((from num in tempmsg.Split(';') select int.Parse(num)).ToList());
                        temppos.CopyTo(pos);
                    }else if(msg.StartsWith("select:") && msg.Split(";").Length == 5){
                        string tempmsg = msg.Split(":")[1];
                        List<int> tempArea =  new List<int>((from num in tempmsg.Split(';') select int.Parse(num)).ToList());
                        tempArea.CopyTo(selectedArea);
                    }
                }
            }
        }else{
            activited = false;
            pos = new int[0];
            // Quit();
        }
    }
}
