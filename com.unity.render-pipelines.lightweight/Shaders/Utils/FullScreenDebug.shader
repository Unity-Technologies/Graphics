Shader "Hidden/Lightweight Render Pipeline/FullScreenDebug"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "LightweightPipeline"}
        LOD 100

        Pass
        {
            Name "Blit"
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
            #pragma multi_compile _ _KILL_ALPHA

            #include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
            #ifdef _LINEAR_TO_SRGB_CONVERSION
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #endif

            #define DEBUG_MODE_DEPTH (1)
            #define DEBUG_MODE_MAIN_LIGHT_SHADOWS_ONLY (2)
            //#define DEBUG_MODE_OVERDRAW (3)
            //#define DEBUG_MODE_WIRE (4)
            #define DEBUG_MODE_ADDITIONAL_LIGHTS_SHADOW_MAP (5)
            #define DEBUG_MODE_MAIN_LIGHT_SHADOW_MAP (6)

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
            int _DebugMode;
            float _NearPlane;
            float _FarPlane;

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_BlitTex, sampler_BlitTex, input.uv);

                #ifdef _LINEAR_TO_SRGB_CONVERSION
                    col = LinearToSRGB(col);
                #endif
                
                if (_DebugMode == DEBUG_MODE_DEPTH)
                {
                    half linearDepth = _FarPlane / (_FarPlane - _NearPlane) * (1.0f - (_NearPlane / col.r));
                    col.rgb = linearDepth.rrr;
                    col.a = 1.0f;
                    return col;
                }
                else if (_DebugMode == DEBUG_MODE_MAIN_LIGHT_SHADOWS_ONLY)
                {
                    col.rgb = col.rrr;
                    col.a = 1.0f;
                    return col;
                }
                else if (_DebugMode == DEBUG_MODE_ADDITIONAL_LIGHTS_SHADOW_MAP)
                {
                    col.rgb = col.rrr;
                    col.a = 1.0f;
                    return col;
                }
                else if (_DebugMode == DEBUG_MODE_MAIN_LIGHT_SHADOW_MAP)
                {
                    col.rgb = col.rrr;
                    col.a = 1.0f;
                    return col;
                }
                else
                {
                    col.a = 1.0;
                    return col;
                }
            }
            ENDHLSL
        }
    }
}
