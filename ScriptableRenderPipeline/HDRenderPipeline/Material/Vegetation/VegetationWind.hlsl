#include "VegetationProperties.hlsl"

void ApplyVegetationWind( inout float3 positionWS, 
                          float3       positionOS,
                          float3       normalW,
                          float2       texcoord,
                          float4       weights,
                          float4       time)
{
    float3 normalWS = TransformObjectToWorldNormal(normalW);
    float3 windDir = normalize(_VegWindDirection.xyz);
    float windSpeedWS = _VegWindSpeed * time.y * 3.0;

    float vertexToPivotDist = distance(positionOS, float3(0.0, 0.0, 0.0));
    float distOffset = vertexToPivotDist / 128.0;

    float3 posToPivot = positionWS.xyz - _VegPivot.xyz;
    float3 shiftPivot = _VegPivot.xyz + normalize(posToPivot) * vertexToPivotDist * (1.0 + weights.x);
    float3 posToShiftPivot = positionWS.xyz - shiftPivot.xyz;

    float3 shiftPivotDistOffset = distance(positionOS, shiftPivot) / 64.0;
    float3 axis = cross(posToShiftPivot, windDir);

    float4 noiseUV = time.yyyy * 0.05f;
    //float4 noise = tex2Dlod(sampler_VegNoise, float4(noiseUV.x, 0.5, 0.0, 0.0));
    //#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) textureName.SampleLevel(samplerName, coord2, lod)
    float4 noise = SAMPLE_TEXTURE2D_LOD(_VegNoise, sampler_VegNoise, float2(noiseUV.x, 0.5), 0.0);

    half rad = radians(sin(windSpeedWS + (distOffset + shiftPivotDistOffset) / _VegStiffness) * distOffset * _VegWindIntensity * noise.x);  
    half radAssist = radians(sin(windSpeedWS) * distOffset * _VegWindIntensity * noise.x);

    float3x3 rP = float3x3(axis.x * axis.x * (1.0f - cos(rad)) + cos(rad), axis.x * axis.y * (1.0f - cos(rad)) + axis.z * sin(rad), axis.x * axis.z * (1.0f - cos(rad)) - axis.y * sin(rad), 
                           axis.x * axis.y * (1.0f - cos(rad)) - axis.z * sin(rad), axis.y * axis.y * (1.0f - cos(rad)) + cos(rad), axis.y * axis.z * (1.0f - cos(rad)) + axis.x * sin(rad), 
                           axis.x * axis.z * (1.0f - cos(rad)) + axis.y * sin(rad), axis.y * axis.z * (1.0f - cos(rad)) - axis.x * sin(rad), axis.z * axis.z * (1.0f - cos(rad)) + cos(rad));

    float3x3 rPAssist = float3x3(_VegAssistantDirectional.x * _VegAssistantDirectional.x * (1.0f - cos(radAssist)) + cos(radAssist), _VegAssistantDirectional.x * _VegAssistantDirectional.y * (1.0f - cos(radAssist)) + _VegAssistantDirectional.z * sin(radAssist), _VegAssistantDirectional.x * _VegAssistantDirectional.z * (1.0f - cos(radAssist)) - _VegAssistantDirectional.y * sin(radAssist), 
                                 _VegAssistantDirectional.x * _VegAssistantDirectional.y * (1.0f - cos(radAssist)) - _VegAssistantDirectional.z * sin(radAssist), _VegAssistantDirectional.y * _VegAssistantDirectional.y * (1.0f - cos(radAssist)) + cos(radAssist), _VegAssistantDirectional.y * _VegAssistantDirectional.z * (1.0f - cos(radAssist)) + _VegAssistantDirectional.x * sin(radAssist), 
                                 _VegAssistantDirectional.x * _VegAssistantDirectional.z * (1.0f - cos(radAssist)) + _VegAssistantDirectional.y * sin(radAssist), _VegAssistantDirectional.y * _VegAssistantDirectional.z * (1.0f - cos(radAssist)) - _VegAssistantDirectional.x * sin(radAssist), _VegAssistantDirectional.z * _VegAssistantDirectional.z * (1.0f - cos(radAssist)) + cos(radAssist));

    float detailVariation = weights.g * _VegDetailVariation;

    float3 leafShakeDeform = sin((positionWS / _VegLeafShakeScale + windSpeedWS * 5.0 * _VegLeafShakeSpeed) + detailVariation);
    leafShakeDeform = clamp(leafShakeDeform, -1.0, 1.0);
    leafShakeDeform *= _VegWindIntensity * noise  * (1.0 + weights.g);  
    float4 perLeafMask = SAMPLE_TEXTURE2D_LOD(_VegWindMask, sampler_VegWindMask, float2(texcoord.x, texcoord.y), 0.0);
    leafShakeDeform *= (normalWS * perLeafMask.r + (-normalWS) * perLeafMask.g) * _VegLeafShakePower;

    float3 perLeafBending = sin((positionWS / _VegPerLeafBendScale + windSpeedWS * 3.0 * _VegPerLeafBendSpeed) * (windDir+_VegAssistantDirectional) + detailVariation + shiftPivotDistOffset / 0.5);
    perLeafBending *= _VegWindIntensity * noise;
    perLeafBending = float3(0.0, perLeafBending.y, 0.0f);
    perLeafBending *= weights.b * perLeafMask.b * (1.0 + weights.g) * _VegPerLeafBendPower;  

    float3 rPAssistpos = mul(rPAssist, positionOS); 
    float3 pos = mul(rP, rPAssistpos) + perLeafBending + leafShakeDeform;
    positionWS = mul(UNITY_MATRIX_M, float4(pos, 1.0)).xyz;
}