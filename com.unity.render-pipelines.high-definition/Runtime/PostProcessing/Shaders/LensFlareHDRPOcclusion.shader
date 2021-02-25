Shader "Hidden/HDRP/LensFlare Occlusion (HDRP)"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode" = "Forward"  "RenderQueue" = "Transparent" }

            ZWrite Off
            Cull Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex vertOcclusion
            #pragma fragment fragOcclusion

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/LensFlareHDRPCommon.hlsl"

            Varyings vertOcclusion(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float screenRatio = _ScreenSize.x / _ScreenSize.y;
                float2 flareSize = _Size;

                float4 posPreScale = float4(2.0f, 2.0f, 1.0f, 1.0f) * GetQuadVertexPosition(input.vertexID) - float4(1.0f, 1.0f, 0.0f, 0.0);
                output.texcoord = GetQuadTexCoord(input.vertexID);
                float2 screenPos = _FlareScreenPos;

                float occlusion = GetOcclusion(_FlareScreenPos.xy, _FlareDepth, screenRatio);

                //float4 centerPos = float4(local.x,
                //                          local.y,
                //                          posPreScale.z,
                //                          posPreScale.w);
                output.positionCS = posPreScale;

                if (_FlareOffscreen < 0.0f && // No lens flare off screen
                    (any(_FlareScreenPos.xy < -1) || any(_FlareScreenPos.xy >= 1)))
                    occlusion *= 0.0f;

                output.occlusion = occlusion;

                return output;
            }

            float4 fragOcclusion(Varyings i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                //return i.occlusion * 0.5f;
                return 160.0f;
            }

            ENDHLSL
        }
    }
}
