Shader "Hidden/HDRP/FinalPass"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma editor_sync_compilation
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch

        #pragma multi_compile_local _ FXAA
        #pragma multi_compile_local _ GRAIN
        #pragma multi_compile_local _ DITHER
        #pragma multi_compile_local _ ENABLE_ALPHA
        #pragma multi_compile_local _ APPLY_AFTER_POST

        #pragma multi_compile_local _ BILINEAR CATMULL_ROM_4 LANCZOS CONTRASTADAPTIVESHARPEN
        #define DEBUG_UPSCALE_POINT 0

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/PostProcessDefines.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

        TEXTURE2D_X(_InputTexture);
        TEXTURE2D(_GrainTexture);
        TEXTURE2D_X(_AfterPostProcessTexture);
        TEXTURE2D_ARRAY(_BlueNoiseTexture);
        TEXTURE2D_X(_AlphaTexture);

        SAMPLER(sampler_LinearClamp);
        SAMPLER(sampler_LinearRepeat);

        float2 _GrainParams;            // x: intensity, y: response
        float4 _GrainTextureParams;     // xy: _ScreenSize.xy / GrainTextureSize.xy, zw: (random offset in UVs) *  _GrainTextureParams.xy
        float3 _DitherParams;           // xy: _ScreenSize.xy / DitherTextureSize.xy, z: texture_id
        float4 _UVTransform;
        float4 _ViewPortSize;
        float  _KeepAlpha;

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
            #if BILINEAR
                return Bilinear(_InputTexture, UV);
            #elif CATMULL_ROM_4
                return CatmullRomFourSamples(_InputTexture, UV);
            #elif LANCZOS
                return Lanczos(_InputTexture, UV, _ViewPortSize);
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

            // Flip logic
            positionSS = positionSS * _UVTransform.xy + _UVTransform.zw * (_ScreenSize.xy - 1.0);
            positionNDC = positionNDC * _UVTransform.xy + _UVTransform.zw;

            #if defined(BILINEAR) || defined(CATMULL_ROM_4) || defined(LANCZOS)
            CTYPE outColor = UpscaledResult(positionNDC.xy);
            #elif defined(CONTRASTADAPTIVESHARPEN)
            CTYPE outColor = LOAD_TEXTURE2D_X(_InputTexture, ((input.texcoord.xy * _UVTransform.xy) + _UVTransform.zw) * _ViewPortSize.xy).CTYPE_SWIZZLE;
            #else
            CTYPE outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).CTYPE_SWIZZLE;
            #endif

            #if !defined(ENABLE_ALPHA)
            float outAlpha = (_KeepAlpha == 1.0) ? LOAD_TEXTURE2D_X(_AlphaTexture, positionSS).x : 1.0;
            #endif

            #if FXAA
            RunFXAA(_InputTexture, sampler_LinearClamp, outColor.rgb, positionSS, positionNDC);
            #endif

            // Saturate is only needed for dither or grain to work. Otherwise we don't saturate because output might be HDR
            #if defined(GRAIN) || defined(DITHER)
            outColor = saturate(outColor);
            #endif


            #if GRAIN
            {
                // Grain in range [0;1] with neutral at 0.5
                float grain = SAMPLE_TEXTURE2D(_GrainTexture, s_linear_repeat_sampler, (positionNDC * _GrainTextureParams.xy) + _GrainTextureParams.zw).w;

                // Remap [-1;1]
                grain = (grain - 0.5) * 2.0;

                // Noisiness response curve based on scene luminance
                float lum = 1.0 - sqrt(Luminance(outColor));
                lum = lerp(1.0, lum, _GrainParams.y);

                outColor += outColor * grain * _GrainParams.x * lum;
            }
            #endif

            #if DITHER
            // sRGB 8-bit dithering
            {
                float3 ditherParams = _DitherParams;
                // Symmetric triangular distribution on [-1,1] with maximal density at 0
                float noise = SAMPLE_TEXTURE2D_ARRAY(_BlueNoiseTexture, s_linear_repeat_sampler, positionNDC * ditherParams.xy, ditherParams.z).a;
                float3 sRGBColor = LinearToSRGB(outColor.xyz);
                noise = noise * 2.0 - 1.0;
                noise = FastSign(noise) * (1.0 - sqrt(1.0 - abs(noise)));

                //outColor += noise / 255.0;
                outColor.xyz = SRGBToLinear(sRGBColor + noise / 255.0);
            }
            #endif

            // Apply AfterPostProcess target
            #if APPLY_AFTER_POST
            float4 afterPostColor = SAMPLE_TEXTURE2D_X_LOD(_AfterPostProcessTexture, s_point_clamp_sampler, positionNDC.xy * _RTHandleScale.xy, 0);
            // After post objects are blended according to the method described here: https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            outColor.xyz = afterPostColor.a * outColor.xyz + afterPostColor.xyz;
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
