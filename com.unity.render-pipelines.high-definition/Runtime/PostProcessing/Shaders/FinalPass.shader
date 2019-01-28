Shader "Hidden/HDRP/FinalPass"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #pragma multi_compile_local _ FXAA
        #pragma multi_compile_local _ GRAIN

        #pragma multi_compile _ BILINEAR CATMULL_ROM_4 LANCZOS
        #define DEBUG_UPSCALE_POINT 0

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

        #define FXAA_HDR_MAPUNMAP   0
        #define FXAA_SPAN_MAX       (8.0)
        #define FXAA_REDUCE_MUL     (1.0 / 8.0)
        #define FXAA_REDUCE_MIN     (1.0 / 128.0)

        TEXTURE2D(_InputTexture);
        TEXTURE2D(_GrainTexture);
        TEXTURE2D_ARRAY(_BlueNoiseTexture);

        SAMPLER(sampler_LinearClamp);
        SAMPLER(sampler_LinearRepeat);

        float2 _GrainParams;            // x: intensity, y: response
        float4 _GrainTextureParams;     // x: width, y: height, zw: random offset
        float3 _DitherParams;           // x: width, y: height, z: texture_id
        float4 _UVTransform;

        struct Attributes
        {
            uint vertexID : SV_VertexID;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 texcoord   : TEXCOORD0;
        };

        Varyings Vert(Attributes input)
        {
            Varyings output;
            output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        float3 UpscaledResult(float2 UV)
        {
        #if DEBUG_UPSCALE_POINT
            return Nearest(_InputTexture, UV);
        #else 
            #if BILINEAR
                return Bilinear(_InputTexture, UV);
            #elif CATMULL_ROM_4
                return CatmullRomFourSamples(_InputTexture, UV);
            #elif LANCZOS
                return Lanczos(_InputTexture, UV);
            #else
                return Nearest(_InputTexture, UV);
            #endif
        #endif
        }

        float3 Load(int2 icoords, int idx, int idy)
        {
            return LOAD_TEXTURE2D(_InputTexture, min(icoords + int2(idx, idy), _ScreenSize.xy - 1.0)).xyz;
        }

        float3 GetColor(Varyings input, out uint2 positionSS)
        {
            float2 positionNDC = input.texcoord;
            positionSS = input.texcoord * _ScreenSize.xy;

        #if UNITY_SINGLE_PASS_STEREO
            // TODO: This is wrong, fix me
            positionNDC.x = positionNDC.x / 2.0 + unity_StereoEyeIndex * 0.5;
            positionSS.x = positionSS.x / 2;
        #endif

            // Flip logic
            positionSS = positionSS * _UVTransform.xy + _UVTransform.zw * (_ScreenSize.xy - 1.0);
            positionNDC = positionNDC * _UVTransform.xy + _UVTransform.zw;

        #if defined(BILINEAR) || defined(CATMULL_ROM_4) || defined(LANCZOS)
            float3 outColor = UpscaledResult(positionNDC.xy);
        #else
            float3 outColor = Load(positionSS, 0, 0);
        #endif

            #if FXAA
            RunFXAA(_InputTexture, sampler_LinearClamp, outColor, positionSS, positionNDC);
            #endif

            outColor = saturate(outColor);

            #if GRAIN
            {
                // Grain in range [0;1] with neutral at 0.5
                uint2 icoords = fmod(positionSS + _GrainTextureParams.zw, _GrainTextureParams.xy);
                float grain = LOAD_TEXTURE2D(_GrainTexture, icoords).w;

                // Remap [-1;1]
                grain = (grain - 0.5) * 2.0;

                // Noisiness response curve based on scene luminance
                float lum = 1.0 - sqrt(Luminance(outColor));
                lum = lerp(1.0, lum, _GrainParams.y);

                outColor += outColor * grain * _GrainParams.x * lum;
            }
            #endif

            return outColor;
        }

        float4 FragNoDither(Varyings input) : SV_Target0
        {
            uint2 positionSS;
            return float4(GetColor(input, positionSS), 1.0);
        }

        float4 FragDither(Varyings input) : SV_Target0
        {
            uint2 positionSS;
            float3 outColor = GetColor(input, positionSS);

            // sRGB 8-bit dithering
            {
                float3 ditherParams = _DitherParams;
                uint2 icoords = fmod(positionSS, ditherParams.xy);

                // Symmetric triangular distribution on [-1,1] with maximal density at 0
                float noise = LOAD_TEXTURE2D_ARRAY(_BlueNoiseTexture, icoords, ditherParams.z).a * 2.0 - 1.0;
                noise = FastSign(noise) * (1.0 - sqrt(1.0 - abs(noise)));

                //outColor += noise / 255.0;
                outColor = SRGBToLinear(LinearToSRGB(outColor) + noise / 255.0);
            }

            return float4(outColor, 1.0);
        }

    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }

        Pass
        {
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment FragNoDither

            ENDHLSL
        }

        Pass
        {
            Cull Off ZWrite Off ZTest Always

            HLSLPROGRAM

                #pragma vertex Vert
                #pragma fragment FragDither

            ENDHLSL
        }
    }
}
