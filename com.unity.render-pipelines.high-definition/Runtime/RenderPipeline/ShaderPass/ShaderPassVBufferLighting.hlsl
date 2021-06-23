#if (SHADERPASS != SHADERPASS_VBUFFER_LIGHTING)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VBuffer/VisibilityBufferCommon.hlsl"

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

int _CurrMaterialID;

Varyings Vert(Attributes inputMesh)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(inputMesh.vertexID);
    output.texcoord = GetFullScreenTriangleTexCoord(inputMesh.vertexID);

    float testDepth = float(_CurrMaterialID) / (float)(0xffff);

    output.positionCS.xy /= output.positionCS.w;
    output.positionCS.z = testDepth;
    output.positionCS.w = 1;

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


float3 MeTestey(float3 v0, float3 v1, float3 v2)
{

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

float2 ToNDC(float2 hClip)
{
    // Convert it to screen sample space
    float2 NDC = hClip.xy * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    NDC.y = 1.0 - NDC.y;
#endif
    return NDC;
}

// START FROM HERE

FragInputs EvaluateFragInput(float4 posSS, uint instanceID, uint triangleID, float3 posWS, float3 V, out float3 debugValue)
{
    InstanceVData instanceVData = _InstanceVDataBuffer[instanceID];
    uint i0 = _CompactedIndexBuffer[CLUSTER_SIZE_IN_INDICES * instanceVData.chunkStartIndex + triangleID * 3];
    uint i1 = _CompactedIndexBuffer[CLUSTER_SIZE_IN_INDICES * instanceVData.chunkStartIndex + triangleID * 3 + 1];
    uint i2 = _CompactedIndexBuffer[CLUSTER_SIZE_IN_INDICES * instanceVData.chunkStartIndex + triangleID * 3 + 2];

    // Compute the modelview projection matrix
    float4x4 m = ApplyCameraTranslationToMatrix(instanceVData.localToWorld);

    CompactVertex v0 = _CompactedVertexBuffer[i0];
    CompactVertex v1 = _CompactedVertexBuffer[i1];
    CompactVertex v2 = _CompactedVertexBuffer[i2];

    // Get barycentrics.
    float3 pos0WS = mul(m, float4(v0.pos, 1.0));
    float3 pos1WS = mul(m, float4(v1.pos, 1.0));
    float3 pos2WS = mul(m, float4(v2.pos, 1.0));

    // Compute barycentric

    float4 pos0 = mul(UNITY_MATRIX_VP, float4(pos0WS, 1.0));
    float4 pos1 = mul(UNITY_MATRIX_VP, float4(pos1WS, 1.0));
    float4 pos2 = mul(UNITY_MATRIX_VP, float4(pos2WS, 1.0));

    //pos0.xyz /= pos0.w;
    //pos1.xyz /= pos1.w;
    //pos2.xyz /= pos2.w;

    //float3 barycentricCoordinates = ComputeBarycentricCoords(posSS * _ScreenSize.zw, ToNDC(pos0.xy), ToNDC(pos1.xy), ToNDC(pos2.xy)).xyz;

   // {
        // Compute the supporting plane properties
        float3 triangleCenter = 0;// (pos0WS + pos1WS + pos2WS) / 3.0;
        float3 triangleNormal = normalize(cross(pos1WS - pos0WS, pos2WS - pos0WS));
        // Project all point onto the 2d supporting plane
        float3 projectedWS = posWS - dot(posWS - triangleCenter, triangleNormal) * triangleNormal;
        float3 projected0WS = pos0WS - dot(pos0WS - triangleCenter, triangleNormal) * triangleNormal;
        float3 projected1WS = pos1WS - dot(pos1WS - triangleCenter, triangleNormal) * triangleNormal;
        float3 projected2WS = pos2WS - dot(pos2WS - triangleCenter, triangleNormal) * triangleNormal;
        // Evaluate the barycentrics
        float3 barycentricCoordinates = ComputeBarycentricCoords(projectedWS.xz, projected0WS.xz, projected1WS.xz, projected2WS.xz);
    //}

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


    // Compute the world space normal and tangent. [IMPORTANT, we assume uniform scale here]
    float3 normalWS = normalize(mul(float4(normalOS, 0), instanceVData.localToWorld));
    float3 tangentWS = normalize(mul(float4(tangentOS.xyz, 0), instanceVData.localToWorld));


    // DEBG
    debugValue = barycentricCoordinates;// float3(texCoord0.xy, 0);
    ///

    FragInputs outFragInputs;
    ZERO_INITIALIZE(FragInputs, outFragInputs);
    outFragInputs.positionSS = posSS;
    outFragInputs.positionRWS = posWS;
    outFragInputs.texCoord0 = float4(texCoord0, 0.0, 1.0);
    //outFragInputs.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, 1.0);
    outFragInputs.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, sign(tangentOS.w));
    outFragInputs.isFrontFace = dot(V, outFragInputs.tangentToWorld[2]) < 0.0f;
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

    float depthValue = LOAD_TEXTURE2D_X(_CameraDepthTexture, pixelCoord);
    if (depthValue == UNITY_RAW_FAR_CLIP_VALUE)
    {
        outColor = 0;
        return;
    }

    float2 ndc = pixelCoord * _ScreenSize.zw;
    //ndc = (ndc);
    float3 posWS = ComputeWorldSpacePosition(ndc, depthValue, UNITY_MATRIX_I_VP);
    float3 V = GetWorldSpaceNormalizeViewDir(posWS);
    float3 debugVal = 0;
    FragInputs input = EvaluateFragInput(packedInput.positionCS, instanceID, triangleID, posWS, V, debugVal);


    // Build the position input
    int2 tileCoord = (float2)input.positionSS.xy / GetTileSize();
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depthValue, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), tileCoord);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;
    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    outColor.xyz = lightLoopOutput.diffuseLighting * GetCurrentExposureMultiplier();
    outColor.a = 1;
    outColor.xyz = bsdfData.diffuseColor;
    //outColor.xyz = debugVal;

}
