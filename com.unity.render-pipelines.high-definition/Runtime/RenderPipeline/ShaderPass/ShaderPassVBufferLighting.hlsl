#if (SHADERPASS != SHADERPASS_VBUFFER_LIGHTING)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;

        varyingsType.vmesh = VertMesh(inputMesh);

    return PackVaryingsType(varyingsType);
}

#define INTERPOLATE_ATTRIBUTE(A0, A1, A2, BARYCENTRIC_COORDINATES) (A0 * BARYCENTRIC_COORDINATES.x + A1 * BARYCENTRIC_COORDINATES.y + A2 * BARYCENTRIC_COORDINATES.z)

float3 CalculateTriangleBarycentrics(float2 PixelClip, float4 PointClip0, float4 PointClip1, float4 PointClip2, float2 ViewInvSize)
{
    float3 Pos0 = PointClip0.xyz / PointClip0.w;
    float3 Pos1 = PointClip1.xyz / PointClip1.w;
    float3 Pos2 = PointClip2.xyz / PointClip2.w;
    float3 RcpW = rcp(float3(PointClip0.w, PointClip1.w, PointClip2.w));
    float3 Pos120X = float3(Pos1.x, Pos2.x, Pos0.x);
    float3 Pos120Y = float3(Pos1.y, Pos2.y, Pos0.y);
    float3 Pos201X = float3(Pos2.x, Pos0.x, Pos1.x);
    float3 Pos201Y = float3(Pos2.y, Pos0.y, Pos1.y);
    float3 C_dx = Pos201Y - Pos120Y;
    float3 C_dy = Pos120X - Pos201X;
    float3 C = C_dx * (PixelClip.x - Pos120X) + C_dy * (PixelClip.y - Pos120Y); // Evaluate the 3 edge functions
    float3 G = C * RcpW;
    float H = dot(C, RcpW);
    float RcpH = rcp(H);
    return G * RcpH;
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

// START FROM HERE

FragInputs EvaluateFragInput(float4 posSS, float3 V, uint geometryID, uint triangleID)
{
    InstanceVData instanceVData = _InstanceVDataBuffer[max(geometryID - 1, 0)];
    uint i0 = _CompactedIndexBuffer[instanceVData.startIndex + triangleID * 3];
    uint i1 = _CompactedIndexBuffer[instanceVData.startIndex + triangleID * 3 + 1];
    uint i2 = _CompactedIndexBuffer[instanceVData.startIndex + triangleID * 3 + 2];

    // Compute the modelview projection matrix
    float4x4 mvp = UNITY_MATRIX_VP * instanceVData.localToWorld;

    CompactVertex v0 = _CompactedVertexBuffer[i0];
    CompactVertex v1 = _CompactedVertexBuffer[i1];
    CompactVertex v2 = _CompactedVertexBuffer[i2];
    float4 pos0 = mul(mvp, float4(v0.posX, v0.posY, v0.posZ, 1.0));
    float4 pos1 = mul(mvp, float4(v1.posX, v1.posY, v1.posZ, 1.0));
    float4 pos2 = mul(mvp, float4(v2.posX, v2.posY, v2.posZ, 1.0));
    float3 barycentricCoordinates = CalculateTriangleBarycentrics(posSS.xy * _ScreenSize.zw * 2.0 - 1.0, pos0, pos1, pos2, _ScreenSize.zw);

    float3 normalOS0 = DecompressVector3(v0.N);
    float3 normalOS1 = DecompressVector3(v1.N);
    float3 normalOS2 = DecompressVector3(v2.N);

    float3 tangentOS0 = DecompressVector3(v0.T);
    float3 tangentOS1 = DecompressVector3(v1.T);
    float3 tangentOS2 = DecompressVector3(v2.T);

    float2 UV0 = DecompressVector2(v0.uv);
    float2 UV1 = DecompressVector2(v1.uv);
    float2 UV2 = DecompressVector2(v2.uv);

    // Interpolate all the data
    float3 normalOS   = INTERPOLATE_ATTRIBUTE(normalOS0, normalOS1, normalOS2, barycentricCoordinates);
    float3 tangentOS  = INTERPOLATE_ATTRIBUTE(tangentOS0, tangentOS1, tangentOS2, barycentricCoordinates);
    float2 texCoord0  = INTERPOLATE_ATTRIBUTE(UV0, UV1, UV2, barycentricCoordinates);

    // Compute the world space normal
    float3 normalWS = normalize(mul(instanceVData.localToWorld, normalOS));
    float3 tangentWS = normalize(mul(instanceVData.localToWorld, tangentOS));

    FragInputs outFragInputs;
    ZERO_INITIALIZE(FragInputs, outFragInputs);
    outFragInputs.positionSS = posSS;
    outFragInputs.positionRWS = 0.0;
    outFragInputs.texCoord0 = float4(texCoord0, 0.0, 1.0);
    outFragInputs.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, 1.0);
    //outFragInputs.tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, sign(currentVertex.tangentOS.w));
    outFragInputs.isFrontFace = dot(V, outFragInputs.tangentToWorld[2]) < 0.0f;
    return outFragInputs;
}

void Frag(PackedVaryingsToPS packedInput, out float3 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    
    uint2 pixelCoord = packedInput.vmesh.positionCS.xy;
    // Grab the geometry information
    uint triangleID = LOAD_TEXTURE2D_X(_VBuffer0, pixelCoord).x;
    uint geometryID = LOAD_TEXTURE2D_X(_VBuffer1, pixelCoord).x;

    FragInputs input = EvaluateFragInput(packedInput.vmesh.positionCS, float3(0.0, 0.0, 0.0), geometryID, triangleID);

    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);

    // Build the position input
    float depthValue = LOAD_TEXTURE2D_X(_CameraDepthTexture, input.positionSS.xy);
    int2 tileCoord = (float2)input.positionSS.xy / GetTileSize();
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depthValue, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), tileCoord);
    input.positionRWS = posInput.positionWS;

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;
    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    outColor = lightLoopOutput.diffuseLighting * GetCurrentExposureMultiplier();
}
