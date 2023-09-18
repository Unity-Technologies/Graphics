Shader "Hidden/HDRP/FinalPass"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #pragma multi_compile_fragment _ SCREEN_COORD_OVERRIDE
        #pragma multi_compile_local_fragment _ FXAA
        #pragma multi_compile_local_fragment _ GRAIN
        #pragma multi_compile_local_fragment _ DITHER
        #pragma multi_compile_local_fragment _ ENABLE_ALPHA
        #pragma multi_compile_local_fragment _ APPLY_AFTER_POST
        #pragma multi_compile_local_fragment _ HDR_INPUT HDR_ENCODING

        #pragma multi_compile_local_fragment _ CATMULL_ROM_4 RCAS BYPASS
        #define DEBUG_UPSCALE_POINT 0

        #ifdef HDR_ENCODING
        #define HDR_INPUT 1 // this should be defined when HDR_ENCODING is defined
        #endif

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ScreenCoordOverride.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"
#if defined(HDR_ENCODING)
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/HDROutput.hlsl"
#endif

        TEXTURE2D_X(_InputTexture);
        TEXTURE2D(_GrainTexture);

        TEXTURE2D_X(_AfterPostProcessTexture);
        TEXTURE2D_ARRAY(_BlueNoiseTexture);
        TEXTURE2D_X(_AlphaTexture);

        TEXTURE2D_X(_UITexture);

        SAMPLER(sampler_LinearClamp);
        SAMPLER(sampler_LinearRepeat);

        #define FSR_INPUT_TEXTURE _InputTexture
        #define FSR_INPUT_SAMPLER s_linear_clamp_sampler
        #if ENABLE_ALPHA
            // When alpha is in use, activate the alpha-passthrough mode in the RCAS implementation.
            // When this mode is active, ApplyRCAS returns a four component vector (rgba) instead of a three component vector (rgb).
            #define FSR_ENABLE_ALPHA 1
        #endif

        float2 _GrainParams;            // x: intensity, y: response
        float4 _GrainTextureParams;     // xy: _ScreenSize.xy / GrainTextureSize.xy, zw: (random offset in UVs) *  _GrainTextureParams.xy
        float3 _DitherParams;           // xy: _ScreenSize.xy / DitherTextureSize.xy, z: texture_id
        float4 _UVTransform;
        float4 _ViewPortSize;
        float  _KeepAlpha;

        float4 _HDROutputParams;
        float4 _HDROutputParams2;
        #define _MinNits            _HDROutputParams.x
        #define _MaxNits            _HDROutputParams.y
        #define _PaperWhite         _HDROutputParams.z
        #define _OneOverPaperWhite  _HDROutputParams.w
        #define _RangeReductionMode (int)_HDROutputParams2.x

        #define FSR_EASU_ONE_OVER_PAPER_WHITE _OneOverPaperWhite
        #include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/Shaders/FSRCommon.hlsl"

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
            output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
            return output;
        }

        CTYPE UpscaledResult(float2 UV)
        {
        #if DEBUG_UPSCALE_POINT
            return Nearest(_InputTexture, UV);
        #else
            #if CATMULL_ROM_4
                return CatmullRomFourSamples(_InputTexture, UV);
            #else
                return Nearest(_InputTexture, UV);
            #endif
        #endif
        }

        float4 Frag(Varyings input) : SV_Target0
        {

            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 positionNDC = input.texcoord;
            uint2 positionSS = input.texcoord * _ScreenSize.xy;
            uint2 scaledPositionSS = ((input.texcoord.xy * _UVTransform.xy) + _UVTransform.zw) * _ViewPortSize.xy;

            // Flip logic
            positionSS = positionSS * _UVTransform.xy + _UVTransform.zw * (_ScreenSize.xy - 1.0);
            positionNDC = positionNDC * _UVTransform.xy + _UVTransform.zw;

            #ifdef CATMULL_ROM_4
            CTYPE outColor = UpscaledResult(positionNDC.xy);
            #elif defined(RCAS)
            CTYPE outColor = ApplyRCAS(scaledPositionSS);
            #elif defined(BYPASS)
            CTYPE outColor = LOAD_TEXTURE2D_X(_InputTexture, scaledPositionSS).CTYPE_SWIZZLE;
            #else
            CTYPE outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).CTYPE_SWIZZLE;
            #endif

            #if !defined(ENABLE_ALPHA)
            float outAlpha = (_KeepAlpha == 1.0) ? LOAD_TEXTURE2D_X(_AlphaTexture, positionSS).x : 1.0;
            #endif

            #if FXAA
            CTYPE beforeFXAA = outColor;
            RunFXAA(_InputTexture, sampler_LinearClamp, outColor, positionSS, positionNDC, _PaperWhite, _OneOverPaperWhite);

            #if defined(ENABLE_ALPHA)
            // When alpha processing is enabled, FXAA should not affect pixels with zero alpha
            outColor.xyz = outColor.a > 0 ? outColor.xyz : beforeFXAA.xyz;
            #endif
            #endif //FXAA

            // Saturate is only needed for dither or grain to work. Otherwise we don't saturate because output might be HDR
            #if (defined(GRAIN) || defined(DITHER)) && !defined(HDR_INPUT)
            outColor = saturate(outColor);
            #endif

            #if GRAIN
            {
                // Grain in range [0;1] with neutral at 0.5
                float grain = SAMPLE_TEXTURE2D(_GrainTexture, s_linear_repeat_sampler, (SCREEN_COORD_APPLY_SCALEBIAS(positionNDC) * _GrainTextureParams.xy) + _GrainTextureParams.zw).w;

                // Remap [-1;1]
                grain = (grain - 0.5) * 2.0;

                // Noisiness response curve based on scene luminance
                float lum = Luminance(outColor);

                #ifdef HDR_INPUT
                // Color values are in nits. So divide by the paperWhite nits to get an approximation for perceptual luminance.
                lum *= _OneOverPaperWhite;
                #endif

                lum = 1.0 - sqrt(lum);
                lum = lerp(1.0, lum, _GrainParams.y);
                outColor.xyz += outColor.xyz * grain * _GrainParams.x * lum;
            }
            #endif

            #if DITHER
            // sRGB 8-bit dithering
            {
                float3 ditherParams = _DitherParams;
                // Symmetric triangular distribution on [-1,1] with maximal density at 0
                float noise = SAMPLE_TEXTURE2D_ARRAY(_BlueNoiseTexture, s_linear_repeat_sampler, positionNDC * ditherParams.xy, ditherParams.z).a;
                #ifdef HDR_INPUT
                float3 sRGBColor = LinearToSRGB(outColor.xyz * _OneOverPaperWhite);
                #else
                float3 sRGBColor = LinearToSRGB(outColor.xyz);
                #endif
                noise = noise * 2.0 - 1.0;
                noise = FastSign(noise) * (1.0 - sqrt(1.0 - abs(noise)));

                #ifdef HDR_INPUT
                outColor.xyz = SRGBToLinear(sRGBColor + noise / 255.0) * _PaperWhite;
                #else
                outColor.xyz = SRGBToLinear(sRGBColor + noise / 255.0);
                #endif
            }
            #endif

            // Apply AfterPostProcess target
            #if APPLY_AFTER_POST
            float4 afterPostColor = SAMPLE_TEXTURE2D_X_LOD(_AfterPostProcessTexture, s_point_clamp_sampler, positionNDC.xy * _RTHandleScale.xy, 0);
            #ifdef HDR_ENCODING
                afterPostColor.rgb = ProcessUIForHDR(afterPostColor.rgb, _PaperWhite, _MaxNits);
            #endif
            // After post objects are blended according to the method described here: https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            outColor.xyz = afterPostColor.a * outColor.xyz + afterPostColor.xyz;
            #endif


            #ifdef HDR_ENCODING
            // Screen space overlay blending.
            {
                float4 uiValue = SAMPLE_TEXTURE2D_X_LOD(_UITexture, s_point_clamp_sampler, positionNDC.xy * _RTHandleScale.xy, 0);
                outColor.rgb = SceneUIComposition(uiValue, outColor.rgb, _PaperWhite, _MaxNits);

                outColor.rgb = OETF(outColor.rgb, _MaxNits);
            }
            #endif

        #if !defined(ENABLE_ALPHA)
            return float4(outColor, outAlpha);
        #else
            return outColor;
        #endif
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
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
