Shader "Unlit/BufferDisplay"
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

            Buffer<float> buffer;
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
                // sample the buffer
                uint x = (uint)(i.uv.x * elementCount);
                uint y = (uint)(i.uv.y * groupCount);
                float lum = buffer[y * elementCount + x];
                //lum = pow(lum, 2.2f); // gamma correction
                return float4(lum.xxx, 1.0);
            }
            ENDCG
        }
    }
}
