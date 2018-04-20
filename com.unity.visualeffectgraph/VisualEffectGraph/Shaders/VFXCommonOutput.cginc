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

float VFXGetSoftParticleFade(VFX_VARYING_PS_INPUTS i)
{
    float fade = 1.0f;
    #if USE_SOFT_PARTICLE && defined(VFX_VARYING_POSSS) && defined(VFX_VARYING_INVSOFTPARTICLEFADEDISTANCE)
    float sceneZ = VFXLinearEyeDepth(VFXSampleDepth(i.VFX_VARYING_POSSS));
    fade = saturate(i.VFX_VARYING_INVSOFTPARTICLEFADEDISTANCE * (sceneZ - i.VFX_VARYING_POSSS.w));
    fade = fade * fade * (3.0 - (2.0 * fade)); // Smoothsteping the fade
    #endif
    return fade;
}

float4 VFXGetTextureColor(VFXSampler2D s,VFX_VARYING_PS_INPUTS i)
{
    float4 texColor = 1.0f;
    #if defined(VFX_VARYING_UV)
    texColor = s.t.Sample(s.s,i.VFX_VARYING_UV.xy);
    #if USE_FLIPBOOK_INTERPOLATION && defined(VFX_VARYING_FRAMEBLEND)
    float4 texColor2 = s.t.Sample(s.s,i.VFX_VARYING_UV.zw);
    texColor = lerp(texColor,texColor2,i.VFX_VARYING_FRAMEBLEND);
    #endif
    #endif
    return texColor;
}

float4 VFXGetTextureColorWithProceduralUV(VFXSampler2D s,VFX_VARYING_PS_INPUTS i,float2 uv)
{
#if USE_FLIPBOOK
#ifdef VFX_VARYING_FLIPBOOKSIZE
    float2 flipBookSize = i.VFX_VARYING_FLIPBOOKSIZE;
    float2 invFlipBookSize = i.VFX_VARYING_INVFLIPBOOKSIZE;
    float texIndex = i.VFX_VARYING_TEXINDEX;
#else
    float2 flipBookSize = float2(1, 1);
    float2 invFlipBookSize = float2(1, 1);
    float texIndex = 0;
#endif

#if USE_FLIPBOOK_INTERPOLATION
    float blend;
    float4 uvs = float4(uv, 0, 0);
    ProcessFlipBookUV(flipBookSize, invFlipBookSize, texIndex, uvs, blend);
    return lerp(s.t.Sample(s.s, uvs.xy), s.t.Sample(s.s, uvs.zw), blend);
#else
    ProcessFlipBookUV(flipBookSize, invFlipBookSize, texIndex, uv);
#endif
#endif

    return s.t.Sample(s.s, uv);
}

float3 VFXGetTextureNormal(VFXSampler2D s,float2 uv)
{
    float4 packedNormal = s.t.Sample(s.s,uv);
    packedNormal.w *= packedNormal.x;
    float3 normal;
    normal.xy = packedNormal.wy * 2.0 - 1.0;
    normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
    return normal;
}

float4 VFXGetFragmentColor(VFX_VARYING_PS_INPUTS i)
{
    float4 color = VFXGetParticleColor(i);
    color.a *= VFXGetSoftParticleFade(i);
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
