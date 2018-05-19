#ifndef LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
#define LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED

#include "LightweightPassMetaCommon.hlsl"

half4 LightweightFragmentMetaSimple(MetaVertexOuput i) : SV_Target
{
    float2 uv = i.uv;
    MetaInput o;
    o.Albedo = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).rgb;
    o.SpecularColor = SampleSpecularGloss(uv, 1.0h, _SpecColor, TEXTURE2D_PARAM(_SpecGlossMap, sampler_SpecGlossMap)).xyz;
    o.Emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_PARAM(_EmissionMap, sampler_EmissionMap));

    return MetaFragment(o);
}

#endif // LIGHTWEIGHT_PASS_META_SIMPLE_INCLUDED
