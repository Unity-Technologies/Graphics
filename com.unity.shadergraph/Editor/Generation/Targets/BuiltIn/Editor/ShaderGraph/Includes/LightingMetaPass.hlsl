#ifndef SG_LIT_META_INCLUDED
#define SG_LIT_META_INCLUDED

#include "UnityMetaPass.cginc"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/SurfaceData.hlsl"

SurfaceData SurfaceDescriptionToSurfaceData(SurfaceDescription surfaceDescription)
{
    #if _AlphaClip
       half alpha = surfaceDescription.Alpha;
       clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
       half alpha = surfaceDescription.Alpha;
    #else
       half alpha = 1;
    #endif

    SurfaceData surface         = (SurfaceData)0;
    surface.albedo              = surfaceDescription.BaseColor;
    surface.alpha               = saturate(alpha);
    surface.clearCoatMask       = 0;
    surface.clearCoatSmoothness = 1;
    surface.emission            = surfaceDescription.Emission;
    return surface;
}

SurfaceOutputStandard BuildStandardSurfaceOutput(SurfaceDescription surfaceDescription)
{
    SurfaceData surface = SurfaceDescriptionToSurfaceData(surfaceDescription);

    SurfaceOutputStandard o = (SurfaceOutputStandard)0;
    o.Albedo = surface.albedo;
    o.Metallic = surface.metallic;
    o.Smoothness = surface.smoothness;
    o.Occlusion = surface.occlusion;
    o.Emission = surface.emission;
    o.Alpha = surface.alpha;
    return o;
}

v2f_surf MetaVertex(appdata_full v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    v2f_surf o;
    UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
    UNITY_TRANSFER_INSTANCE_ID(v,o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityMetaVertexPosition(v.vertex, v.texcoord1.xy, v.texcoord2.xy, unity_LightmapST, unity_DynamicLightmapST);
    #ifdef EDITOR_VISUALIZATION
    o.vizUV = 0;
    o.lightCoord = 0;
    if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
        o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.texcoord.xy, v.texcoord1.xy, v.texcoord2.xy, unity_EditorViz_Texture_ST);
    else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
    {
        o.vizUV = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
    }
    #endif

    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    float3 worldNormal = UnityObjectToWorldNormal(v.normal);
    fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
    fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
    fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
    o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
    o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
    o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
    o.worldPos.xyz = worldPos;
    return o;
}

void MetaVertex(Attributes input, VertexDescription vertexDescription, inout Varyings varyings)
{
    appdata_full v;
    ZERO_INITIALIZE(appdata_full, v);
    BuildAppDataFull(input, vertexDescription, v);

    v2f_surf o = MetaVertex(v);
    SurfaceVertexToVaryings(o, varyings);
}

half4 MetaFragment(v2f_surf IN, SurfaceOutputStandard o)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    #ifdef FOG_COMBINED_WITH_TSPACE
        UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
    #elif defined FOG_COMBINED_WITH_WORLD_POS
        UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
    #else
        UNITY_EXTRACT_FOG(IN);
    #endif
    #ifdef FOG_COMBINED_WITH_TSPACE
        UNITY_RECONSTRUCT_TBN(IN);
    #else
        UNITY_EXTRACT_TBN(IN);
    #endif

    float3 worldPos = IN.worldPos.xyz;//float3(IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w);
    #ifndef USING_DIRECTIONAL_LIGHT
        fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
    #else
        fixed3 lightDir = _WorldSpaceLightPos0.xyz;
    #endif
    fixed3 normalWorldVertex = fixed3(0,0,1);

    UnityMetaInput metaIN;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, metaIN);
    metaIN.Albedo = o.Albedo;
    metaIN.Emission = o.Emission;
    #ifdef EDITOR_VISUALIZATION
    metaIN.VizUV = IN.vizUV;
    metaIN.LightCoord = IN.lightCoord;
    #endif
    return UnityMetaFragment(metaIN);
}

half4 MetaFragment(SurfaceDescription surfaceDescription, Varyings varyings)
{
    v2f_surf vertexSurf;
    ZERO_INITIALIZE(v2f_surf, vertexSurf);
    VaryingsToSurfaceVertex(varyings, vertexSurf);

    SurfaceOutputStandard o = BuildStandardSurfaceOutput(surfaceDescription);
    return MetaFragment(vertexSurf, o);
}

PackedVaryings vert(Attributes input)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    output = BuildVaryings(input);

    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
    MetaVertex(input, vertexDescription, output);

    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    half4 color = MetaFragment(surfaceDescription, unpacked);
    return color;
}

#endif
