using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SharedMMF;
using System.Linq;
using System;
using System.Threading;

using MouseDraw;
using System.Drawing;
using Image = UnityEngine.UI.Image;

public class IPCClient : MonoBehaviour
{
    // Start is called before the first frame update
    MouseDrawer mouseDrawer;
    public Image image;
    public Shader mouseDrawerTrailShader;
    int[] imageSize;
    Sharedmm sharedmm;
    Moving moving;
    UIUpdate uiUpdate;
    bool activited = false;      
    public bool Activated{get {return activited;} set{activited = value;} }
    public bool Silent = true;
    // int frameInd = -1;

    /// <summary>
    /// centerx, centery, radius, initDir
    /// </summary>
    /// <typeparam name="float"></typeparam>
    /// <returns></returns>
    List<float> sceneInfo = new List<float>();

    /// <summary>
    /// [x,y,frameInd, pythonTime, rawVideoInd]
    /// </summary>
    /// <value></value>
    long[] pos = new long[]{-1, -1, -1, -1, -1};
    Dictionary<int, int[]> circledAreas = new Dictionary<int, int[]>();//key: 0~359, value:pos array
    List<int[]> currentArea = new List<int[]>();
    
    /// <summary>    selectPlace: list[int] = [-1, -1, -1, -1, -1, -1]#type: mark; type(check pos region), 0-rectange, 1-circle ; x/centerx ; y/centery ; w/rad ; h/inner
    /// 
    /// </summary> <summary>
    /// 
    /// </summary>
    /// <typeparam name="int[]"></typeparam>
    /// <returns></returns>
    List<int[]> selectedAreas = new List<int[]>();
    double pythonTimeOffset = 0;//差值不可能为0

    #region mouse pos and selectarea related

    /// <summary>
    /// [x,y,frameInd]
    /// </summary>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public long[] GetPos(){
        return pos;
    }

    /// <summary>
    /// #type: 0-rectange, 1-circle ; lu/centerx ; lb/centery ; ru/rad ; rb/- ; soft(0-255)
    /// </summary>
    /// <returns></returns>
    public List<int[]> GetselectedArea(){
        return selectedAreas;
    }

    /// <summary>
    /// centerx, centery, radius, initDir
    /// </summary>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public float[] GetSceneInfo(){
        return sceneInfo.ToArray();
    }

    int UpdateCircledAreas(){
        int[] tempPos = moving.GetAvaiableBarPos().ToArray();
        for(int i = 0; i < tempPos.Count(); i++){
            List<int[]> rightArea = selectedAreas.Where(arr => arr[0] % 32 == i).ToList();
            if(rightArea.Count() > 0){
                if(circledAreas.TryGetValue(tempPos[i], out _)){circledAreas.Remove(tempPos[i]);}
                circledAreas.Add(tempPos[i], rightArea[0]);
            }
        }
        return 1;
    }

    public int[] GetCircledShiftArea(int angle){
        if (circledAreas.ContainsKey(angle)){
            return circledAreas[angle];
        }else{
            return new int[]{};
        }
    }

    public int[] CreateShiftedSelectedCircle(int[] _selectedPos, int shiftAngle, int oAngle){
        if(_selectedPos.Count() == 0){return new int[]{};}
        if(_selectedPos[1] != 0){Debug.Log("wrong arg of selectedPos"); return new int[]{};}
        if(shiftAngle == 0){
            circledAreas.TryAdd(oAngle, _selectedPos);
        }
        int tempangle = (shiftAngle + oAngle) % 360;
        circledAreas.TryAdd(tempangle, GetShiftedSelectedCircle(_selectedPos, shiftAngle));
        return circledAreas[tempangle];
    }

    int[] GetShiftedSelectedCircle(int[] _selectedPos, float shiftAngle){//selectPlace: list[int] = [-1, -1, -1, -1, -1, -1]#type: mark; type(check pos region), 0-rectange, 1-circle ; lu/centerx ; lb/centery ; ru/rad ; rb/inner
        return GetShiftedSelectedCircle(_selectedPos, new int[]{(int)sceneInfo[0], (int)sceneInfo[1]}, (int)sceneInfo[2], shiftAngle);
    }

    int[] GetShiftedSelectedCircle(int[] _selectedPos, int[] center, float radius, float shiftAngle){//selectPlace: list[int] = [-1, -1, -1, -1, -1, -1]#type: mark; type(check pos region), 0-rectange, 1-circle ; lu/centerx ; lb/centery ; ru/rad ; rb/inner
        if(_selectedPos.Length != 6 || _selectedPos[1] != 0){Debug.Log("wrong arg of selectedPos"); return _selectedPos;}
        int[] selectedCircle = new int[_selectedPos.Length];
        _selectedPos.CopyTo(selectedCircle, 0);
        double tempangle = Math.Atan2(_selectedPos[2]-center[0], _selectedPos[3]-center[1]);
        selectedCircle[2] = center[0] + (int)(radius * Math.Sin(shiftAngle * Math.PI / 180 + tempangle));
        selectedCircle[3] = center[1] + (int)(radius * Math.Cos(shiftAngle * Math.PI / 180 + tempangle));
        return selectedCircle;
    }

    public Vector2Int[] GetCircledRotatedRectange(float angle){
        float radians = (float)(angle * Math.PI / 180);
        Vector2Int[] rectVertices = new Vector2Int[4];
        int _w = 12;
        int _h = 25;
        int offset = 10;
        Vector2Int rectCenter = new Vector2Int(
            (int)(sceneInfo[0] + (sceneInfo[2] + offset) * (float)Math.Cos(radians)),
            (int)(sceneInfo[1] + (sceneInfo[2] + offset) * (float)Math.Sin(radians))
        );
        for (int i = 0; i < 4; i++)
        {
            float x = (i < 2 ? -_w : _w);
            float y = ((i % 3 == 0) ? -_h : _h);
            rectVertices[i] = new Vector2Int(
                (int)(rectCenter[0] + x * (float)Math.Cos(radians) - y * (float)Math.Sin(radians)),
                (int)(rectCenter[1] + x * (float)Math.Sin(radians) + y * (float)Math.Cos(radians))
            );
        }
        return rectVertices;
    }

    public List<int[]> GetCurrentSelectArea(){
        return currentArea;
    }

    public int SetCurrentSelectArea(List<int[]> _selectedPoses, int shiftAngle, int oAngle){
        currentArea.Clear();
        foreach(int[] _selectedPos in _selectedPoses){
        currentArea.Add(CreateShiftedSelectedCircle(_selectedPos, shiftAngle, oAngle));
        }
        return 1;
    }

    #endregion

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

    #region mouseDrawer related

    public int MDInit(){
        if(mouseDrawer == null){return -1;}
        // if(mouseDrawerThread.IsAlive){
        //     threadRunning = false;
        // }
        mouseDrawer.Init();
        return 1;
    }
    public int MDDrawInit(List<int[]> areas){
        if(mouseDrawer == null){return -1;}
        
        mouseDrawer.DrawInitialShapes(areas);
        

        return 1;
    }
    
    public int MDUpdate(){
        if(mouseDrawer == null){return -1;}
        
        mouseDrawer.UpdateDisplayTexture();
        return 0;
    }

    public int MDUpdatePos(int[] pos){
        if(mouseDrawer == null){return -1;}
        mouseDrawer.UpdateTrail(new Vector2(pos[0], pos[1]));
        return 1;
    }

    public int MDDrawTemp(List<int[]> areas = null, List<Vector2Int[]> vectorAreas = null){
        if(mouseDrawer == null){return -1;}
        bool clear = areas == null;
        if(areas != null){
            mouseDrawer.DrawTemporaryLayer(areas);
        }
        if(vectorAreas != null){
            mouseDrawer.DrawTemporaryLayer(vectorAreas, clear:clear);
        }
        return 1;
    }

    public int MDClearTemp(){
        mouseDrawer.ClearTempLayer();
        return 1;
    }

    #endregion

    void Awake()
    {
        uiUpdate = GetComponent<UIUpdate>();
        moving = GetComponent<Moving>();
        imageSize = new int[2];
        imageSize[0] = (int)image.GetComponent<RectTransform>().rect.width;
        imageSize[1] = (int)image.GetComponent<RectTransform>().rect.height;

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
        if(activited){MDUpdate();}
        else{MDInit(); mouseDrawer = null;}

        // if(mouseDrawerThread != null && mouseDrawerThread.IsAlive){
        //     // mouseDrawer.UpdateTrail();
        // }
        

        if(!Silent && sharedmm == null){
            if(CreateSharedmm() < 0){
                Silent = true;
                sharedmm = null;
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
                                if(!posUpdated && msg.Split(";").Length == 5){
                                    string tempmsg = msg.Split(":")[1];
                                    List<long> temppos =  new List<long>((from num in tempmsg.Split(';') select long.Parse(num)).ToList());
                                    temppos.CopyTo(pos);
                                    posUpdated = true;
                                    MDUpdatePos(new int[]{(int)pos[0], (int)pos[1]});

                                }else if(!posUpdated){
                                    Debug.Log($"Incomplete Pos data: {msg}, posUpdated:{posUpdated}, length:{msg.Split(";").Length}");
                                }
                                break;
                            }
                            case "select":{
                                Debug.Log("from Python:" + msg);
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

                                    UpdateCircledAreas();
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
                        float waitTime = 0.01f;
                        float nowTime = Time.time;
                        while(sharedmm.WriteContent($"time:{Time.fixedUnscaledTime}") == -1){
                            if(Time.time - nowTime >= waitTime){
                                break;
                            }
                        };
                        List<string> timeMsgs = sharedmm.ReadMsg(0, "new");

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

                                    mouseDrawer = new MouseDrawer(image, mouseDrawerTrailShader, 0.002f, 30);
                                    MDDrawInit(new List<int[]>{new int[] {-1, 0, (int)sceneInfo[0], (int)sceneInfo[1], (int)sceneInfo[2], -1}});
                                    // threadRunning = true;
                                    // mouseDrawerThread = new Thread(mouseDrawerThreadFunc);
                                    // mouseDrawerThread.IsBackground = true;
                                    // mouseDrawerThread.Start();
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
