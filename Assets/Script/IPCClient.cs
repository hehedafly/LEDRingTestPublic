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
    
    /// <summary>
    /// -1: not init, 0~max: inited and connected -2:inited max:0.5s*50fps = 25
    /// </summary> <summary>
    /// 
    /// </summary>
    int EnableInitAfterConnection = -1;
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
    List<int[]> currentArea = new List<int[]>();//0:destarea, 1:triggerarea
    
    /// <summary>    selectPlace: list[int] = [-1, -1, -1, -1, -1, -1]#type: mark; type(check pos region), 0-rectange, 1-circle ; x/centerx ; y/centery ; w/rad ; h/inner
    /// 
    /// </summary> <summary>
    /// 
    /// </summary>
    /// <typeparam name="int[]"></typeparam>
    /// <returns></returns>
    List<int[]> selectedAreas = new List<int[]>();
    int selectAreaCount = -1;

    /// <summary>
    /// 0-markType(0:dest, 1:trigger); 1-radius; 2-distance to center
    /// </summary>
    /// <typeparam name="float[]"></typeparam>
    /// <returns></returns>
    List<float[]> meanSelectArea = new List<float[]>();
    double pythonTimeOffset = 0;//差值不可能为0

    float lastTime = 0;

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

    int UpdateDestCircledAreas(){
        if(moving.GetContextInfoDestAreaFollow()){return 0;}
        
        int[] tempPos = moving.GetAvaiableBarPos().ToArray();
        for(int i = 0; i < tempPos.Count(); i++){
            List<int[]> rightArea = selectedAreas.Where(arr => arr[0] - 32 == i).ToList();
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

    public int[] CreateShiftedSelectedCircle(int[] _selectedPos, int shiftAngle){
        if(_selectedPos.Count() == 0){return new int[]{};}
        if(_selectedPos[1] != 0){Debug.Log("wrong arg of selectedPos"); return new int[]{};}
        // int oAngle = (int)sceneInfo[3];
        // if(shiftAngle == 0){
        //     circledAreas.TryAdd(oAngle, _selectedPos);
        // }
        int tempangle = shiftAngle % 360;
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
        selectedCircle[3] = center[1] - (int)(radius * Math.Cos(shiftAngle * Math.PI / 180 + tempangle));
        return selectedCircle;
    }

    /// <summary>
    /// angle: without sceneInfo added
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns> <summary>
    /// 
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    public Vector2Int[] GetCircledRotatedRectange(float angle){
        Debug.Log($"draw rotated rect at angle{angle} + {sceneInfo[3]}");
        angle += sceneInfo[3];

        float radians = (float)(angle * Math.PI / 180);
        Vector2Int[] rectVertices = new Vector2Int[4];
        int _w = 12;
        int _h = 25;
        int offset = 10;
        Vector2Int rectCenter = new Vector2Int(
            (int)(sceneInfo[0] + (sceneInfo[2] + offset) * (float)Math.Sin(radians)),
            (int)(sceneInfo[1] - (sceneInfo[2] + offset) * (float)Math.Cos(radians))
        );
 
        float cos = Mathf.Cos(radians + 90 * Mathf.Deg2Rad);
        float sin = Mathf.Sin(radians + 90 * Mathf.Deg2Rad);

        for (int i = 0; i < 4; i++)
        {

            float x = i < 2 ? _w : -_w;
            float y = (i % 3 == 0) ? _h : -_h;
            
            // 应用旋转矩阵（绕原点逆时针旋转）
            rectVertices[i] = new Vector2Int(
            (int)(rectCenter.x + x * cos - y * sin),
            (int)(rectCenter.y + x * sin + y * cos)
            );
            
        }
        return rectVertices;
    }

    /// <summary>
    /// currentArea[0]:current dest area, [1]:trigger area
    /// </summary>
    /// <returns></returns>
    public List<int[]> GetCurrentSelectArea(){
        return currentArea;
    }

    public int SetCurrentDestArea(int mark){
        if(currentArea.Count == 0){currentArea.Add(new int[]{-1, -1, -1, -1, -1, -1});}

        if(selectedAreas.Any(area => area[0] == mark)){
            currentArea[0] = selectedAreas.Find(area => area[0] == mark);
        }
        return 0;
    }

    public int SetCurrentDestArea(int shiftAngle, bool circle, List<int[]> _selectedPoses = null){
        if(!circle){return -1;}
        if(currentArea.Count == 0){currentArea.Add(new int[]{-1, -1, -1, -1, -1, -1});}
        if(circledAreas.ContainsKey(shiftAngle)){
            currentArea[0] = circledAreas[shiftAngle];
            return 1;
        }
        
        if(_selectedPoses == null){
            int tempx = (int)(sceneInfo[0] + (int)(meanSelectArea[0][2] * Math.Sin((shiftAngle + sceneInfo[3]) * Math.PI / 180)));
            int tempy = (int)(sceneInfo[1] - (int)(meanSelectArea[0][2] * Math.Cos((shiftAngle + sceneInfo[3]) * Math.PI / 180)));
            int[] tempArea = new int[]{64, 0, tempx, tempy, (int)meanSelectArea[0][1], -1};
            circledAreas.TryAdd(shiftAngle, tempArea);
            currentArea[0] = tempArea;
        }else{
            foreach(int[] _selectedPos in _selectedPoses){
                currentArea[0] = CreateShiftedSelectedCircle(_selectedPos, shiftAngle);
            }
        }
        return 1;
    }

    public int SetCurrentTriggerArea(int mark){
        while(currentArea.Count < 2){currentArea.Add(new int[]{-1, -1, -1, -1, -1, -1});}
    
        if(selectedAreas.Any(area => area[0] == mark)){
            currentArea[1] = selectedAreas.Find(area => area[0] == mark);
        }
        return 0;
    }

    public int SetCurrentTriggerArea(int shiftAngle, bool circle, List<int[]> _selectedPoses = null){
        if(!circle){return -1;}
        while(currentArea.Count < 2){currentArea.Add(new int[]{-1, -1, -1, -1, -1, -1});}

        
        if(_selectedPoses == null){
            int[] tempArea = meanSelectArea.Find(area => area[0] == 0).Select(x => (int)x).ToArray();
            currentArea[1] = CreateShiftedSelectedCircle(tempArea, shiftAngle);
        }else{
            foreach(int[] _selectedPos in _selectedPoses){
                currentArea[1] = CreateShiftedSelectedCircle(_selectedPos, shiftAngle);
            }
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

    public int MDDrawInit(List<int[]> areas = null, float[] sceneInfo = null){
        if(mouseDrawer == null){return -1;}

        mouseDrawer.DrawInitialShapes(areas, sceneInfo);
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

    void InitAfterConnection(){
        mouseDrawer = new MouseDrawer(image, mouseDrawerTrailShader, 0.002f, 30);
        MDDrawInit(sceneInfo:sceneInfo.ToArray());
        
        if(selectedAreas.Count() != selectAreaCount){
            MessageBoxForUnity.Ensure($"only {selectedAreas.Count()} selectarea received, missed {selectAreaCount - selectedAreas.Count()}, sync failed", "sync error");
            Silent = true;
            activited = false;
        }else{
            List<int[]>DestinationAreas = GetselectedArea().Where(area => area[0] / 32 == 1 && area[1] == 0).ToList();
            int tempradius = 0;
            double tempdisttocenter = 0;
            foreach(int[] area in DestinationAreas){
                if(tempradius == 0 || area[4] == tempradius){tempradius = area[4];}
                else{tempradius = -1;}

                double tempDistance = Math.Sqrt(Math.Pow(area[2] - sceneInfo[0], 2) + Math.Pow(area[3] - sceneInfo[1], 2));
                if(tempdisttocenter == 0 || Math.Abs(1 - tempdisttocenter/tempDistance) <= 0.1){tempdisttocenter = tempDistance;}
                else{tempdisttocenter = -1;}
            }

            if(tempradius != -1 && tempdisttocenter != -1){
                meanSelectArea.Clear();
                meanSelectArea.Add(new float[]{0, tempradius, (float)tempdisttocenter});
            }else{
                meanSelectArea.Add(new float[]{0, 60, sceneInfo[2]});
            }

            List<int[]>TriggerAreas = GetselectedArea().Where(area => area[0] / 32 == 0 && area[1] == 0).ToList();
            tempradius = 0;
            tempdisttocenter = 0;
            foreach(int[] area in TriggerAreas){
                if(tempradius == 0 || area[4] == tempradius){tempradius = area[4];}
                else{tempradius = -1;}

                double tempDistance = Math.Sqrt(Math.Pow(area[2] - sceneInfo[0], 2) + Math.Pow(area[2] - sceneInfo[0], 2));
                if(tempdisttocenter == 0 || Math.Abs(1 - tempdisttocenter/tempDistance) <= 0.1){tempdisttocenter = tempDistance;}
                else{tempdisttocenter = -1;}
            }
            if(tempradius != -1 && tempdisttocenter != -1){
                meanSelectArea.Add(new float[]{1, tempradius, (float)tempdisttocenter});
            }

            foreach(int[] area in selectedAreas){
                List<float> areaList = area.Select(v => (float)v).ToList();
                moving.WriteInfo(areaList);
            }

        }
    }
  
    public void CloseSharedmm(){
        if(sharedmm != null) {sharedmm.CloseSharedmm(manually:true);}
        activited = false;
        Silent = true;
    }

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
  
    void Update(){
        if(activited && Time.unscaledDeltaTime - lastTime >= 1){
            lastTime = Time.unscaledDeltaTime;
            int res = sharedmm.UpdateOnlineStatus();
            if(res < 0){
                CloseSharedmm();
                if(res == -1){uiUpdate.MessageUpdate($"lost connection");}
                else if(res == -2){uiUpdate.MessageUpdate($"server shutdown");}
                else if(res == -3){uiUpdate.MessageUpdate($"offline by server, check heartbeat setting");}
                
            };
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {   
        if(activited){MDUpdate();}
        else{MDInit(); mouseDrawer = null;EnableInitAfterConnection = -1;}

        if(EnableInitAfterConnection >= 25){
            InitAfterConnection();
            EnableInitAfterConnection = -2;
        }else if(EnableInitAfterConnection >= 0){
            EnableInitAfterConnection ++;
        }
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

        if(!Silent && sharedmm != null && sharedmm.CheckServerOnlineStatus()){//if set slient to true, "else" part will close sharedmm

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

                                    UpdateDestCircledAreas();
                                }
                                break;
                            }
                            default:break;
                        }
                    }
                }
            }else{
                if(sharedmm.WriteContent($"time:{Time.realtimeSinceStartup}") != -1){//被读了
                    int failTimes = 0;
                    while(!activited && failTimes < 99){
                        float waitTime = 0.01f;
                        float nowTime = Time.time;
                        while(sharedmm.WriteContent($"time:{Time.realtimeSinceStartup}") == -1){
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
                                    pythonTimeOffset = Time.realtimeSinceStartup - time;
                                    timeUpdated = true;
                                }
                                // break;
                            }else if(msg.StartsWith("scene:")){
                            // Debug.Log("msg from pyhon: "+ msg);
                                if(pythonTimeOffset != 0){
                                    sceneInfo = msg[6..].Split(";").ToList().Select(v => Convert.ToSingle(v)).ToList();
                                    selectAreaCount = (int)sceneInfo.Last();    sceneInfo.RemoveAt(sceneInfo.Count - 1);
                                    activited = true;
                                    moving.WriteInfo(sceneInfo);
                                    uiUpdate.MessageUpdate($"time synchronized: offset: {pythonTimeOffset}");
                                    uiUpdate.MessageUpdate("scene info:" + string.Join(";", sceneInfo));

                                    EnableInitAfterConnection = 0;
                                    break;
                                }
                            }
                        }
                        failTimes ++;
                    }
                    if(failTimes >= 99){
                        uiUpdate.MessageUpdate($"failed to sync");
                        CloseSharedmm();
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
                Array.Fill(pos, -1);
                sharedmm.CloseSharedmm(manually:true);
                sharedmm = null;
            }
        }
    }
}
