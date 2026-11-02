#ifndef UNIVERSAL_UNLIT_META_PASS_INCLUDED
#define UNIVERSAL_UNLIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

half4 UniversalFragmentMetaUnlit(Varyings input) : SV_Target
{
    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = _BaseColor.rgb * SampleBaseMap(input.uv).rgb;

    return UniversalFragmentMeta(input, metaInput);
}
#endif
