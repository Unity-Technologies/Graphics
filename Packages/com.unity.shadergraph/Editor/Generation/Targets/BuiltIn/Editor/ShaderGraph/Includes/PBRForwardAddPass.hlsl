
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacyBuilding.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/BuildInputData.hlsl"

v2f_surf PBRForwardAddVertex(appdata_full v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    v2f_surf o;
    UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
    UNITY_TRANSFER_INSTANCE_ID(v,o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.pos = UnityObjectToClipPos(v.vertex);
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    float3 worldNormal = UnityObjectToWorldNormal(v.normal);
    o.worldPos.xyz = worldPos;
    o.worldNormal = worldNormal;

    UNITY_TRANSFER_LIGHTING(o,v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader
    UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
    return o;
}

void PBRForwardAddVertex(Attributes input, VertexDescription vertexDescription, inout Varyings varyings)
{
    appdata_full v;
    ZERO_INITIALIZE(appdata_full, v);
    BuildAppDataFull(input, vertexDescription, v);

    v2f_surf o = PBRForwardAddVertex(v);
    SurfaceVertexToVaryings(o, varyings);
}

half4 PBRForwardAddFragment(v2f_surf vertexSurf, SurfaceOutputStandard o)
{
    v2f_surf IN = vertexSurf;
    UNITY_SETUP_INSTANCE_ID(IN);

    #ifdef FOG_COMBINED_WITH_TSPACE
        UNITY_EXTRACT_FOG_FROM_TSPACE(IN);
    #elif defined FOG_COMBINED_WITH_WORLD_POS
        UNITY_EXTRACT_FOG_FROM_WORLD_POS(IN);
    #else
        UNITY_EXTRACT_FOG(IN);
    #endif

    float3 worldPos = IN.worldPos.xyz;
    #ifndef USING_DIRECTIONAL_LIGHT
        fixed3 lightDir = normalize(UnityWorldSpaceLightDir(worldPos));
    #else
        fixed3 lightDir = _WorldSpaceLightPos0.xyz;
    #endif
    float3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));

    fixed3 normalWorldVertex = fixed3(0,0,1);
    normalWorldVertex = IN.worldNormal;

    UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
    fixed4 c = 0;

    // Setup lighting environment
    UnityGI gi;
    UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
    gi.indirect.diffuse = 0;
    gi.indirect.specular = 0;
    gi.light.color = _LightColor0.rgb;
    gi.light.dir = lightDir;
    gi.light.color *= atten;
    c += LightingStandard (o, worldViewDir, gi);
    
    UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
    #ifndef _SURFACE_TYPE_TRANSPARENT
    UNITY_OPAQUE_ALPHA(c.a);
    #endif
    return c;
}

half4 PBRForwardAddFragment(SurfaceDescription surfaceDescription, InputData inputData, Varyings varyings)
{
    v2f_surf vertexSurf;
    ZERO_INITIALIZE(v2f_surf, vertexSurf);
    VaryingsToSurfaceVertex(varyings, vertexSurf);

    SurfaceOutputStandard o = BuildStandardSurfaceOutput(surfaceDescription, inputData);
    return PBRForwardAddFragment(vertexSurf, o);
}

PackedVaryings vert(Attributes input)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    output = BuildVaryings(input);

    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
    PBRForwardAddVertex(input, vertexDescription, output);

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

    InputData inputData;
    BuildInputData(unpacked, surfaceDescription, inputData);

    half4 color = PBRForwardAddFragment(surfaceDescription, inputData, unpacked);
    return color;
}
