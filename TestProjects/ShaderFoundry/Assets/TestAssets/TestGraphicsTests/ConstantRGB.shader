Shader "Hidden/Test/ConstantRGB"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _COLOR_RED _COLOR_GREEN _COLOR_BLUE

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                #if _COLOR_RED
                float4 col = float4(1.0f, 0.0f, 0.0f, 1.0f);
                #elif _COLOR_GREEN
                float4 col = float4(0.0f, 1.0f, 0.0f, 1.0f);
                #else // _COLOR_BLUE
                float4 col = float4(0.0f, 0.0f, 1.0f, 1.0f);
                #endif
                return col;
            }
            ENDCG
        }
    }
}
