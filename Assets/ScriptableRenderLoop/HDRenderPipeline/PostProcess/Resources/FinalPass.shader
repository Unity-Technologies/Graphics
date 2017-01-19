Shader "Hidden/HDRenderPipeline/FinalPass"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
    }

    HLSLINCLUDE

        #pragma target 4.5
        #include "Common.hlsl"
        #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"
        #include "ColorGrading.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER2D(sampler_MainTex);

        TEXTURE2D(_AutoExposure);
        SAMPLER2D(sampler_AutoExposure);

        TEXTURE2D(_LogLut);
        SAMPLER2D(sampler_LogLut);

        float4 _LogLut_Params;

        float _Exposure;

        float4 _NeutralTonemapperParams1;
        float4 _NeutralTonemapperParams2;

        struct Attributes 
        {
            float3 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        struct Varyings 
        {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.vertex = TransformWorldToHClip(input.vertex);
            output.texcoord = input.texcoord.xy;
            return output;
        }

        // Neutral tonemapping (Hable/Hejl/Frostbite)
        // Input is linear RGB
        float3 NeutralCurve(float3 x, float a, float b, float c, float d, float e, float f)
        {
            return ((x * (a * x + c * b) + d * e) / (x * (a * x + b) + d * f)) - e / f;
        }

        float3 NeutralTonemap(float3 x, float4 params1, float4 params2)
        {
            // ACES supports negative color values and WILL output negative values when coming from ACES or ACEScg
            // Make sure negative channels are clamped to 0.0 as this neutral tonemapper can't deal with them properly
            x = max((0.0).xxx, x);

            // Tonemap
            float a = params1.x;
            float b = params1.y;
            float c = params1.z;
            float d = params1.w;
            float e = params2.x;
            float f = params2.y;
            float whiteLevel = params2.z;
            float whiteClip = params2.w;

            float3 whiteScale = (1.0).xxx / NeutralCurve(whiteLevel, a, b, c, d, e, f);
            x = NeutralCurve(x * whiteScale, a, b, c, d, e, f);
            x *= whiteScale;

            // Post-curve white point adjustment
            x /= whiteClip.xxx;

            return x;
        }

        float4 Frag(Varyings input) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);

            #if EYE_ADAPTATION
            {
                float autoExposure = SAMPLE_TEXTURE2D(_AutoExposure, sampler_AutoExposure, input.texcoord).r;
                color *= autoExposure;
            }
            #endif

            color.rgb *= _Exposure; // Exposure is in ev units (or 'stops'), precomputed CPU-side

            #if NEUTRAL_GRADING
            {
                color.rgb = NeutralTonemap(color.rgb, _NeutralTonemapperParams1, _NeutralTonemapperParams2);
            }
            #elif CUSTOM_GRADING
            {
                float3 uvw = saturate(LinearToLogC(color));

                // Strip lut format where `height = sqrt(width)`
                uvw.z *= _LogLut_Params.z;
                half shift = floor(uvw.z);
                uvw.xy = uvw.xy * _LogLut_Params.z * _LogLut_Params.xy + _LogLut_Params.xy * 0.5;
                uvw.x += shift * _LogLut_Params.y;
                uvw.xyz = lerp(
                    SAMPLE_TEXTURE2D(_LogLut, sampler_LogLut, uvw.xy).rgb,
                    SAMPLE_TEXTURE2D(_LogLut, sampler_LogLut, uvw.xy + half2(_LogLut_Params.y, 0)).rgb,
                    uvw.z - shift
                );

                color.rgb = uvw;
            }
            #else
            {
                color = saturate(color);
            }
            #endif

            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment Frag
                #pragma multi_compile __ NEUTRAL_GRADING CUSTOM_GRADING
                #pragma multi_compile __ EYE_ADAPTATION

            ENDHLSL
        }
    }

    Fallback Off
}
