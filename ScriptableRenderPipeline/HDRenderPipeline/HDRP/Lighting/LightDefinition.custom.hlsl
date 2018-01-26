#ifndef LIGHTDEFINITION_CUSTOM_HLSL
#define LIGHTDEFINITION_CUSTOM_HLSL

//-----------------------------------------------------------------------------
// Packing accessors
//-----------------------------------------------------------------------------

#define GETTER_FLOAT3(Type, field)\
float3 Type##_##Get##_##field(Type data)\
{\
    return float3(data.##field##X, data.##field##Y, data.##field##Z);\
}

#define SETTER_FLOAT3(data, field, value)\
 data.##field##X = value.x;\
 data.##field##Y = value.y;\
 data.##field##Z = value.z

GETTER_FLOAT3(EnvLightData, capturePositionWS);
#define EnvLightData_Set_capturePositionWS(data, value) SETTER_FLOAT3(data, capturePositionWS, value)

GETTER_FLOAT3(EnvLightData, proxyPositionWS);
#define EnvLightData_Set_proxyPositionWS(data, value) SETTER_FLOAT3(data, proxyPositionWS, value)

GETTER_FLOAT3(EnvLightData, proxyForward);
#define EnvLightData_Set_proxyForward(data, value) SETTER_FLOAT3(data, proxyForward, value)

GETTER_FLOAT3(EnvLightData, proxyUp);
#define EnvLightData_Set_proxyUp(data, value) SETTER_FLOAT3(data, proxyUp, value)

GETTER_FLOAT3(EnvLightData, proxyRight);
#define EnvLightData_Set_proxyRight(data, value) SETTER_FLOAT3(data, proxyRight, value)

GETTER_FLOAT3(EnvLightData, proxyExtents);
#define EnvLightData_Set_proxyExtents(data, value) SETTER_FLOAT3(data, proxyExtents, value)

GETTER_FLOAT3(EnvLightData, influencePositionWS);
#define EnvLightData_Set_influencePositionWS(data, value) SETTER_FLOAT3(data, influencePositionWS, value)

GETTER_FLOAT3(EnvLightData, influenceForward);
#define EnvLightData_Set_influenceForward(data, value) SETTER_FLOAT3(data, influenceForward, value)

GETTER_FLOAT3(EnvLightData, influenceUp);
#define EnvLightData_Set_influenceUp(data, value) SETTER_FLOAT3(data, influenceUp, value)

GETTER_FLOAT3(EnvLightData, influenceRight);
#define EnvLightData_Set_influenceRight(data, value) SETTER_FLOAT3(data, influenceRight, value)

GETTER_FLOAT3(EnvLightData, influenceExtents);
#define EnvLightData_Set_influenceExtents(data, value) SETTER_FLOAT3(data, influenceExtents, value)

GETTER_FLOAT3(EnvLightData, blendDistancePositive);
#define EnvLightData_Set_blendDistancePositive(data, value) SETTER_FLOAT3(data, blendDistancePositive, value)

GETTER_FLOAT3(EnvLightData, blendDistanceNegative);
#define EnvLightData_Set_blendDistanceNegative(data, value) SETTER_FLOAT3(data, blendDistanceNegative, value)

GETTER_FLOAT3(EnvLightData, blendNormalDistancePositive);
#define EnvLightData_Set_blendNormalDistancePositive(data, value) SETTER_FLOAT3(data, blendNormalDistancePositive, value)

GETTER_FLOAT3(EnvLightData, blendNormalDistanceNegative);
#define EnvLightData_Set_blendNormalDistanceNegative(data, value) SETTER_FLOAT3(data, blendNormalDistanceNegative, value)

GETTER_FLOAT3(EnvLightData, boxSideFadePositive);
#define EnvLightData_Set_boxSideFadePositive(data, value) SETTER_FLOAT3(data, boxSideFadePositive, value)

GETTER_FLOAT3(EnvLightData, boxSideFadeNegative);
#define EnvLightData_Set_boxSideFadeNegative(data, value) SETTER_FLOAT3(data, boxSideFadeNegative, value)

GETTER_FLOAT3(EnvLightData, sampleDirectionDiscardWS);
#define EnvLightData_Set_sampleDirectionDiscardWS(data, value) SETTER_FLOAT3(data, sampleDirectionDiscardWS, value)

#undef GETTER_FLOAT3

#endif