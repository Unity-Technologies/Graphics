
#if defined(VFX_VARYING_PS_INPUTS)
float4 GetFlipbookMotionVectors(VFX_VARYING_PS_INPUTS i, float4 uvs, float blend)
{
    float4 mvs = (float4)0;
#if USE_FLIPBOOK_MOTIONVECTORS && defined(VFX_VARYING_MOTIONVECTORSCALE)
    #if USE_FLIPBOOK_ARRAY_LAYOUT
    float2 mvPrev = SampleTexture(VFX_SAMPLER(motionVectorMap), uvs.xy, uvs.z).rg * 2 - 1;
    float2 mvNext = SampleTexture(VFX_SAMPLER(motionVectorMap), uvs.xy, uvs.w).rg * 2 - 1;
    #else
    float2 mvPrev = SampleTexture(VFX_SAMPLER(motionVectorMap), uvs.xy).rg * 2 - 1;
    float2 mvNext = SampleTexture(VFX_SAMPLER(motionVectorMap), uvs.zw).rg * 2 - 1;
    #endif
    mvs.xy = mvPrev * (-i.VFX_VARYING_MOTIONVECTORSCALE * blend);
    mvs.zw = mvNext * (i.VFX_VARYING_MOTIONVECTORSCALE * (1.0 - blend));
#endif
    return mvs;
}

VFXUVData GetUVData(VFX_VARYING_PS_INPUTS i) // uvs are provided from interpolants
{
    VFXUVData data = (VFXUVData)0;
#if USE_FLIPBOOK_ARRAY_LAYOUT
    #ifdef VFX_VARYING_UV
        data.uvs.xyz = i.VFX_VARYING_UV.xyz;
        #if USE_FLIPBOOK_INTERPOLATION && defined(VFX_VARYING_FRAMEBLEND) && defined(VFX_VARYING_UV)
            data.uvs.w = i.VFX_VARYING_UV.w;
            data.blend = i.VFX_VARYING_FRAMEBLEND;
            data.mvs = GetFlipbookMotionVectors(i, data.uvs, data.blend);
        #endif
    #endif
#else
    #ifdef VFX_VARYING_UV
        data.uvs.xy = i.VFX_VARYING_UV.xy;
        #if USE_FLIPBOOK_INTERPOLATION && defined(VFX_VARYING_FRAMEBLEND) && defined(VFX_VARYING_UV)
            data.uvs.zw = i.VFX_VARYING_UV.zw;
            data.blend = i.VFX_VARYING_FRAMEBLEND;
            data.mvs = GetFlipbookMotionVectors(i, data.uvs, data.blend);
        #endif
    #endif
#endif
    return data;
}

VFXUVData GetUVData(VFX_VARYING_PS_INPUTS i,float2 uv) // uvs are provided from ps directly
{
#if USE_FLIPBOOK_ARRAY_LAYOUT
    #ifdef VFX_VARYING_FLIPBOOKSIZE
        float flipBookSize = i.VFX_VARYING_FLIPBOOKSIZE;
    #else
        float flipBookSize = 1.0f;
    #endif
#else
    #ifdef VFX_VARYING_FLIPBOOKSIZE
        float2 flipBookSize = i.VFX_VARYING_FLIPBOOKSIZE;
    #else
        float2 flipBookSize = float2(1, 1);
    #endif
#endif

#ifdef VFX_VARYING_INVFLIPBOOKSIZE
    float2 invFlipBookSize = i.VFX_VARYING_INVFLIPBOOKSIZE;
#else
    float2 invFlipBookSize = 1.0f / flipBookSize;
#endif

#ifdef VFX_VARYING_TEXINDEX
    float texIndex = i.VFX_VARYING_TEXINDEX;
#else
    float texIndex = 0.0f;
#endif

#if USE_UV_SCALE_BIAS && defined(VFX_VARYING_UV_SCALE)
    uv.xy = uv.xy * i.VFX_VARYING_UV_SCALE + i.VFX_VARYING_UV_BIAS;
#endif

    VFXUVData data;
    #if USE_FLIPBOOK_ARRAY_LAYOUT
        data = GetUVData(flipBookSize, uv, texIndex);
    #else
        data = GetUVData(flipBookSize, invFlipBookSize, uv, texIndex);
    #endif
    data.mvs = GetFlipbookMotionVectors(i, data.uvs, data.blend);
    return data;
}

float4 VFXGetParticleColor(VFX_VARYING_PS_INPUTS i)
{
    float4 color = 1.0f;
    #if VFX_NEEDS_COLOR_INTERPOLATOR
    #ifdef VFX_VARYING_COLOR
    color.rgb *= i.VFX_VARYING_COLOR;
    #endif
    #ifdef VFX_VARYING_ALPHA
    color.a *= i.VFX_VARYING_ALPHA;
    #endif
    #endif
    return color;
}
#endif

float VFXLinearEyeDepth(float depth)
{
    return LinearEyeDepth(depth, _ZBufferParams);
}

float VFXLinearEyeDepthOrthographic(float depth)
{
#if UNITY_REVERSED_Z
    return float(_ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * depth);
#else
    return float(_ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * depth);
#endif
}

#if defined(VFX_VARYING_PS_INPUTS)
float VFXGetSoftParticleFade(VFX_VARYING_PS_INPUTS i)
{
    float fade = 1.0f;
    #if USE_SOFT_PARTICLE && defined(VFX_VARYING_INVSOFTPARTICLEFADEDISTANCE)
    float sceneZ, selfZ;
    float sampledDepth = VFXSampleDepth(i.VFX_VARYING_POSCS);
    if(IsPerspectiveProjection())
    {
        sceneZ = VFXLinearEyeDepth(sampledDepth);
        selfZ = i.VFX_VARYING_POSCS.w;
    }
    else
    {
        sceneZ = VFXLinearEyeDepthOrthographic(sampledDepth);
        selfZ = VFXLinearEyeDepthOrthographic(i.VFX_VARYING_POSCS.z);
    }
    fade = saturate(i.VFX_VARYING_INVSOFTPARTICLEFADEDISTANCE * (sceneZ - selfZ));
    fade = fade * fade * (3.0 - (2.0 * fade)); // Smoothsteping the fade
    #endif
    return fade;
}
float4 VFXApplySoftParticleFade(VFX_VARYING_PS_INPUTS i, float4 color)
{
    float fade = VFXGetSoftParticleFade(i);
    #if VFX_BLENDMODE_PREMULTIPLY
    color *= fade;
    #else
    color.a *= fade;
    #endif
    return color;
}
float4 VFXGetTextureColor(VFXSampler2D s,VFX_VARYING_PS_INPUTS i)
{
    return SampleTexture(s, GetUVData(i));
}

float4 VFXGetTextureColor(VFXSampler2DArray s, VFX_VARYING_PS_INPUTS i)
{
    return SampleTexture(s, GetUVData(i));
}

float4 VFXGetTextureColorWithProceduralUV(VFXSampler2D s, VFX_VARYING_PS_INPUTS i, float2 uv)
{
    return SampleTexture(s, GetUVData(i, uv));
}

float4 VFXGetTextureColorWithProceduralUV(VFXSampler2DArray s, VFX_VARYING_PS_INPUTS i, float2 uv)
{
    return SampleTexture(s, GetUVData(i, uv));
}
#endif

float3 VFXGetTextureNormal(VFXSampler2D s,float2 uv)
{
    float4 packedNormal = SampleTexture(s, uv);
    packedNormal.w *= packedNormal.x;
    float3 normal;
    normal.xy = packedNormal.wy * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

#if defined(VFX_VARYING_PS_INPUTS)
float4 VFXGetFragmentColor(VFX_VARYING_PS_INPUTS i)
{
    float4 color = VFXGetParticleColor(i);
    color = VFXApplySoftParticleFade(i, color);
    return color;
}

void VFXClipFragmentColor(float alpha,VFX_VARYING_PS_INPUTS i)
{
    #if USE_ALPHA_TEST
    #if defined(VFX_VARYING_ALPHATHRESHOLD)
    clip(alpha - i.VFX_VARYING_ALPHATHRESHOLD);
    #else
    clip(alpha - VFX_EPSILON);
    #endif
    #endif
}
#endif

float3 VFXGetPositionRWS(float3 posWS)
{
#if VFX_WORLD_SPACE
    return GetCameraRelativePositionWS(posWS);
#else
    return posWS;
#endif
}

float3 VFXGetPositionAWS(float3 posWS)
{
#if VFX_LOCAL_SPACE
    return GetAbsolutePositionWS(posWS);
#else
    return posWS;
#endif
}

#if defined(VFX_VARYING_PS_INPUTS)
float4 VFXApplyFog(float4 color,VFX_VARYING_PS_INPUTS i)
{
    #if USE_FOG && defined(VFX_VARYING_POSCS)
        #if defined(VFX_VARYING_POSWS)
            return VFXApplyFog(color, i.VFX_VARYING_POSCS, i.VFX_VARYING_POSWS);
        #else
            return VFXApplyFog(color, i.VFX_VARYING_POSCS, (float3)0); //Some pipeline (LWRP) doesn't require WorldPos
        #endif
    #else
        return color;
    #endif
}

float4 VFXApplyAO(float4 color, VFX_VARYING_PS_INPUTS i)
{
#if defined(VFX_VARYING_POSCS)
    return VFXApplyAO(color, i.VFX_VARYING_POSCS);
#else
    return color;
#endif
}

#endif

bool TryGetElementToVFXBaseIndex(uint elementIndex, uint instanceIndex, out uint elementToVFXBaseIndex, uint currentFrameIndex)
{
    elementToVFXBaseIndex = ~0u;
#if defined(VFX_FEATURE_MOTION_VECTORS)
    elementIndex += RAW_CAPACITY * instanceIndex;
#if defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
    uint viewTotal = asuint(cameraXRSettings.x);
    uint viewCount = asuint(cameraXRSettings.y);
    uint viewOffset = asuint(cameraXRSettings.z);
    elementToVFXBaseIndex = elementIndex * (VFX_FEATURE_MOTION_VECTORS_VERTS * 2 * viewTotal + 1);
#else
    elementToVFXBaseIndex = elementIndex * 13;
#endif
    uint previousFrameIndex = elementToVFXBufferPrevious.Load(elementToVFXBaseIndex++ << 2);
    return currentFrameIndex - previousFrameIndex == 1u;
#endif
    return false;
}

float4 VFXGetPreviousClipPosition(uint elementToVFXBaseIndex, uint vertexIndex)
{
    float4 previousClipPos = float4(0.0f, 0.0f, 0.0f, 1.0f);
#if VFX_FEATURE_MOTION_VECTORS && defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
    uint viewTotal = asuint(cameraXRSettings.x);
    uint viewCount = asuint(cameraXRSettings.y);
    uint viewOffset = asuint(cameraXRSettings.z);

#if HAS_STRIPS
    vertexIndex = (vertexIndex / 2) % VFX_FEATURE_MOTION_VECTORS_VERTS;
#else
    vertexIndex = vertexIndex % VFX_FEATURE_MOTION_VECTORS_VERTS;
#endif
    uint elementToVFXIndex = elementToVFXBaseIndex + vertexIndex * viewCount * 2;
    elementToVFXIndex += viewOffset * viewCount * VFX_FEATURE_MOTION_VECTORS_VERTS * 2;
    elementToVFXIndex += unity_StereoEyeIndex * 2;
    uint2 read = elementToVFXBufferPrevious.Load2(elementToVFXIndex << 2);
    previousClipPos.xy = asfloat(read);

#endif
    return previousClipPos;
}

float4x4 VFXGetPreviousElementToVFX(uint elementToVFXBaseIndex)
{
    float4x4 previousElementToVFX = (float4x4)0;
    previousElementToVFX[3] = float4(0, 0, 0, 1);
#if VFX_FEATURE_MOTION_VECTORS && !defined(VFX_FEATURE_MOTION_VECTORS_VERTS)
    UNITY_UNROLL
    for (int itIndexMatrixRow = 0; itIndexMatrixRow < 3; ++itIndexMatrixRow)
    {
        uint4 read = elementToVFXBufferPrevious.Load4((elementToVFXBaseIndex + itIndexMatrixRow * 4) << 2);
        previousElementToVFX[itIndexMatrixRow] = asfloat(read);
    }
#endif
    return previousElementToVFX;
}

