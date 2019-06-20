Shader "VFX/Test/Rim"
{
    Properties
    {
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimCoef ("Rim Coef", Float) = 0
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "Queue" = "Geometry"
                "IgnoreProjector" = "True"
                "RenderType" = "Opaque"
            }

            CGPROGRAM

            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            float4 _RimColor;
            float _RimCoef;

            struct vertexIn
            {
                float4 pos : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 posWS : TEXCOORD0;
                float3 normal : NORMAL;
            };

            v2f vert(vertexIn i)
            {
                v2f o = (v2f)0;
                o.posWS = mul(unity_ObjectToWorld,float4(i.pos)).xyz;
                o.pos = UnityWorldToClipPos(o.posWS);
                o.normal = UnityObjectToWorldNormal(i.normal);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                const float r0 = _RimCoef;
                float3 posToCam = normalize(UNITY_MATRIX_I_V._m03_m13_m23 - i.posWS);
                const float3 fresnel = 1-r0 + r0 * pow(1.0f - saturate(dot(i.normal,posToCam)),5.0f);
                float3 color = saturate(fresnel) * _RimColor;
                return float4(color,1.0f);
            }

            ENDCG
        }
    }

}
