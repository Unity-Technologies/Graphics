Shader "Hidden/DebugVTBlit"
{

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local __ USE_TEXARRAY

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
#ifdef USE_TEXARRAY
            UNITY_DECLARE_TEX2DARRAY(_MainTex);
#else
            sampler2D _MainTex;
#endif

            fixed4 frag(v2f i) : SV_Target
            {
#ifdef USE_TEXARRAY
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_MainTex,float3(i.uv,0.0f));
#else
                fixed4 col = tex2D(_MainTex, i.uv);
#endif

                float4 swiz;
                swiz = col;
                swiz *= 255.0f;

                float tileX = swiz.x + fmod(swiz.y, 8.0f) * 256.0f;
                float tileY = floor(swiz.y / 8.0f) + fmod(swiz.z, 64.0f) * 32.0f;
                float level = floor((swiz.z) / 64.0f) + fmod(swiz.w, 4.0f) * 4.0f;
                float tex = floor(swiz.w / 4.0f);

                return float4(tileX,tileY,level,tex);
            }
            ENDCG
        }
    }
}
