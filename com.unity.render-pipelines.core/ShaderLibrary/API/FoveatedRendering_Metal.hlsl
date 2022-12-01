#ifndef UNITY_FOVEATED_RENDERING_METAL_INCLUDED
#define UNITY_FOVEATED_RENDERING_METAL_INCLUDED

#if defined(SHADER_API_METAL) && defined(_FOVEATED_RENDERING_NON_UNIFORM_RASTER) && !defined(UNITY_COMPILER_DXC)

    // These are tokens that hlslcc is looking for in order
    // to inject variable rasterization rate MSL code.
    // DO NOT RENAME unless you also change logic in translation
    float3 _UV_HlslccVRRDistort0 = float3(0.0, 0.0, 0.0);
    float3 _UV_HlslccVRRDistort1 = float3(0.0, 0.0, 0.0);
    float3 _UV_HlslccVRRResolve0 = float3(0.0, 0.0, 0.0);
    float3 _UV_HlslccVRRResolve1 = float3(0.0, 0.0, 0.0);

    float2 RemapFoveatedRenderingResolve(float2 uv, bool yFlip = false)
    {
        if (yFlip)
            uv.y = 1.0 - uv.y;

        // TODO: This is not ideal looking code, but our hlsl to msl translation
        // layer can rearrange instructions while doing optimizations.
        // That can easily break things because we expect certain tokens and swizzles.
        // When changing this make sure to check the compiled msl code for foveation.
        if (unity_StereoEyeIndex == 1)
        {
            uv += _UV_HlslccVRRResolve0.yz;
            uv = uv * _UV_HlslccVRRResolve1.xy;
        }
        else
        {
            uv += _UV_HlslccVRRResolve1.yz;
            uv = uv * _UV_HlslccVRRResolve0.xy;
        }

        if (yFlip)
            uv.y = 1.0 - uv.y;

        return uv;
    }

    float2 RemapFoveatedRenderingDensity(float2 uv, bool yFlip = false)
    {
        // TODO: Implement density look up
        return uv;
    }

    float2 RemapFoveatedRenderingDistort(float2 uv, bool yFlip = false)
    {
        if (yFlip)
            uv.y = 1.0 - uv.y;

        // NOTE: Check comment for similar code in RemapFoveatedRenderingResolve
        if (unity_StereoEyeIndex == 1)
        {
            uv += _UV_HlslccVRRDistort0.yz;
            uv = uv * _UV_HlslccVRRDistort1.xy;
        }
        else
        {
            uv += _UV_HlslccVRRDistort1.yz;
            uv = uv * _UV_HlslccVRRDistort0.xy;
        }

        if (yFlip)
            uv.y = 1.0 - uv.y;

        return uv;
    }

    float2 RemapFoveatedRenderingDistortCS(float2 uv, bool yFlip = false)
    {
        uv /= _ScreenSize.xy;

        if (yFlip)
            uv.y = 1.0 - uv.y;

        // NOTE: Check comment for similar code in RemapFoveatedRenderingResolve
        if (unity_StereoEyeIndex == 1)
        {
            uv += _UV_HlslccVRRDistort0.yz;
            uv = uv * _UV_HlslccVRRDistort1.xy;
        }
        else
        {
            uv += _UV_HlslccVRRDistort1.yz;
            uv = uv * _UV_HlslccVRRDistort0.xy;
        }

        if (yFlip)
            uv.y = 1.0 - uv.y;

        return uv * _ScreenSize.xy;
    }

#endif // SHADER_API_METAL && _FOVEATED_RENDERING_NON_UNIFORM_RASTER && !UNITY_COMPILER_DXC

#endif // UNITY_FOVEATED_RENDERING_METAL_INCLUDED
