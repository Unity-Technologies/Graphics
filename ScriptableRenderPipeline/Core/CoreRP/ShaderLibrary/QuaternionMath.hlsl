#ifndef UNITY_QUATERNIONMATH_INCLUDED
#define UNITY_QUATERNIONMATH_INCLUDED

// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4

float4 TangentSpaceToQuat(float3 tagent, float3 bitangent, float3 normal)
{
    float4 quat;
    quat.x = normal.y - bitangent.z;
    quat.y = tangent.z - normal.x;
    quat.z = bitangent.x - tangent.y;
    quat.w = 1.0 + tangent.x + bitangent.y + normal.z;

    return normalize(quat);
}

void QuatToTangentSpace(float4 quaterion, out float3 tangent, out float3 bitangent, out float3 normal)
{
    tangent =   float3(1.0, 0.0, 0.0)
                + float3(-2.0, 2.0, 2.0) * quat.y * quat.yxw
                + float3(-2.0, -2.0, 2.0) * quat.z * quaternion.zwx;

    bitangent = float3(0.0, 1.0, 0.0)
                + float3(2.0, -2.0, 2.0) * quat.z * quat.wzy
                + float3(2.0, -2.0, -2.0) * quat.x * quaternion.yxw;

    normal =    float3(0.0, 0.0, 1.0)
                + float3(2.0, 2.0, -2.0) * quat.x * quat.zwx
                + float3(-2.0, 2.0, -2.0) * quat.y * quaternion.wzy;
}

#endif // UNITY_QUATERNIONMATH_INCLUDED
