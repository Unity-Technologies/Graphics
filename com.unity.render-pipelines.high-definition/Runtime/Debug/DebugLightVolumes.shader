Shader "Hidden/HDRP/DebugLightVolumes"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Range("Range", Vector) = (1.0, 1.0, 1.0, 1.0)
        _Offset("Offset", Vector) = (1.0, 1.0, 1.0, 1.0)
    }

    SubShader
    {
        Pass
        {
            Cull Back
            ZWrite Off
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            struct AttributesDefault
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VaryingsDefault
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 _Range;
            float3 _Offset;
            float4 _Color;

            VaryingsDefault vert(AttributesDefault att)
            {
                VaryingsDefault output;
                UNITY_SETUP_INSTANCE_ID(att);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionRWS = TransformObjectToWorld(att.positionOS.xyz * _Range + _Offset);
                output.positionCS = TransformWorldToHClip(positionRWS);

                return output;
            }

            void frag(VaryingsDefault varying, out float outLightCount : SV_Target0, out float4 outColorAccumulation : SV_Target1)
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varying);
                outLightCount = 1.0f;
                outColorAccumulation = _Color;
            }

            ENDHLSL
        }

        Pass
        {
            ZWrite Off Blend One One  ZTest Always Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SamplerState sampler_PointClamp;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 texcoord   : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.texcoord   = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, input.texcoord, 0);
            }
            ENDHLSL
        }
    }
}
