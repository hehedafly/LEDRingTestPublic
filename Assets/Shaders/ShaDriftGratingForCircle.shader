Shader "Custom/ShaDriftGrating"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Speed", Float) = 1.0
        _Frequency ("Frequency", Float) = 10.0
        _BackgroundLight ("BackgroundLight", Float) = 0
        _Direction ("Direction", Int) = 1       //竖直时1为左-1为右，垂直时1为上-1为下
        _Horizontal("Horizontal", Int) = 0      //0:竖直条纹左右运动, 1:水平条纹上下运动
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
            float _BackgroundLight;
            int _Direction;
            int _Horizontal;

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
                float stripeOffset = _Time.y * _Speed * _Direction;
                float2 uv = float2(i.uv.x * (1 - _Horizontal) + i.uv.y * _Horizontal, i.uv.x * _Horizontal + i.uv.y * (1 - _Horizontal));
                //uv.x = frac(uv.x + stripeOffset);

                // 使用正弦函数生成黑白条纹
                float stripePattern = max((sin((uv.x  - stripeOffset) * _Frequency * (1 + _Horizontal * 9)) + 1 - 0.4), 0) / 2;

                // 应用纹理和条纹图案
                fixed4 col = tex2D(_MainTex, i.uv);
                col = lerp(col, fixed4(1,1,1,1), stripePattern); // 白色条纹
                col = lerp(col, fixed4(0,0,0,1), 1 - stripePattern*1); // 黑色条纹

                float isMargin = sign(length(float2(uv.x - 0.5, uv.y - 0.5)) - 0.5);
                // float isMargin = max(uv.x - 0.5, 0);
                //float isMargin = uv.x;
                // float isMargin = sign(length(float2(uv.x, uv.y)) - 0.25);
                col = col * (1 - isMargin) +isMargin * fixed4(_BackgroundLight, _BackgroundLight, _BackgroundLight, 0);

                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
