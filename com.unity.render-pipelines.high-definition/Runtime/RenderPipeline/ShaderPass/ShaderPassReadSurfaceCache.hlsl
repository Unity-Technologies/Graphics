#if SHADERPASS != SHADERPASS_READ_SURFACE_CACHE
#error SHADERPASS_is_not_correctly_define
#endif

struct VaryingsPassToPS
{
    uint arrayIndex;
};
struct PackedVaryingsPassToPS
{
    uint arrayIndex : BLENDINDICES1;
};
PackedVaryingsPassToPS PackVaryingsPassToPS(VaryingsPassToPS input)
{
    PackedVaryingsPassToPS output;
    output.arrayIndex = input.arrayIndex;
    return output;
}
VaryingsPassToPS UnpackVaryingsPassToPS(PackedVaryingsPassToPS input)
{
    VaryingsPassToPS output;
    output.arrayIndex = input.arrayIndex;
    return input;
}

#define VARYINGS_NEED_PASS
#define VARYINGS_NEED_RT_ARRAY_INDEX
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh, uint instanceID : SV_InstanceID)
{
    g_activeArrayIndex = instanceID;

    VaryingsType varyingsType;

#if (SHADERPASS == SHADERPASS_DEPTH_ONLY) && defined(HAVE_RECURSIVE_RENDERING) && !defined(SCENESELECTIONPASS) && !defined(SCENEPICKINGPASS)
    // If we have a recursive raytrace object, we will not render it.
    // As we don't want to rely on renderqueue to exclude the object from the list,
    // we cull it by settings position to NaN value.
    // TODO: provide a solution to filter dyanmically recursive raytrace object in the DrawRenderer
    if (_EnableRecursiveRayTracing && _RayTracing > 0.0)
    {
        ZERO_INITIALIZE(VaryingsType, varyingsType); // Divide by 0 should produce a NaN and thus cull the primitive.
    }
    else
#endif
    {
        varyingsType.vmesh = VertMesh(inputMesh);
    }

    varyingsType.vpass.arrayIndex = g_activeArrayIndex;
    varyingsType.rtArrayIndex = g_activeArrayIndex;

    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    g_activeArrayIndex = instanceID;

    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    varyingsType.vpass.arrayIndex = g_activeArrayIndex;
    output.rtArrayIndex = g_activeArrayIndex;
    return PackVaryingsToPS(output);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

TEXTURE2D(_SurfaceCacheLit);

float4 Frag(  PackedVaryingsToPS packedInput
            #ifdef _DEPTHOFFSET_ON
            , out float outputDepth : DEPTH_OFFSET_SEMANTIC
            #endif
            ) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    g_activeArrayIndex = packedInput.vpass.arrayIndex;
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.deviceDepth;
#endif

    // read from surface cache (requires lightmap uvs)
#ifdef LIGHTMAP_ON
    float2 lightmapUV = input.texCoord1 * unity_LightmapST.xy + unity_LightmapST.zw;
    float4 cached = SAMPLE_TEXTURE2D(_SurfaceCacheLit, s_linear_clamp_sampler, lightmapUV);
#else
    float4 cached = 0;
#endif

    // TODO: actually read from the surface cache
    return float4(cached.xyz, 1.0f);
}
