
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacyBuilding.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/BuildInputData.hlsl"

v2f_surf PBRStandardVertex(appdata_full v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    v2f_surf o;
    UNITY_INITIALIZE_OUTPUT(v2f_surf,o);
    UNITY_TRANSFER_INSTANCE_ID(v,o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.pos = UnityObjectToClipPos(v.vertex);
    float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    float3 worldNormal = UnityObjectToWorldNormal(v.normal);
    #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED)
    fixed3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
    fixed tangentSign = v.tangent.w * unity_WorldTransformParams.w;
    fixed3 worldBinormal = cross(worldNormal, worldTangent) * tangentSign;
    #endif
    #if defined(LIGHTMAP_ON) && defined(DIRLIGHTMAP_COMBINED) && !defined(UNITY_HALF_PRECISION_FRAGMENT_SHADER_REGISTERS)
    o.tSpace0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
    o.tSpace1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
    o.tSpace2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
    #endif
    o.worldPos.xyz = worldPos;
    o.worldNormal = worldNormal;
    #ifdef DYNAMICLIGHTMAP_ON
    o.lmap.zw = v.texcoord2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif
    #ifdef LIGHTMAP_ON
    o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #endif

    // SH/ambient and vertex lights
    #ifndef LIGHTMAP_ON
        #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
        o.sh = 0;
        // Approximated illumination from non-important point lights
        #ifdef VERTEXLIGHT_ON
            o.sh += Shade4PointLights (
            unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
            unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
            unity_4LightAtten0, worldPos, worldNormal);
        #endif
        o.sh = ShadeSHPerVertex (worldNormal, o.sh);
        #endif
    #endif // !LIGHTMAP_ON

    UNITY_TRANSFER_LIGHTING(o,v.texcoord1.xy); // pass shadow and, possibly, light cookie coordinates to pixel shader
    #ifdef FOG_COMBINED_WITH_TSPACE
        UNITY_TRANSFER_FOG_COMBINED_WITH_TSPACE(o,o.pos); // pass fog coordinates to pixel shader
    #elif defined FOG_COMBINED_WITH_WORLD_POS
        UNITY_TRANSFER_FOG_COMBINED_WITH_WORLD_POS(o,o.pos); // pass fog coordinates to pixel shader
    #else
        UNITY_TRANSFER_FOG(o,o.pos); // pass fog coordinates to pixel shader
    #endif
    return o;
}

void PBRStandardVertex(Attributes input, VertexDescription vertexDescription, inout Varyings varyings)
{
    appdata_full v;
    ZERO_INITIALIZE(appdata_full, v);
    BuildAppDataFull(input, vertexDescription, v);

    v2f_surf o = PBRStandardVertex(v);
    SurfaceVertexToVaryings(o, varyings);
}

half4 PBRStandardFragment(v2f_surf vertexSurf, SurfaceOutputStandard o)
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

  // compute lighting & shadowing factor
  UNITY_LIGHT_ATTENUATION(atten, IN, worldPos)
  fixed4 c = 0;

  // Setup lighting environment
  UnityGI gi;
  UNITY_INITIALIZE_OUTPUT(UnityGI, gi);
  gi.indirect.diffuse = 0;
  gi.indirect.specular = 0;
  gi.light.color = _LightColor0.rgb;
  gi.light.dir = lightDir;
  // Call GI (lightmaps/SH/reflections) lighting function
  UnityGIInput giInput;
  UNITY_INITIALIZE_OUTPUT(UnityGIInput, giInput);
  giInput.light = gi.light;
  giInput.worldPos = worldPos;
  giInput.worldViewDir = worldViewDir;
  giInput.atten = atten;
  #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    giInput.lightmapUV = IN.lmap;
  #else
    giInput.lightmapUV = 0.0;
  #endif
  #if UNITY_SHOULD_SAMPLE_SH && !UNITY_SAMPLE_FULL_SH_PER_PIXEL
    giInput.ambient = IN.sh;
  #else
    giInput.ambient.rgb = 0.0;
  #endif
  giInput.probeHDR[0] = unity_SpecCube0_HDR;
  giInput.probeHDR[1] = unity_SpecCube1_HDR;
  #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
    giInput.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
  #endif
  #ifdef UNITY_SPECCUBE_BOX_PROJECTION
    giInput.boxMax[0] = unity_SpecCube0_BoxMax;
    giInput.probePosition[0] = unity_SpecCube0_ProbePosition;
    giInput.boxMax[1] = unity_SpecCube1_BoxMax;
    giInput.boxMin[1] = unity_SpecCube1_BoxMin;
    giInput.probePosition[1] = unity_SpecCube1_ProbePosition;
  #endif
  LightingStandard_GI(o, giInput, gi);

  // realtime lighting: call lighting function
  c += LightingStandard (o, worldViewDir, gi);
  c.rgb += o.Emission;
  UNITY_APPLY_FOG(_unity_fogCoord, c); // apply fog
  #ifndef _SURFACE_TYPE_TRANSPARENT
  UNITY_OPAQUE_ALPHA(c.a);
  #endif
  return c;
}

half4 PBRStandardFragment(SurfaceDescription surfaceDescription, InputData inputData, Varyings varyings)
{
    v2f_surf vertexSurf;
    ZERO_INITIALIZE(v2f_surf, vertexSurf);
    VaryingsToSurfaceVertex(varyings, vertexSurf);

    SurfaceOutputStandard o = BuildStandardSurfaceOutput(surfaceDescription, inputData);
    return PBRStandardFragment(vertexSurf, o);
}

PackedVaryings vert(Attributes input)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    output = BuildVaryings(input);

    VertexDescriptionInputs vertexDescriptionInputs = BuildVertexDescriptionInputs(input);
    VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
    PBRStandardVertex(input, vertexDescription, output);

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

    half4 color = PBRStandardFragment(surfaceDescription, inputData, unpacked);
    return color;
}
