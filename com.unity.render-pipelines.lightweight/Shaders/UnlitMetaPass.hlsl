#ifndef LIGHTWEIGHT_UNLIT_META_PASS_INCLUDED
#define LIGHTWEIGHT_UNLIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/MetaInput.hlsl"

Varyings LightweightVertexMeta(Attributes input)
{
    Varyings output;
    output.positionCS = MetaVertexPosition(input.positionOS, input.uvLM, input.uvDLM, unity_LightmapST);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 LightweightFragmentMetaUnlit(Varyings input) : SV_Target
{
    MetaInput metaInput = (MetaInput)0;
    metaInput.Albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;

    return MetaFragment(metaInput);
}

#endif
