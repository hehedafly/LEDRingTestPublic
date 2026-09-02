using System;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;
using System.Text;
using System.Linq;
//using UnityEditor.Experimental.GraphView;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.UIElements;
//using UnityEditor.PackageManager;

public class LinearTrackMoving : MonoBehaviour
{

    [Range(0.0f, 2.0f)]
    public float maxSpeed=0.5f;
    public int maxRolling_value=100;

    [Range(0.0f, 1.0f)]
    public float turnSpeed;

    [Range(0.01f, 5.0f)]
    public float scaleFactor;//defalut: 1
    public float scaleFactorRolling;//defalut: 1
    public int sensor_smooth_interval = 5;//fixupdate rate(0.01s)*5
    public float sensor_pause_interval = 0.2f;
    //--------------------------------------keyboard encoder simulation (仅测试用, 与原生编码器数据处理隔离)-----------------------------------------------
    [Header("键盘模拟编码器(仅测试)")]
    public float keyboardRampTime = 1.5f;//按住方向键达到最快速度所需时间(s)
    float keyboardHoldTime = 0f;//按住累计时间, 松键归零
    //public float roll_timer_interval = 0.1f;
    public Rigidbody m_rb;
    public LineChartMultiChannel lineChart; 
    CommandConverter command_Converter;
    List<string> lsTypes = new List<string>(){"mv", "ci", "log", "echo", "vc", "cmd"};//顺序即字节索引: move/context_info/log/echo/value_change/command，勿改顺序

    LinearTrackUIUpdate ui_update;
    Alarm alarm = new Alarm();  public Alarm AlarmPublic { get { return alarm; } }
    public Context_generate context_Generate;
    //int sensor_now;
    int sensorSmooth = 0;
    //int sensor_smooth_silence = 0;
    float[] rollingTimer;//{sensor_stop, now}
    int[] rollingData;
    int[,] rollingDataSmooth;
    int[] thresholds;
    //--------------------------------------Serial-----------------------------------------------
    bool DebugWithoutArduino = false;
    SerialPort sp = null;   public SerialPort SP { get { return sp; } }
    volatile bool StopSerialThread = false;
    Thread serialThread;
    int serialSpeed = 115200;
    List<string> compatibleVersion = new List<string>();
    List<string> recommendPort = new List<string>();
    List<string> portBlackList = new List<string>();
    readonly object serialLock = new object();
    List<byte[]> serial_read_content_ls = new List<byte[]>();//仅在串口线程中改变
    public string[] port_black_list = new string[]{};

    int serialReadContentLsMark = -1;
    readonly object lockObjectMovement = new object();
    float commandVerifyExpireTime = 2;//2s
    ManualResetEvent manualResetEventVerify = new ManualResetEvent(true);
    //readonly object lockObject_command = new object();
    ConcurrentQueue<byte[]> commandQueue = new ConcurrentQueue<byte[]>();
    public ConcurrentDictionary<float, string> commandVerifyDict = new ConcurrentDictionary<float, string>();
    List<string> Arduino_var_list =  "p_enter_reward_context, p_in_reward_context, p_lick_time_accu, p_lick_count, p_start_water, p_lick_mode, p_trial, p_lick_count_max, p_lick_mode0_delay, p_lick_mode1_delay, p_serveWaterReward".Replace(" ", "").Split(',').ToList();
    Dictionary<string, string> Arduino_var_map =  new Dictionary<string, string>{};//{"p_...", "0"}, {"p_...", "1"}...
    
    //--------------------------------------file writing-----------------------------------------------
    StreamWriter streamWriter;
    string filePath = "";
    Queue<string> writeQueue = new Queue<string>();
    const int BUFFER_SIZE = 256;
    const int BUFFER_THRESHOLD = 32;
    float[] time_rec_for_log = new float[2]{0, 0};

    //--------------------------------------position / context / trial (合并自 Position_control)-----------------------------------------------
    public int[] contextZoneStartAndEnd;//start_pos, end_pos, start_pos, end_pos...
    public int[] rewardZoneStartAndEnd;//reward_pos, end_pos, reward_pos, end_pos...
    public List<Context_info> context_info_ls = new List<Context_info>();
    public bool now_context_success;
    int now_trial=1; public int Now_trial{get{return now_trial;}}
    public int trial_per_section=0;
    public int serve_water_mode=0;
    public int lick_count_correct=0;
    public int lick_count_max=0;
    public int waterServedCount = 0;//每个trial刷新
    public int[] lick_count_rec=new int[]{0, 0, 0};//before reward_zone, in_reward_zone, after_reward_zone
    public int[] lick_count_succes_threshold=new int[]{4, 8};
    public struct Context_info{
        public Context_info(string _start_method, int _lick_count_max, string _s_color, string _f_color, float _wait_sec, int[] _lick_threshold, int trial_count, bool _is_inf=false):this(){
            start_pos = new float[trial_count];
            if(_start_method.StartsWith("random")){
                string[] temp_ls=_start_method[_start_method.IndexOf("random")..].Split("~");
                if(temp_ls.Length==2){
                    for(int i=0; i<trial_count; i++){
                        start_pos[i] = UnityEngine.Random.Range(Convert.ToSingle(temp_ls[0]), Convert.ToSingle(temp_ls[1]));
                    }
                }
            }else{
                for(int i=0; i<trial_count; i++){start_pos[i] = Convert.ToSingle(_start_method);}
            }
            lick_count_max = _lick_count_max;
            if (ColorUtility.TryParseHtmlString(_s_color, out Color temp_color)){
                succes_color = temp_color;
            }
            if (ColorUtility.TryParseHtmlString(_f_color, out temp_color)){
                fail_color=temp_color;
            }
            wait_sec = _wait_sec;
            is_inf = _is_inf;
            lick_threshold = new int[2];
            _lick_threshold.CopyTo(lick_threshold, 0);
        }
        public float[]  start_pos       {get;}//relative to start of context
        public int      lick_count_max  {get;}
        public Color    succes_color    {get;}
        public Color    fail_color      {get;}
        public float    wait_sec        {get;}
        public int[]    lick_threshold  {get;}
        public bool     is_inf          {get;}
    }
    float X {get{ return transform.position.x;}set{ transform.position = new Vector3(value, transform.position.y, transform.position.z);}}
    public float RelativeX {get{ return X- contextZoneStartAndEnd[now_context*2];}}
    int now_context=0;
    public int NowContext{get{return now_context;}}
    bool context_available=false;
    bool trial_syncing=false;   public bool Trial_syncing{get{return trial_syncing;}}
    float counter=-999f;//context结束计时
    int waiting=-1;//0:false, 1:true, 2:waiting for sync, -1:inital sync
    float pre_pos=-1f;
    float[] lengthRec = new float[]{-1, 0, 0};//{indicator, begin<可正可负>, length passed}
    public bool LengthRecClear  {set{ if(value){lengthRec = new float[]{-1, 0, 0}; LengthRecClear = false;}}}
    Transform tf;
    public GameObject fullScreenColor;
    Dictionary<string, float> dicWaterServingSpdVariables = new Dictionary<string, float>(){
        {"speed_threshold", 0.2f}, {"lasting_time_threshold", 0.4f}, {"running_time_threshold", 2}, {"time_serve_interval", 2}, {"time_serve_interval_when_runing", 1}, {"time_run_begin", 0}, {"time_served", 0}, {"random_delayed", 0}
    };   
    Dictionary<string, float> dicWaterServingLengthVariables = new Dictionary<string, float>(){
        {"length_threshold", 1f}, {"running_threshold", 10f}, {"time_serve_interval", 2}, {"time_serve_interval_when_runing", 1}, {"time_served", 0}, {"random_delayed", 0}
    };    
    public float dic_water_serving_speed_threshold{set{dicWaterServingSpdVariables["speed_threshold"]=value;}}
    public float dic_water_serving_runbegin{set{dicWaterServingSpdVariables["time_run_begin"]=value;}}
    Vector3 rec_pos;
    //--------------------------------------licking decision (给水判定上移, 对齐 Moving.cs)-----------------------------------------------
    int reward_remaining=0;//进奖励区置为 lick_count_max，每次给水递减
    float lastRewardTime=-999f;
    float minIgnoreLickInterval=0.3f;//两次给水最小间隔(s)
    float extraRewardTimeInSec=1f;//离开奖励区后仍可给水的额外窗口(s)

    string[] ScanPorts_API(){
        string[] portList = SerialPort.GetPortNames();
        return portList;
    }

    int CreateSerialConnection(ref List<string> portInfo){//参照主项目 Moving.cs：扫描端口 -> 握手(initialed:版本) -> 版本校验 -> ACK/ACK_OK
        sp = null;
        List<string> portLs = ScanPorts_API().ToList();
        for(int i = 0; i < portLs.Count; i++){
            if(recommendPort.Contains(portLs[i])){
                portLs.Insert(0, portLs[i]);
                portLs.RemoveAt(i+1);
            }
        }
        if(portLs.Count == 0){ portInfo.Add("No Port Found!"); }
        portInfo.Add($"Ports Scaned:{string.Join(", ", portLs)}");
        bool connected = false;
        foreach(string port in portLs){
            if(!connected && port.Contains("COM") && !portBlackList.Contains(port)){
                SerialPort tempSp = null;
                try{
                    tempSp = new SerialPort(port, serialSpeed, Parity.None, 8, StopBits.One);
                    tempSp.RtsEnable = true;
                    tempSp.DtrEnable = true;
                    tempSp.ReadTimeout = 3000;
                    tempSp.WriteTimeout = 1000;
                    tempSp.Open();
                    Debug.Log("COM available: " + port);

                    tempSp.WriteLine("//forceinit");
                    int failCount = 0; int maxFailCount = 10;
                    string initMsg = tempSp.ReadLine();
                    while(initMsg.Length == 0 && failCount < maxFailCount){
                        initMsg = tempSp.ReadLine();
                        failCount++;
                    }
                    // Debug.Log("Received: " + initMsg);

                    if(initMsg.StartsWith("initialed:")){
                        string version = initMsg.Length > 10 ? initMsg[10..].Trim() : "";
                        if(compatibleVersion.Count() > 0 && !compatibleVersion.Contains(version)){
                            throw new Exception($"Incompatible version: {version}, required: {string.Join(", ", compatibleVersion)}");
                        }
                        string response = "";
                        int newlineCount = 0;
                        for(int i = 0; i < 3; i++){tempSp.WriteLine("\nACK\n");}
                        while(newlineCount < maxFailCount){
                            char c = (char)tempSp.ReadChar();
                            if(c == '\n' || c == '\r'){
                                newlineCount++;
                                response = response.Replace("\r", "").Replace("\n", "");
                            }else{
                                response += c;
                            }
                            if(response.EndsWith("ACK_OK")){break;}
                        }
                        if(response.EndsWith("ACK_OK")){
                            Debug.Log("ACK received, handshake complete");
                            sp = tempSp;
                            connected = true;
                            return 1;
                        }else{
                            throw new Exception($"Unexpected response: {response}");
                        }
                    }else{
                        throw new Exception($"Unexpected init message: {initMsg}");
                    }
                }
                catch(Exception e){
                    Debug.Log(e.Message);
                    portInfo.Add($"{port}: {e.Message}");
                    SafeCloseSerialPort(ref tempSp);
                }
            }
        }
        return connected? 1: -1;
    }

    void SafeCloseSerialPort(ref SerialPort port){
        if(port != null){
            try{ if(port.IsOpen){ port.Close(); } }
            catch(Exception e){ Debug.Log(e.Message); }
            try{ port.Dispose(); }catch{}
            port = null;
        }
    }

    void RecreateSerialConnection(bool showMsg = false, string trace = ""){
        lock(serialLock){
            SafeCloseSerialPort(ref sp);
            List<string> portInfo = new List<string>();
            CreateSerialConnection(ref portInfo);
            if(sp != null && showMsg){
                ui_update.MessageUpdate($"serial reconnected ({trace})\n");
            }
        }
    }

    void CommandParse(byte[] _command){//在主线程调用时内容不能有锁
        //"move", "context_info", "log"
        int startInd = -1;
        int temp_type = command_Converter.GetCommandType(_command, out startInd);
        switch(temp_type){
            case 0:{//move, still byte[] format
                lock(lockObjectMovement){
                    int _dx, _dy;
                    _dx = _command[startInd] - 64;
                    _dy = _command[startInd+1] - 64;
                    rollingData[0] += Math.Abs(_dx) < 32? _dx: 0;
                    rollingData[1] += Math.Abs(_dy) < 32? _dx: 0;
                    rollingDataSmooth[0, 0] = Math.Max(rollingDataSmooth[0, 0], rollingData[0]);
                    rollingDataSmooth[0, 1] = Math.Min(rollingDataSmooth[0, 1], rollingData[0]);
                    rollingDataSmooth[1, 0] = Math.Max(rollingDataSmooth[1, 0], rollingData[1]);
                    rollingDataSmooth[1, 1] = Math.Min(rollingDataSmooth[1, 1], rollingData[1]);
                    //rolling_data[2]+= dz;  
                }
                break;
            }
            case 1:{//cotext_info
                string command=command_Converter.ConvertToString(_command);
                string body = command.Contains(":")? command[(command.IndexOf(":")+1)..] : command;//去除类型头(短码长度可变)
                if(body.StartsWith("lick:")){
                    LickingCheck();//舔水中央决策（含分区记录、指示灯、mode0 给水判定）
                }else if(body.StartsWith("ws")){
                    waterServedCount++;
                    ui_update.WaterServeIndicateUpdate(RelativeX);
                }
                break;
            }
            case 2:{//log
                string command=command_Converter.ConvertToString(_command);
                if(command.StartsWith("log:") && !command.Contains("received")){ui_update.MessageUpdate(command+"\n");}
                // Debug.Log($"received :\"{command}\" at {Time.unscaledTime}");
                break;
            }
            case 3:{//echo
                string command = command_Converter.ConvertToString(_command);
                command = command["echo:".Length..];
                if(command.Contains(":echo")){
                    List<float> temp_keys = commandVerifyDict.Keys.ToList();
                    temp_keys.Sort();
                    foreach(float time in temp_keys){
                        if(commandVerifyDict.ContainsKey(time) && commandVerifyDict[time].CompareTo(command[..command.IndexOf(":echo")]) == 0){
                            // Debug.Log("verified: "+commandVerifyDict[time]);
                            commandVerifyDict.Remove(time, out _);
                        }
                    }
                }
                // Debug.Log($"received :\"{command}\" at {Time.unscaledTime}");
                break;
            }
            default: break;
        }
    }

    void DataReceived(){
        while (!StopSerialThread){
            manualResetEventVerify.WaitOne(20);  // 超时唤醒以便及时响应关闭/校验暂停
            if (StopSerialThread) break;
            if (sp!= null && sp.IsOpen){
                try{
                    if (sp.ReadTimeout != 50) sp.ReadTimeout = 50;
                    int count = sp.BytesToRead;
                    if (count > 0){
                        byte[] readBuffer = new byte[count];
                        try{
                            sp.Read(readBuffer, 0, count);
                        }
                        catch (TimeoutException){ continue; }
                        catch (Exception ex){ Debug.Log(ex.Message); continue; }
                        serial_read_content_ls.Add(readBuffer);
                        if(command_Converter.FindMarkOfMessage(true, readBuffer, 0)!=-1){
                            serialReadContentLsMark=serial_read_content_ls.Count()-1;
                        }
                        int temp_end=-1;
                        if(serialReadContentLsMark!=-1){
                            temp_end=command_Converter.FindMarkOfMessage(false, readBuffer, 0);
                        }

                        if(serialReadContentLsMark!=-1 && temp_end!=-1){
                            byte[] temp_complete_msg;
                            temp_complete_msg = command_Converter.ProcessSerialPortBytes(command_Converter.Read_buffer_concat(serial_read_content_ls, serialReadContentLsMark, -1));
                            if(temp_complete_msg.Length>0){
                                if (command_Converter.GetCommandType(temp_complete_msg, out _)==0){
                                    CommandParse(temp_complete_msg);
                                }
                                else{commandQueue.Enqueue(temp_complete_msg);}
                            }

                            serial_read_content_ls.Clear();
                            if(readBuffer.Length-temp_end>0){
                                byte[] temp_readBuffer=new byte[readBuffer.Length-temp_end];
                                Array.Copy(readBuffer, temp_end ,temp_readBuffer, 0, temp_readBuffer.Length);
                                if(command_Converter.FindMarkOfMessage(true, temp_readBuffer, 0)!=-1){
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
                catch(System.IO.IOException e){
                    Debug.Log(e.Message);
                    SafeCloseSerialPort(ref sp);
                }
                catch(ObjectDisposedException){
                    sp = null;
                }
            }
            else{
                if(!StopSerialThread && !DebugWithoutArduino){
                    RecreateSerialConnection(false, "serial thread");
                    Debug.Log("lost connection to serial port, try to reconnect...");
                }else{
                    Thread.Sleep(50);
                }
            }
        }
    }

    public int DataSend(string message, bool variable_change = false, bool inVerifyOrVerifyNeedless=false){
        if(sp!= null && sp.IsOpen){
            if(variable_change){//form: p_.... = 1
                // if(simple_mode){
                //     //byte[] temp_msg = new byte[]{0xAA, 0xBB, 0xCC, 0xDD};
                //     byte[] temp_msg = new byte[]{0xAA, 0xBB, 0xCC, 0xDD};
                //     sp.Write(temp_msg, 0, temp_msg.Length);
                //     Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                // }
                //else{
                    string temp_var_name = message.Split('=')[0];
                    temp_var_name = temp_var_name.Replace("/","");

                    if(Arduino_var_map.ContainsKey(temp_var_name)){//从p_xxx转为int=int
                        string temp_command=Arduino_var_map[temp_var_name]+"="+message.Split('=')[1];
                        if(!inVerifyOrVerifyNeedless){
                            commandVerifyDict.TryAdd(Time.fixedUnscaledTime, temp_command);
                        }
                        byte[] temp_msg = command_Converter.ConvertToByteArray($"{lsTypes[4]}:{temp_command}");
                        sp.Write(temp_msg, 0, temp_msg.Length);
                        // Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                        return 2;
                    }
                    else{
                        if(Int16.TryParse(temp_var_name, out short temp_id) && temp_id<255){//重发int=int
                            string temp_command=temp_var_name+"="+message.Split('=')[1];
                            byte[] temp_msg = command_Converter.ConvertToByteArray($"{lsTypes[4]}:{temp_command}");
                            sp.Write(temp_msg, 0, temp_msg.Length);
                            // Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                            return 2;
                        }
                        return 0;
                    }
                //}
            }else{
                byte[] temp_msg = command_Converter.ConvertToByteArray($"{lsTypes[5]}:{message}");
                sp.Write(temp_msg, 0, temp_msg.Length);
                // Debug.Log("Data sent: "+message);
            }
            return 1;
        }
        else{
            if(!DebugWithoutArduino){
                Debug.LogError("port not open");
                return -1;
            }else{
                return -3;
            }
            
        }
    }
    
    public int Context_verify(List<string> messages, List<int> values){
        if(sp == null){return -3;}
        manualResetEventVerify.Reset();
        if(!sp.IsOpen){
            Debug.Log("sp not open");
            RecreateSerialConnection(trace:"Context_verify");
        }
        if(sp == null){ manualResetEventVerify.Set(); return -2; }
        sp.ReadTimeout = 100;
        int fail_count = 0;
        bool succes = false;
        int fail_countMax = 10;
        int tempMsgInd = 0;//记录已经同步完成的内容
        try{
            while(!succes && fail_count < fail_countMax){
                try{
                    for(int i=tempMsgInd; i<messages.Count; i++){
                        string temp_echo = "error";
                        DataSend("ping", inVerifyOrVerifyNeedless:true);
                        DataSend(messages[i]+"="+values[i].ToString(), true, inVerifyOrVerifyNeedless:true);
                        while(true){
                            temp_echo = sp.ReadLine();
                            if(temp_echo.StartsWith("echo:")){
                                temp_echo=temp_echo[5..temp_echo.IndexOf(":echo")];
                                break;
                            }
                            else if(temp_echo.Length > 3){//非echo行回灌，避免丢失真实消息
                                serial_read_content_ls.Add(new byte[]{0xAA}.Concat(Encoding.UTF8.GetBytes(temp_echo)[1..(temp_echo.Length-1)]).Concat(new byte[]{0xDD}).ToArray());
                            }
                        }
                        string temp_aim = Arduino_var_list.FindIndex(str => str==messages[i]).ToString() + "=" + values[i].ToString();
                        if(temp_echo.Replace(" ", "")==temp_aim){
                            tempMsgInd = i+1;
                            continue;
                        }
                        else{ throw new Exception("verify mismatch"); }
                    }
                    succes = tempMsgInd >= messages.Count;
                }
                catch(TimeoutException){ fail_count++; }
                catch(Exception e){
                    if(e.Message.Contains("not open")){ manualResetEventVerify.Set(); return -2; }
                    fail_count++;
                }
            }
        }
        finally{
            manualResetEventVerify.Set();
        }
        return succes? 1: -1;
    }

    public void PauseMoving(){
        manualResetEventVerify.Reset();
    }

    public void ContinueMoving(){
        manualResetEventVerify.Set();
    }

    public int Context_verify(string message, int value){
        List<string> variables = new List<string>(){message};
        List<int> values = new List<int>(){value};
        return Context_verify(variables, values);
    }

    public int Context_verify(string message, int value, string message2, int value2){
        List<string> variables = new List<string>(){message, message2};
        List<int> values = new List<int>(){value, value2};
        return Context_verify(variables, values);
    }


    void InitializeStreamWriter(){
        try{
            #if UNITY_EDITOR
                if(!Directory.Exists("Assets/Resources/Logs/")){Directory.CreateDirectory("Assets/Resources/Logs/");}
                filePath ="Assets/Resources/Logs/"+DateTime.Now.ToString("yyyy_MM_dd_HH_mm") + "_rec.txt";
            #else
                if(!Directory.Exists(Application.dataPath+"/Logs")){Directory.CreateDirectory(Application.dataPath+"/Logs");}
                filePath=Application.dataPath+"/Logs/"+DateTime.Now.ToString("yyyy_MM_dd_HH_mm") + "_rec.txt";
            #endif
            FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
            streamWriter = new StreamWriter(fileStream);
        }
        catch (Exception e){
            Debug.LogError($"Error initializing StreamWriter: {e.Message}");
        }
    }

    void ProcessWriteQueue(bool writeAll = false)//txt文件写入，位于主进程
    {
        while (writeQueue.Count > 0 && streamWriter !=  null){
            string chunk = writeQueue.Peek();
            streamWriter.WriteLine(chunk);

            if (writeAll || streamWriter.BaseStream.Position >=  streamWriter.BaseStream.Length - BUFFER_THRESHOLD){
                streamWriter.Flush();
            }

            writeQueue.Dequeue();
        }
    }

    void CleanupStreamWriter()
    {
        if (streamWriter !=  null)
        {
            streamWriter.Close();
            streamWriter.Dispose();
            streamWriter = null;
        }
    }

    string WriteInfo(string _rollingData = "/", bool write = true){
        if(write){
            time_rec_for_log[1] = Time.fixedUnscaledTime;
            float[] temp_context_info=GetContextInfo();
            // results[0] = now_context;
            // results[1] = X - contextZoneStartAndEnd[now_context*2];
            // results[2] = contextZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
            // results[3] = rewardZoneStartAndEnd[now_context*2] - contextZoneStartAndEnd[now_context*2];
            // results[4] = rewardZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
            // results[5] = context_info_ls[now_context].is_inf? 1: 0;
            //string temp_time = (time_rec_for_log[1]-time_rec_for_log[0]).ToString(".00");
            string data_write = $"{time_rec_for_log[1]-time_rec_for_log[0]}\t{serve_water_mode}\t{Now_trial}\t{temp_context_info[0]}\t{temp_context_info[1]}\t{_rollingData}\t{m_rb.velocity.magnitude}\t{string.Join("\t", lick_count_rec)}\t{lick_count_correct}\t{waterServedCount}";
            writeQueue.Enqueue(data_write);
            ProcessWriteQueue();
        }
        return "delta time\tmode\ttrial\tcontext\trelative pos\traw data\tspeed\tlick before-\tlick in-\t lick after rewardzone\tlick_count_correct\twater served";
    }

    void Quit(){
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void Awake(){
        sensorSmooth = sensor_smooth_interval;// = 0时结算一次
        rollingTimer = new float[] {Time.unscaledTime, Time.unscaledTime, Time.unscaledTime, Time.unscaledTime};
        rollingData = new int[] {0, 0, 0};
        rollingDataSmooth = new int[,] {{0, 0}, {0, 0}, {0, 0}};
        time_rec_for_log[0] = Time.fixedUnscaledTime;
        //tf = GetComponent<Transform>();
        m_rb = GetComponent<Rigidbody>();
        tf = GetComponent<Transform>();
        command_Converter = new CommandConverter(lsTypes);
        ui_update = GetComponent<LinearTrackUIUpdate>();
        rec_pos = new Vector3(X, transform.position.y, transform.position.z);
        DeactivateFillScreenColor();

        thresholds = ui_update.thresholds;

        for(int i = 0; i<Arduino_var_list.Count; i++){
            Arduino_var_map.Add(Arduino_var_list[i], i.ToString());
        }

        IniReader iniReader = new IniReader(context_Generate.GetConfigPath());

        portBlackList.Clear();
        foreach(string com in iniReader.ReadIniContent("serialSettings", "blackList", "").Split(",")){
            if(com.Length > 0 && !portBlackList.Contains(com)){portBlackList.Add(com);}
        }
        foreach(string com in iniReader.ReadIniContent("serialSettings", "recommendPort", "").Split(",")){
            if(com.Length > 0 && !recommendPort.Contains(com)){recommendPort.Add(com);}
        }
        foreach(string matchVersion in iniReader.ReadIniContent("serialSettings", "compatibleVersion", "").Split(",")){
            if(matchVersion.Length > 0){compatibleVersion.Add(matchVersion);}
        }
        int[] availableSerialSpeed = new int[]{115200, 230400, 250000, 460800, 500000, 921600};
        if(!int.TryParse(iniReader.ReadIniContent("serialSettings", "serialSpeed", "115200"), out serialSpeed) || !availableSerialSpeed.Contains(serialSpeed)){
            serialSpeed = 115200;
        }

        List<string> portInfo = new List<string>();
        CreateSerialConnection(ref portInfo);

        if(sp != null){
            serialThread = new Thread(new ThreadStart(DataReceived));
            serialThread.Start();
            Debug.Log("serial thread started");
            InitializeStreamWriter();
            string data_write = WriteInfo(write: false);
            writeQueue.Enqueue(data_write);
        }else{
            Debug.LogWarning("No Connection to Arduino! ports' info as follow:\n" + string.Join("\n", portInfo));
            if(MessageBoxForUnity.YesOrNo("No Connection to Arduino! ports' info:\n" + string.Join("\n", portInfo) + "\nContinue without connection to Arduino?", "Serial Error") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                DebugWithoutArduino = true;
                InitializeStreamWriter();
                string data_write = WriteInfo(write: false);
                writeQueue.Enqueue(data_write);
            }else{
                Quit();
            }
        }
    }

    void Start(){
        lineChart = ui_update.lineChart;
        if(context_info_ls.Count()>0){
            lick_count_max=context_info_ls[0].lick_count_max;
        }
        else{//无 context 加载则退出
            Quit();
        }
    }

    void Update(){
        PositionUpdate();
    }

    // 键盘模拟编码器(仅测试用, 与原生 rollingData 处理隔离)：按住越久越快(~keyboardRampTime 达 maxSpeed)，
    // 复用原生 0.1→maxSpeed 速度曲线；仅返回等效 forward 并借用原生 sensorPause 停车通路(松键后由 FixedUpdate 顶部 alarm 归零)。
    float SimulateEncoderByKeyboard(){
        int dir = Convert.ToInt32(Input.GetKey(KeyCode.UpArrow)) - Convert.ToInt32(Input.GetKey(KeyCode.DownArrow));
        if(dir == 0){ keyboardHoldTime = 0f; return 0f; }//松键归零, 停车交由原生 sensorPause 处理
        keyboardHoldTime = Mathf.Min(keyboardHoldTime + Time.fixedDeltaTime, keyboardRampTime);
        float p = keyboardRampTime > 0? keyboardHoldTime / keyboardRampTime : 1f;//0→1 斜坡, ~keyboardRampTime 到顶
        alarm.TrySetAlarm("sensorPause", sensor_pause_interval, out _, force:true);//与编码器一致：持续输入即重置停车倒计时
        return dir * (0.1f + (maxSpeed - 0.1f) * p);//方向 * (地板0.1 → maxSpeed)，幅值对称
    }

    void FixedUpdate(){
        alarm.AlarmFixUpdate();
        foreach(string finishedAlarm in alarm.GetAlarmFinish()){
            if(finishedAlarm == "sensorPause"){//停车间隔到，判定为停止前进
                dic_water_serving_runbegin = 0;
                m_rb.velocity = m_rb.transform.forward * 0;
            }
        }
        float forward = SimulateEncoderByKeyboard();//键盘模拟编码器(独立方法)；原生编码器速度在下方 rollingData 分支叠加
        float rotate = Convert.ToInt32(Input.GetKey(KeyCode.RightArrow))-Convert.ToInt32(Input.GetKey(KeyCode.LeftArrow));

        //if(Math.Abs(rolling_data[0])>thresholds[0] || Math.Abs(rolling_data[1])>thresholds[1] || Math.Abs(rolling_data[2])>thresholds[2]){
        //if(rolling_data[0]>thresholds[0] || rolling_data[1]>thresholds[1]){
        //if(rolling_data[0]>thresholds[0]){
        if(rollingData[0]!=0){
            sensorSmooth = sensorSmooth< 0? sensor_smooth_interval: sensorSmooth;//开始平均，若正在滑动平均则略。
            if(rollingData[0]>thresholds[0]){
                alarm.TrySetAlarm("sensorPause", sensor_pause_interval, out _, force:true);//运动超阈值即重置停车倒计时(见FixedUpdate顶部GetAlarmFinish处理)
            }
        }else if(sensorSmooth<= 0){//兼顾了键盘的停止
            lock(lockObjectMovement){
                rollingData = new int[] {0, 0, 0};
            }
        }
        
        string temp_rollingData = "/";
        if(sensorSmooth==0){
            //rollingData[0] -= rollingDataSmooth[0, 0] + rollingDataSmooth[0, 1];
            rollingData[1] -= rollingDataSmooth[1, 0] + rollingDataSmooth[1, 1];
            //if(rollingData[0] != 0){Debug.Log($"minus {rollingDataSmooth[0, 0]} and {rollingDataSmooth[0, 1]}, now {rollingData[0]}");}

            // lineChart.AddDataPoint(new List<float>(){ rollingData[0], rollingData[1], rollingData[2]});
            lineChart.AddDataPoint(new List<float>(){ rollingData[0], rollingData[0] + (rollingDataSmooth[0, 0] + rollingDataSmooth[0, 1]), rollingData[2]});
            temp_rollingData = rollingData[0].ToString();

            rollingDataSmooth = new int[,] {{0, 0}, {0, 0}, {0, 0}};
            //Debug.Log("rolling_data:"+rolling_data[0].ToString()+","+rolling_data[1].ToString()+","+rolling_data[2].ToString());
            
            //forward+= (Math.Abs(rolling_data[0])>thresholds[0] ? 1:0) * (rolling_data[0]>0? 1: 0);
            if(rollingData[0] > maxRolling_value){rollingData[0] = maxRolling_value;}
            forward+= (rollingData[0]>thresholds[0] ? 1:0) * (0.1f + (maxSpeed - 0.1f) * (rollingData[0]-thresholds[0])/(maxRolling_value-thresholds[0]));
            ui_update.MessageUpdate();
            //rotate+= (Math.Abs(rolling_data[1])>thresholds[1]? 1:0) * (rolling_data[1]>0? 1: -1) * (int)turnSpeed;

            lock(lockObjectMovement){
                rollingData = new int[] {0, 0, 0};
            }
        }if(sensorSmooth>= 0){sensorSmooth-= 1;}

        // Debug.Log("lc: "+Input.GetKey(KeyCode.LeftControl) + "; forward: "+forward);
        if((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) && forward>0){
            m_rb.velocity = forward * scaleFactor * m_rb.transform.forward;
        }
        else{
            m_rb.velocity = forward!= 0? (forward > maxSpeed? maxSpeed: forward) * scaleFactor * m_rb.transform.forward: m_rb.velocity;//后面继续改这部分
        }

        Quaternion deltaRotation = Quaternion.Euler(0, rotate, 0);
        //m_rb.MoveRotation(m_rb.rotation * deltaRotation);

        WriteInfo(temp_rollingData);

        while(commandQueue.Count()>0){//重新发送之前未能同步成功的内容
            commandQueue.TryDequeue(out byte[] _command);
            CommandParse(_command);
        }
        if(commandVerifyDict.Count>0){
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

        if (Input.GetKey(KeyCode.Escape)){
            try{
                StopSerialThread = true;
                manualResetEventVerify.Set();
                writeQueue.Enqueue(ui_update.log_message.text);
                ProcessWriteQueue();
                CleanupStreamWriter();
                SafeCloseSerialPort(ref sp);
                Debug.Log("serial closed");
            }
            catch{}
            finally{
                Quit();
            }
        }
    }

    // ==================== 合并自 Position_control：context/trial/给水决策 ====================
    public void Trial_args_init(bool update=true ,bool init_all=false){//清理位置信息，更新位置以及pcontext相关参数
        if(update){
            if(now_context==context_info_ls.Count-1){now_trial++;}
            now_context = (now_context+ 1) % context_info_ls.Count;
            context_available=false;
            float temp_length_diff = X - lengthRec[1];
            X = context_info_ls[now_context].start_pos[now_trial]+contextZoneStartAndEnd[now_context*2];
            lengthRec[1] = X - temp_length_diff;
            rec_pos=tf.position;
        }
        waterServedCount = 0;
        lick_count_correct=0;
        now_context_success=false;
        lick_count_rec=new int[]{0, 0, 0};
        DataSend("init");

        if(init_all){
            now_context=0;
            counter=0f;
            waiting=0;
            pre_pos=-1f;
        }
    }

    int Trial_context_sync(int context_id){
        //"p_enter_reward_context, p_in_reward_context, p_lick_time_accu, p_lick_count, p_start_water, p_lick_mode, p_trial, p_lick_count_max, p_lick_mode0_delay, p_lick_mode1_delay"
        trial_syncing=true;
        lick_count_max=context_info_ls[context_id].lick_count_max;
        context_info_ls[context_id].lick_threshold.CopyTo(lick_count_succes_threshold, 0);
        List<string> variables = new List<string>(){"p_enter_reward_context", "p_lick_count_max"};
        List<int> values = new List<int>(){-1, lick_count_max};
        variables.Add("p_trial");
        values.Add(now_trial);

        int sync_result = Context_verify(variables, values);
        if(sync_result==-2){
            ui_update.MessageUpdate("serial port not open!\n");
        }else{
            int sync_max_time=100;
            while(sync_result!=1 && sync_max_time>0){
                sync_result = Context_verify(variables, values);
                if(sync_result!=1 && sync_result != -3){
                    Debug.LogError("context info sync failed");
                }
                else{break;}
                sync_max_time--;
            }
        }

        variables.Clear();
        values.Clear();

        if(sync_result!=1 && sync_result != -3){
            Debug.LogError("context info sync failed");
            trial_syncing=false; 
            return -1;
        }

        context_available=true;
        trial_syncing=false;
        return 1;
    }

    public void Set_trial_info(int _trial_per_section){
        trial_per_section=_trial_per_section;
    }

    public float[] GetContextInfo(){
        //  0           1           2           3           4           5       6,7,8
        //context, pos<relative>, length, reward start, reward end, is_inf, lick_count_rec
        float[] results = new float[9];
        results[0] = now_context;
        results[1] = X - contextZoneStartAndEnd[now_context*2];
        results[2] = contextZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
        results[3] = rewardZoneStartAndEnd[now_context*2] - contextZoneStartAndEnd[now_context*2];
        results[4] = rewardZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
        results[5] = context_info_ls[now_context].is_inf? 1: 0;
        Array.ConstrainedCopy(lick_count_rec, 0, results, 6, 3);
        return results;
    }

    void RecordLickByZone(int add_num=1){//仅按位置分区记录舔水，正确性判定移至 LickingCheck
        if (X >= contextZoneStartAndEnd[now_context*2] && X <= contextZoneStartAndEnd[now_context*2+1]){
            if (X >= rewardZoneStartAndEnd[now_context*2]){
                if (X < rewardZoneStartAndEnd[now_context*2+1]){
                    lick_count_rec[1] += add_num;
                }else{lick_count_rec[2] += add_num;}
            }else{lick_count_rec[0] += add_num;}
        }
    }

    public void LickingCheck(){//舔水中央决策（对齐 Moving.cs）：mode0 由 Unity 判定并下发出水命令
        bool inRewardZone = X>=rewardZoneStartAndEnd[now_context*2] && X<rewardZoneStartAndEnd[now_context*2+1];
        RecordLickByZone();
        ui_update.LickIndicateUpdate(RelativeX);
        if(serve_water_mode==0 && waiting==0
           && (inRewardZone || alarm.GetAlarm("rewardEnable")>0)   // 奖励区内 或 额外奖励窗口内
           && reward_remaining>0
           && Time.fixedUnscaledTime-lastRewardTime > minIgnoreLickInterval){  // 最小给水间隔
            lick_count_correct++;
            reward_remaining--;
            lastRewardTime = Time.fixedUnscaledTime;
            string temp_str="p_serveWaterReward=1";
            if(DataSend(temp_str, true, true)==-1){Debug.LogError("missing variable name: "+temp_str);}  // Unity 决策→Arduino 执行
        }
    }

    public float[] Get_set_dic_water_serving(string key, int mode){//get value
        float[] array=new float[]{-1, -1};//length, aim value
        if(mode == 1){
            if(dicWaterServingSpdVariables.ContainsKey(key)){
                array[0]=0;
                array[1]=dicWaterServingSpdVariables[key];
            }
            return array;
        }
        else if(mode == 2){
            if(dicWaterServingLengthVariables.ContainsKey(key)){
                array[0]=0;
                array[1]=dicWaterServingLengthVariables[key];
            }
            return array;
        }
        else{
            return array;
        }
    }
    public void Get_set_dic_water_serving(string key, float value, int mode){//set value
        if(mode ==1){
            if(dicWaterServingSpdVariables.ContainsKey(key)){
                dicWaterServingSpdVariables[key]=value;
            }
            return;
        }
        else if(mode == 2){
            if(dicWaterServingLengthVariables.ContainsKey(key)){
                dicWaterServingLengthVariables[key]=value;
            }
            return;
        }
    }
    public float[] Get_set_dic_water_serving(int mode, out string key_text, int index=-1){//后面改成return keys
        float[] array=new float[]{-1, -1};//length, aim value
        if(mode==1){
            List<string> temp_list = new List<string>(dicWaterServingSpdVariables.Keys);
            array[0]=temp_list.Count;

            string temp_text=index==-1? "": temp_list[index];
            key_text=temp_text;
            if(dicWaterServingSpdVariables.ContainsKey(temp_text)){
                array[1]=dicWaterServingSpdVariables[temp_text];
            }
            return array;
        }
        else if(mode==2){
            List<string> temp_list = new List<string>(dicWaterServingLengthVariables.Keys);
            array[0]=temp_list.Count;

            string temp_text = index == -1? "": temp_list[index];
            key_text=temp_text;
            if(dicWaterServingLengthVariables.ContainsKey(temp_text)){
                array[1]=dicWaterServingLengthVariables[temp_text];
            }
            return array;
        }
        else{
            key_text = "";
            return array;
        }
    }

    void ServeWaterSpeedDepend(float spd, int random_delay_sec=0, bool IsInRewardZone=true, bool RewardZoneNeeded=false){//random_delay_sec: 延迟随机x秒后给水
        if(spd>dicWaterServingSpdVariables["speed_threshold"]){//在跑了
            float now_time=Time.fixedUnscaledTime;
            if(dicWaterServingSpdVariables["time_run_begin"]==0){dicWaterServingSpdVariables["time_run_begin"]=now_time;}//开始跑时记录开始时间，每次跑动结束后归零
            else{//正在跑动
                if(now_time-dicWaterServingSpdVariables["time_run_begin"]>dicWaterServingSpdVariables["lasting_time_threshold"]){//至少达到最低时间要求了
                    if(now_time-dicWaterServingSpdVariables["time_run_begin"]>dicWaterServingSpdVariables["running_time_threshold"]){
                        if(now_time-dicWaterServingSpdVariables["time_served"]>dicWaterServingSpdVariables["time_serve_interval_when_runing"]){//持续跑动过程中随机给水
                            if(IsInRewardZone || !RewardZoneNeeded){    
                                if(dicWaterServingSpdVariables["random_delayed"]==1){//上次已经delay过
                                    Debug.Log("water served in running");    
                                    string temp_str="p_serveWaterReward=1";
                                    if(DataSend(temp_str, true, true)==-1){Debug.LogError("missing variable name: "+temp_str);}

                                    dicWaterServingSpdVariables["random_delayed"]=0;
                                    dicWaterServingSpdVariables["time_served"]=now_time;
                                }else{//随机delay
                                    dicWaterServingSpdVariables["time_served"]+=UnityEngine.Random.Range(0, random_delay_sec);
                                    dicWaterServingSpdVariables["random_delayed"]=1;
                                }
                            }
                        }
                    }else{
                        if(now_time-dicWaterServingSpdVariables["time_served"]>dicWaterServingSpdVariables["time_serve_interval"]){
                            if(IsInRewardZone || !RewardZoneNeeded){
                                Debug.Log("water served in trying");
                                string temp_str="p_serveWaterReward=1";
                                if(DataSend(temp_str, true, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
                                dicWaterServingSpdVariables["time_served"]=now_time;
                            }
                        }
                    }
                }
            }
        }else{
            //没在跑
        }
    }

    void ServeWaterLengthDepend(float length, bool IsInRewardZone=true, bool RewardZoneNeeded=false){
        if(length>dicWaterServingLengthVariables["length_threshold"]){//在跑了
            float now_time=Time.fixedUnscaledTime;
            //间断短途跑动
            if(now_time-dicWaterServingLengthVariables["time_served"]>dicWaterServingLengthVariables["time_serve_interval"]){
                if(IsInRewardZone || !RewardZoneNeeded){
                    Debug.Log("water served in trying");
                    string temp_str="p_serveWaterReward=1";
                    if(DataSend(temp_str, true, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
                    dicWaterServingLengthVariables["time_served"]=now_time;
                    LengthRecClear = true;
                }
            }
        }
    }

    void ActivateFullSCreenColor(Color color){
        if(fullScreenColor==null){Debug.LogWarning("fullScreenColor 未在 Inspector 指定(合并自 Position_control)"); return;}
        fullScreenColor.SetActive(true);
        fullScreenColor.GetComponent<Camera>().backgroundColor = color;
    }

    void DeactivateFillScreenColor(){
        if(fullScreenColor==null){return;}
        fullScreenColor.SetActive(false);
    }

    void PositionUpdate(){//合并自 Position_control.Update() 的位置/context 驱动，由 Update() 调用
        float now_pos=tf.position[0];

        if(waiting!=0){//context结束结算
            PauseMoving();
            ui_update.MessageUpdate(_pauseChange:true, _pauseMoving: true);
            if(! context_available){//trial init之后设为false,即结算后再同步
                if(!trial_syncing){
                    Trial_context_sync(now_context);
                }
                if(waiting!=-1){waiting=2;}
            }else{
                waiting=1;
            }

            tf.position=rec_pos;
            if(waiting==1 && alarm.GetAlarm("contextEndWait") <= -1){//已同步完成且等待alarm到期(未设置返-2/到期返-1)，正常进入下一context
                waiting = 0;
                counter = 0;
                DeactivateFillScreenColor();
                pre_pos=tf.position[0];
                ContinueMoving();
                ui_update.PositionIndicateUpdate(GetContextInfo());
                ui_update.MessageUpdate(_pauseChange:true, _pauseMoving: false);
                //保证黑屏时不能给水
                if(context_info_ls[now_context].start_pos[now_trial]+contextZoneStartAndEnd[now_context*2] >= rewardZoneStartAndEnd[now_context*2] && context_info_ls[now_context].start_pos[now_trial]+contextZoneStartAndEnd[now_context*2] < rewardZoneStartAndEnd[now_context*2+1]){
                    Context_verify("p_enter_reward_context", 1);
                }

                if(now_trial>trial_per_section){
                    alarm.TrySetAlarm("contextEndWait", 100000f, out _, force:true);//会话结束，长时间暂停
                    ActivateFullSCreenColor(Color.green);
                    DeactivateFillScreenColor();
                    waiting = 1;
                }
            }
        }
        else{//移动中
            int i_context_zone=now_context*2;
            bool IsInRewardZone = transform.position[0]>rewardZoneStartAndEnd[i_context_zone] && transform.position[0]<rewardZoneStartAndEnd[i_context_zone+1];

            ui_update.PositionIndicateUpdate(X - contextZoneStartAndEnd[now_context*2]);
            if (pre_pos<=contextZoneStartAndEnd[i_context_zone] && now_pos>contextZoneStartAndEnd[i_context_zone]){//进入context
            }
            else if (pre_pos<contextZoneStartAndEnd[i_context_zone+1] && now_pos>=contextZoneStartAndEnd[i_context_zone+1]){//离开context
                Context_verify("p_enter_reward_context", -1);//保证黑屏时舔水不能给水
                if(context_info_ls[now_context].is_inf){
                    float temp_length_diff = X - lengthRec[1];
                    X = contextZoneStartAndEnd[i_context_zone];
                    lengthRec[1] = X - temp_length_diff;
                    rec_pos=tf.position;
                }
                else{
                    waiting = 1;
                    float temp_length_diff = X - lengthRec[1];
                    X = contextZoneStartAndEnd[i_context_zone+1];//不超过context end
                    lengthRec[1] = X - temp_length_diff;
                    alarm.TrySetAlarm("contextEndWait", context_info_ls[now_context].wait_sec, out _, force:true);//离开context，启动结束等待alarm
                    if(context_info_ls[now_context].wait_sec>0){
                        now_context_success=lick_count_rec[1]>=lick_count_succes_threshold[0] && lick_count_rec[1]<=lick_count_succes_threshold[1];
                        ActivateFullSCreenColor(now_context_success? context_info_ls[now_context].succes_color: context_info_ls[now_context].fail_color);
                    }
                    ui_update.MessageUpdate($"now trial: {now_trial}, now_context: {now_context}, lick status: correct:{lick_count_correct}, rec: before {lick_count_rec[0]} in {lick_count_rec[1]} after {lick_count_rec[2]}\n");
                    Trial_args_init();//所有参数更新为下一个context，并开始等待
                }
            }//进入或离开context

            if (pre_pos<=rewardZoneStartAndEnd[i_context_zone] && now_pos>rewardZoneStartAndEnd[i_context_zone]){//进入奖励区
                reward_remaining = lick_count_max;//本次奖励区可给水次数
                alarm.TrySetAlarm("rewardEnable", extraRewardTimeInSec, out _, force:true);//额外奖励窗口(对齐 Moving.cs)
                string temp_str="p_enter_reward_context=1";
                if(DataSend(temp_str, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
            }
            else if(pre_pos<rewardZoneStartAndEnd[i_context_zone+1] && now_pos>rewardZoneStartAndEnd[i_context_zone+1]){//离开奖励区
                alarm.TrySetAlarm("rewardEnable", extraRewardTimeInSec, out _, force:true);//离开奖励区后仍保留额外奖励窗口
                string temp_str="p_enter_reward_context=-1";
                if(DataSend(temp_str, true)==-1){Debug.LogError("missing variable name: "+temp_str);}
            }//进入或离开reward_zone

            pre_pos=tf.position[0];

            if(serve_water_mode==1){ServeWaterSpeedDepend(m_rb.velocity.magnitude, 2, IsInRewardZone, true);}
            else if(serve_water_mode==2){
                if(lengthRec[0] != -1){
                    lengthRec[2] = X - lengthRec[1];
                }
                else{
                    lengthRec = new float[]{0, X, 0};
                }
                ServeWaterLengthDepend(lengthRec[2], IsInRewardZone, true);
            }
        }
    }

    void OnDestroy()
    {
        StopSerialThread = true;
        manualResetEventVerify.Set();
        try{ if(serialThread != null && serialThread.IsAlive){ serialThread.Join(200); } }catch{}
        SafeCloseSerialPort(ref sp);
        CleanupStreamWriter();
    }
}