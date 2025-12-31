// using System;
// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System.Threading;
// using System.IO.Ports;
// using System.Text;
// using System.Linq;

// public class Moving_for_test : MonoBehaviour
// {   
//     public int[] thresholds=new int[]{50, 50, 50};
//     public int[] thresholds_rec=new int[]{50, 50, 50};
//     public int sensor_smooth_interval=5;
//     public float roll_pause_interval=0.5f; //0.5s
//     public float roll_timer_interval=0.1f;
//     public Material activeMaterial;    // 激活状态的材质  
//     public Material inactiveMaterial;  // 非激活状态的材质
//     public Material stopMaterial;  // 非激活状态的材质
//     public Line_Chart lineChart;  
//     private SerialPort sp;
//     //private int dx, dy, dz;
//     private int sensor_now;
//     private int sensor_smooth;
//     private int sensor_smooth_silence=-1;
//     private float[] rolling_timer;//{sensor_recording, sensor_rec_start, interval_mean}
//     private int[] rolling_data;
//     //private int serial_receive_num;
//     private readonly object lockObject_movement = new object();
//     private readonly object lockObject_count = new object();
//     private GameObject arrowforward;  
//     private GameObject arrowbackward;  
//     private GameObject arrowLeft;  
//     private GameObject arrowRight;  
//     private Material activeMaterialInstance;  
//     private Material inactiveMaterialInstance;  
//     private Material stopMaterialInstance;

//     private string[] ScanPorts_API(){
//         string[] portList = SerialPort.GetPortNames();
//         return portList;
//     }
    
//     private void DataProcessing(string data){
//         //lock(lockObject_count){serial_receive_num++;}
//         //Debug.Log(data);
//         // if (data.Contains("context_info:") || data.Contains("received:")){//"context_info:success/fail"
//         //     lock(lockObject_command){
//         //         command_queue.Enqueue(data);
//         //     }
//             //Debug.Log(temp_data);
//         //}
//         if(data.StartsWith("S") && data.Length==8){
//             int dx=0, dy=0, dz=0;
//             lock(lockObject_movement){
//                 for (int i = 0; i < data.Length; i++){         
//                     //Debug.Log(data);
//                     if (data[i]=='S'){
//                         sensor_now=Int32.Parse(data[i+1].ToString());
//                     }
//                     else if (data[i]=='X'){
//                         dx=Int32.Parse(data[i+1].ToString())-5;
//                         //Debug.Log(Int32.Parse(data[i].ToString()));
//                     }
//                     else if (data[i]=='Y'){
//                         dy=Int32.Parse(data[i+1].ToString())-5;
//                         //Debug.Log(Int32.Parse(data[i].ToString()));
//                     }
//                     else if (data[i]=='Z'){
//                         dz=Int32.Parse(data[i+1].ToString())-5;
//                         //Debug.Log(Int32.Parse(data[i].ToString()));
//                     }
//                 }
//             }
//             rolling_data[0]-=dx;//传感器安装方向
//             rolling_data[1]+=dy;
//             rolling_data[2]+=dz;            
//             //sb.AppendFormat(data[i]);
//             //Debug.Log("dx="+dx.ToString()+"; dy="+dy.ToString()+"; dz="+dz.ToString());
//         }
//         //Debug.Log(sb.ToString());
//     }

//     private void DataReceived(){
//         while (true){
//             if (sp.IsOpen){
//                 int count = sp.BytesToRead;
//                 if (count > 0){
//                     //Debug.Log("thread read content");
//                     byte[] readBuffer = new byte[count];
//                     try{
//                         sp.Read(readBuffer, 0, count);
//                         string data=System.Text.Encoding.UTF8.GetString(readBuffer);
//                         foreach(string temp_data in data.Split('\n')){
//                             DataProcessing(temp_data.Replace("\r", ""));
//                         }
//                     }
//                     catch (Exception ex){
//                         Debug.Log(ex.Message);
//                     }
//                 }
//             }
//             Thread.Sleep(1);
//             // dx=0; dy=0;
//         }
//     }

//     private void UpdateArrow(GameObject arrow, int isActive)//0:inactive, 1:active, 2:stoped
//     {  
//         if (arrow != null)  
//         {  
//             // 使用预先实例化的材质 
//             if(isActive==2){arrow.GetComponent<Renderer>().material = stopMaterialInstance;} 
//             else{arrow.GetComponent<Renderer>().material = isActive==1 ? activeMaterialInstance : inactiveMaterialInstance;}
//         }  
//     }  
  
//     private void SetAllArrowsInactive(bool stop=false)  
//     {  
//         if(!stop){
//             UpdateArrow(arrowforward,   0);  
//             UpdateArrow(arrowbackward,  0);  
//             UpdateArrow(arrowLeft,      0);  
//             UpdateArrow(arrowRight,     0);
//         }else{
//             UpdateArrow(arrowforward,   2);  
//             UpdateArrow(arrowbackward,  2);  
//             UpdateArrow(arrowLeft,      2);  
//             UpdateArrow(arrowRight,     2);
//         }
//     }  

//     void Start(){
//         arrowforward = GameObject.Find("arrow_forward");  
//         arrowbackward = GameObject.Find("arrow_backward");  
//         arrowLeft = GameObject.Find("arrow_left");  
//         arrowRight = GameObject.Find("arrow_right");  
  
//         // 检查是否找到了游戏对象  
//         if (!arrowforward || !arrowbackward || !arrowLeft || !arrowRight)  
//         {  
//             Debug.LogError("One or more arrow objects were not found in the scene!");  
//             return;  
//         }  
  
//         // 实例化材质  
//         activeMaterialInstance = Instantiate(activeMaterial);  
//         inactiveMaterialInstance = Instantiate(inactiveMaterial); 
//         stopMaterialInstance = Instantiate(stopMaterial); 
  
//         // 初始时将所有箭头设置为非激活状态  
//         SetAllArrowsInactive();  

//         //dx=0;dy=0;
//         //serial_receive_num=0;
//         sensor_smooth=sensor_smooth_interval;//=0时结算一次
//         rolling_timer = new float[] {Time.time, Time.time, Time.time};
//         rolling_data = new int[] {0, 0, 0};
//         //Debug.Log("nowtime"+rolling.ToString());
//         // m_rb = GetComponent<Rigidbody>();
//         foreach(string port in ScanPorts_API()){
//             if(port.Contains("COM")){
//                 try{
//                     //Debug.Log(sp.IsOpen);
//                     //if (!sp.IsOpen){
//                     Debug.Log("try normal");
//                     sp = new SerialPort(port, 115200, Parity.None, 8, StopBits.One);
//                     sp.Open();
//                     Debug.Log("COM avaible: "+port);
//                     Thread thread = new Thread(new ThreadStart(DataReceived));
//                     thread.Start();
//                     Debug.Log("thread started");
//                     break;
//                     //}
//                 }
//                 catch (Exception e){
//                     sp = new SerialPort();
//                     Debug.Log(e);
//                     sp.Close();
//                     #if UNITY_EDITOR
//                         UnityEditor.EditorApplication.isPlaying = false;
//                     #else
//                         Application.Quit();
//                     #endif
//                 } 
//             }
//             Debug.Log(port);
//         }
//     }

//     void Update(){
//         // if(Input.GetKeyDown(KeyCode.Alpha0)){sp.WriteLine("0=0");}
//         // else if(Input.GetKeyDown(KeyCode.Alpha1)){sp.WriteLine("0=1");}
//         // else if(Input.GetKeyDown(KeyCode.Alpha2)){sp.WriteLine("0=2");}

//         if(!Enumerable.SequenceEqual(thresholds_rec, thresholds)){
//             Array.Copy(thresholds, thresholds_rec, thresholds.Length);
//             lineChart.Reference_line_Update(thresholds);  
//         }
//     }

//     void FixedUpdate(){
//         int forward=Convert.ToInt32(Input.GetKey(KeyCode.UpArrow))-Convert.ToInt32(Input.GetKey(KeyCode.DownArrow));
//         int rotate=Convert.ToInt32(Input.GetKey(KeyCode.RightArrow))-Convert.ToInt32(Input.GetKey(KeyCode.LeftArrow));
        
//         if(Math.Abs(rolling_data[0])>thresholds[0] || Math.Abs(rolling_data[1])>thresholds[1] || Math.Abs(rolling_data[2])>thresholds[2]){
//             sensor_smooth=sensor_smooth_interval<=0? sensor_smooth_interval: sensor_smooth;//开始平均，若正在滑动平均则略。
//             sensor_smooth_silence=-1;
//             rolling_timer[0]=Time.time;
//             lock(lockObject_movement){
//                 rolling_data = new int[] {0, 0, 0};
//             }
//         }else if(sensor_smooth<=0){
//             sensor_smooth_silence=sensor_smooth;
//             lineChart.AddDataPoint(new List<float>(){ rolling_data[0], rolling_data[1], rolling_data[2]});
//             rolling_timer[1]=Time.time;
//             if(rolling_timer[1]-rolling_timer[0]>roll_pause_interval){
//                 rolling_timer[0]=rolling_timer[1];
//                 SetAllArrowsInactive(true);
//                 //Debug.Log("no movement");
//             }
//         }
//         if(sensor_smooth_silence==0 && sensor_smooth<0){
//             sensor_smooth_silence=sensor_smooth_interval;
//             lineChart.AddDataPoint(new List<float>(){ rolling_data[0], rolling_data[1], rolling_data[2]});
//             lock(lockObject_movement){
//                 rolling_data = new int[] {0, 0, 0};
//             }
//         }
//         else if(sensor_smooth_silence>=0){sensor_smooth_silence-=1;}

//         if(sensor_smooth==0){
//             //Debug.Log("rolling_data:"+rolling_data[0].ToString()+","+rolling_data[1].ToString()+","+rolling_data[2].ToString());
//             lineChart.AddDataPoint(new List<float>(){ rolling_data[0], rolling_data[1], rolling_data[2] });
//             forward+=(Math.Abs(rolling_data[0])>thresholds[0] ? 1:0) * (rolling_data[0]>0? 1: -1) * 10;
//             rotate+=(Math.Abs(rolling_data[1])>thresholds[1]? 1:0) * (rolling_data[1]>0? 1: -1) * 1;

//             UpdateArrow(arrowforward,   forward > 0? 1: 0);  
//             UpdateArrow(arrowbackward,  forward < 0? 1: 0);  
//             UpdateArrow(arrowRight,     rotate > 0? 1: 0);  
//             UpdateArrow(arrowLeft,      rotate < 0? 1: 0);
//             // }
//             lock(lockObject_movement){
//                 rolling_data = new int[] {0, 0, 0};
//             }

//         }if(sensor_smooth>=0){sensor_smooth-=1;}

//         if (Input.GetKey(KeyCode.Space)){
//             try{
//                 sp.Close();
//                 Debug.Log("serial closed");
//                 #if UNITY_EDITOR
//                     UnityEditor.EditorApplication.isPlaying = false;
//                 #else
//                     Application.Quit();
//                 #endif
//             }
//             catch{}
//         }
//         else if(Input.GetKeyDown(KeyCode.Alpha0)){sp.WriteLine("0=0");}
//         else if(Input.GetKeyDown(KeyCode.Alpha1)){
//             sp.WriteLine("0=1");
//         }
//         else if(Input.GetKeyDown(KeyCode.Alpha2)){sp.WriteLine("0=2");}
//     }

//     void /// <summary>
//     /// This function is called when the MonoBehaviour will be destroyed.
//     /// </summary>
//     OnDestroy()
//     {
//         sp.Close();
//     }
// }