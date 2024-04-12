Shader "Hidden/Universal Render Pipeline/GaussianDepthOfField"
{
    HLSLINCLUDE

        #pragma target 3.5

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        TEXTURE2D_X(_ColorTexture);
        TEXTURE2D_X(_FullCoCTexture);
        TEXTURE2D_X(_HalfCoCTexture);

        float4 _SourceSize;
        float4 _DownSampleScaleFactor;

        float3 _CoCParams;

        #define FarStart        _CoCParams.x
        #define FarEnd          _CoCParams.y
        #define MaxRadius       _CoCParams.z

        #define BLUR_KERNEL 0

        #if BLUR_KERNEL == 0

        // Offsets & coeffs for optimized separable bilinear 3-tap gaussian (5-tap equivalent)
        const static int kTapCount = 3;
        const static float kOffsets[] = {
            -1.33333333,
             0.00000000,
             1.33333333
        };
        const static half kCoeffs[] = {
             0.35294118,
             0.29411765,
             0.35294118
        };

        #elif BLUR_KERNEL == 1

        // Offsets & coeffs for optimized separable bilinear 5-tap gaussian (9-tap equivalent)
        const static int kTapCount = 5;
        const static float kOffsets[] = {
            -3.23076923,
            -1.38461538,
             0.00000000,
             1.38461538,
             3.23076923
        };
        const static half kCoeffs[] = {
             0.07027027,
             0.31621622,
             0.22702703,
             0.31621622,
             0.07027027
        };

        #endif

        half FragCoC(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            float depth = LOAD_TEXTURE2D_X(_CameraDepthTexture, _SourceSize.xy * uv).x;
            depth = LinearEyeDepth(depth, _ZBufferParams);
            half coc = (depth - FarStart) / (FarEnd - FarStart);
            return saturate(coc);
        }

        struct PrefilterOutput
        {
            half  coc   : SV_Target0;
            half4 color : SV_Target1;
        };

        PrefilterOutput FragPrefilter(Varyings input)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        #if _HIGH_QUALITY_SAMPLING

            // Use a rotated grid to minimize artifacts coming from horizontal and vertical boundaries
            // "High Quality Antialiasing" [Lorach07]
            const int kCount = 5;
            const float2 kTaps[] = {
                float2( 0.0,  0.0),
                float2( 0.9, -0.4),
                float2(-0.9,  0.4),
                float2( 0.4,  0.9),
                float2(-0.4, -0.9)
            };

            half4 colorAcc = 0.0;
            half farCoCAcc = 0.0;

            UNITY_UNROLL
            for (int i = 0; i < kCount; i++)
            {
                float2 tapCoord = _SourceSize.zw * kTaps[i] + uv;
                half4 tapColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, tapCoord);
                half coc = SAMPLE_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, tapCoord).x;

                // Pre-multiply CoC to reduce bleeding of background blur on focused areas
                colorAcc += tapColor * coc;
                farCoCAcc += coc;
            }

            half4 color = colorAcc * rcp(kCount);
            half farCoC = farCoCAcc * rcp(kCount);

        #else

            // Bilinear sampling the coc is technically incorrect but we're aiming for speed here
            half farCoC = SAMPLE_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, uv).x;

            // Fast bilinear downscale of the source target and pre-multiply the CoC to reduce
            // bleeding of background blur on focused areas
            half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            color *= farCoC;

        #endif

            PrefilterOutput o;
            o.coc   = farCoC;
            #if _ENABLE_ALPHA_OUTPUT
            o.color = color;
            #else
            o.color = half4(color.xyz, 0);
            #endif
            return o;
        }

        half4 Blur(Varyings input, float2 dir, float premultiply)
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            // Use the center CoC as radius
            int2 positionSS = int2(_SourceSize.xy * _DownSampleScaleFactor.xy * uv);
            half samp0CoC = LOAD_TEXTURE2D_X(_HalfCoCTexture, positionSS).x;

            float2 offset = _SourceSize.zw * _DownSampleScaleFactor.zw * dir * samp0CoC * MaxRadius;
            half4 acc = 0.0;
            half accAlpha = 0.0;

            UNITY_UNROLL
            for (int i = 0; i < kTapCount; i++)
            {
                float2 sampCoord = uv + kOffsets[i] * offset;
                half sampCoC = SAMPLE_TEXTURE2D_X(_HalfCoCTexture, sampler_LinearClamp, sampCoord).x;
                half4 sampColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, sampCoord);

                // Weight & pre-multiply to limit bleeding on the focused area
                half weight = saturate(1.0 - (samp0CoC - sampCoC));
                acc += half4(sampColor.xyz, premultiply ? sampCoC : 1.0) * kCoeffs[i] * weight;

                #if _ENABLE_ALPHA_OUTPUT
                accAlpha  += sampColor.a * kCoeffs[i] * weight;
                #endif

            }
            acc.xyz /= acc.w + 1e-4; // Zero-div guard

            #if _ENABLE_ALPHA_OUTPUT
            accAlpha /= acc.w + 1e-4; // Zero-div guard
            return half4(acc.xyz, accAlpha);
            #else
            return half4(acc.xyz, 1.0);
            #endif
        }

        half4 FragBlurH(Varyings input) : SV_Target
        {
            return Blur(input, float2(1.0, 0.0), 1.0);
        }

        half4 FragBlurV(Varyings input) : SV_Target
        {
            return Blur(input, float2(0.0, 1.0), 0.0);
        }

        half4 FragComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            half4 baseColor = LOAD_TEXTURE2D_X(_BlitTexture, _SourceSize.xy * uv);
            half coc = LOAD_TEXTURE2D_X(_FullCoCTexture, _SourceSize.xy * uv).x;

        #if _HIGH_QUALITY_SAMPLING
            half4 farColor = SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_ColorTexture, sampler_LinearClamp), uv, _SourceSize * _DownSampleScaleFactor, 1.0, unity_StereoEyeIndex);
        #else
            half4 farColor = SAMPLE_TEXTURE2D_X(_ColorTexture, sampler_LinearClamp, uv);
        #endif

            half4 dstColor = 0.0;
            half dstAlpha = 1.0;

            UNITY_BRANCH
            if (coc > 0.0)
            {
                // Non-linear blend
                // "CryEngine 3 Graphics Gems" [Sousa13]
                half blend = sqrt(coc * TWO_PI);
                dstColor = farColor * saturate(blend);
                dstAlpha = saturate(1.0 - blend);
            }

            #if _ENABLE_ALPHA_OUTPUT
            half4 outColor = dstColor + baseColor * dstAlpha;
            // Preserve the original value of the pixels with zero alpha
            outColor.rgb = outColor.a > 0 ? outColor.rgb : baseColor.rgb;
            return outColor;
            #else
            return half4(dstColor.rgb + baseColor.rgb * dstAlpha, 1.0);
            #endif
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Gaussian Depth Of Field CoC"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCoC
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Depth Of Field Prefilter"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPrefilter
                #pragma multi_compile_local_fragment _ _HIGH_QUALITY_SAMPLING
                #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Depth Of Field Blur Horizontal"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurH

                #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Depth Of Field Blur Vertical"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlurV

                #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT
            ENDHLSL
        }

        Pass
        {
            Name "Gaussian Depth Of Field Composite"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragComposite
                #pragma multi_compile_local_fragment _ _HIGH_QUALITY_SAMPLING
                #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT
            ENDHLSL
        }
    }
}
