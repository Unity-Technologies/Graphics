#if (SHADERPASS != SHADERPASS_VBUFFER_LIGHTING)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityCommon.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
    uint instanceID : SV_InstanceID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    uint lightAndMaterialFeatures : FEATURES0;
    uint debugIndex : FEATURES1;
    UNITY_VERTEX_OUTPUT_STEREO
};

TEXTURE2D_X(_VisBufferDepthTexture);
float4 _VisBufferTileData;
#define _NumVBufferTileX (uint)_VisBufferTileData.x
#define _NumVBufferTileY (uint)_VisBufferTileData.y
#define _QuadTileSize (uint)_VisBufferTileData.z
#define _TotalTiles (uint) _VisBufferTileData.w

uint GetCurrentMaterialBatchGPUKey()
{
#ifdef DOTS_INSTANCING_ON
    return unity_DOTSVisibleInstances[0].VisibleData.x;
#else
    return 0;
#endif
}

uint getCurrentMaterialGPUKey()
{
    return GetCurrentMaterialBatchGPUKey() >> 8;
}

uint GetCurrentBatchID()
{
    return GetCurrentMaterialBatchGPUKey() & 0xFF;
}

Varyings Vert(Attributes inputMesh)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);

#ifdef DOTS_INSTANCING_ON
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    uint tileIndex = inputMesh.vertexID >> 2;

    if (tileIndex >= _TotalTiles)
        return output;

    int tileX = tileIndex % _NumVBufferTileX;
    int tileY = tileIndex / _NumVBufferTileX;
    int quadVertexID = inputMesh.vertexID % 4;

    int2 tileCoord = int2(tileX, tileY);
    float2 tileSizeInUV = rcp(float2(_NumVBufferTileX, _NumVBufferTileY));
    float2 tileStartUV = tileCoord * rcp(float2(_NumVBufferTileX, _NumVBufferTileY));
    float2 vertPos = (float2(quadVertexID & 1, (quadVertexID >> 1) & 1) + tileCoord) * tileSizeInUV;

    int tapTileY = (_NumVBufferTileY-1) - tileY;

    uint2 tileCoords = uint2(tileX, tapTileY);
    uint bucketIDMask = Visibility::LoadBucketTile(tileCoords);
    uint2 matMinMax = Visibility::LoadMaterialTile(tileCoords);
    output.lightAndMaterialFeatures = Visibility::LoadFeatureTile(tileCoords);
    uint currentTileCategory = Visibility::GetLightTileCategory(output.lightAndMaterialFeatures);

    uint shaderTileCategory = 0;
    #if defined(VARIANT_DIR_ENV)
    shaderTileCategory = LIGHTVBUFFERTILECATEGORY_ENV;
    #elif defined(VARIANT_DIR_PUNCTUAL_ENV)
    shaderTileCategory = LIGHTVBUFFERTILECATEGORY_ENV_PUNCTUAL;
    #elif defined(VARIANT_DIR_PUNCTUAL_AREA_ENV)
    shaderTileCategory = LIGHTVBUFFERTILECATEGORY_EVERYTHING;
    #endif

    if (((getCurrentMaterialGPUKey() & bucketIDMask) != 0) && (getCurrentMaterialGPUKey() >= matMinMax.x && getCurrentMaterialGPUKey() <= matMinMax.y) && shaderTileCategory == currentTileCategory)
    {
        output.positionCS.xy = vertPos * 2 - 1;
        output.positionCS.w = 1;
        output.positionCS.z = Visibility::PackDepthMaterialKey(GetCurrentMaterialBatchGPUKey());
    }

#endif

    return output;
}

#define INTERPOLATE_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

// Analytical derivatives, Hable 2021
//http://filmicworlds.com/blog/visibility-buffer-rendering-with-material-graphs/
struct BarycentricDeriv
{
    float3 m_lambda;
    float3 m_ddx;
    float3 m_ddy;
};

BarycentricDeriv CalcFullBary(float4 pt0, float4 pt1, float4 pt2, float2 pixelNdc, float2 winSize)
{
    BarycentricDeriv ret = (BarycentricDeriv)0;

    float3 invW = rcp(float3(pt0.w, pt1.w, pt2.w));

    float2 ndc0 = pt0.xy * invW.x;
    float2 ndc1 = pt1.xy * invW.y;
    float2 ndc2 = pt2.xy * invW.z;

    float invDet = rcp(determinant(float2x2(ndc2 - ndc1, ndc0 - ndc1)));
    ret.m_ddx = float3(ndc1.y - ndc2.y, ndc2.y - ndc0.y, ndc0.y - ndc1.y) * invDet;
    ret.m_ddy = float3(ndc2.x - ndc1.x, ndc0.x - ndc2.x, ndc1.x - ndc0.x) * invDet;

    float2 deltaVec = pixelNdc - ndc0;
    float interpInvW = (invW.x + deltaVec.x * dot(invW, ret.m_ddx) + deltaVec.y * dot(invW, ret.m_ddy));
    float interpW = rcp(interpInvW);

    ret.m_lambda.x = interpW * (invW[0] + deltaVec.x * ret.m_ddx.x * invW[0] + deltaVec.y * ret.m_ddy.x * invW[0]);
    ret.m_lambda.y = interpW * (0.0f    + deltaVec.x * ret.m_ddx.y * invW[1] + deltaVec.y * ret.m_ddy.y * invW[1]);
    ret.m_lambda.z = interpW * (0.0f    + deltaVec.x * ret.m_ddx.z * invW[2] + deltaVec.y * ret.m_ddy.z * invW[2]);

    ret.m_ddx *= (2.0f/winSize.x);
    ret.m_ddy *= (2.0f/winSize.y);

    ret.m_ddy *= -1.0f;

    return ret;
}


FragInputs EvaluateFragInput(
    float2 ndc,
    float4 posSS,
    in GeoPoolMetadataEntry geoMetadata,
    in Visibility::VisibilityData visData,
    float3 posWS, float3 V, out float3 debugValue)
{
    uint i0 = _GeoPoolGlobalIndexBuffer.Load((geoMetadata.indexOffset + 3 * visData.primitiveID + 0) << 2);
    uint i1 = _GeoPoolGlobalIndexBuffer.Load((geoMetadata.indexOffset + 3 * visData.primitiveID + 1) << 2);
    uint i2 = _GeoPoolGlobalIndexBuffer.Load((geoMetadata.indexOffset + 3 * visData.primitiveID + 2) << 2);

    GeoPoolVertex v0 = GeometryPool::LoadVertex(i0, geoMetadata);
    GeoPoolVertex v1 = GeometryPool::LoadVertex(i1, geoMetadata);
    GeoPoolVertex v2 = GeometryPool::LoadVertex(i2, geoMetadata);

    // Convert the positions to world space
    float3 pos0WS = TransformObjectToWorld(v0.pos);
    float3 pos1WS = TransformObjectToWorld(v1.pos);
    float3 pos2WS = TransformObjectToWorld(v2.pos);

    float4 p0h = mul(GetWorldToHClipMatrix(), float4(pos0WS, 1.0));
    float4 p1h = mul(GetWorldToHClipMatrix(), float4(pos1WS, 1.0));
    float4 p2h = mul(GetWorldToHClipMatrix(), float4(pos2WS, 1.0));

    BarycentricDeriv baryResult = CalcFullBary(p0h, p1h, p2h, ndc, (float2)_ScreenSize.xy);

    // Evaluate the barycentrics
    float3 barycentricCoordinates = baryResult.m_lambda;

    // Get normal at position
    float3 normalOS0 = v0.N;
    float3 normalOS1 = v1.N;
    float3 normalOS2 = v2.N;
    float3 normalOS = INTERPOLATE_ATTRIBUTE(normalOS0, normalOS1, normalOS2, barycentricCoordinates);

    // Get tangent at position
    float4 tangentOS0 = float4(v0.T, 1.0);
    float4 tangentOS1 = float4(v1.T, 1.0);
    float4 tangentOS2 = float4(v2.T, 1.0);
    float4 tangentOS = INTERPOLATE_ATTRIBUTE(tangentOS0, tangentOS1, tangentOS2, barycentricCoordinates);

    // Get UV at position
    float2 UV0 = (v0.uv);
    float2 UV1 = (v1.uv);
    float2 UV2 = (v2.uv);
    float2 texCoord0 = INTERPOLATE_ATTRIBUTE(UV0, UV1, UV2, barycentricCoordinates);

    // Get UV1 at position
    float2 UV1_0 = (v0.uv1);
    float2 UV1_1 = (v1.uv1);
    float2 UV1_2 = (v2.uv1);
    float2 texCoord1 = INTERPOLATE_ATTRIBUTE(UV1_0, UV1_1, UV1_2, barycentricCoordinates);

    // Compute the world space normal and tangent. [IMPORTANT, we assume uniform scale here]
    float3 normalWS = TransformObjectToWorldDir(normalOS);
    float3 tangentWS = TransformObjectToWorldDir(tangentOS.xyz);

    // DEBG
    //debugValue = saturate(dot(V, normalize(v0.N + v1.N + v2.N))).xxx;
    //debugValue = saturate(dot(V, normalWS)).xxx;
    debugValue = barycentricCoordinates.xyz;
    ///

    FragInputs outFragInputs;
    ZERO_INITIALIZE(FragInputs, outFragInputs);
    outFragInputs.positionSS = posSS;
    outFragInputs.positionRWS = posWS;
    outFragInputs.texCoord0 = float4(texCoord0, 0.0, 1.0);
    outFragInputs.texCoord1 = float4(texCoord1, 0.0, 1.0);
    //outFragInputs.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, 1.0);
    outFragInputs.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, sign(tangentOS.w));
    outFragInputs.isFrontFace = dot(V, normalWS) > 0.0f;
    return outFragInputs;
}

void Frag(Varyings packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
#ifdef DOTS_INSTANCING_ON
    Visibility::VisibilityData visData = Visibility::LoadVisibilityData(packedInput.positionCS.xy);

    //Setup visibility buffer
    {
        DOTSVisibleData dotsVisData;
        dotsVisData.VisibleData = uint4(visData.DOTSInstanceIndex, 0, 0, 0);
        unity_SampledDOTSVisibleData = dotsVisData;
    }

    GeoPoolMetadataEntry geometryMetadata;
    uint materialKey = Visibility::GetMaterialKey(visData, geometryMetadata);
    if (materialKey != getCurrentMaterialGPUKey())
    {
        outColor = float4(0,0,0,0);
        return;
    }

    float2 pixelCoord = packedInput.positionCS.xy;
    float depthValue = LOAD_TEXTURE2D_X(_VisBufferDepthTexture, pixelCoord);
    float2 ndc = pixelCoord * _ScreenSize.zw;
    float3 posWS = ComputeWorldSpacePosition(ndc, depthValue, UNITY_MATRIX_I_VP);
    ndc = (ndc * 2.0 - 1.0) * float2(1.0, -1.0);
    float3 V = GetWorldSpaceNormalizeViewDir(posWS);

    float3 debugValue = float3(0,0,0);
    FragInputs input = EvaluateFragInput(ndc, packedInput.positionCS, geometryMetadata, visData, posWS, V, debugValue);

    int2 tileCoord = (float2)input.positionSS.xy / GetTileSize();
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depthValue, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), tileCoord);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    float3 colorVariantColor = 0;
    uint featureFlags = packedInput.lightAndMaterialFeatures;

    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    float3 diffuseLighting =  lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    diffuseLighting *= GetCurrentExposureMultiplier();
    specularLighting *= GetCurrentExposureMultiplier();


    outColor.rgb = diffuseLighting + specularLighting;
    outColor.a = 1;
    //outColor = float4(debugValue, 1.0);
#else
    outColor = 0;
#endif
}
