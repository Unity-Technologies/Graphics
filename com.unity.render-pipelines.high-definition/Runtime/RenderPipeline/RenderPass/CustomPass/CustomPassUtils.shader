Shader "Hidden/HDRP/CustomPassUtils"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch
    // #pragma enable_d3d11_debug_symbols

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D_X(_Source);
    float       _SourceMip;
    float4      _ViewPortSize; // We need the viewport size because we have a non fullscreen render target (blur buffers are downsampled in half res)
    float4      _SourceScaleBias;
    float4      _ViewportScaleBias;
    float4      _SourceSize;
    float4      _SourceScaleFactor;

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
        float2 uv01 = (varyings.positionCS.xy * _ViewPortSize.zw - _ViewportScaleBias.zw) * _ViewportScaleBias.xy;
        // Apply scale and bias
        float2 uv = uv01 * _SourceScaleBias.xy + _SourceScaleBias.zw;

        return LOAD_TEXTURE2D_X_LOD(_Source, uv * _SourceSize.xy, _SourceMip);
    }

    // We need to clamp the UVs to avoid bleeding from bigger render tragets (when we have multiple cameras)
    float2 ClampUVs(float2 uv)
    {
        // Clamp UV to the current viewport:
        // Note that uv here are scaled with _RTHandleScale.xy to support sampling from RTHandle
        float2 offset = _ViewportScaleBias.zw * _RTHandleScale.xy;
        float2 size = rcp(_ViewportScaleBias.xy) * _RTHandleScale.xy;
        float2 halfPixelSize = _SourceSize.zw * _RTHandleScale.xy / 2 * _SourceScaleFactor.zw;
        uv = clamp(uv, offset + halfPixelSize, size + offset - halfPixelSize);
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
        return SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, uv, 0);
    }

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
