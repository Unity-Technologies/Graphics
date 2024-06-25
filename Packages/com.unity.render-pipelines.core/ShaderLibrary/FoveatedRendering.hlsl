#ifndef UNITY_FOVEATED_RENDERING_INCLUDED
#define UNITY_FOVEATED_RENDERING_INCLUDED

#if defined(SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER)

#if defined(SHADER_API_PS5)
    #include "Packages/com.unity.render-pipelines.ps5/ShaderLibrary/API/FoveatedRendering_PSSL.hlsl"
#endif

#if defined(SHADER_API_METAL)
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/FoveatedRendering_Metal.hlsl"
#endif

// coordinate remapping functions for foveated rendering
#define FOVEATED_FLIP_Y(uv) uv.y = 1.0f - uv.y
float2 FoveatedRemapLinearToNonUniform(float2 uv)
{
    UNITY_BRANCH if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        FOVEATED_FLIP_Y(uv);
        uv = RemapFoveatedRenderingLinearToNonUniform(uv);
        FOVEATED_FLIP_Y(uv);
    }
    return uv;
}

float2 FoveatedRemapPrevFrameLinearToNonUniform(float2 uv)
{
    UNITY_BRANCH if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        FOVEATED_FLIP_Y(uv);
        uv = RemapFoveatedRenderingPrevFrameLinearToNonUniform(uv);
        FOVEATED_FLIP_Y(uv);
    }
    return uv;
}

float2 FoveatedRemapDensity(float2 uv)
{
    UNITY_BRANCH if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        FOVEATED_FLIP_Y(uv);
        uv = RemapFoveatedRenderingDensity(uv);
        FOVEATED_FLIP_Y(uv);
    }
    return uv;
}

float2 FoveatedRemapPrevFrameDensity(float2 uv)
{
    UNITY_BRANCH if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        FOVEATED_FLIP_Y(uv);
        uv = RemapFoveatedRenderingPrevFrameDensity(uv);
        FOVEATED_FLIP_Y(uv);
    }
    return uv;
}

float2 FoveatedRemapNonUniformToLinear(float2 uv)
{
    UNITY_BRANCH if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        FOVEATED_FLIP_Y(uv);
        uv = RemapFoveatedRenderingNonUniformToLinear(uv);
        FOVEATED_FLIP_Y(uv);
    }
    return uv;
}

float2 FoveatedRemapPrevFrameNonUniformToLinear(float2 uv)
{
    UNITY_BRANCH if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
    {
        FOVEATED_FLIP_Y(uv);
        uv = RemapFoveatedRenderingPrevFrameNonUniformToLinear(uv);
        FOVEATED_FLIP_Y(uv);
    }
    return uv;
}
#undef FOVEATED_FLIP_Y

float2 FoveatedRemapLinearToNonUniformCS(float2 positionCS)
{
    return FoveatedRemapLinearToNonUniform(positionCS * _ScreenSize.zw) * _ScreenSize.xy;
}

float2 FoveatedRemapNonUniformToLinearCS(float2 positionCS)
{
    UNITY_BRANCH if(_FOVEATED_RENDERING_NON_UNIFORM_RASTER)
        positionCS = RemapFoveatedRenderingNonUniformToLinearCS(positionCS, true);
    return positionCS;
}

#else // SUPPORTS_FOVEATED_RENDERING_NON_UNIFORM_RASTER

// dummy coordinate remapping functions for non-foveated rendering
float2 FoveatedRemapLinearToNonUniform(float2 uv) {return uv;}
float2 FoveatedRemapPrevFrameLinearToNonUniform(float2 uv) {return uv;}
float2 FoveatedRemapDensity(float2 uv) {return uv;}
float2 FoveatedRemapPrevFrameDensity(float2 uv) {return uv;}
float2 FoveatedRemapNonUniformToLinear(float2 uv) {return uv;}
float2 FoveatedRemapPrevFrameNonUniformToLinear(float2 uv) {return uv;}
float2 FoveatedRemapLinearToNonUniformCS(float2 positionCS) {return positionCS;}
float2 FoveatedRemapNonUniformToLinearCS(float2 positionCS) {return positionCS;}

#endif

// foveated version of GetPositionInput() functions
PositionInputs FoveatedGetPositionInput(float2 positionSS, float2 invScreenSize, uint2 tileCoord)
{
    PositionInputs posInput = GetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionNDC = FoveatedRemapNonUniformToLinear(posInput.positionNDC);
    return posInput;
}

PositionInputs FoveatedPrevFrameGetPositionInput(float2 positionSS, float2 invScreenSize, uint2 tileCoord)
{
    PositionInputs posInput = GetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionNDC = FoveatedRemapPrevFrameNonUniformToLinear(posInput.positionNDC);
    return posInput;
}

PositionInputs FoveatedGetPositionInput(float2 positionSS, float2 invScreenSize)
{
    return FoveatedGetPositionInput(positionSS, invScreenSize, uint2(0, 0));
}

PositionInputs FoveatedPrevFrameGetPositionInput(float2 positionSS, float2 invScreenSize)
{
    return FoveatedPrevFrameGetPositionInput(positionSS, invScreenSize, uint2(0, 0));
}

PositionInputs FoveatedGetPositionInput(float2 positionSS, float2 invScreenSize, float3 positionWS)
{
    PositionInputs posInput = FoveatedGetPositionInput(positionSS, invScreenSize, uint2(0, 0));
    posInput.positionWS = positionWS;
    return posInput;
}

PositionInputs FoveatedPrevFrameGetPositionInput(float2 positionSS, float2 invScreenSize, float3 positionWS)
{
    PositionInputs posInput = FoveatedPrevFrameGetPositionInput(positionSS, invScreenSize, uint2(0, 0));
    posInput.positionWS = positionWS;
    return posInput;
}

PositionInputs FoveatedGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth, float linearDepth, float3 positionWS, uint2 tileCoord)
{
    PositionInputs posInput = FoveatedGetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionWS = positionWS;
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = linearDepth;

    return posInput;
}

PositionInputs FoveatedPrevFrameGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth, float linearDepth, float3 positionWS, uint2 tileCoord)
{
    PositionInputs posInput = FoveatedPrevFrameGetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionWS = positionWS;
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = linearDepth;

    return posInput;
}

PositionInputs FoveatedGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth, float linearDepth, float3 positionWS)
{
    return FoveatedGetPositionInput(positionSS, invScreenSize, deviceDepth, linearDepth, positionWS, uint2(0, 0));
}

PositionInputs FoveatedPrevFrameGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth, float linearDepth, float3 positionWS)
{
    return FoveatedPrevFrameGetPositionInput(positionSS, invScreenSize, deviceDepth, linearDepth, positionWS, uint2(0, 0));
}

PositionInputs FoveatedGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth,
    float4x4 invViewProjMatrix, float4x4 viewMatrix,
    uint2 tileCoord)
{
    PositionInputs posInput = FoveatedGetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionWS = ComputeWorldSpacePosition(posInput.positionNDC, deviceDepth, invViewProjMatrix);
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = LinearEyeDepth(posInput.positionWS, viewMatrix);

    return posInput;
}

PositionInputs FoveatedPrevFrameGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth,
    float4x4 invViewProjMatrix, float4x4 viewMatrix,
    uint2 tileCoord)
{
    PositionInputs posInput = FoveatedPrevFrameGetPositionInput(positionSS, invScreenSize, tileCoord);
    posInput.positionWS = ComputeWorldSpacePosition(posInput.positionNDC, deviceDepth, invViewProjMatrix);
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = LinearEyeDepth(posInput.positionWS, viewMatrix);

    return posInput;
}

PositionInputs FoveatedGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth,
                                float4x4 invViewProjMatrix, float4x4 viewMatrix)
{
    return FoveatedGetPositionInput(positionSS, invScreenSize, deviceDepth, invViewProjMatrix, viewMatrix, uint2(0, 0));
}

PositionInputs FoveatedPrevFrameGetPositionInput(float2 positionSS, float2 invScreenSize, float deviceDepth,
                                float4x4 invViewProjMatrix, float4x4 viewMatrix)
{
    return FoveatedPrevFrameGetPositionInput(positionSS, invScreenSize, deviceDepth, invViewProjMatrix, viewMatrix, uint2(0, 0));
}

#endif // UNITY_FOVEATED_RENDERING_INCLUDED
