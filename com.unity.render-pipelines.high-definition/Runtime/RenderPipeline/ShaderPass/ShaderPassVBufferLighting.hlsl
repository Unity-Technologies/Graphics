#if (SHADERPASS != SHADERPASS_VBUFFER_LIGHTING)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VBuffer/VisibilityBufferCommon.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VBuffer/VisibilityBufferCommon.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
    uint instanceID : SV_InstanceID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    uint lightAndMaterialFeatures : FEATURES;
    uint2 tileCoord : TILE_COORD;
    UNITY_VERTEX_OUTPUT_STEREO
};

int _CurrMaterialID;
int _CurrFeatureSet;
float4 _VBufferTileData;
#define _NumVBufferTileX (uint)_VBufferTileData.x
#define _NumVBufferTileY (uint)_VBufferTileData.y
#define _QuadTileSize (uint)_VBufferTileData.z

static const float2 QuadVertices[6] = { float2(0,0), float2(1, 0), float2(1, 1), float2(0, 0), float2(1, 1), float2(0, 1)};

TEXTURE2D_X_UINT(_VBufferTileClassification);

Varyings Vert(Attributes inputMesh)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);

#ifdef UNITY_ANY_INSTANCING_ENABLED
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    uint instanceID = inputMesh.instanceID;

    int tileX = instanceID % _NumVBufferTileX;
    int tileY = instanceID / _NumVBufferTileX;
    int quadVertexID = inputMesh.vertexID % 6;

    int2 tileCoord = int2(tileX, tileY);
    float2 tileSizeInUV = rcp(float2(_NumVBufferTileX, _NumVBufferTileY));
    float2 tileStartUV = tileCoord * rcp(float2(_NumVBufferTileX, _NumVBufferTileY));
    float2 vertPos = (QuadVertices[quadVertexID] + tileCoord) * tileSizeInUV;

    output.positionCS.xy = vertPos * 2 - 1;

    output.lightAndMaterialFeatures = _VBufferTileClassification[COORD_TEXTURE2D_X(uint2(tileX, (_NumVBufferTileY-1) - tileY))];
    output.positionCS.z = float(_CurrMaterialID) / (float)(0xffff);
    output.positionCS.w = 1;

    output.tileCoord = uint2(tileX, tileY);
#endif

    return output;
}

#define INTERPOLATE_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

float3 ComputeBarycentricCoords(float2 p, float2 a, float2 b, float2 c)
{
    float2 v0 = b - a, v1 = c - a, v2 = p - a;
    float d00 = dot(v0, v0);
    float d01 = dot(v0, v1);
    float d11 = dot(v1, v1);
    float d20 = dot(v2, v0);
    float d21 = dot(v2, v1);
    float denom = d00 * d11 - d01 * d01;
    float3 barycentricCoords;
    barycentricCoords.y = (d11 * d20 - d01 * d21) / denom;
    barycentricCoords.z = (d00 * d21 - d01 * d20) / denom;
    barycentricCoords.x = 1.0f - barycentricCoords.y - barycentricCoords.z;
    return barycentricCoords;
}

float2 DecompressVector2(uint direction)
{
    float x = f16tof32(direction);
    float y = f16tof32(direction >> 16);
    return float2(x,y);
}

float3 DecompressVector3(uint direction)
{
    float x = f16tof32(direction);
    float y = f16tof32(direction >> 16);
    return UnpackNormalOctQuadEncode(float2(x,y) * 2.0 - 1.0);
}

FragInputs EvaluateFragInput(float4 posSS, uint instanceID, uint triangleID, float3 posWS, float3 V, InstanceVData instanceVData, out float3 debugValue)
{
    uint i0 = _CompactedIndexBuffer[CLUSTER_SIZE_IN_INDICES * instanceVData.chunkStartIndex + triangleID * 3];
    uint i1 = _CompactedIndexBuffer[CLUSTER_SIZE_IN_INDICES * instanceVData.chunkStartIndex + triangleID * 3 + 1];
    uint i2 = _CompactedIndexBuffer[CLUSTER_SIZE_IN_INDICES * instanceVData.chunkStartIndex + triangleID * 3 + 2];

    // Compute the modelview projection matrix
    float4x4 m = ApplyCameraTranslationToMatrix(instanceVData.localToWorld);

    CompactVertex v0 = _CompactedVertexBuffer[i0];
    CompactVertex v1 = _CompactedVertexBuffer[i1];
    CompactVertex v2 = _CompactedVertexBuffer[i2];

    // Convert the positions to world space
    float3 pos0WS = mul(m, float4(v0.pos, 1.0));
    float3 pos1WS = mul(m, float4(v1.pos, 1.0));
    float3 pos2WS = mul(m, float4(v2.pos, 1.0));

    // Compute the supporting plane properties
    float3 triangleCenter = (pos0WS + pos1WS + pos2WS) / 3.0;
    float3 triangleNormal = normalize(cross(pos2WS - pos0WS, pos1WS - pos0WS));

    // Compute the world to plane matrix
    float3 yLocalPlane = normalize(pos1WS - pos0WS);
    float3x3 worldToPlaneMatrix = float3x3(cross(yLocalPlane, triangleNormal), yLocalPlane, triangleNormal);

    // Project all point onto the 2d supporting plane
    float3 projectedWS = mul(worldToPlaneMatrix, posWS - dot(posWS - triangleCenter, triangleNormal) * triangleNormal);
    float3 projected0WS = mul(worldToPlaneMatrix, pos0WS - dot(pos0WS - triangleCenter, triangleNormal) * triangleNormal);
    float3 projected1WS = mul(worldToPlaneMatrix, pos1WS - dot(pos1WS - triangleCenter, triangleNormal) * triangleNormal);
    float3 projected2WS = mul(worldToPlaneMatrix, pos2WS - dot(pos2WS - triangleCenter, triangleNormal) * triangleNormal);

    // Evaluate the barycentrics
    float3 barycentricCoordinates = ComputeBarycentricCoords(projectedWS.xy, projected0WS.xy, projected1WS.xy, projected2WS.xy);

    // Get normal at position
    float3 normalOS0 = v0.N;
    float3 normalOS1 = v1.N;
    float3 normalOS2 = v2.N;
    float3 normalOS = INTERPOLATE_ATTRIBUTE(normalOS0, normalOS1, normalOS2, barycentricCoordinates);

    // Get tangent at position
    float4 tangentOS0 = (v0.T);
    float4 tangentOS1 = (v1.T);
    float4 tangentOS2 = (v2.T);
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
    float3 normalWS = normalize(mul((float3x3)instanceVData.localToWorld, normalOS));
    float3 tangentWS = normalize(mul((float3x3)instanceVData.localToWorld, tangentOS.xyz));

    // DEBG
    debugValue = float3(texCoord1, 0);// float3(texCoord0.xy, 0);
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

    uint2 pixelCoord = packedInput.positionCS.xy;
    // Grab the geometry information
    uint vbuffer = LOAD_TEXTURE2D_X(_VBuffer0, pixelCoord).x;
    uint triangleID, instanceID;
    UnpackVisibilityBuffer(vbuffer, instanceID, triangleID);

    float depthValue = LOAD_TEXTURE2D_X(_VBufferDepthTexture, pixelCoord);
    if (depthValue == UNITY_RAW_FAR_CLIP_VALUE)
    {
        outColor = 0;
        return;
    }

    float2 ndc = pixelCoord * _ScreenSize.zw;
    float3 posWS = ComputeWorldSpacePosition(ndc, depthValue, UNITY_MATRIX_I_VP);
    float3 V = GetWorldSpaceNormalizeViewDir(posWS);
    float3 debugVal = 0;

    InstanceVData instanceVData = _InstanceVDataBuffer[instanceID];
    FragInputs input = EvaluateFragInput(packedInput.positionCS, instanceID, triangleID, posWS, V, instanceVData, debugVal);

    // Build the position input
    int2 tileCoord = (float2)input.positionSS.xy / GetTileSize();
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depthValue, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), tileCoord);

    unity_LightmapST = instanceVData.lightmapST;
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
    builtinData.bakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    builtinData.backBakeDiffuseLighting = float3(0.0, 0.0, 0.0);
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    uint featureFlags = packedInput.lightAndMaterialFeatures;
    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    float3 diffuseLighting =  lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    diffuseLighting *= GetCurrentExposureMultiplier();
    specularLighting *= GetCurrentExposureMultiplier();


    outColor.rgb = float4(diffuseLighting + specularLighting, 1.0);
    outColor.a = 1;
}
