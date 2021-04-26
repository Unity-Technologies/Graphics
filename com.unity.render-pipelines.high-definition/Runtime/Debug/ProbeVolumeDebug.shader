Shader "Hidden/HDRP/ProbeVolumeDebug"
{
    Properties
    {
    }

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" "RenderType" = "Opaque" }
        LOD 100

        HLSLINCLUDE
        #pragma editor_sync_compilation
        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile PROBE_VOLUMES_OFF PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeReferenceVolume.Debug.cs.hlsl"

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
            UNITY_DEFINE_INSTANCED_PROP(float4, _Position)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Validity)
        UNITY_INSTANCING_BUFFER_END(Props)

        v2f vert(appdata v)
        {
            v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);

            o.vertex = mul(UNITY_MATRIX_VP, mul(UNITY_MATRIX_M, float4(v.vertex.xyz * _ProbeSize, 1.0)));
            o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));

            return o;
        }

        float4 frag(v2f i) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(i);

            if (_ShadingMode == DEBUGPROBESHADINGMODE_SH)
            {
                float4 position = UNITY_ACCESS_INSTANCED_PROP(Props, _Position);
                float3 normal = normalize(i.normal);
                float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
                float3 backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);
                APVResources apvRes = FillAPVResources();

                float3 uvw;
                if (TryToGetPoolUVW(apvRes, position.xyz, 0.0, uvw))
                {
                    float L1Rx;
                    float3 L0 = EvaluateAPVL0Point(apvRes, uvw, L1Rx);

#ifdef PROBE_VOLUMES_L1
                    EvaluateAPVL1Point(apvRes, L0, L1Rx, normal, normal, uvw, bakeDiffuseLighting, backBakeDiffuseLighting);
#elif PROBE_VOLUMES_L2
                    EvaluateAPVL1Point(apvRes, L0, L1Rx, normal, normal, uvw, bakeDiffuseLighting, backBakeDiffuseLighting);
#endif

                    bakeDiffuseLighting += L0;
                }
                else
                {
                    bakeDiffuseLighting = EvaluateAmbientProbe(normal);
                }

                return float4(bakeDiffuseLighting * exp2(_ExposureCompensation) * GetCurrentExposureMultiplier(), 1);
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_VALIDITY)
            {
                return UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
            }

            return _Color;
        }
        ENDHLSL

        Pass
        {
            Name "DepthForwardOnly"
            Tags{ "LightMode" = "DepthForwardOnly" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            ZTest LEqual
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            ENDHLSL
        }
    }
}
