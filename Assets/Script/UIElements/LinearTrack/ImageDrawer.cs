using System;
using System.Collections;  
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using UnityEngine;  
using UnityEngine.UI;  
  
public class ImageDrawer  
{  
    public int width;
    public int height;
    public Image image;
    protected Texture2D imageTexture; // 用于绘制折线图的Texture  
    protected Color[] pixels; // Texture2D的像素数据  

    public ImageDrawer(int _width, int _height, Image _image){
        this.width = _width;
        this.height = _height;
        this.image = _image;
    }
 
    protected void DrawClear(){
        for (int i = 0; i < pixels.Length; i++)  {  
            pixels[i] = Color.white; // 暂时没用
        }  
        imageTexture.SetPixels(pixels);  
        imageTexture.Apply();
    }

    public int DrawLinePublic(int x0, int y0, int x1, int y1, Color color, bool _apply = true){
        int v = DrawLine(x0, y0, x1, y1, color);
        if (_apply){Apply();}
        return v;
    }  

    public int DrawLinePublic(double x0, double y0, double x1, double y1, Color color, bool _apply = true){
        int v = DrawLine((int)x0, (int)y0, (int)x1, (int)y1, color);
        if (_apply){Apply();}
        return v;
    }

    protected int DrawLine(int x0, int y0, int x1, int y1, Color color)  {  
        int dx = Mathf.Abs(x1 - x0);  
        int sx = x0 < x1 ? 1 : -1;  
        int dy = -Mathf.Abs(y1 - y0);  
        int sy = y0 < y1 ? 1 : -1;  
        int err = dx + dy;  

        if(y0 * width + x0 >= pixels.Length || y0 * width + x0 <0 || y1 * width + x1 >= pixels.Length || y1 * width + x1<0){
            Debug.LogError($"wrong position: {x0}, {y0} to {x1}, {y1}");
            return -1;
        }
  
        while (true){  
            
            pixels[y0 * width + x0] = color;  
            if (x0 == x1 && y0 == y1) break;  
            int e2 = 2 * err;  
            if (e2 >= dy){  
                err += dy;  
                x0 += sx;  
            }  
            if (e2 <= dx){  
                err += dx;  
                y0 += sy;  
            }  
        }

        return 1;  
    }

    public void Init() {

        imageTexture = new Texture2D(width, height);  
        pixels = new Color[width * height]; 
        Sprite sprite = Sprite.Create(imageTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        image.sprite=sprite;

        for (int i = 0; i < pixels.Length; i++)  {  
            pixels[i] = Color.white; 
        }  
  
        Apply();
    }

    protected void Apply(){
        imageTexture.SetPixels(pixels);  
        imageTexture.Apply(); 
    }
    void Start()  
    {  
        // 填充背景色
    }  

    void Update()
    {

    } 
}