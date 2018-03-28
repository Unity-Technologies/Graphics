#ifndef LIGHTWEIGHT_PASS_META_PBR_INCLUDED
#define LIGHTWEIGHT_PASS_META_PBR_INCLUDED

#include "LightweightPassMetaCommon.hlsl"

half4 LightweightFragmentMeta(MetaVertexOuput i) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(i.uv, surfaceData);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    MetaInput o;
    o.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
    o.SpecularColor = surfaceData.specular;
    o.Emission = surfaceData.emission;

    return MetaFragment(o);
}

#endif // LIGHTWEIGHT_PASS_META_PBR_INCLUDED
