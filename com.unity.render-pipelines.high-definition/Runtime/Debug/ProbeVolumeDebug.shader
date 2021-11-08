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
        #pragma multi_compile_fragment PROBE_VOLUMES_OFF PROBE_VOLUMES_L1 PROBE_VOLUMES_L2

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeReferenceVolume.Debug.cs.hlsl"

        uniform int _ShadingMode;
        uniform float _ExposureCompensation;
        uniform float _ProbeSize;
        uniform float4 _Color;
        uniform int _SubdivLevel;
        uniform float _CullDistance;
        uniform int _MaxAllowedSubdiv;
        uniform float _ValidityThreshold;

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
            UNITY_DEFINE_INSTANCED_PROP(float4, _IndexInAtlas)
            UNITY_DEFINE_INSTANCED_PROP(float, _Validity)
            UNITY_DEFINE_INSTANCED_PROP(float, _RelativeSize)
        UNITY_INSTANCING_BUFFER_END(Props)

        v2f vert(appdata v)
        {
            v2f o;

            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_TRANSFER_INSTANCE_ID(v, o);


            // Finer culling, degenerate the vertices of the sphere if it lies over the max distance.
            // Coarser culling has already happened on CPU.
            float4 position = float4(UNITY_MATRIX_M._m03_m13_m23, 1);
            int brickSize = UNITY_ACCESS_INSTANCED_PROP(Props, _IndexInAtlas).w;

            if (distance(position.xyz, GetCurrentViewPosition()) > _CullDistance ||
                brickSize > _MaxAllowedSubdiv)
            {
                o.vertex = 0;
                o.normal = 0;
            }
            else
            {
                float4 wsPos = mul(UNITY_MATRIX_M, float4(v.vertex.xyz * _ProbeSize, 1.0));
                o.vertex = mul(UNITY_MATRIX_VP, wsPos);

                o.normal = normalize(mul(v.normal, (float3x3)UNITY_MATRIX_M));
            }

            return o;
        }

        float3 EvalL1(float3 L0, float3 L1_R, float3 L1_G, float3 L1_B, float3 N)
        {
            float3 outLighting = 0;
            L1_R = DecodeSH(L0.r, L1_R);
            L1_G = DecodeSH(L0.g, L1_G);
            L1_B = DecodeSH(L0.b, L1_B);
            outLighting += SHEvalLinearL1(N, L1_R, L1_G, L1_B);

            return outLighting;
        }

        float3 EvalL2(inout float3 L0, float4 L2_R, float4 L2_G, float4 L2_B, float4 L2_C, float3 N)
        {
            DecodeSH_L2(L0, L2_R, L2_G, L2_B, L2_C);

            return SHEvalLinearL2(N, L2_R, L2_G, L2_B, L2_C);
        }

        float4 frag(v2f i) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(i);

            if (_ShadingMode == DEBUGPROBESHADINGMODE_SH)
            {
                APVResources apvRes = FillAPVResources();
                int3 texLoc = UNITY_ACCESS_INSTANCED_PROP(Props, _IndexInAtlas).xyz;
                float3 normal = normalize(i.normal);

                float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
                float3 backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);

                float4 L0_L1Rx = apvRes.L0_L1Rx[texLoc].rgba;
                float3 L0 = L0_L1Rx.xyz;
                float  L1Rx = L0_L1Rx.w;
                float4 L1G_L1Ry = apvRes.L1G_L1Ry[texLoc].rgba;
                float4 L1B_L1Rz = apvRes.L1B_L1Rz[texLoc].rgba;

                bakeDiffuseLighting = EvalL1(L0, float3(L1Rx, L1G_L1Ry.w, L1B_L1Rz.w), L1G_L1Ry.xyz, L1B_L1Rz.xyz, normal);

        #ifdef PROBE_VOLUMES_L2
                float4 L2_R = apvRes.L2_0[texLoc].rgba;
                float4 L2_G = apvRes.L2_1[texLoc].rgba;
                float4 L2_B = apvRes.L2_2[texLoc].rgba;
                float4 L2_C = apvRes.L2_3[texLoc].rgba;

                bakeDiffuseLighting += EvalL2(L0, L2_R, L2_G, L2_B, L2_C, normal);
        #endif
                bakeDiffuseLighting += L0;
                return float4(bakeDiffuseLighting * exp2(_ExposureCompensation) * GetCurrentExposureMultiplier(), 1);
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_VALIDITY)
            {
                float validity = UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
                return lerp(float4(0, 1, 0, 1), float4(1, 0, 0, 1), validity);
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_VALIDITY_OVER_DILATION_THRESHOLD)
            {
                float validity = UNITY_ACCESS_INSTANCED_PROP(Props, _Validity);
                if (validity > _ValidityThreshold)
                {
                    return float4(1, 0, 0, 1);
                }
                else
                {
                    return float4(0, 1, 0, 1);
                }
            }
            else if (_ShadingMode == DEBUGPROBESHADINGMODE_SIZE)
            {
                float4 col = lerp(float4(0, 1, 0, 1), float4(1, 0, 0, 1), UNITY_ACCESS_INSTANCED_PROP(Props, _RelativeSize));
                return col;
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
