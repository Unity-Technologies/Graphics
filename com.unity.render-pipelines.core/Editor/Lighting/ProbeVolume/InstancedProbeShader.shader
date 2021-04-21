Shader "Hidden/InstancedProbeShader"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"

            uniform int _ShadingMode;
            uniform float _ExposureCompensation;
            uniform float _ProbeSize;
            uniform float4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _R)
                UNITY_DEFINE_INSTANCED_PROP(float4, _G)
                UNITY_DEFINE_INSTANCED_PROP(float4, _B)
                UNITY_DEFINE_INSTANCED_PROP(float4, _Validity)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex * _ProbeSize);
                o.normal = UnityObjectToWorldNormal(v.normal);

                return o;
            }

            float3 evalSH(float3 normal, float4 SHAr, float4 SHAg, float4 SHAb)
            {
                float4 normalPadded = float4(normal, 1);

                float3 x;

                SHAr.xyz = DecodeSH(SHAr.w, SHAr.xyz);
                SHAg.xyz = DecodeSH(SHAg.w, SHAg.xyz);
                SHAb.xyz = DecodeSH(SHAb.w, SHAb.xyz);

                // Linear (L1) + constant (L0) polynomial terms
                x.r = dot(SHAr, normalPadded);
                x.g = dot(SHAg, normalPadded);
                x.b = dot(SHAb, normalPadded);

                return x;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                if (_ShadingMode == 1)
                {
                    float4 r = UNITY_ACCESS_INSTANCED_PROP(Props, _R);
                    float4 g = UNITY_ACCESS_INSTANCED_PROP(Props, _G);
                    float4 b = UNITY_ACCESS_INSTANCED_PROP(Props, _B);

                    return float4(evalSH(normalize(i.normal), r, g, b) * exp2(_ExposureCompensation), 1);
                }
                else if (_ShadingMode == 2)
                {
                    return UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
                }
                return _Color;
            }
            ENDCG
        }
    }
}
