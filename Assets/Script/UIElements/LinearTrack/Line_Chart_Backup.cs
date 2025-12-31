// using System;
// using System.Collections;  
// using System.Collections.Generic;
// using System.Runtime.InteropServices;
// using System.Linq;
// using UnityEngine;  
// using UnityEngine.UI;  
  
// public class Line_Chart_Backup : MonoBehaviour  
// {  
//     public Image lineChartImage; // 折线图背景Image  
//     public int lineChartWidth = 400; // 折线图的宽度  
//     public int lineChartHeight = 300; // 折线图的高度 
//     public List<float[]> dataValueMax;
//     //public int margin = 20; // 图表边距  
//     // public List<string> variableNames = new List<string>(){"dx", "dy", "dz"}; // 变量名称列表  
//     public List<string> variableNames = new List<string>(){"dx"}; // 变量名称列表  
//     private int data_count_max;
//     private List<List<float>> data; // 数据列表，每个列表对应一个变量的数据  
//     private int[] reference_line;
//     private int[] baseline;
//     private Texture2D lineChartTexture; // 用于绘制折线图的Texture  
//     private Color fade_color = new Color(0.25f, 0.25f, 0.25f, 1);
//     private Color[] pixels; // Texture2D的像素数据  
//     private List<Color> line_colors = new List<Color>(){Color.red, Color.green, Color.blue};
//     private LinearTrackMoving moving = null;
//     private LinearTrackMoving_for_test moving_For_Test = null;
//     private void Draw_clear(bool force_clear = false){
//         for (int i = 0; i < pixels.Length; i++)  {  
//             pixels[i] = Color.white; // 暂时没用
//         }  
//         lineChartTexture.SetPixels(pixels);  
//         lineChartTexture.Apply();

//         if(force_clear){
//             reference_line= new int[variableNames.Count];
//             data=new List<List<float>>(){};
//             dataValueMax = new List<float[]>{};
//             baseline = new int[variableNames.Count];

//             for(int i=0; i<variableNames.Count; i++){
//                 List<float> temp_empty_list=new List<float>{}; data.Add(temp_empty_list);
//                 dataValueMax.Add(new float[]{200, -20});
//             }

//             for(int i=0; i<data.Count; i++){
//                 int data_value_max_diff = (int)(dataValueMax[i][0]-dataValueMax[i][1]);
//                 baseline[i] = i* (lineChartHeight / data.Count) + (int)(lineChartHeight / data.Count * dataValueMax[i][1]/data_value_max_diff);
//             }
//             DrawChart(true);
//         }
//     }
//     private void DrawLine(int x0, int y0, int x1, int y1, Color color)  {  
//         int dx = Mathf.Abs(x1 - x0);  
//         int sx = x0 < x1 ? 1 : -1;  
//         int dy = -Mathf.Abs(y1 - y0);  
//         int sy = y0 < y1 ? 1 : -1;  
//         int err = dx + dy;  
  
//         while (true){  
//             pixels[y0 * lineChartWidth + x0] = color;  
  
//             if (x0 == x1 && y0 == y1) break;  
//             int e2 = 2 * err;  
//             if (e2 >= dy){  
//                 err += dy;  
//                 x0 += sx;  
//             }  
//             if (e2 <= dx){  
//                 err += dx;  
//                 y0 += sy;  
//             }  
//         }  
//     }
//     private void DrawChart(bool draw_all, int _start=1, int _end=0, bool is_color_fade = false){//x从左到右，y从上到下
//         float scaleX = lineChartWidth / (data_count_max - 1);  

//         if(draw_all){
//             for(int i=0; i<data.Count; i++){
//                 int data_value_max_diff = (int)(dataValueMax[i][0]-dataValueMax[i][1]);
//                 baseline[i] = i* (lineChartHeight / data.Count) + (int)(lineChartHeight / data.Count * dataValueMax[i][0]/data_value_max_diff);
//             }

//             _end=data[0].Count-1;
//             Draw_clear();
//             for(int i=0; i<data.Count; i++){
//                 int data_value_max_diff = (int)(dataValueMax[i][0]-dataValueMax[i][1]);
//                 float scaleY = lineChartHeight / data.Count *0.9f / data_value_max_diff; // 根据数值范围缩放Y轴    0.9：绘制范围为各通道实际高度*0.9
//                 int ref_height = baseline[i] - (int)(reference_line[i] * scaleY);//reference line
//                 DrawLine(0, baseline[i], lineChartWidth, baseline[i], Color.black);
//                 DrawLine(0, ref_height, lineChartWidth, ref_height, Color.yellow);
//                 if(dataValueMax[i][0]>moving.maxRolling_value){
//                     int max_height = baseline[i] - (int)(moving.maxRolling_value * scaleY);//maxline
//                     DrawLine(0, max_height, lineChartWidth, max_height, new Color(0.5f, 0, 0.5f, 1));
//                 }
//                 lineChartTexture.SetPixels(pixels);  
//                 lineChartTexture.Apply();  
//             }
//         }

//         if(_start>=_end || data[0].Count==0){return;}
        
  
//         for (int i = 0; i < data.Count; i++)  { 
//             int data_value_max_diff = (int)(dataValueMax[i][0]-dataValueMax[i][1]);
//             float scaleY = lineChartHeight / data.Count *0.9f / data_value_max_diff; // 根据数值范围缩放Y轴     0.9：绘制范围为各通道实际高度*0.9
//             for (int j = _start; j < _end; j++){
//                 int prevX = (int)((j - 1) * scaleX);  
//                 int prevY = (int)(baseline[i] - data[i][j - 1] * scaleY);
//                 int currX = (int)(j * scaleX);  
//                 int currY = (int)(baseline[i] - data[i][j]     * scaleY);

//                 if(prevY>=lineChartHeight || currY>=lineChartHeight){
//                     Debug.Log("error!");
//                 }
  
//                 if(!is_color_fade){DrawLine(prevX, prevY, currX, currY, line_colors[i]);}
//                 else{DrawLine(prevX, prevY, currX, currY, fade_color);}
//             }  
//         }  
  
//         // 应用更改到Texture2D  
//         lineChartTexture.SetPixels(pixels);  
//         lineChartTexture.Apply();  
//     }

//     public void Reference_line_Update(int[] new_reference_line){
//         if(!Enumerable.SequenceEqual(new_reference_line, reference_line)){
//             Array.Copy(new_reference_line, reference_line, reference_line.Length);
//             for(int i=0; i<data.Count; i++){
//                 dataValueMax[i][0]=Math.Max(dataValueMax[i][0], reference_line[i]);
//                 dataValueMax[i][1]=Math.Min(dataValueMax[i][1], reference_line[i]);
//             }
//             DrawChart(true);
//         }
//     }

//     public void AddDataPoint(List<float> newDataPoint, bool Draw=true)  
//     {  
//         // 确保数据点数量和变量名称匹配  
//         if (newDataPoint.Count < variableNames.Count)  {  
//             Debug.LogError("数据点数量和变量名称不匹配！");  
//             return;  
//         }  

//         bool temp_clear_all=false;

//         for (int i = 0; i < data.Count; i++){//一行绘制满，重绘
//             data[i].Add(newDataPoint[i]);  
//             if (data[i].Count > data_count_max){  
//                 temp_clear_all=true;
//             }  
//         }

//         bool temp_draw_all=false;
//         for (int i = 0; i < data.Count; i++){//最大值更新，全部重绘
//             //Debug.Log($"{i}: added: {newDataPoint[i]} " + $"now data value max: {data_value_max[i][0]}, {data_value_max[i][1]}");
//             if(newDataPoint[i]>dataValueMax[i][0]){
//                 dataValueMax[i][0]=newDataPoint[i]; 
//                 temp_draw_all=true;
//             }else if(newDataPoint[i]<dataValueMax[i][1]){
//                 dataValueMax[i][1]=newDataPoint[i]; 
//                 temp_draw_all=true;
//             }
//         }
//         if(temp_draw_all){DrawChart(true);}

//         if(temp_clear_all){//清空重绘
//             for (int i = 0; i < data.Count; i++){
//                 dataValueMax[i] = new float[]{data[i].Max(), data[i].Min()};//记录当前最大最小，后续以fade形式绘制当前内容
//                 Reference_line_Update(reference_line);
//             }
//             DrawChart(true, is_color_fade:true);
//             for (int i = 0; i < data.Count; i++){
//                 List<float> newList = new List<float>();
//                 newList.Add(data[i][data[i].Count-1]);//留下最后一个值以供连续绘制
//                 data[i] = newList;
//             }
//         }

//         if(Draw && data[0].Count>1){DrawChart(false, data[0].Count-1, data[0].Count);}//单点绘制
//         //if(reDraw && data[0].Count>1){DrawChart(true);}
//     } 

//     void Awake() {
//         moving=GetComponent<LinearTrackMoving>();
//         moving_For_Test=GetComponent<LinearTrackMoving_for_test>();

//         data_count_max= lineChartWidth / 2;
//         // 初始化Texture2D和像素数据  
//         lineChartTexture = new Texture2D(lineChartWidth, lineChartHeight);  
//         pixels = new Color[lineChartWidth * lineChartHeight]; 
//         Sprite sprite = Sprite.Create(lineChartTexture, new Rect(0, 0, lineChartWidth, lineChartHeight), new Vector2(0.5f, 0.5f));
//         lineChartImage.sprite=sprite;

//         reference_line= new int[variableNames.Count];
//         data=new List<List<float>>(){};
//         dataValueMax = new List<float[]>{};
//         baseline = new int[variableNames.Count];

//         for(int i=0; i<variableNames.Count; i++){
//             List<float> temp_empty_list=new List<float>{}; data.Add(temp_empty_list);
//             dataValueMax.Add(new float[]{200, -20});
//         }

//         for(int i=0; i<data.Count; i++){
//             int data_value_max_diff = (int)(dataValueMax[i][0]-dataValueMax[i][1]);
//             baseline[i] = (i+1)* (lineChartHeight / data.Count) + (int)(lineChartHeight / data.Count * dataValueMax[i][1]/data_value_max_diff);
//         }
//     }

//     void Start()  
//     {  
//         // 填充背景色
//         for (int i = 0; i < pixels.Length; i++)  
//         {  
//             pixels[i] = Color.white; 
//         }  
  
//         lineChartTexture.SetPixels(pixels);  
//         lineChartTexture.Apply(); 

//         if(moving_For_Test!=null){Reference_line_Update(moving_For_Test.thresholds);}
//         else{Reference_line_Update(moving.thresholds);}
//         //DrawChart(true);
//     }  

//     void Update(){
//         // if(UnityEngine.Random.Range(0, 20)==1){
//         //     AddDataPoint(new List<float>(){(float)UnityEngine.Random.Range(-200, 300), (float)UnityEngine.Random.Range(-100, 100), (float)UnityEngine.Random.Range(-100, 100)});
//         // }
//     } 
// }