using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;

namespace MouseDraw{
public class MouseDrawer
{
    Image targetImage;
    Material targteMaterial;
    Shader historyShader;
    Material historyMaterial;
    int width;
    int height;
    private Texture2D baseLayer; // 初始图形层
    private Texture2D tempLayer; // 临时图形层
    private Texture2D trailLayer; // 拖尾层
    private Texture2D historyLayer; // 轨迹层
    public RenderTexture targetTexture;
    private Queue<Vector2> trailPoints = new Queue<Vector2>();
    private int maxTrailLength;
    private int shaderMaxTrialLength = 50;
    // private Color trailStartColor = Color.black;
    // private Color trailEndColor = new Color(1, 0.8f, 0.8f); // 淡粉色
    private Color historyColor = new Color(1, 0.8f, 0.8f, 0.05f);
    
    // Shader属性
    private int _ThicknessID;
    private int _PointCountID;

    // 初始化画布 [需求2]
    public MouseDrawer(Image image, Shader mouseDrawerTrailShader, float trailThick, int trailLength)
    {
        targetImage = image;
        targteMaterial = targetImage.material;
        maxTrailLength = Math.Clamp(trailLength, 0, shaderMaxTrialLength);
        historyShader = mouseDrawerTrailShader;
        historyMaterial = new Material(historyShader);
        width   = (int)image.GetComponent<RectTransform>().rect.width;
        height  = (int)image.GetComponent<RectTransform>().rect.height;
        baseLayer = CreateClearTexture(Color.white);
        tempLayer = CreateClearTexture(Color.clear);
        trailLayer= CreateClearTexture(Color.clear);
        historyLayer = CreateClearTexture(Color.clear);
           
        // 获取材质引用
        _ThicknessID = Shader.PropertyToID("_Thickness");
        _PointCountID = Shader.PropertyToID("_PointCount");

        targteMaterial.SetFloat(_ThicknessID, trailThick);
        targteMaterial.SetInt(_PointCountID, 0);
        
        UpdateDisplayTexture();
    }

    public void Init(){
        baseLayer = CreateClearTexture(Color.white);
        tempLayer = CreateClearTexture(Color.clear);
        trailLayer= CreateClearTexture(Color.clear);
        historyLayer = CreateClearTexture(Color.clear);
        historyMaterial = new Material(historyShader);

        UpdateDisplayTexture();
    }

    #region 基础工具方法
    // 创建空白纹理
    private Texture2D CreateClearTexture(Color color)
    {
        Texture2D tex = new Texture2D(width, height);
        ClearLayer(tex, color);
        return tex;
    }

    public void ClearLayer(Texture2D tex, Color color){
        Color[] pixels = tex.GetPixels();
        // for (int i = 0; i < pixels.Length; i++)
        // {
        //     pixels[i] = color;
        // }
        Array.Fill(pixels, color);
        tex.SetPixels(pixels);
        tex.Apply();
    }

    public void ClearTempLayer(){
        ClearLayer(tempLayer, Color.clear);
    }

    // 检查坐标有效性 [需求1]
    private bool IsPointInCanvas(Vector2 point)
    {
        return point.x >= 0 && point.x < width && point.y >= 0 && point.y < height;
    }

    // 更新显示纹理 [需求2]
    public void UpdateDisplayTexture()
    {
        targteMaterial.SetTexture("_Layer1", historyLayer   );
        targteMaterial.SetTexture("_Layer2", tempLayer      );
        targteMaterial.SetTexture("_Layer3", baseLayer      );
    }
    #endregion

    #region 初始图形绘制
    
    void DrawArea(Color[] texPixels, int[] area, int markTypeCount = 32){
        Color color = area[0] < 0? Color.green : area[0] / markTypeCount == 0? Color.blue : Color.red;
        Debug.Log($"DrawArea : {string.Join(",", area)}, color: {color}");
        if (area.Length == 6){
                if(area[1] == 1){
                    DrawRectangle(texPixels, new Vector2(area[2], area[3]), new Vector2(area[4], area[5]), color, 2);
                }else if(area[1] == 0){
                    DrawCircle(texPixels, new Vector2(area[2], area[3]), area[4], color, 2);
                }
        }
    }

    public void DrawVectorArea(Color[] texPixels, Vector2[] area){
        Color color = Color.black;
        Debug.Log($"DrawArea : {string.Join(",", area)}, color: {color}");
        if (area.Length == 2){
            DrawRectangle(texPixels, area[0], area[1], color, 2);
        }else if(area.Length == 4){
            DrawRectangle(texPixels,  area[0], area[1], area[2], area[3], color, 2);
        }
    }

    public void DrawInitialShapes(List<int[]> areas, float[] sceneInfo)
    {
    //selectPlace: list[int] = [-1, -1, -1, -1, -1, -1]#type: mark; type(check pos region), 0-rectange, 1-circle ; lu/centerx ; lb/centery ; ru/rad ; rb/inner
        Color[] pixels = baseLayer.GetPixels();
        if(areas != null){
            foreach(int[] area in areas){
                DrawArea(pixels, area);
            }
        }
        if(sceneInfo != null){
            float angle = sceneInfo[3] * Mathf.Deg2Rad;
            DrawArea(pixels, new int[]{-1, 0, (int)sceneInfo[0], (int)sceneInfo[1], (int)sceneInfo[2], -1});
            DrawLine(pixels, new Vector2(sceneInfo[0], sceneInfo[1]), new Vector2(sceneInfo[0] + sceneInfo[2] * (float)Math.Sin(angle), sceneInfo[1] - sceneInfo[2] * (float)Math.Cos(angle)), Color.green, 2);
        }
        baseLayer.SetPixels(pixels);
        baseLayer.Apply();
    }

    // 绘制圆形轮廓
    public void DrawCircle(Color[] texPixels, Vector2 center, int radius, Color color, int lineWidth)
    {
        if (!IsPointInCanvas(center) || radius <= 0)
        {
            Debug.Log($"绘制圆形超出范围：{center}");
            return;
        }

        int x = radius;
        int y = 0;
        int err = 0;

        while (x >= y)
        {
            PlotPoints(texPixels, center, x, y, color, lineWidth);
            if (err <= 0)
            {
                y += 1;
                err += 2 * y + 1;
            }
            if (err > 0)
            {
                x -= 1;
                err -= 2 * x + 1;
            }
        }
    }

    // 绘制矩形轮廓
    public void DrawRectangle(Color[] texPixels, Vector2 point1, Vector2 point2, Vector2 point3, Vector2 point4, Color color, int lineWidth)
    {
        // 绘制四条边
        DrawLine(texPixels, point1, point2, color, lineWidth);
        DrawLine(texPixels, point2, point3, color, lineWidth); // 右边
        DrawLine(texPixels, point3, point4, color, lineWidth); // 下边
        DrawLine(texPixels, point4, point1, color, lineWidth); // 左边
    }

    public void DrawRectangle(Color[] texPixels, Vector2 topLeft, Vector2 bottomRight, Color color, int lineWidth)
    {
        DrawRectangle(texPixels, topLeft, new Vector2(bottomRight.x, topLeft.y), bottomRight, new Vector2(topLeft.x, bottomRight.y), color, lineWidth);
        // 绘制四条边
        // DrawLine(texPixels, topLeft, new Vector2(bottomRight.x, topLeft.y), color, lineWidth); // 上边
        // DrawLine(texPixels, new Vector2(bottomRight.x, topLeft.y), bottomRight, color, lineWidth); // 右边
        // DrawLine(texPixels, bottomRight, new Vector2(topLeft.x, bottomRight.y), color, lineWidth); // 下边
        // DrawLine(texPixels, new Vector2(topLeft.x, bottomRight.y), topLeft, color, lineWidth); // 左边
    }
    #endregion

    #region 轨迹系统
    public void UpdateTrail(Vector2 pos, bool force = false, float distanceThreshold = 1)
    {
        if(!force && trailPoints.Count>0 && Vector2.Distance(trailPoints.Last(), pos) < distanceThreshold){return;}
        trailPoints.Enqueue(pos);
        if (trailPoints.Count > maxTrailLength){
            // Color[] historypixels = historyLayer.GetPixels();
            while (trailPoints.Count > maxTrailLength){

                // DrawLine(historypixels, trailPoints.Dequeue(), trailPoints.Peek(), historyColor, 1);
                trailPoints.Dequeue();
            }
            // historyLayer.SetPixels(historypixels);
            // historyLayer.Apply();
        }

        Vector2[] points = trailPoints.ToArray();
        targteMaterial.SetInt(_PointCountID, points.Length);
        Vector4[] extendPoints = new Vector4[shaderMaxTrialLength];
        Array.Copy(points.Select(p => new Vector4(p.x/width, (1 - p.y/height))).ToArray(), extendPoints, points.Length);

        targteMaterial.SetVectorArray("_PointsData", extendPoints);

        // Graphics.Blit(null, targetTexture, historyMaterial);
    }

    // public void UpdateTrail(List<int[]> poses){
    //     foreach(int[] pos in poses){
    //     historyLayer.SetPixel(pos[0], pos[1], historyColor);
    //     }
    //     // historyLayer.Apply();
    // }
    #endregion

    #region 临时层绘制
    public void DrawTemporaryLayer(List<int[]> areas, bool clear = true)
    {
        if(clear){ClearLayer(tempLayer, Color.clear);}
        Color[] pixels = tempLayer.GetPixels();

        foreach(int[] area in areas){
            DrawArea(pixels, area);
        }
        
        tempLayer.SetPixels(pixels);
        tempLayer.Apply();
    }
    public void DrawTemporaryLayer(List<Vector2Int[]> areas, bool clear = true){
        if(clear){ClearLayer(tempLayer, Color.clear);}
        Color[] pixels = tempLayer.GetPixels();

        foreach(Vector2Int[] area in areas){
            DrawVectorArea(pixels, area.Select(v => new Vector2(v.x, v.y)).ToArray());
        }
        
        tempLayer.SetPixels(pixels);
        tempLayer.Apply();

    }
    #endregion

    #region 底层绘图工具
    // 绘制单个点（带线宽）
    // private void PlotPoint(Texture2D tex, Vector2 center, Color color, int size)
    private void PlotPoint(Color[] pixels, Vector2 center, Color color, int size){

        // center[0] = width - center[0];
        center[1] = height - center[1];

        int halfSize = size / 2;
        int minX = Mathf.Clamp((int)center.x - halfSize, 0, width-1);
        int maxX = Mathf.Clamp((int)center.x + halfSize, 0, width-1);
        int minY = Mathf.Clamp((int)center.y - halfSize, 0, height-1);
        int maxY = Mathf.Clamp((int)center.y + halfSize, 0, height-1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                pixels[x + y * width] = color;
            }
        }
    }

    // 绘制圆形轮廓辅助方法
    private void PlotPoints(Color[] texPixels, Vector2 center, int x, int y, Color color, int width)
    {
        float cx = center.x;
        float cy = center.y;
    
        for (int sx = -1; sx <= 1; sx += 2) {
            for (int sy = -1; sy <= 1; sy += 2) {
                PlotPoint(texPixels, new Vector2(cx + x*sx, cy + y*sy), color, width);
                PlotPoint(texPixels, new Vector2(cx + y*sx, cy + x*sy), color, width);
            }
        }
    }

    // Bresenham直线算法
    private void DrawLine(Color[] texPixels, Vector2 start, Vector2 end, Color color, int width)
    {
        int x0 = (int)start.x;
        int y0 = (int)start.y;
        int x1 = (int)end.x;
        int y1 = (int)end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            PlotPoint(texPixels, new Vector2(x0, y0), color, width);
            
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    #endregion

    public enum ShapeType { Circle, Rectangle }
}

}