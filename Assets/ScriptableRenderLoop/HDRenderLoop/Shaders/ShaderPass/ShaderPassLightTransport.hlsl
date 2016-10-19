
float4 UnityMetaVertexPosition(float4 vertex, float2 uv1, float2 uv2, float4 lightmapST, float4 dynlightmapST)
{
    if (unity_MetaVertexControl.x)
    {
        vertex.xy = uv1 * lightmapST.xy + lightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        vertex.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
    }
    if (unity_MetaVertexControl.y)
    {
        vertex.xy = uv2 * dynlightmapST.xy + dynlightmapST.zw;
        // OpenGL right now needs to actually use incoming vertex position,
        // so use it in a very dummy way
        vertex.z = vertex.z > 0 ? 1.0e-4f : 0.0f;
    }
    return UnityObjectToClipPos(vertex);
}

v2f_meta vert_meta(Attributes v)
{
    Varyings output;
    // Output UV coordinate in vertex shader
    output.positionHS = UnityMetaVertexPosition(v.positionOS, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
    output.texCoord0 = v.uv0;
    output.texCoord1 = v.uv1;
    return PackVaryings(output);
}

#if SHADER_STAGE_FRAGMENT

// TODO: This is the max value allowed for emissive (bad name - but keep for now to retrieve it) (It is 8^2.2 (gamma) and 8 is the limit of punctual light slider...), comme from UnityCg.cginc. Fix it!
// Ask Jesper if this can be change for HDRenderLoop
#define EMISSIVE_RGBM_SCALE 97.0

float4 Frag(PackedVaryings packedInput) : SV_Target
{
    Varyings input = UnpackVaryings(packedInput);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
    float3 positionWS = input.positionWS;

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(surfaceData);
    LighTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);

    // This shader is call two time. Once for getting emissiveColor, the other time to get diffuseColor
    // We use unity_MetaFragmentControl to make the distinction.

    float4 res = float4(0.0, 0.0, 0.0, 1.0);

    // TODO: No if / else in original code from Unity, why ? keep like original code but should be either diffuse or emissive
    if (unity_MetaFragmentControl.x)
    {
        // Apply diffuseColor Boost from LightmapSettings.
        res.rgb = clamp(pow(lightTransportData.diffuseColor, saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }
    
    if (unity_MetaFragmentControl.y)
    {
        // TODO: THIS LIMIT MUST BE REMOVE, IT IS NOT HDR, change when RGB9e5 is here.
        // Do we assume here that emission is [0..1] ?
        res = PackRGBM(lightTransportData.emissiveColor, EMISSIVE_RGBM_SCALE);
    }

    return res;
}

#endif
