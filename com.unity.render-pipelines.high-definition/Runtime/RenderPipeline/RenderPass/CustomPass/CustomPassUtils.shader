Shader "Hidden/HDRP/CustomPassUtils"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    #pragma enable_d3d11_debug_symbols

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D_X(_Source);
    float       _SourceMip;
    float4      _ViewPortSize; // We need the viewport size because we have a non fullscreen render target (blur buffers are downsampled in half res)
    float4      _SourceScaleBias;
    float4      _ViewportScaleBias;

    float           _Radius;
    float           _SampleCount;
    Buffer<float>   _GaussianWeights;

    float2 GetScaledUVs(Varyings varyings)
    {
        // Remap UV from part of the screen (due to viewport scale / offset) to 0 - 1
        float2 uv01 = (varyings.positionCS.xy * _RTHandleScale.xy * _ViewPortSize.zw - _ViewportScaleBias.zw) * _ViewportScaleBias.xy;
        float2 uv = uv01;

        // Apply scale and bias
        return uv * _SourceScaleBias.xy + _SourceScaleBias.zw;
    }

    float4 Copy(Varyings varyings) : SV_Target
    {
        // TODO: handle scale and bias
        return LOAD_TEXTURE2D_X_LOD(_Source, varyings.positionCS.xy * _SourceScaleBias.xy + _SourceScaleBias.zw, _SourceMip);
    }

    // We need to clamp the UVs to avoid bleeding from bigger render tragets (when we have multiple cameras)
    float2 ClampUVs(float2 uv)
    {
        // Clamp UV to the current viewport:
        float2 offset = _ViewportScaleBias.zw * _RTHandleScale.xy;
        float2 halfPixelSize = _ViewPortSize.zw / 2;
        uv = clamp(uv, offset + halfPixelSize, rcp(_ViewportScaleBias.xy) * _RTHandleScale.xy + offset - halfPixelSize);
        return saturate(uv);
    }

    float4 Blur(float2 uv, float2 direction)
    {
        // Horizontal blur from the camera color buffer
        float4 result = 0;
        for (int i = 0; i < _SampleCount; i++)
        {
            float offset = lerp(-_Radius, _Radius, (float(i) / _SampleCount));
            float2 offsetUV = ClampUVs(uv + direction * offset * _ScreenSize.zw);
            float4 sourceValue = SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, offsetUV, 0);

            result += sourceValue * _GaussianWeights[i];
        }

        return result;

    }

    float4 HorizontalBlur(Varyings varyings) : SV_Target
    {
        float2 uv = GetScaledUVs(varyings);
        return Blur(uv, float2(1, 0));
    }

    float4 VerticalBlur(Varyings varyings) : SV_Target
    {
        float2 uv = GetScaledUVs(varyings);
        return Blur(uv, float2(0, 1));
    }

    float4 DownSample(Varyings varyings) : SV_Target
    {
        float2 uv = GetScaledUVs(varyings);
        // TODO: add half pixel offset ?
        return SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, uv, 0);
    }

    // float4 CompositeMaskedBlur(Varyings varyings) : SV_Target
    // {
    //     float depth = LoadCameraDepth(varyings.positionCS.xy);
    //     float2 uv = ClampUVs(GetSampleUVs(varyings));

    //     float4 colorBuffer = SAMPLE_TEXTURE2D_X_LOD(_ColorBufferCopy, s_linear_clamp_sampler, uv, 0).rgba;
    //     float4 blurredBuffer = SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, uv, 0).rgba;
    //     float4 mask = SAMPLE_TEXTURE2D_X_LOD(_Mask, s_linear_clamp_sampler, uv, 0);
    //     float maskDepth = SAMPLE_TEXTURE2D_X_LOD(_MaskDepth, s_linear_clamp_sampler, uv, 0).r;
    //     float maskValue = 0;

    //     maskValue = any(mask.rgb > 0.1) || (maskDepth > depth - 0.0001);

    //     if (_InvertMask > 0.5)
    //         maskValue = !maskValue;

    //     return float4(lerp(blurredBuffer.rgb, colorBuffer.rgb, maskValue), colorBuffer.a);
    // }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "Copy"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment Copy
            ENDHLSL
        }

        Pass
        {
            Name "Downsample"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment DownSample
            ENDHLSL
        }

        Pass
        {
            Name "HorizontalBlur"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment HorizontalBlur
            ENDHLSL
        }

        Pass
        {
            Name "VerticalBlur"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment VerticalBlur
            ENDHLSL
        }
    }
    Fallback Off
}
