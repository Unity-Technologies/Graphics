Shader "Hidden/FullScreen/Blur"
{
    HLSLINCLUDE

    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
    // #pragma enable_d3d11_debug_symbols

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"

    TEXTURE2D_X(_Source);
    float _SourceMip;
    float _Radius;
    float _SampleCount;
    float4 _ViewPortSize; // We need the viewport size because we have a non fullscreen render target (blur buffers are downsampled in half res)

    float4 Copy(Varyings varyings) : SV_Target
    {
        return LOAD_TEXTURE2D_X_LOD(_Source, varyings.positionCS.xy, _SourceMip);
    }

    float3 BlurPixels(float3 taps[9])
    {
        return 0.27343750 * (taps[4]          )
             + 0.21875000 * (taps[3] + taps[5])
             + 0.10937500 * (taps[2] + taps[6])
             + 0.03125000 * (taps[1] + taps[7])
             + 0.00390625 * (taps[0] + taps[8]);
    }

    // We need to clamp the UVs to avoid bleeding from bigger render tragets (when we have multiple cameras)
    float2 ClampUVs(float2 uv)
    {
        uv = clamp(uv, 0, _RTHandleScale.xy - _ViewPortSize.zw); // clamp UV to 1 pixel to avoid bleeding
        return uv;
    }

    float2 GetSampleUVs(Varyings varyings)
    {
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ViewPortSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
        return posInput.positionNDC.xy * _RTHandleScale;
    }

    float4 HorizontalBlur(Varyings varyings) : SV_Target
    {
        float2 texcoord = GetSampleUVs(varyings);

        // Horizontal blur from the camera color buffer
        float2 offset = _ScreenSize.zw * _Radius; // We don't use _ViewPortSize here because we want the offset to be the same between all the blur passes.
        float3 taps[9];
        for (int i = -4; i <= 4; i++)
        {
            float2 uv = ClampUVs(texcoord + float2(i, 0) * offset);
            taps[i + 4] = SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, uv, 0).rgb;
        }

        return float4(BlurPixels(taps), 1);
    }

    float4 VerticalBlur(Varyings varyings) : SV_Target
    {
        float2 texcoord = GetSampleUVs(varyings);

        // Vertical blur from the blur color buffer
        float2 offset = _ScreenSize.zw * _Radius;
        float3 taps[9];
        for (int i = -4; i <= 4; i++)
        {
            float2 uv = ClampUVs(texcoord + float2(0, i) * offset);
            taps[i + 4] = SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, uv, 0).rgb;
        }

        return float4(BlurPixels(taps), 1);
    }

    float4 CompositeMaskedBlur(Varyings varyings) : SV_Target
    {
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        float2 uv = ClampUVs(GetSampleUVs(varyings));

        float4 colorBuffer = SAMPLE_TEXTURE2D_X_LOD(_ColorBufferCopy, s_linear_clamp_sampler, uv, 0).rgba;
        float4 blurredBuffer = SAMPLE_TEXTURE2D_X_LOD(_Source, s_linear_clamp_sampler, uv, 0).rgba;
        float4 mask = SAMPLE_TEXTURE2D_X_LOD(_Mask, s_linear_clamp_sampler, uv, 0);
        float maskDepth = SAMPLE_TEXTURE2D_X_LOD(_MaskDepth, s_linear_clamp_sampler, uv, 0).r;
        float maskValue = 0;

        maskValue = any(mask.rgb > 0.1) || (maskDepth > depth - 0.0001);

        if (_InvertMask > 0.5)
            maskValue = !maskValue;

        return float4(lerp(blurredBuffer.rgb, colorBuffer.rgb, maskValue), colorBuffer.a);
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
            // Horizontal Blur
            Name "Horizontal Blur"

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
            // Vertical Blur
            Name "Vertical Blur"

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
