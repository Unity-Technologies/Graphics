Shader "Hidden/Universal Render Pipeline/TemporalAA"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile_local _ _USE_RGBM
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        TEXTURE2D_X(_SourceTex);
        float4 _SourceTex_TexelSize;
        TEXTURE2D_X(_SourceTexLowMip);
        float4 _SourceTexLowMip_TexelSize;


        TEXTURE2D_X(_AccumulationTex);
        TEXTURE2D_X(_MotionVectorTexture);

        float4 _Params; // x: scatter, y: clamp, z: threshold (linear), w: threshold knee

        #define Scatter             _Params.x
        #define ClampMax            _Params.y
        #define Threshold           _Params.z
        #define ThresholdKnee       _Params.w

        half4 EncodeHDR(half3 color)
        {
        #if _USE_RGBM
            half4 outColor = EncodeRGBM(color);
        #else
            half4 outColor = half4(color, 1.0);
        #endif

        #if UNITY_COLORSPACE_GAMMA
            return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
        #else
            return outColor;
        #endif
        }


        half3 DecodeHDR(half4 color)
        {
        #if UNITY_COLORSPACE_GAMMA
            color.xyz *= color.xyz; // γ to linear
        #endif

        #if _USE_RGBM
            return DecodeRGBM(color);
        #else
            return color.xyz;
        #endif
        }

        half4 TaaTest(Varyings input) : SV_Target
        {

            //half depth = LoadSceneDepth(input.uv.xy).x;

            float depth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, input.uv.xy).r;

        #if UNITY_REVERSED_Z
            depth = 1.0 - depth;
        #endif

            depth = 2.0 * depth - 1.0;

            //float3 viewPos = ComputeViewSpacePosition(input.uv.zw, depth, unity_CameraInvProjection);
            //float4 worldPos = float4(mul(unity_CameraToWorld, float4(viewPos, 1.0)).xyz, 1.0);
            //float3 worldP = worldPos.xyz / worldPos.w;

            //outDepth = depth;
            half2 screenSize = _ScreenSize.zw;



            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float texelSize = _SourceTex_TexelSize.x * 2.0;
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

            half2 motionVec = SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_LinearClamp, uv);
            half2 prevUv = uv -motionVec; // motionVec is offset in NDC space [0,1]

            //half3 accumulation = DecodeHDR(SAMPLE_TEXTURE2D_X(_AccumulationTex, sampler_LinearClamp, uv));
            half3 accumulation = DecodeHDR(SAMPLE_TEXTURE2D_X(_AccumulationTex, sampler_LinearClamp, prevUv));

            // 9-tap gaussian blur on the downsampled source
            half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2(-1.0,-1.0)));
            half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2( 0.0,-1.0)));
            half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2( 1.0,-1.0)));
            half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2(-1.0, 0.0)));
            half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2( 0.0, 0.0)));
            half3 c5 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2( 1.0, 0.0)));
            half3 c6 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2(-1.0, 1.0)));
            half3 c7 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2( 0.0, 1.0)));
            half3 c8 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + _SourceTex_TexelSize.xy*float2( 1.0, 1.0)));


            half3 boxMax = c0;
            boxMax = max(boxMax, c1);
            boxMax = max(boxMax, c2);
            boxMax = max(boxMax, c3);
            boxMax = max(boxMax, c4);
            boxMax = max(boxMax, c5);
            boxMax = max(boxMax, c6);
            boxMax = max(boxMax, c7);
            boxMax = max(boxMax, c8);

            half3 boxMin = c0;
            boxMin = min(boxMin, c1);
            boxMin = min(boxMin, c2);
            boxMin = min(boxMin, c3);
            boxMin = min(boxMin, c4);
            boxMin = min(boxMin, c5);
            boxMin = min(boxMin, c6);
            boxMin = min(boxMin, c7);
            boxMin = min(boxMin, c8);

            half3 clampAccum = clamp(accumulation, boxMin, boxMax);
            //half3 clampAccum = max(boxMin,accumulation);

            half3 color = lerp(clampAccum, c4, 0.04f);

            //color = saturate(frac(depth * 100));
            //color = saturate(frac(worldP));

            //color = half3(saturate(motionVec*100), 0.0f);


            color = float3(saturate(motionVec * 100), .0f);





                //half3(c4.x,0,0);

            return EncodeHDR(color);
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "TAA Test"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment TaaTest
            ENDHLSL
        }
    }
}
