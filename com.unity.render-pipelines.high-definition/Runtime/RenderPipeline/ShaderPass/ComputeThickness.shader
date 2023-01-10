Shader "Renderers/Thickness"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

    //enable GPU instancing support
    #pragma multi_compile_instancing
    #pragma multi_compile _ DOTS_INSTANCING_ON

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderersV2.hlsl"

    #if SHADERPASS != SHADERPASS_FORWARD_UNLIT
    #error SHADERPASS_is_not_correctly_define
    #endif

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

    PackedVaryingsType Vert(AttributesMesh inputMesh)
    {
        VaryingsType varyingsType;
        varyingsType.vmesh = VertMesh(inputMesh);
        return PackVaryingsType(varyingsType);
    }

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "ComputeThicknessOpaque"
            Tags { "LightMode" = "Forward" }

            ZWrite Off
            ZTest Always
            Cull Off

            Blend One One
            BlendOp Add

            HLSLPROGRAM

            float _DownsizeScale;
            uint _ViewId;

            void Frag(PackedVaryingsToPS packedInput, bool isFrontFace : SV_IsFrontFace, out float2 outColor : SV_Target0 )
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);

#ifdef UNITY_STEREO_INSTANCING_ENABLED
                // Work around to discard undesired pixel for XR to bypass de default dispatch
                if (unity_StereoEyeIndex != _ViewId)
                    discard;
#endif

                FragInputs input = UnpackVaryingsToFragInputs(packedInput);

                float usedDepth = LinearEyeDepth(input.positionSS.z, _ZBufferParams);

                float sign = isFrontFace ? -1.0f : 1.0f;
                float value = sign * usedDepth;

                outColor = float2(value, 1.0f);
            }

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "ComputeThicknessTransparent"
            Tags { "LightMode" = "Forward" }

            ZWrite Off
            ZTest Always
            Cull Off

            Blend One One
            BlendOp Add

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

            float _DownsizeScale;
            uint _ViewId;

            void Frag(PackedVaryingsToPS packedInput, bool isFrontFace : SV_IsFrontFace, out float2 outColor : SV_Target0 )
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);

#ifdef UNITY_STEREO_INSTANCING_ENABLED
                // Work around to discard undesired pixel for XR to bypass de default dispatch
                if (unity_StereoEyeIndex != _ViewId)
                    discard;
#endif

                FragInputs input = UnpackVaryingsToFragInputs(packedInput);

                float usedDepth = LinearEyeDepth(input.positionSS.z, _ZBufferParams);

                float sceneDeviceDepth = LoadCameraDepth(round(_DownsizeScale * input.positionSS.xy));
                float sceneLinearDepth = LinearEyeDepth(sceneDeviceDepth, _ZBufferParams);
                usedDepth = min(usedDepth, sceneLinearDepth);

                float sign = isFrontFace ? -1.0f : 1.0f;
                float value = sign * usedDepth;

                outColor = float2(value, 1.0f);
            }

            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }
}
