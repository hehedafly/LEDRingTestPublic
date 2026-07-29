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
    //public float roll_timer_interval = 0.1f;
    public Rigidbody m_rb;
    public LineChartMultiChannel lineChart; 
    Position_control position_control;
    Command_Converter command_Converter;

    LinearTrackUIUpdate ui_update;
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
    List<byte[]> serial_read_content_ls = new List<byte[]>();//仅在串口线程中改变
    public string[] port_black_list = new string[]{};

    int serialReadContentLsMark = -1;
    readonly object lockObjectMovement = new object();
    float commandVerifyExpireTime = 2;//2s
    ManualResetEvent manualResetEventVerify = new ManualResetEvent(true);
    //readonly object lockObject_command = new object();
    ConcurrentQueue<byte[]> commandQueue = new ConcurrentQueue<byte[]>();
    public ConcurrentDictionary<float, string> commandVerifyDict = new ConcurrentDictionary<float, string>();
    List<string> Arduino_var_list =  "p_enter_reward_context, p_in_reward_context, p_lick_time_accu, p_lick_count, p_start_water, p_lick_mode, p_trial, p_lick_count_max, p_lick_mode0_delay, p_lick_mode1_delay".Replace(" ", "").Split(',').ToList();
    Dictionary<string, string> Arduino_var_map =  new Dictionary<string, string>{};//{"p_...", "0"}, {"p_...", "1"}...
    
    //--------------------------------------file writing-----------------------------------------------
    StreamWriter streamWriter;
    string filePath = "";
    Queue<string> writeQueue = new Queue<string>();
    const int BUFFER_SIZE = 256;
    const int BUFFER_THRESHOLD = 32;
    float[] time_rec_for_log = new float[2]{0, 0};

    string[] ScanPorts_API(){
        string[] portList = SerialPort.GetPortNames();
        return portList;
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
                if(command[13..].StartsWith("lick:")){
                    position_control.Lick_rec(1, command[13..].Contains("correct"));
                    ui_update.LickIndicateUpdate(position_control.RelativeX);
                }else if(command[13..].StartsWith("ws")){
                    position_control.waterServedCount++;
                    ui_update.WaterServeIndicateUpdate(position_control.RelativeX);
                }
                break;
            }
            case 2:{//log
                string command=command_Converter.ConvertToString(_command);
                if(command.StartsWith("log:") && !command.Contains("received")){ui_update.MessageUpdate(command+"\n");}
                Debug.Log($"received :\"{command}\" at {Time.unscaledTime}");
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
                            Debug.Log("verified: "+commandVerifyDict[time]);
                            commandVerifyDict.Remove(time, out _);
                        }
                    }
                }
                Debug.Log($"received :\"{command}\" at {Time.unscaledTime}");
                break;
            }
            default: break;
        }
    }

    void DataReceived(){
        while (true){
            manualResetEventVerify.WaitOne();
            if (sp!= null && sp.IsOpen){
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
                    if(command_Converter.FindMarkOfMessage(true, readBuffer, 0)!=-1){
                        serialReadContentLsMark=serial_read_content_ls.Count()-1;
                    }
                    int temp_end=-1;
                    if(serialReadContentLsMark!=-1){
                        temp_end=command_Converter.FindMarkOfMessage(false, readBuffer, 0);
                        //if(temp_end==-1){serial_read_content_ls.Add(readBuffer);}
                    }

                    if(serialReadContentLsMark!=-1 && temp_end!=-1){
                        byte[] temp_complete_msg;
                        temp_complete_msg = command_Converter.ProcessSerialPortBytes(command_Converter.Read_buffer_concat(serial_read_content_ls, serialReadContentLsMark, -1));
                        //Debug.Log("process: "+string.Join(",", temp_complete_msg));
                        if(temp_complete_msg.Length>0){
                            if (command_Converter.GetCommandType(temp_complete_msg, out _)==0){
                                //Debug.Log(string.Join(",", temp_complete_msg));
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
            else{break;}
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
                        byte[] temp_msg = command_Converter.ConvertToByteArray("value_change:"+temp_command);
                        sp.Write(temp_msg, 0, temp_msg.Length);
                        Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                        return 2;
                    }
                    else{
                        if(Int16.TryParse(temp_var_name, out short temp_id) && temp_id<255){//重发int=int
                            string temp_command=temp_var_name+"="+message.Split('=')[1];
                            byte[] temp_msg = command_Converter.ConvertToByteArray("value_change:"+temp_command);
                            sp.Write(temp_msg, 0, temp_msg.Length);
                            Debug.Log($"Data sent: {"value:"+message}, now time:{Time.unscaledTime}");
                            return 2;
                        }
                        return 0;
                    }
                //}
            }else{
                byte[] temp_msg = command_Converter.ConvertToByteArray("command:"+message);
                sp.Write(temp_msg, 0, temp_msg.Length);
                Debug.Log("Data sent: "+message);
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
        sp.ReadTimeout = 200;
        try{
            int temp_i = 0;//记录已经同步完成的内容
            for(int i=temp_i; i<messages.Count; i++){
                string temp_echo = "error";
                DataSend("test", inVerifyOrVerifyNeedless:true); 
                DataSend(messages[i]+"="+values[i].ToString(), true, inVerifyOrVerifyNeedless:true);
                while(true){
                    temp_echo = sp.ReadLine();
                    Debug.Log("echo received: "+temp_echo);
                    if(temp_echo.StartsWith("echo:")){
                        temp_echo=temp_echo[5..temp_echo.IndexOf(":echo")];
                        temp_i = i+1;
                        break;
                    }
                }
                string temp_aim = Arduino_var_list.FindIndex(str => str==messages[i]).ToString() + "=" + values[i].ToString();
                if(temp_echo.Replace(" ", "")==temp_aim){
                    Debug.Log("verified:"+temp_aim);
                    //ui_update.Message_update("verified:"+temp_aim+"\n");
                    continue;
                }
                manualResetEventVerify.Set();
                return -1;
            }
        }
        catch(Exception e){
            Debug.Log(e.Message);
            manualResetEventVerify.Set();
            if(e.Message.Contains("not open")){
                return -2;
            }
            return -1;
        }
        finally{
            manualResetEventVerify.Set();
        }
        return 1;
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
            FileStream fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, BUFFER_SIZE, true);
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
            float[] temp_context_info=position_control.GetContextInfo();
            // results[0] = now_context;
            // results[1] = X - contextZoneStartAndEnd[now_context*2];
            // results[2] = contextZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
            // results[3] = rewardZoneStartAndEnd[now_context*2] - contextZoneStartAndEnd[now_context*2];
            // results[4] = rewardZoneStartAndEnd[now_context*2+1] - contextZoneStartAndEnd[now_context*2];
            // results[5] = context_info_ls[now_context].is_inf? 1: 0;
            //string temp_time = (time_rec_for_log[1]-time_rec_for_log[0]).ToString(".00");
            string data_write = $@"{time_rec_for_log[1]-time_rec_for_log[0]}
                                \t{position_control.serve_water_mode}
                                \t{position_control.Now_trial}
                                \t{temp_context_info[0]}
                                \t{temp_context_info[1]}
                                \t{_rollingData}
                                \t{m_rb.velocity.magnitude}
                                \t{string.Join("\t",position_control.lick_count_rec)}
                                \t{position_control.lick_count_correct}
                                \t{position_control.waterServedCount}
                                ";
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
        position_control = this.GetComponent<Position_control>();
        command_Converter = new Command_Converter();
        ui_update = GetComponent<LinearTrackUIUpdate>();

        thresholds = ui_update.thresholds;

        for(int i = 0; i<Arduino_var_list.Count; i++){
            Arduino_var_map.Add(Arduino_var_list[i], i.ToString());
        }

        IniReader iniReader = new IniReader(context_Generate.GetConfigPath());

        List<string> portBlackList = new List<string>();
        foreach(string com in iniReader.ReadIniContent("serialSettings", "blackList", "").Split(",")){
            if(!portBlackList.Contains(com)){portBlackList.Add(com);}
        }
        foreach(string port in ScanPorts_API()){
            if(port.Contains("COM") && !portBlackList.Contains(port)){
                try{
                    //Debug.Log(sp.IsOpen);
                    //if (!sp.IsOpen){
                        Debug.Log("try normal");
                        sp = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
                        sp.RtsEnable = true;
                        sp.DtrEnable = true;
                        sp.Open();
                        Debug.Log("COM avaible: "+port);

                        sp.ReadTimeout = 1000;
                        while(true){
                            string temp_readline=sp.ReadLine();
                            //Debug.Log(temp_readline);
                            if(temp_readline=="initialed"){
                                break;
                            }
                            else{
                                continue;
                            }
                        }
                    //}
                }
                catch (Exception e){
                    sp = new SerialPort();
                    Debug.Log(e);
                    ui_update.MessageUpdate(e.Message+"\n");
                    sp.Close();
                    if(e.Message.Contains("拒绝访问")){
                        MessageBoxForUnity.Ensure("Accssion Denied", "Serial Error");
                        Quit();
                    }else{
                        MessageBoxForUnity.Ensure("Can not connect to Arduino, please try another port or use Arduino IDE to Reopen The Serial Communicator", "Serial Error");
                        Quit();
                    }
                }
                finally{
                    Thread thread = new Thread(new ThreadStart(DataReceived));
                    thread.Start();
                    Debug.Log("thread started");
                }
            }
            Debug.Log(port);
        }

        if(sp!= null){
            InitializeStreamWriter();
            string data_write = WriteInfo(write: false);
            writeQueue.Enqueue(data_write);
        }else{
            MessageBoxForUnity.Ensure("No Connection to Arduino!", "Serial Error");
            if(MessageBoxForUnity.YesOrNo("Continue without connection to Arduino?", "Serial Error") == (int)MessageBoxForUnity.MessageBoxReturnValueType.Button_YES){
                DebugWithoutArduino = true;
            }else{
                Quit();
            }
        }
    }

    void Start(){
        lineChart = ui_update.lineChart;
    }

    void Update(){
        
    }

    void FixedUpdate(){
        float forward = (Convert.ToInt32(Input.GetKey(KeyCode.UpArrow))-Convert.ToInt32(Input.GetKey(KeyCode.DownArrow)))*5;
        float rotate = Convert.ToInt32(Input.GetKey(KeyCode.RightArrow))-Convert.ToInt32(Input.GetKey(KeyCode.LeftArrow));

        //if(Math.Abs(rolling_data[0])>thresholds[0] || Math.Abs(rolling_data[1])>thresholds[1] || Math.Abs(rolling_data[2])>thresholds[2]){
        //if(rolling_data[0]>thresholds[0] || rolling_data[1]>thresholds[1]){
        //if(rolling_data[0]>thresholds[0]){
        if(rollingData[0]!=0){
            sensorSmooth = sensorSmooth< 0? sensor_smooth_interval: sensorSmooth;//开始平均，若正在滑动平均则略。
            if(rollingData[0]>thresholds[0]){
                //sensor_smooth_silence = -1;
                rollingTimer[0] = Time.unscaledTime;
            }
            // lock(lockObject_movement){
            //     rolling_data = new int[] {0, 0, 0};
            // }
        }else if(sensorSmooth<= 0){//兼顾了键盘的停止
            rollingTimer[1] = Time.unscaledTime;//不符合运动条件后多长时间停止前进
            if(rollingTimer[1]-rollingTimer[0]>sensor_pause_interval){
                position_control.dic_water_serving_runbegin=0;
                rollingTimer[0] = rollingTimer[1];
                m_rb.velocity = m_rb.transform.forward*0;
                //position_control.LengthRecClear = true;
                //Debug.Log("no movement");
            }
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
                writeQueue.Enqueue(ui_update.log_message.text);
                ProcessWriteQueue();
                CleanupStreamWriter();
                if(sp!= null){
                    sp.Close();
                    Debug.Log("serial closed");
                }
            }
            catch{}
            finally{
                Quit();
            }
        }
    }

    void OnDestroy()
    {
        if(sp!= null && sp.IsOpen){
            //sp.WriteLine("init");
            sp.Close();
        }
        CleanupStreamWriter();
    }
}