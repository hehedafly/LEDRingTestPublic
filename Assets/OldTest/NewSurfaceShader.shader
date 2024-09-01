Shader "Custom/MultiCameraCompositeShader" {
    Properties {
        _MainTex ("Main Texture", 2D) = "white" {}
        _Camera1Tex ("Camera 1 Texture", 2D) = "white" {}
        _Camera2Tex ("Camera 2Tex", 2D) = "white" {}
        _Camera3Tex ("Camera 3 Texture", 2D) = "white" {}
        _Camera4Tex ("Camera 4 Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _Camera1Tex;
            sampler2D _Camera2Tex;
            sampler2D _Camera3Tex;
            sampler2D _Camera4Tex;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // 根据uv坐标区域选择不同的摄像机纹理
                fixed4 color = tex2D(_MainTex, i.uv);
                if (i.uv.x < 0.25) {
                    // 第一个区域，使用Camera 1的纹理
                    color = tex2D(_Camera1Tex, i.uv * 4.0);
                } else if (i.uv.x < 0.5) {
                    // 第二个区域，使用Camera 2的纹理
                    color = tex2D(_Camera2Tex, (i.uv - float2(0.25, 0.0)) * 4.0);
                } else if (i.uv.x < 0.75) {
                    // 第三个区域，使用Camera 3的纹理
                    color = tex2D(_Camera3Tex, (i.uv - float2(0.5, 0.0)) * 4.0);
                } else {
                    // 第四个区域，使用Camera 4的纹理
                    color = tex2D(_Camera4Tex, (i.uv - float2(0.75, 0.0)) * 4.0);
                }
                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}