#ifndef LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
#define LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED

#include "LightweightPassMetaCommon.hlsl"

half4 LightweightFragmentMetaUnlit(MetaVertexOuput i) : SV_Target
{
    float2 uv = i.uv;

    MetaInput o = (MetaInput)0;
    o.Albedo = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;

    return MetaFragment(o);
}

#endif // LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
