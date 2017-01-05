// Final compositing pass, just does gamma correction for now.
Shader "Hidden/HDRenderPipeline/FinalPass"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}

        _ToneMapCoeffs1("Parameters for neutral tonemap", Vector) = (0.0, 0.0, 0.0, 0.0)
        _ToneMapCoeffs2("Parameters for neutral tonemap", Vector) = (0.0, 0.0, 0.0, 0.0)
        _Exposure("Exposure", Range(-32.0, 32.0)) = 0
        [ToggleOff] _EnableToneMap("Enable Tone Map", Float) = 0
    }

    SubShader {
        Pass {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 5.0

            #include "Common.hlsl"
            #include "Color.hlsl"
            #include "Assets/ScriptableRenderLoop/HDRenderPipeline/ShaderVariables.hlsl"

  			TEXTURE2D(_MainTex);
			SAMPLER2D(sampler_MainTex);

            float4      _ToneMapCoeffs1;
            float4      _ToneMapCoeffs2;

            #define InBlack         _ToneMapCoeffs1.x
            #define OutBlack        _ToneMapCoeffs1.y
            #define InWhite         _ToneMapCoeffs1.z
            #define OutWhite        _ToneMapCoeffs1.w
            #define WhiteLevel      _ToneMapCoeffs2.z
            #define WhiteClip       _ToneMapCoeffs2.w

            float _Exposure;
            float _EnableToneMap;

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

            float3 evalCurve(float3 x, float A, float B, float C, float D, float E, float F)
            {
                return ((x*(A*x + C*B) + D*E) / (x*(A*x + B) + D*F)) - E / F;
            }

            float3 applyTonemapFilmicAD(float3 linearColor)
            {
                float blackRatio = InBlack / OutBlack;
                float whiteRatio = InWhite / OutWhite;

                // blend tunable coefficients
                float B = lerp(0.57, 0.37, blackRatio);
                float C = lerp(0.01, 0.24, whiteRatio);
                float D = lerp(0.02, 0.20, blackRatio);

                // constants
                float A = 0.2;
                float E = 0.02;
                float F = 0.30;

                // eval and correct for white point
                float3 whiteScale = 1.0f / evalCurve(WhiteLevel, A, B, C, D, E, F);
                float3 curr = evalCurve(linearColor * whiteScale, A, B, C, D, E, F);

                return curr * whiteScale;
            }

            float3 remapWhite(float3 inPixel, float whitePt)
            {
                //  var breakout for readability
                const float inBlack = 0;
                const float outBlack = 0;
                float inWhite = whitePt;
                const float outWhite = 1;

                // remap input range to output range
                float3 outPixel = ((inPixel.rgb) - inBlack.xxx) / (inWhite.xxx - inBlack.xxx) * (outWhite.xxx - outBlack.xxx) + outBlack.xxx;
                return (outPixel.rgb);
            }

            float3 NeutralTonemap(float3 x)
            {
                float3 finalColor = applyTonemapFilmicAD(x); // curve (dynamic coeffs differ per level)
                finalColor = remapWhite(finalColor, WhiteClip); // post-curve white point adjustment
                finalColor = saturate(finalColor);
                return finalColor;
            }

            float3 ApplyToneMap(float3 color)
            {
                if (_EnableToneMap > 0.0)
                {
                    return NeutralTonemap(color);
                }
                else
                {
                    return saturate(color);
                }
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texcoord);
                // Gamma correction

                // TODO: Currenlt in the editor there a an additional pass were the result is copyed in a render target RGBA8_sRGB.
                // So we must not correct the sRGB here else it will be done two time.
                // To fix!

                c.rgb = ApplyToneMap(c.rgb * exp2(_Exposure));

                // return LinearToSRGB(c);
                return c;


            }
            ENDHLSL

        }
    }
    Fallback Off
}
