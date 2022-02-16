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
            #pragma vertex Vert
            #pragma fragment FragBoxDownsample

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;

            float _SampleOffset;

            half4 FragBoxDownsample(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
                float4 d = _BlitTexture_TexelSize.xyxy * float4(-_SampleOffset, -_SampleOffset, _SampleOffset, _SampleOffset);

                half4 s;
                s =  SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + d.xy);
                s += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + d.zy);
                s += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + d.xw);
                s += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + d.zw);

                return s * 0.25h;
            }
            ENDHLSL
        }
    }
}
