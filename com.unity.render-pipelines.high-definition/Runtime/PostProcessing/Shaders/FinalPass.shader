Shader "Hidden/HDRP/FinalPass"
{
    HLSLINCLUDE

        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

        #pragma multi_compile_local _ FXAA
        #pragma multi_compile_local _ GRAIN

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

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

        float3 Fetch(float2 coords, float2 offset)
        {
            float2 uv = saturate(coords + offset) * _ScreenToTargetScale.xy;
            return SAMPLE_TEXTURE2D_LOD(_InputTexture, sampler_LinearClamp, uv, 0.0).xyz;
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

            float3 outColor = Load(positionSS, 0, 0);

            #if FXAA
            {
                // Edge detection
                float3 rgbNW = Load(positionSS, -1, -1);
                float3 rgbNE = Load(positionSS,  1, -1);
                float3 rgbSW = Load(positionSS, -1,  1);
                float3 rgbSE = Load(positionSS,  1,  1);

                #if !FXAA_HDR_MAPUNMAP
                rgbNW = saturate(rgbNW);
                rgbNE = saturate(rgbNE);
                rgbSW = saturate(rgbSW);
                rgbSE = saturate(rgbSE);
                outColor = saturate(outColor);
                #endif

                float lumaNW = Luminance(rgbNW);
                float lumaNE = Luminance(rgbNE);
                float lumaSW = Luminance(rgbSW);
                float lumaSE = Luminance(rgbSE);
                float lumaM  = Luminance(outColor);

                float2 dir;
                dir.x = -((lumaNW + lumaNE) - (lumaSW + lumaSE));
                dir.y =  ((lumaNW + lumaSW) - (lumaNE + lumaSE));

                float lumaSum   = lumaNW + lumaNE + lumaSW + lumaSE;
                float dirReduce = max(lumaSum * (0.25 * FXAA_REDUCE_MUL), FXAA_REDUCE_MIN);
                float rcpDirMin = rcp(min(abs(dir.x), abs(dir.y)) + dirReduce);

                dir = min((FXAA_SPAN_MAX).xx, max((-FXAA_SPAN_MAX).xx, dir * rcpDirMin)) * _ScreenSize.zw;

                // Blur
                float3 rgb03 = Fetch(positionNDC, dir * (0.0 / 3.0 - 0.5));
                float3 rgb13 = Fetch(positionNDC, dir * (1.0 / 3.0 - 0.5));
                float3 rgb23 = Fetch(positionNDC, dir * (2.0 / 3.0 - 0.5));
                float3 rgb33 = Fetch(positionNDC, dir * (3.0 / 3.0 - 0.5));

                #if FXAA_HDR_MAPUNMAP
                rgb03 = FastTonemap(rgb03);
                rgb13 = FastTonemap(rgb13);
                rgb23 = FastTonemap(rgb23);
                rgb33 = FastTonemap(rgb33);
                #else
                rgb03 = saturate(rgb03);
                rgb13 = saturate(rgb13);
                rgb23 = saturate(rgb23);
                rgb33 = saturate(rgb33);
                #endif

                float3 rgbA = 0.5 * (rgb13 + rgb23);
                float3 rgbB = rgbA * 0.5 + 0.25 * (rgb03 + rgb33);

                float lumaB = Luminance(rgbB);

                float lumaMin = Min3(lumaM, lumaNW, Min3(lumaNE, lumaSW, lumaSE));
                float lumaMax = Max3(lumaM, lumaNW, Max3(lumaNE, lumaSW, lumaSE));

                float3 rgb = ((lumaB < lumaMin) || (lumaB > lumaMax)) ? rgbA : rgbB;

                #if FXAA_HDR_MAPUNMAP
                outColor = FastTonemapInvert(rgb);
                #else
                outColor = rgb;
                #endif
            }
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
