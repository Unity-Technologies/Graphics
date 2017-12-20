#ifndef UNITY_QUATERNIONMATH_INCLUDED
#define UNITY_QUATERNIONMATH_INCLUDED

// Ref: https://cedec.cesa.or.jp/2015/session/ENG/14698.html The Rendering Materials of Far Cry 4

REAL4 TangentSpaceToQuat(REAL3 tagent, REAL3 bitangent, REAL3 normal)
{
    REAL4 quat;
    quat.x = normal.y - bitangent.z;
    quat.y = tangent.z - normal.x;
    quat.z = bitangent.x - tangent.y;
    quat.w = 1.0 + tangent.x + bitangent.y + normal.z;

    return normalize(quat);
}

void QuatToTangentSpace(REAL4 quaterion, out REAL3 tangent, out REAL3 bitangent, out REAL3 normal)
{
    tangent =   REAL3(1.0, 0.0, 0.0)
                + REAL3(-2.0, 2.0, 2.0) * quat.y * quat.yxw
                + REAL3(-2.0, -2.0, 2.0) * quat.z * quaternion.zwx;

    bitangent = REAL3(0.0, 1.0, 0.0)
                + REAL3(2.0, -2.0, 2.0) * quat.z * quat.wzy
                + REAL3(2.0, -2.0, -2.0) * quat.x * quaternion.yxw;

    normal =    REAL3(0.0, 0.0, 1.0)
                + REAL3(2.0, 2.0, -2.0) * quat.x * quat.zwx
                + REAL3(-2.0, 2.0, -2.0) * quat.y * quaternion.wzy;
}

#endif // UNITY_QUATERNIONMATH_INCLUDED
