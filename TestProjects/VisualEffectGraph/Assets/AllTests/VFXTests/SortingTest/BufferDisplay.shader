Shader "Unlit/BufferDisplay"
{
    SubShader
    {
        Tags{ "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM

            #define DISPLAY_HUE 1

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct KVP
            {
                float key;
                uint value;
            };

            StructuredBuffer<KVP> buffer;
            uint totalCount;
            int elementCount;
            int groupCount;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 HUEtoRGB(float H)
            {
                float R = abs(H * 6 - 3) - 1;
                float G = 2 - abs(H * 6 - 2);
                float B = 2 - abs(H * 6 - 4);
                return saturate(float3(R, G, B));
            }

            float3 GammaCorrection(float3 col)
            {
                return pow(col, 2.2f);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the buffer
                uint x = (uint)(i.uv.x * elementCount);
                uint y = (uint)(i.uv.y * groupCount);
                uint index = y * elementCount + x;
                if (index >= totalCount)
#if DISPLAY_HUE
                    return float4(0, 0, 0, 1);
#else
                    return float4(1, 0, 1, 1);
#endif

                float value = buffer[index].key;
#if DISPLAY_HUE
                return float4(GammaCorrection(HUEtoRGB(value)), 1.0);
#else
                //value = pow(value, 2.2f); // gamma correction
                return float4(value.xxx, 1.0);
#endif
            }
            ENDCG
        }
    }
}
