Shader "Unlit/ErrorDisplay"
{
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

            struct KVP
            {
                float key;
                uint value;
            };

            StructuredBuffer<KVP> buffer0;
            StructuredBuffer<KVP> buffer1;

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

            fixed4 frag(v2f i) : SV_Target
            {
                uint x = (uint)(i.uv.x * elementCount);
                uint y = (uint)(i.uv.y * groupCount);
                float diff = step(1e-5,abs(buffer0[y * elementCount + x].key - buffer1[y * elementCount + x].key));
                return float4(diff.xxx, 1.0);
            }
            ENDCG
        }
    }
}
