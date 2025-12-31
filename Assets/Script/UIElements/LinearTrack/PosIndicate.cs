using System;
using System.Collections;  
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine;  
using UnityEngine.UI;  
  
public class PosIndicate : ImageDrawer  
{  
    public PosIndicate(int _Width, int _Height, Image _image, Position_control _position_control, LinearTrackUIUpdate _ui_update) :base(_Width, _Height, _image){
        width = _Width;
        height = _Height;
        position_control = _position_control;
        ui_update = _ui_update;

        ceil = 40;
        bottom = height - 10;
    }

    private readonly int ceil;
    private readonly int bottom;
    private bool draw_ready = false;
    private bool is_inf = false;
    private int infOffset = 0;//draw相关均为relative_pos - offset
    private float length;
    private float[] rewardZone = new float[]{-1, -1};
    private List<float> lickRecordLs = new List<float>();
    private List<float> waterServeRecordLs = new List<float>();

    private GameObject[] posIndicateLabels;// greenlight, redlight, arrow
    private Text[] posIndicateTexts; // pos, start, end, rew start, rew end
    private LinearTrackUIUpdate ui_update = null;
    private Position_control position_control = null;
    //private Moving_for_test moving_For_Test = null;
    private void DrawClear(bool force_clear = false){
        for (int i = 0; i < pixels.Length; i++)  {  
            pixels[i] = Color.white; // 暂时没用
        }  
        // lineChartTexture.SetPixels(pixels);  
        // lineChartTexture.Apply();
        Apply();
    }

    public new void Init(){
        base.Init();
        posIndicateLabels = ui_update.posIndicateLabels;
        posIndicateTexts = ui_update.posIndicateTexts;

        for(int i = 0; i < posIndicateLabels.Length; i++){
            posIndicateLabels[i].SetActive(false);
        }
        for(int i = 1; i < posIndicateTexts.Length; i++){
            posIndicateTexts[i].text = "";
        }
        
    }

    // public void RecordClear(){
    //     lickRecordLs.Clear();
    //     waterServeRecordLs.Clear();
    // }

    public void DrawPause(){
        draw_ready = false;
    }

    public void PositionIndicateUpdate(float[] _contextInfo){//update all
        DrawClear();

        //context, pos<relative>, length, reward start, reward end, is_inf
        is_inf = _contextInfo[5]!=0;
        length = _contextInfo[2];
        float lengthScale = (height -ceil -10 ) / (is_inf? 100 : _contextInfo[2]);
        DrawLinePublic(width*0.3, ceil, width*0.8, ceil,        Color.black, false);
        DrawLinePublic(width*0.8, ceil, width*0.8, bottom,      Color.black, false);
        DrawLinePublic(width*0.3, bottom, width*0.8, bottom,    Color.black, false);
        ChangeY(posIndicateTexts[1], ceil);
        ChangeY(posIndicateTexts[2], bottom);
        posIndicateTexts[1].text = "0";
        posIndicateTexts[2].text = length.ToString("0");

        if(_contextInfo[3]>=0){
            rewardZone[0] = _contextInfo[3];
            rewardZone[1] = _contextInfo[4];
            int rew_ceil    = (int)(ceil + lengthScale*_contextInfo[3]);
            int rew_bottom  = (int)(ceil + lengthScale*_contextInfo[4]);
            if(rew_bottom > bottom){rew_bottom = bottom;}
            if(rew_ceil > ceil){
                DrawLinePublic(width*0.3, rew_ceil, width*0.8-1, rew_ceil,          new Color(0.5f, 0, 0.5f, 1), false);
                DrawLinePublic(width*0.8-1, rew_ceil+1, width*0.8-1, rew_bottom-1,  new Color(0.5f, 0, 0.5f, 1), false);
            }
            if(rew_bottom < bottom){
                DrawLinePublic(width*0.3, rew_bottom, width*0.8-1, rew_bottom,      new Color(0.5f, 0, 0.5f, 1), false);
            }
            ChangeY(posIndicateTexts[3], ceil + Math.Max(15, rew_ceil - ceil));
            ChangeY(posIndicateTexts[4], bottom - Math.Max(15, bottom - rew_bottom));
            posIndicateTexts[3].text = _contextInfo[3].ToString("0");
            posIndicateTexts[4].text = _contextInfo[4].ToString("0");
        }

        Apply();
        posIndicateLabels[2].SetActive(true);
        ChangeY(posIndicateLabels[2], ceil);
        draw_ready = true;

        // int temp_width, temp_height;
        // temp_width = (int)posIndicateImage.GetComponent<RectTransform>().rect.width;
        // temp_height = (int)posIndicateImage.GetComponent<RectTransform>().rect.height;
        // posIndicateTex = new Texture2D(temp_width, temp_height);
        // posIndicatePixels = new Color[temp_width * temp_height]; 
        // Sprite sprite = Sprite.Create(posIndicateTex, new Rect(0, 0, temp_width, temp_height), new Vector2(0.5f, 0.5f));
        // posIndicateImage.sprite=sprite;
        
    }

    public void PositionIndicateUpdateINF(float relative_pos){//update all in inf condition

        DrawClear();

        //context, pos<relative>, length, reward start, reward end, is_inf
        float lengthScale = (height -ceil -10 ) / 100;
        DrawLinePublic(width*0.3, ceil, width*0.8, ceil,        Color.black, false);
        DrawLinePublic(width*0.8, ceil, width*0.8, bottom,      Color.black, false);
        DrawLinePublic(width*0.3, bottom, width*0.8, bottom,    Color.black, false);
        ChangeY(posIndicateTexts[1], ceil);
        ChangeY(posIndicateTexts[2], bottom);
        posIndicateTexts[1].text = infOffset.ToString();
        posIndicateTexts[2].text = (infOffset + 100).ToString("0");

        if(rewardZone[0] >= 0){
            int rew_ceil    = (int)(ceil + lengthScale*(rewardZone[0] - infOffset));
            int rew_bottom  = (int)(ceil + lengthScale*(rewardZone[1] - infOffset));
            if(rew_bottom > bottom){rew_bottom = bottom;}
            if(rew_ceil > ceil){
                DrawLinePublic(width*0.3, rew_ceil, width*0.8-1, rew_ceil,          new Color(0.5f, 0, 0.5f, 1), false);
                DrawLinePublic(width*0.8-1, rew_ceil+1, width*0.8-1, rew_bottom-1,  new Color(0.5f, 0, 0.5f, 1), false);
            }
            if(rew_bottom < ceil){

            }else if(rew_bottom < bottom){
                DrawLinePublic(width*0.3, rew_bottom, width*0.8-1, rew_bottom,      new Color(0.5f, 0, 0.5f, 1), false);
            }
            ChangeY(posIndicateTexts[3], ceil + Math.Max(15, rew_ceil - ceil));
            ChangeY(posIndicateTexts[4], bottom - Math.Max(15, bottom - (rew_bottom > 0? rew_bottom: (bottom - 15))));
            posIndicateTexts[3].text = rewardZone[0].ToString("0");
            posIndicateTexts[4].text = rewardZone[1].ToString("0");
        }

        LickIndicate(lickRecordLs);
        WaterServeIndicate(waterServeRecordLs);

        Apply();
        posIndicateLabels[2].SetActive(true);
        ChangeY(posIndicateLabels[2], relative_pos - infOffset);
        draw_ready = true;

        float[] temp_info = position_control.GetContextInfo();
        //  0           1           2           3           4           5       6,7,8
        //context, pos<relative>, length, reward start, reward end, is_inf, lick_count_rec
        ui_update.MessageUpdate($"now pos: {temp_info[1]:F3}, lick status: rec: before {temp_info[6]} in {temp_info[7]} after {temp_info[8]}\n");

        // int temp_width, temp_height;
        // temp_width = (int)posIndicateImage.GetComponent<RectTransform>().rect.width;
        // temp_height = (int)posIndicateImage.GetComponent<RectTransform>().rect.height;
        // posIndicateTex = new Texture2D(temp_width, temp_height);
        // posIndicatePixels = new Color[temp_width * temp_height]; 
        // Sprite sprite = Sprite.Create(posIndicateTex, new Rect(0, 0, temp_width, temp_height), new Vector2(0.5f, 0.5f));
        // posIndicateImage.sprite=sprite;
        
    }

    public void PositionIndicateUpdate(float relative_pos){//update position only
        if(draw_ready){
            //Debug.Log(infOffset);
            infOffset = is_inf? infOffset : 0;
            float lengthScale = (height -ceil -10 ) / (is_inf? 100 : length);
            float draw_pos = relative_pos - (infOffset > 0? infOffset: 0);
            //relative_pos += infOffset >0 ? infOffset+50 : 0;//一旦存在offset，绘制内容将维持在50-100的区间内

            posIndicateTexts[0].text = $"now:\n{relative_pos :F3}";
            ChangeY(posIndicateLabels[2], ceil + draw_pos*lengthScale);
            bool inRewardZone = rewardZone[0] != -1 && relative_pos >= rewardZone[0] && relative_pos < rewardZone[1];
            if(inRewardZone){
                if(!posIndicateLabels[0].activeInHierarchy){posIndicateLabels[0].SetActive(true);}
                if(posIndicateLabels[1].activeInHierarchy){posIndicateLabels[1].SetActive(false);}
            }else{
                if(posIndicateLabels[0].activeInHierarchy){posIndicateLabels[0].SetActive(false);}
                if(!posIndicateLabels[1].activeInHierarchy){posIndicateLabels[1].SetActive(true);}
            }

            if(draw_pos >= 100){
                infOffset += 50;
                PositionIndicateUpdateINF(relative_pos);
            }
        }
    }

    public void LickIndicate(float relative_pos){
        if(draw_ready){
            //lickRecordLs.Append(relative_pos + (is_inf? infOffset : 0));
            lickRecordLs.Add(relative_pos);
            float lengthScale = (height -ceil -10 ) / (is_inf? 100 : length);
            int temp_y = (int)(ceil + (relative_pos - infOffset)*lengthScale);
            DrawLinePublic(width*0.2, temp_y, width*0.45, temp_y, Color.red);
        }
    }

    public void LickIndicate(List<float> relative_pos){
        if(draw_ready){
            float lengthScale = (height -ceil -10 ) / (is_inf? 100 : length);
            foreach (float pos in relative_pos){
                int temp_y = (int)(ceil + (pos - infOffset)*lengthScale);
                if(temp_y < ceil || temp_y > bottom){continue;}
                DrawLinePublic(width*0.2, temp_y, width*0.45, temp_y, Color.red);
            }
        }
    }

    public void WaterServeIndicate(float relative_pos){
        if(draw_ready){
            waterServeRecordLs.Add(relative_pos);
            float lengthScale = (height -ceil -10 ) / (is_inf? 100 : length);
            int temp_y = (int)(ceil + (relative_pos - infOffset)*lengthScale);
            DrawLinePublic(width*0.45, temp_y, width*0.7, temp_y, Color.blue);
        }
    }

    public void WaterServeIndicate(List<float> relative_pos){
        if(draw_ready){
            float lengthScale = (height -ceil -10 ) / (is_inf? 100 : length);
            foreach (float pos in relative_pos){
                int temp_y = (int)(ceil + (pos - infOffset)*lengthScale);
                if(temp_y < ceil || temp_y > bottom){continue;}
                DrawLinePublic(width*0.45, temp_y, width*0.7, temp_y, Color.blue);
            }
        }
    }

    private void ChangeY(GameObject go, float _y){
        Transform tf;
        if(go.TryGetComponent<Transform>(out tf)){
            tf.localPosition = new Vector3(tf.localPosition.x, -1 * _y, tf.localPosition.z);
            return;
        }
    }
    private void ChangeY(Text go, float _y){
        Transform tf;
        if(go.TryGetComponent<Transform>(out tf)){
            tf.localPosition = new Vector3(tf.localPosition.x, -1 * _y, tf.localPosition.z);
            return;
        }
    }

    void Start()  
    {  

    }  

    void Update(){

    } 
}