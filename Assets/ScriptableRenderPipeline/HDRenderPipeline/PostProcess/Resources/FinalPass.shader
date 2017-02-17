Shader "Hidden/HDRenderPipeline/FinalPass"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
    }

    HLSLINCLUDE

        #pragma target 4.5
        #include "ShaderLibrary/Color.hlsl"
        #include "ShaderLibrary/Common.hlsl"
        #include "HDRenderPipeline/ShaderVariables.hlsl"
        #include "ColorGrading.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER2D(sampler_MainTex);
        float4 _MainTex_TexelSize;

        TEXTURE2D(_AutoExposure);
        SAMPLER2D(sampler_AutoExposure);

        TEXTURE2D(_LogLut);
        SAMPLER2D(sampler_LogLut);

        float4 _LogLut_Params;

        float _Exposure;

        float4 _NeutralTonemapperParams1;
        float4 _NeutralTonemapperParams2;

        float _ChromaticAberration_Amount;
        TEXTURE2D(_ChromaticAberration_Lut);
        SAMPLER2D(sampler_ChromaticAberration_Lut);

        float3 _Vignette_Color;
        float4 _Vignette_Settings; // x: intensity, y: smoothness, zw: center (uv space)

        TEXTURE2D(_DitheringTex);
        SAMPLER2D(sampler_DitheringTex);
        float4 _DitheringCoords;

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
            float2 uv = input.texcoord;
            float3 color = (0.0).xxx;

            // Chromatic Aberration
            // Inspired by the method described in "Rendering Inside" [Playdead 2016]
            // https://twitter.com/pixelmager/status/717019757766123520
            #if CHROMATIC_ABERRATION
            {
                float2 coords = 2.0 * uv - 1.0;
                float2 end = uv - coords * dot(coords, coords) * _ChromaticAberration_Amount;

                float2 diff = end - uv;
                int samples = clamp(int(length(_MainTex_TexelSize.zw * diff / 2.0)), 3, 16);
                float2 delta = diff / samples;
                float2 pos = uv;
                float3 sum = (0.0).xxx, filterSum = (0.0).xxx;

                for (int i = 0; i < samples; i++)
                {
                    float t = (i + 0.5) / samples;
                    float3 s = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, pos, 0).rgb;
                    float3 filter = SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_Lut, sampler_ChromaticAberration_Lut, float2(t, 0.0), 0).rgb;

                    sum += s * filter;
                    filterSum += filter;
                    pos += delta;
                }

                color = sum / filterSum;
            }
            #else
            {
                color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
            }
            #endif

            #if EYE_ADAPTATION
            {
                float autoExposure = SAMPLE_TEXTURE2D(_AutoExposure, sampler_AutoExposure, uv).r;
                color *= autoExposure;
            }
            #endif

            #if VIGNETTE
            {
                float2 d = abs(uv - _Vignette_Settings.zw) * _Vignette_Settings.x;
                d.x *= _ScreenParams.x / _ScreenParams.y;
                float vfactor = pow(saturate(1.0 - dot(d, d)), _Vignette_Settings.y);
                color *= lerp(_Vignette_Color, (1.0).xxx, vfactor);
            }
            #endif

            color *= _Exposure; // Exposure is in ev units (or 'stops'), precomputed CPU-side

            #if NEUTRAL_GRADING
            {
                color = NeutralTonemap(color, _NeutralTonemapperParams1, _NeutralTonemapperParams2);
            }
            #elif CUSTOM_GRADING
            {
                float3 uvw = saturate(LinearToLogC(color));

                // Strip lut format where `height = sqrt(width)`
                uvw.z *= _LogLut_Params.z;
                float shift = floor(uvw.z);
                uvw.xy = uvw.xy * _LogLut_Params.z * _LogLut_Params.xy + _LogLut_Params.xy * 0.5;
                uvw.x += shift * _LogLut_Params.y;
                uvw.xyz = lerp(
                    SAMPLE_TEXTURE2D(_LogLut, sampler_LogLut, uvw.xy).rgb,
                    SAMPLE_TEXTURE2D(_LogLut, sampler_LogLut, uvw.xy + float2(_LogLut_Params.y, 0)).rgb,
                    uvw.z - shift
                );

                color = uvw;
            }
            #else
            {
                color = saturate(color);
            }
            #endif

            #if DITHERING
            {
                // Symmetric triangular distribution on [-1,1] with maximal density at 0
                float noise = SAMPLE_TEXTURE2D(_DitheringTex, sampler_DitheringTex, uv * _DitheringCoords.xy + _DitheringCoords.zw).a * 2.0 - 1.0;
                noise = sign(noise) * (1.0 - sqrt(1.0 - abs(noise)));
                color = SRGBToLinear(LinearToSRGB(color) + noise / 255.0);
            }
            #endif

            return float4(color, 1.0);
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
                #pragma multi_compile __ CHROMATIC_ABERRATION
                #pragma multi_compile __ VIGNETTE
                #pragma multi_compile __ DITHERING

            ENDHLSL
        }
    }

    Fallback Off
}
