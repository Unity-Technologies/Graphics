#ifndef LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
#define LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED

#include "LightweightPassMetaCommon.hlsl"

half4 LightweightFragmentMetaUnlit(Varyings input) : SV_Target
{
    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).rgb;

    return MetaFragment(metaInput);
}

#endif // LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
