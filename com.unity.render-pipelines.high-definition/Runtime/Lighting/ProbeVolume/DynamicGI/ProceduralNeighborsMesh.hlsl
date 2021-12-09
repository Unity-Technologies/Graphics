#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbeVolumeDynamicGI.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbePropagationGlobals.hlsl"

StructuredBuffer<PackedNeighborHit> _ProbeVolumeDebugNeighborHits;
int _ProbeVolumeDebugNeighborHitCount;
float _ProbeVolumeDebugNeighborQuadScale;
int _ProbeVolumeDebugNeighborMode;

float _ProbeVolumeDGIMaxNeighborDistance;
uint _ProbeVolumeDGIResolutionXY;
uint _ProbeVolumeDGIResolutionX;
float3 _ProbeVolumeDGIResolutionInverse;

float3 _ProbeVolumeDGIBoundsRight;
float3 _ProbeVolumeDGIBoundsUp;
float3 _ProbeVolumeDGIBoundsExtents;
float3 _ProbeVolumeDGIBoundsCenter;


void OrhoNormalAxis(float3 n, out float3 b1, out float3 b2)
{
    float a = 1.0 / (1.0 + n.z);
    float b = -n.x * n.y * a;
    bool isNegativeZFrame = n.z < -0.9999999;
    b1 = isNegativeZFrame ? float3(0.0, -1.0, 0.0) : float3(1.0 - n.x * n.x * a, b, -n.x);
    b2 = isNegativeZFrame ? float3(-1.0, 0.0, 0.0) : float3(b, 1.0 - n.y * n.y * a, -n.y);
}

float3 ProbeIndexToProbeCoordinates(uint probeIndex)
{
    uint probeZ = probeIndex / _ProbeVolumeDGIResolutionXY;
    probeIndex -= probeZ * _ProbeVolumeDGIResolutionXY;

    uint probeY = probeIndex / _ProbeVolumeDGIResolutionX;
    uint probeX = probeIndex % _ProbeVolumeDGIResolutionX;

    return float3(probeX, probeY, probeZ) + 0.5;
}

float3 ProbeCoordinatesToWorldPosition(float3 probeCoordinates)
{
    float3x3 probeVolumeLtw = float3x3(_ProbeVolumeDGIBoundsRight, _ProbeVolumeDGIBoundsUp, cross(_ProbeVolumeDGIBoundsRight, _ProbeVolumeDGIBoundsUp));
    float3 localPosition = ((probeCoordinates * _ProbeVolumeDGIResolutionInverse) * 2.0 - 1.0) * _ProbeVolumeDGIBoundsExtents;
    return mul(localPosition, probeVolumeLtw) + _ProbeVolumeDGIBoundsCenter;
}


VaryingsType VertMeshProcedural(uint vertexID, uint instanceID)
{
    VaryingsType output;

    const float2 QuadOffsetsScale[6] = {
        float2(1, 1),
        float2(1, -1),
        float2(-1, 1),
        float2(-1, 1),
        float2(1, -1),
        float2(-1, -1)
    };


    uint axisLookupIndex = clamp(instanceID, 0, _ProbeVolumeDebugNeighborHitCount - 1);
    PackedNeighborHit neighborData = _ProbeVolumeDebugNeighborHits[axisLookupIndex];
    uint vertIndex = vertexID % 6;

    uint probeIndex, axisIndex;
    float probeValidity;
    UnpackIndicesAndValidity(neighborData.indexValidity, probeIndex, axisIndex, probeValidity);

    float3 probeCoordinate = ProbeIndexToProbeCoordinates(probeIndex);
    float3 worldPosition =  ProbeCoordinatesToWorldPosition(probeCoordinate);

    float3 normal = UnpackNormal(neighborData.normalAxis);
    float4 albedoDistance = UnpackAlbedoAndDistance(neighborData.albedoDistance, _ProbeVolumeDGIMaxNeighborDistance);
    float3 axis = UnpackAxis(neighborData.normalAxis).xyz;

    float3x3 probeVolumeLtw = float3x3(_ProbeVolumeDGIBoundsRight, _ProbeVolumeDGIBoundsUp, cross(_ProbeVolumeDGIBoundsRight, _ProbeVolumeDGIBoundsUp));
    axis = mul(axis, probeVolumeLtw);
    normal = mul(normal, probeVolumeLtw);

    float3 right = 0, up = 0;
    OrhoNormalAxis(normal, up, right);
    right = normalize(right);
    up = normalize(cross(right, normal));

    float2 density = _ProbeVolumeDebugNeighborQuadScale * 0.1; // use max density xz, & density y
    float2 quadOffset = QuadOffsetsScale[vertIndex];
    quadOffset.xy *= density;// * probeValidity; // scale down size if less valid

    float3 positionRWS = worldPosition
                            + axis.xyz * albedoDistance.w * 0.95 +
                            + right * quadOffset.x + up * quadOffset.y;

    float3 normalWS = normal;

    positionRWS = TransformObjectToWorld(positionRWS);
    normalWS = TransformObjectToWorldNormal(normal);

    float validity = pow(1.0 - probeValidity, 8.0);

    float3 color;
    switch (_ProbeVolumeDebugNeighborMode)
    {
        case 0:
            color = albedoDistance.xyz;
            break;
        case 1:
            color = UnpackEmission(neighborData.emission);
            break;
    }
    
#ifdef VARYINGS_NEED_POSITION_WS
    output.vmesh.positionRWS = positionRWS;
#endif

output.vmesh.positionCS = TransformWorldToHClip(positionRWS);

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    output.vmesh.normalWS = normalWS;
    output.vmesh.tangentWS = normalWS.xyzz;
#endif

#if defined(VARYINGS_NEED_TEXCOORD0) || defined(VARYINGS_DS_NEED_TEXCOORD0)
    output.vmesh.texCoord0 = 0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD1) || defined(VARYINGS_DS_NEED_TEXCOORD1)
    output.vmesh.texCoord1 = 0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD2) || defined(VARYINGS_DS_NEED_TEXCOORD2)
    output.vmesh.texCoord2 = 0;
#endif
#if defined(VARYINGS_NEED_TEXCOORD3) || defined(VARYINGS_DS_NEED_TEXCOORD3)
    output.vmesh.texCoord3 = 0;
#endif
#if defined(VARYINGS_NEED_COLOR) || defined(VARYINGS_DS_NEED_COLOR)
    output.vmesh.color = float4(color, 1);
    //output.vmesh.color = lerp(float4(1, 0, 0, 1), float4(1, 1, 1, 1), validity);
#endif

#if defined(VARYINGS_NEED_PASS)
    output.vpass.positionCS = output.vmesh.positionCS;
    output.vpass.previousPositionCS = mul(UNITY_MATRIX_PREV_VP, float4(positionRWS, 1.0));
#endif

#if UNITY_ANY_INSTANCING_ENABLED
    output.vmesh.instanceID = instanceID;
#endif

    return output;
}


PackedVaryingsType VertProcedural(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
    VaryingsType varyingsType;
    varyingsType = VertMeshProcedural(vertexID, instanceID);


    return PackVaryingsType(varyingsType);
}
