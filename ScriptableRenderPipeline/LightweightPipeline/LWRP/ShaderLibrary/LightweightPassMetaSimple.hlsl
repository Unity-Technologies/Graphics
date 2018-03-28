#ifndef LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
#define LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED

#include "LightweightPassMetaCommon.hlsl"

half4 LightweightFragmentMetaSimple(MetaVertexOuput i) : SV_Target
{
    float2 uv = i.uv;
    MetaInput o;
    o.Albedo = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
    o.SpecularColor = SpecularGloss(uv, 1.0).xyz;
    o.Emission = Emission(uv);

    return MetaFragment(o);
}

#endif // LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
