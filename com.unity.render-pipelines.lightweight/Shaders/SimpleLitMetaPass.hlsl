#ifndef LIGHTWEIGHT_SIMPLE_LIT_META_PASS_INCLUDED
#define LIGHTWEIGHT_SIMPLE_LIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/MetaInput.hlsl"

half4 LightweightFragmentMetaSimple(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    MetaInput metaInput;
    metaInput.Albedo = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
    metaInput.SpecularColor = SampleSpecularGloss(uv, 1.0h, _SpecColor, TEXTURE2D_PARAM(_SpecGlossMap, sampler_SpecGlossMap)).xyz;
    metaInput.Emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap));

    return MetaFragment(metaInput);
}

#endif
