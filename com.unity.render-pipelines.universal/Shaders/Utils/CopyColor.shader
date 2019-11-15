Shader "Hidden/Universal Render Pipeline/CopyColor"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "CopyColor"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile _ _LINEAR_TO_SRGB_CONVERSION

            // Enable Pure URP Camera Management
            #pragma multi_compile _ UNITY_PURE_URP_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifdef _LINEAR_TO_SRGB_CONVERSION
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                half4 positionCS    : SV_POSITION;
                half2 uv            : TEXCOORD0;
            };

            TEXTURE2D(_BlitTex);
            SAMPLER(sampler_BlitTex);

            Varyings Vertex(Attributes input)
            {
                Varyings output;

#if defined(UNITY_PURE_URP_ENABLED) 
                output.positionCS = float4(input.positionOS.xyz, 1.0f);
    #if UNITY_UV_STARTS_AT_TOP
                    // Our world space, view space, screen space and NDC space are Y-up.
                    // Our clip space is flipped upside-down due to poor legacy Unity design.
                    // To ensure consistency with the rest of the pipeline, we have to y-flip clip space here
                    output.positionCS.y = -output.positionCS.y;
    #endif
#else
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
#endif
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BlitTex, sampler_BlitTex, input.uv);
                #ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
                #endif
                return col;
            }
            ENDHLSL
        }
    }
}
