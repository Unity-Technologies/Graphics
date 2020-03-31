Shader "Hidden/HDRP/ApplyDistortion"
{
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 2
        [HideInInspector] _StencilMask("_StencilMask", Int) = 2
    }

    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 playstation xboxone vulkan metal switch
        #pragma editor_sync_compilation
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Builtin/BuiltinData.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

        TEXTURE2D_X(_DistortionTexture);

        CBUFFER_START(cb)
            float4 _Size;
        CBUFFER_END

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

        Varyings Vert(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetNormalizedFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            // We use a bias when fetching distortion source pixels
            // This avoid artifacts when a distortion is overlapped by an opaque object
            const float _FetchBias = 0.9;

            // Get distortion values
            float4 encodedDistortion = LOAD_TEXTURE2D_X(_DistortionTexture, input.positionCS.xy);

            float2 distortion;
            float distortionBlur;
            bool distortionIsSourceValid;
            DecodeDistortion(encodedDistortion, distortion, distortionBlur, distortionIsSourceValid);

            // Reject the pixel if it is not in the distortion mask
            if (!distortionIsSourceValid)
            {
                discard;
                return 0;
            }

            int2 distortedEncodedDistortionId = input.positionCS.xy + int2(distortion);

            // Reject distortion if we try to fetch a pixel out of the buffer
            if (any(distortedEncodedDistortionId < 0)
                || any(distortedEncodedDistortionId > int2(_Size.xy)))
            {
                // In this case we keep the blur, but we offset don't distort the uv coords.
                distortion = 0.0f;
            }

            // We fetch a second time the _DistortionTexture for the at the distorded coordinate
            // Then we check if this pixels is in the distorded mask (eg: behind any distorted object)
            // Otherwise we reject the distortion
            float2 distordedDistortion;
            float distordedDistortionBlur;
            bool distordedIsSourceValid;
            float4 encodedDistordedDistortion = LOAD_TEXTURE2D_X(_DistortionTexture, distortedEncodedDistortionId);
            DecodeDistortion(encodedDistordedDistortion, distordedDistortion, distordedDistortionBlur, distordedIsSourceValid);

            if (!distordedIsSourceValid)
            {
                // In this case we keep the blur, but we offset don't distort the uv coords.
                distortion = 0.0f;
            }

            // Get source pixel for distortion
            float2 distordedUV = float2(input.positionCS.xy + distortion * _FetchBias) * _Size.zw;
            float mip = (_ColorPyramidScale.z - 1) * clamp(distortionBlur, 0.0, 1.0);
            float4 sampled = SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, distordedUV * _ColorPyramidScale.xy, mip);

            return sampled;
        }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Stencil
            {
                WriteMask [_StencilMask]
                ReadMask [_StencilMask] 
                Ref  [_StencilRef]
                Comp Equal
                Pass Zero   // We can clear the bit since we won't need anymore.
            }

            ZWrite Off Blend Off Cull Off
            ZTest Less // Required for XR occlusion mesh optimization

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment Frag
            ENDHLSL
        }
    }
    Fallback Off
}
