#ifndef LIGHTWEIGHT_SIMPLE_LIT_META_PASS_INCLUDED
#define LIGHTWEIGHT_SIMPLE_LIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/MetaInput.hlsl"

Varyings LightweightVertexMeta(Attributes input)
{
    Varyings output;
    output.positionCS = MetaVertexPosition(input.positionOS, input.uvLM, input.uvDLM, unity_LightmapST);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 LightweightFragmentMetaSimple(Varyings input) : SV_Target
{
    float2 uv = input.uv;
    MetaInput metaInput;
    metaInput.Albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb;
    metaInput.SpecularColor = SampleSpecularSmoothness(uv, 1.0h, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap)).xyz;
    metaInput.Emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

    return MetaFragment(metaInput);
}

#endif
