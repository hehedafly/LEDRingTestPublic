Shader "Unlit/ShaMousePos"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _Layer1("Layer 1", 2D) = "white" {}
        _Layer2("Layer 2", 2D) = "white" {}
        _Layer3("Layer 3", 2D) = "white" {}

        _TrailStartColor ("Start Color", Color) = (0,0,0,1)
        _TrailEndColor ("End Color", Color) = (0.9,0.7,0.7,1)
        _Thickness ("Thickness", Range(0,0.1)) = 0.02
    }
    SubShader
    {
        Tags{ "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _Layer1;
            sampler2D _Layer2;
            sampler2D _Layer3;
            float4 _TrailStartColor;
            float4 _TrailEndColor;
            float _Thickness;
            int _PointCount;
            float4 _PointsData[50];

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                // 轨迹颜色计算
                float closest = 1;
                fixed4 trailCol = fixed4(0,0,0,0);
                
                for(int idx = 0; idx < _PointCount-1; idx++) {
                    // 获取线段端点（已转换为UV坐标）
                    float2 p0 = _PointsData[idx].xy;
                    float2 p1 = _PointsData[idx+1].xy;
                    
                    // 线段方向计算
                    float2 dir = p1 - p0;
                    float segLength = length(dir);
                    if(segLength < 0.0001) continue;
                    
                    // 计算投影参数
                    float t = saturate(dot(i.uv - p0, dir) / (segLength*segLength));
                    float2 projection = p0 + t * dir;
                    float dist = length(i.uv - projection);
                    
                    // 厚度检测
                    if(dist < _Thickness) {
                        // 颜色插值
                        float colorT = lerp(idx/(float)(_PointCount-1), (idx+1)/(float)(_PointCount-1), t);
                        fixed4 segColor = lerp(_TrailEndColor, _TrailStartColor, colorT);
                        
                        // 抗锯齿
                        float falloff = 1 - smoothstep(_Thickness*0.8, _Thickness, dist);
                        trailCol = lerp(trailCol, segColor, falloff*segColor.a);
                    }
                }

                // 按顺序从上到下采样图层
                fixed4 color1 = tex2D(_Layer1, i.uv);
                fixed4 color2 = tex2D(_Layer2, i.uv);
                fixed4 color3 = tex2D(_Layer3, i.uv);
                float4 Layeralpha = fixed4(max(0, color1.a - trailCol.a), max(0, color2.a - color1.a - trailCol.a), max(0, color3.a - color2.a - color1.a - trailCol.a), 0);
                
                return trailCol + color1 * Layeralpha[0] + color2 * Layeralpha[1] + color3 * Layeralpha[2];
            }

            
            ENDCG
        }
    }
}