//Collection of vertex shaders and pixel shader bootstrappers to simulate deferred materials / virtual geometry.

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityCommon.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    uint lightAndMaterialFeatures : FEATURES0;
    uint currentMaterialKey : FEATURES1;
    UNITY_VERTEX_OUTPUT_STEREO
};

TEXTURE2D_X(_VisBufferDepthTexture);
float4 _VisBufferTileData;
#define _NumVBufferTileX (uint)_VisBufferTileData.x
#define _NumVBufferTileY (uint)_VisBufferTileData.y
#define _QuadTileSize (uint)_VisBufferTileData.z
#define _TotalTiles (uint) _VisBufferTileData.w

uint GetCurrentMaterialGPUKey(uint materialBatchGPUKey)
{
    return materialBatchGPUKey >> 8;
}

uint GetCurrentBatchID(uint materialBatchGPUKey)
{
    return materialBatchGPUKey & 0xFF;
}

uint GetShaderTileCategory()
{
    uint shaderTileCategory = 0;
    #if defined(VARIANT_DIR_ENV)
    shaderTileCategory = LIGHTVBUFFERTILECATEGORY_ENV;
    #elif defined(VARIANT_DIR_PUNCTUAL_ENV)
    shaderTileCategory = LIGHTVBUFFERTILECATEGORY_ENV_PUNCTUAL;
    #elif defined(VARIANT_DIR_PUNCTUAL_AREA_ENV)
    shaderTileCategory = LIGHTVBUFFERTILECATEGORY_EVERYTHING;
    #endif
    return shaderTileCategory;
}

uint GetShaderFeatureMask()
{
    uint featureMasks = 0;
    #if defined(VARIANT_DIR_ENV)
    featureMasks = VBUFFER_LIGHTING_FEATURES_ENV;
    #elif defined(VARIANT_DIR_PUNCTUAL_ENV)
    featureMasks = VBUFFER_LIGHTING_FEATURES_ENV_PUNCTUAL;
    #elif defined(VARIANT_DIR_PUNCTUAL_AREA_ENV)
    featureMasks = VBUFFER_LIGHTING_FEATURES_EVERYTHING;
    #endif
    return featureMasks;
}

Varyings Vert(Attributes inputMesh)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

   // NaN SV_Position causes GPU to reject all triangles that use this vertex
    //float nan = sqrt(-1);
    float nan = 0.0f / 0.0f;
    output.positionCS = float4(nan, nan, nan, nan);

#ifdef DOTS_INSTANCING_ON

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

    uint shaderTileCategory = GetShaderTileCategory();
    uint materialBatchGPUKey = UNITY_GET_INSTANCE_ID(inputMesh);
    uint currentMaterialKey = GetCurrentMaterialGPUKey(materialBatchGPUKey);

    if (((currentMaterialKey & bucketIDMask) != 0) && (currentMaterialKey >= matMinMax.x && currentMaterialKey <= matMinMax.y) && shaderTileCategory == currentTileCategory)
    {
        output.positionCS.xy = vertPos * 2 - 1;
        output.positionCS.w = 1;
        output.positionCS.z = Visibility::PackDepthMaterialKey(materialBatchGPUKey);
        output.currentMaterialKey = currentMaterialKey;
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

struct VBufferDebugFragmentData
{
    BarycentricDeriv barycentrics;
    float3 vertexNormal;
};

struct VBufferDeferredMaterialFragmentData
{
    bool valid;
    Visibility::VisibilityData visData;
    FragInputs fragInputs;
    float depthValue;
    float3 V;
    VBufferDebugFragmentData debugData;
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
    float3 posWS, float3 V, out VBufferDebugFragmentData debugData)
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

    // DEBUG DATA
    debugData.barycentrics = baryResult;
    debugData.vertexNormal = normalWS;
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

VBufferDeferredMaterialFragmentData BootstrapDeferredMaterialFragmentShader(float4 positionCS, uint currentMaterialKey, Visibility::VisibilityData visData)
{
    VBufferDeferredMaterialFragmentData fragmentData;
    ZERO_INITIALIZE(VBufferDeferredMaterialFragmentData, fragmentData);

#ifdef DOTS_INSTANCING_ON
    if (!visData.valid)
        return fragmentData;

    //Setup visibility buffer
    {
        DOTSVisibleData dotsVisData;
        dotsVisData.VisibleData = uint4(visData.DOTSInstanceIndex, 0, 0, 0);
        unity_SampledDOTSVisibleData = dotsVisData;
    }

    GeoPoolMetadataEntry geometryMetadata;
    uint materialKey = Visibility::GetMaterialKey(visData, geometryMetadata);
    if (materialKey != currentMaterialKey)
        return fragmentData;

    float2 pixelCoord = positionCS.xy;
    float depthValue = LOAD_TEXTURE2D_X(_VisBufferDepthTexture, pixelCoord).x;
    float2 ndc = pixelCoord * _ScreenSize.zw;
    float3 posWS = ComputeWorldSpacePosition(ndc, depthValue, UNITY_MATRIX_I_VP);
    ndc = (ndc * 2.0 - 1.0) * float2(1.0, -1.0);
    float3 V = GetWorldSpaceNormalizeViewDir(posWS);

    FragInputs fragInputs = EvaluateFragInput(ndc, positionCS, geometryMetadata, visData, posWS, V, fragmentData.debugData);

    fragmentData.valid = true;
    fragmentData.visData = visData;
    fragmentData.fragInputs = fragInputs;
    fragmentData.depthValue = depthValue;
    fragmentData.V = V;

#endif
    return fragmentData;
}

VBufferDeferredMaterialFragmentData BootstrapDeferredMaterialFragmentShader(Varyings packedInput)
{
    VBufferDeferredMaterialFragmentData fragmentData;
    ZERO_INITIALIZE(VBufferDeferredMaterialFragmentData, fragmentData);
#ifdef DOTS_INSTANCING_ON
    Visibility::VisibilityData visData = Visibility::LoadVisibilityData(packedInput.positionCS.xy);
    fragmentData = BootstrapDeferredMaterialFragmentShader(packedInput.positionCS, packedInput.currentMaterialKey, visData);
#endif
    return fragmentData;
}
