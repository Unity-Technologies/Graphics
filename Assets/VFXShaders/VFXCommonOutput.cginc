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
    float sceneZ = VFXLinearEyeDepth(i.VFX_VARYING_POSSS);
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

float4 VFXGetFragmentColor(VFX_VARYING_PS_INPUTS i)
{
    float4 color = VFXGetParticleColor(i);
    color.a *= VFXGetSoftParticleFade(i);
    return color;
}

void VFXClipFragmentColor(float alpha,VFX_VARYING_PS_INPUTS i)
{
    #if USE_ALPHA_TEST && defined(VFX_VARYING_ALPHATHRESHOLD)
    clip(alpha - i.VFX_VARYING_ALPHATHRESHOLD);
    #else
    clip(alpha - 1e-5f);
    #endif
}
