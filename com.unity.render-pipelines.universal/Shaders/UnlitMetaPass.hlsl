#ifndef UNIVERSAL_UNLIT_META_PASS_INCLUDED
#define UNIVERSAL_UNLIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

half4 UniversalFragmentMetaUnlit(MetaVaryings input) : SV_Target
{
    UnityMetaInput metaInput = (UnityMetaInput)0;
    metaInput.Albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;

    return UniversalFragmentMeta(input, metaInput);
}

half4 LightweightFragmentMetaUnlit(Varyings input) : SV_Target
{
    return UniversalFragmentMetaUnlit(VaryingsToMetaVaryings(input));
}

#endif
