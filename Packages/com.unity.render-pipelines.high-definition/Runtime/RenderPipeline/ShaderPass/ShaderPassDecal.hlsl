#if (SHADERPASS != SHADERPASS_DEPTH_ONLY) && (SHADERPASS != SHADERPASS_DBUFFER_PROJECTOR) && (SHADERPASS != SHADERPASS_DBUFFER_MESH) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) && (SHADERPASS != SHADERPASS_FORWARD_EMISSIVE_MESH) && (SHADERPASS != SHADERPASS_FORWARD_PREVIEW)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"
#if (SHADERPASS == SHADERPASS_DBUFFER_MESH)
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/DecalMeshBiasTypeEnum.cs.hlsl"
#endif

void MeshDecalsPositionZBias(inout VaryingsToPS input)
{
#if UNITY_REVERSED_Z
    input.vmesh.positionCS.z -= _DecalMeshDepthBias;
#else
    input.vmesh.positionCS.z += _DecalMeshDepthBias;
#endif
}

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    // Usually the instance ID is set up in the call to VertMesh
    // but as we are reading per instance data here we need to set it up early
    UNITY_SETUP_INSTANCE_ID(inputMesh);

    VaryingsType varyingsType;
#if (SHADERPASS == SHADERPASS_DBUFFER_MESH)

    float3 worldSpaceBias = 0.0f;
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_VIEW_BIAS)
    {
        float3 positionRWS = TransformObjectToWorld(inputMesh.positionOS);
        float3 V = GetWorldSpaceNormalizeViewDir(positionRWS);

        worldSpaceBias = V * (_DecalMeshViewBias);
    }
    varyingsType.vmesh = VertMesh(inputMesh, worldSpaceBias);
    if (_DecalMeshBiasType == DECALMESHDEPTHBIASTYPE_DEPTH_BIAS)
    {
        MeshDecalsPositionZBias(varyingsType);
    }
#else
    varyingsType.vmesh = VertMesh(inputMesh);
#endif
    return PackVaryingsType(varyingsType);
}

void Frag(  PackedVaryingsToPS packedInput,
#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
    OUTPUT_DBUFFER(outDBuffer)
#elif defined(SCENEPICKINGPASS) || (SHADERPASS == SHADERPASS_FORWARD_PREVIEW) // Only used for preview in shader graph and scene picking
    out float4 outColor : SV_Target0
#else
    out float4 outEmissive : SV_Target0
#endif
)
{
#ifdef SCENEPICKINGPASS
    outColor = unity_SelectionID;
#else
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);
    DecalSurfaceData surfaceData;
    float clipValue = 1.0;
    float angleFadeFactor = 1.0;

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)

    float depth = LoadCameraDepth(input.positionSS.xy);
#if (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR) && UNITY_REVERSED_Z
    // For the sky adjust the depth so that the following LOD calculation (GetSurfaceData() in DecalData.hlsl) of adjacent
    // non-sky pixels using depth derivatives results in LOD0 sampling
    depth = IsSky(depth) ? UNITY_NEAR_CLIP_VALUE : depth;
#endif
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

    // Decal layer mask accepted by the receiving material
    DecalPrepassData material;
    ZERO_INITIALIZE(DecalPrepassData, material);
    if (_EnableDecalLayers)
    {
        // Clip the decal if it does not pass the decal layer mask of the receiving material.
        // Decal layer of the decal
        uint decalLayerMask = uint(UNITY_ACCESS_INSTANCED_PROP(Decal, _DecalLayerMaskFromDecal).x);

        DecodeFromDecalPrepass(posInput.positionSS, material);

        if ((decalLayerMask & material.renderingLayerMask) == 0)
            clipValue -= 2.0;
    }

    // Transform from relative world space to decal space (DS) to clip the decal
    float3 positionDS = TransformWorldToObject(posInput.positionWS);
    positionDS = positionDS * float3(1.0, -1.0, 1.0) + float3(0.5, 0.5, 0.5);
    if (!(all(positionDS.xyz > 0.0f) && all(1.0f - positionDS.xyz > 0.0f)))
    {
        clipValue -= 2.0; // helper lanes will be clipped
    }

    // call clip as early as possible
#ifndef SHADER_API_METAL
    // Calling clip here instead of inside the condition above shouldn't make any performance difference
    // but case save one clip call.
    clip(clipValue);
#else
    // Metal Shading Language declares that fragment discard invalidates
    // derivatives for the rest of the quad, so we need to reorder when
    // we discard during decal projection, or we get artifacts along the
    // edges of the projection(any partial quads get bad partial derivatives
    //regardless of whether they are computed implicitly or explicitly).
    ZERO_INITIALIZE(DecalSurfaceData, surfaceData); // Require to quiet compiler warning with Metal
    // Note we can't used dynamic branching here to avoid to pay the cost of texture fetch otherwise we need to calculate derivatives ourselves.
#endif
    input.texCoord0.xy = positionDS.xz;
    input.texCoord1.xy = positionDS.xz;
    input.texCoord2.xy = positionDS.xz;
    input.texCoord3.xy = positionDS.xz;

    float3 V = GetWorldSpaceNormalizeViewDir(posInput.positionWS);

    // For now we only allow angle fading when decal layers are enabled
    // TODO: Reconstructing normal from depth buffer result in poor result.
    // We may revisit it in the future
    // This is example code:  float3 vtxNormal = normalize(cross(ddy(posInput.positionWS), ddx(posInput.positionWS)));
    // But better implementation (and costlier) can be find here: https://atyuwen.github.io/posts/normal-reconstruction/
    if (_EnableDecalLayers)
    {
        // Check if this decal projector require angle fading
        float4x4 normalToWorld = UNITY_ACCESS_INSTANCED_PROP(Decal, _NormalToWorld);
        float2 angleFade = float2(normalToWorld[1][3], normalToWorld[2][3]);

        if (angleFade.x > 0.0f) // if angle fade is enabled
        {
            float3 decalNormal = float3(normalToWorld[0].z, normalToWorld[1].z, normalToWorld[2].z);
            angleFadeFactor = DecodeAngleFade(dot(material.geomNormalWS, decalNormal), angleFade);
        }
    }

#else // Decal mesh
    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, uint2(0, 0));

    #ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
    #else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
    #endif
#endif

    GetSurfaceData(input, V, posInput, angleFadeFactor, surfaceData);

#if ((SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_FORWARD_EMISSIVE_PROJECTOR)) && defined(SHADER_API_METAL)
    clip(clipValue);
#endif

#if (SHADERPASS == SHADERPASS_DBUFFER_PROJECTOR) || (SHADERPASS == SHADERPASS_DBUFFER_MESH)
    ENCODE_INTO_DBUFFER(surfaceData, outDBuffer);
#elif (SHADERPASS == SHADERPASS_FORWARD_PREVIEW) // Only used for preview in shader graph
    outColor = 0;
    // Evaluate directional light from the preview
    uint i;
    for (i = 0; i < _DirectionalLightCount; ++i)
    {
        DirectionalLightData light = _DirectionalLightDatas[i];
        outColor.rgb += surfaceData.baseColor.rgb * light.color * saturate(dot(surfaceData.normalWS.xyz, -light.forward.xyz));
    }

    outColor.rgb += surfaceData.emissive;
    outColor.w = 1.0;
#else
    // Emissive need to be pre-exposed
    outEmissive.rgb = surfaceData.emissive * GetCurrentExposureMultiplier();
    outEmissive.a = 1.0;
#endif
#endif
}
