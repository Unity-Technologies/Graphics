#ifndef UNIVERSAL_SIMPLE_LIT_META_PASS_INCLUDED
#define UNIVERSAL_SIMPLE_LIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLitFeatures.hlsl"

half4 UniversalFragmentMetaSimple(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    MetaInput metaInput;
    metaInput.Albedo = _BaseColor.rgb * SampleBaseMap(uv).rgb;
    metaInput.Emission = SampleEmission(uv);

    return UniversalFragmentMeta(input, metaInput);
}
#endif
