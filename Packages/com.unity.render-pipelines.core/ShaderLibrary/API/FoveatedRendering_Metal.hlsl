#ifndef UNITY_FOVEATED_RENDERING_METAL_INCLUDED
#define UNITY_FOVEATED_RENDERING_METAL_INCLUDED

#if !defined(UNITY_COMPILER_DXC) && (defined(UNITY_PLATFORM_OSX) || defined(UNITY_PLATFORM_IOS))

    // These are tokens that hlslcc is looking for in order to inject Variable Rasterization Rate MSL code.
    // DO NOT RENAME unless you also change logic in translation.
    // They should be used in conjunction with a 'mad' instruction where the order of parameters must be:
    // Param 1 - uv to be remapped
    // Param 2 - token
    // Param 3 - stereo eye index
    float2 _UV_HlslccVRRDistort;
    float2 _UV_HlslccVRRResolve;

    float2 RemapFoveatedRenderingLinearToNonUniform(float2 uv, bool yFlip = false)
    {
        if (yFlip)
            uv.y = 1.0 - uv.y;

        uv = mad(uv, _UV_HlslccVRRResolve, unity_StereoEyeIndex);

        if (yFlip)
            uv.y = 1.0 - uv.y;

        return uv;
    }

    float2 RemapFoveatedRenderingPrevFrameLinearToNonUniform(float2 uv, bool yFlip = false)
    {
        // TODO : implement me to support eye tracking that can change the remap each frame
        return RemapFoveatedRenderingLinearToNonUniform(uv, yFlip);
    }

    float2 RemapFoveatedRenderingDensity(float2 uv, bool yFlip = false)
    {
        // TODO: Implement density look up
        return uv;
    }

    float2 RemapFoveatedRenderingPrevFrameDensity(float2 uv, bool yFlip = false)
    {
        // TODO : implement me to support eye tracking that can change the remap each frame
        return RemapFoveatedRenderingDensity(uv, yFlip);
    }

    float2 RemapFoveatedRenderingNonUniformToLinear(float2 uv, bool yFlip = false)
    {
        if (yFlip)
            uv.y = 1.0 - uv.y;

        uv = mad(uv, _UV_HlslccVRRDistort, unity_StereoEyeIndex);

        if (yFlip)
            uv.y = 1.0 - uv.y;

        return uv;
    }

    float2 RemapFoveatedRenderingPrevFrameNonUniformToLinear(float2 uv, bool yFlip = false)
    {
        // TODO : implement me to support eye tracking that can change the remap each frame
        return RemapFoveatedRenderingNonUniformToLinear(uv, yFlip);
    }

    float2 RemapFoveatedRenderingNonUniformToLinearCS(float2 uv, bool yFlip = false)
    {
        uv /= _ScreenSize.xy;

        if (yFlip)
            uv.y = 1.0 - uv.y;

        uv = mad(uv, _UV_HlslccVRRDistort, unity_StereoEyeIndex);

        if (yFlip)
            uv.y = 1.0 - uv.y;

        return uv * _ScreenSize.xy;
    }

    // Adapt old remap functions to their new name
    float2 RemapFoveatedRenderingResolve(float2 uv) { return RemapFoveatedRenderingLinearToNonUniform(uv); }
    float2 RemapFoveatedRenderingPrevFrameResolve(float2 uv) {return RemapFoveatedRenderingPrevFrameLinearToNonUniform(uv); }
    float2 RemapFoveatedRenderingDistort(float2 uv) { return RemapFoveatedRenderingNonUniformToLinear(uv); }
    float2 RemapFoveatedRenderingPrevFrameDistort(float2 uv) { return RemapFoveatedRenderingPrevFrameNonUniformToLinear(uv); }
    int2 RemapFoveatedRenderingDistortCS(int2 positionCS, bool yflip) { return RemapFoveatedRenderingNonUniformToLinearCS(positionCS, yflip); }

#endif

#endif // UNITY_FOVEATED_RENDERING_METAL_INCLUDED
