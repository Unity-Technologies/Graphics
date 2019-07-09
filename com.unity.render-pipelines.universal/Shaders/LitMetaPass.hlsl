#ifndef UNIVERSAL_LIT_META_PASS_INCLUDED
#define UNIVERSAL_LIT_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

Varyings UniversalVertexMeta(Attributes input)
{
    Varyings output;
    output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2,
        unity_LightmapST, unity_DynamicLightmapST);
    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
    return output;
}

half4 UniversalFragmentMeta(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput metaInput;
    metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    metaInput.SpecularColor = surfaceData.specular;
    metaInput.Emission = surfaceData.emission;

    return MetaFragment(metaInput);
}


//LWRP -> Universal Backwards Compatibility
Varyings LightweightVertexMeta(Attributes input)
{
    return UniversalVertexMeta(input);
}

half4 LightweightFragmentMeta(Varyings input) : SV_Target
{
    return UniversalFragmentMeta(input);
}

#endif
