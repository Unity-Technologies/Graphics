Shader "Hidden/Universal Render Pipeline/Sampling"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        // 0 - Downsample - Box filtering
        Pass
        {
            Name "BoxDownsample"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex FullscreenVert
            #pragma fragment FragBoxDownsample

            #pragma multi_compile _ _USE_DRAW_PROCEDURAL

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

            TEXTURE2D_X(_SourceTex);
            SAMPLER(sampler_SourceTex);
            float4 _SourceTex_TexelSize;

            float _SampleOffset;

            half4 FragBoxDownsample(FullscreenVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float4 d = _SourceTex_TexelSize.xyxy * float4(-_SampleOffset, -_SampleOffset, _SampleOffset, _SampleOffset);

                half4 s;
                s =  SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv + d.xy);
                s += SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv + d.zy);
                s += SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv + d.xw);
                s += SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv + d.zw);

                return s * 0.25h;
            }
            ENDHLSL
        }
        // 1 - Read Framebuffer
        Pass
        {
            Name "Framebuffer Fetch"
            ZTest Off
            ZWrite Off


            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma vertex Vertex
            #pragma fragment FragFetch

            DECLARE_FRAMEBUFFER_INPUT_HALF(0);


            half4 FragFetch(Varyings input) : SV_Target
            {
                half4 col = READ_FRAMEBUFFER_INPUT(0, input.positionCS);
                return col;
            }
            ENDHLSL
        }
    }
}
