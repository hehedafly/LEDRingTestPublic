using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SharedMMF;
using System.Linq;

public class IPCClient : MonoBehaviour
{
    // Start is called before the first frame update
    Sharedmm sharedmm;
    bool activited = true;      public bool Activated{get {return activited;}}
    int[] pos;//[l, t, r, b]
    int[] selectedArea;

    // void Quit(){
    //     #if UNITY_EDITOR
    //         UnityEditor.EditorApplication.isPlaying = false;
    //     #else
    //         Application.Quit();
    //     #endif
    // }

    public int[] GetPos(){
        return pos;
    }

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
                    if(msg.StartsWith("pos:") && msg.Split(";").Length == 4){
                        string tempmsg = msg.Split(":")[1];
                        List<int> temppos =  new List<int>((from num in tempmsg.Split(';') select int.Parse(num)).ToList());
                        temppos.CopyTo(pos);
                    }else if(msg.StartsWith("select:") && msg.Split(";").Length == 4){
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
