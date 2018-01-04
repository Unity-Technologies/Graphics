#ifndef UNITY_QUATERNIONMATH_INCLUDED
#define UNITY_QUATERNIONMATH_INCLUDED

// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4

real4 TangentSpaceToQuat(real3 tagent, real3 bitangent, real3 normal)
{
    real4 quat;
    quat.x = normal.y - bitangent.z;
    quat.y = tangent.z - normal.x;
    quat.z = bitangent.x - tangent.y;
    quat.w = 1.0 + tangent.x + bitangent.y + normal.z;

    return normalize(quat);
}

void QuatToTangentSpace(real4 quaterion, out real3 tangent, out real3 bitangent, out real3 normal)
{
    tangent =   real3(1.0, 0.0, 0.0)
                + real3(-2.0, 2.0, 2.0) * quat.y * quat.yxw
                + real3(-2.0, -2.0, 2.0) * quat.z * quaternion.zwx;

    bitangent = real3(0.0, 1.0, 0.0)
                + real3(2.0, -2.0, 2.0) * quat.z * quat.wzy
                + real3(2.0, -2.0, -2.0) * quat.x * quaternion.yxw;

    normal =    real3(0.0, 0.0, 1.0)
                + real3(2.0, 2.0, -2.0) * quat.x * quat.zwx
                + real3(-2.0, 2.0, -2.0) * quat.y * quaternion.wzy;
}

#endif // UNITY_QUATERNIONMATH_INCLUDED
