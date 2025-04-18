Shader "Custom/ShaDriftGrating"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Float) = 1.0
        _Frequency ("Frequency", Float) = 10.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _Speed;
            float _Frequency;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 计算条纹的UV偏移
                float stripeOffset = _Time.y * _Speed;
                float2 uv = i.uv;
                uv.x += stripeOffset;

                // 使用正弦函数生成黑白条纹
                float stripePattern = max((sin(uv.x * _Frequency) + 1 - 0.4), 0) / 2;

                // 应用纹理和条纹图案
                fixed4 col = tex2D(_MainTex, i.uv);
                col = lerp(col, fixed4(1,1,1,1), stripePattern); // 白色条纹
                col = lerp(col, fixed4(0,0,0,1), 1 - stripePattern*1); // 黑色条纹

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
