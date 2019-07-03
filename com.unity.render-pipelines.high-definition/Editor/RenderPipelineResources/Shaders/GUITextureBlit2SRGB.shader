Shader "Hidden/GUITextureBlit2SRGB" {
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _Color("Multiplicative color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            // Shader slightly adapted from the builtin renderer
            // It can consume an exposure texture to setup the exposure in the render

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            UNITY_DECLARE_TEX2D(_Exposure);
            uniform float4 _MainTex_ST;
            uniform float4 _Color;
            uniform bool _ManualTex2SRGB;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 colTex = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.texcoord);
                if (_ManualTex2SRGB)
                    colTex.rgb = LinearToGammaSpace(colTex.rgb);
                float exposure = UNITY_SAMPLE_TEX2D(_Exposure, float2(0, 0)).r;
                return colTex * _Color * exposure;
            }
            ENDCG

        }
    }
    Fallback Off
}
