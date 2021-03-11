#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"


//// L1_c is L1 coeff of a given color (R,G,B)
//float3 OptimalLinearDirection(float3 L1_c)
//{
//    return float3(-L1_c.z, -L1_c.x, L1_c.y);
//}
//
//float ComputeMinError()
//{
//
//}
//
//void GetOrthoBasis(float3 N, out float3 T, out float3 B)
//{
//    if (abs(N.x) > abs(N.z))
//    {
//        B = float3(-N.y, N.x, 0.0f);
//    }
//    else
//    {
//        B = float3(0, -N.z, N.y);
//    }
//
//    B = normalize(B);
//    T = cross(B, N);
//}
//
//float ComputeMinError(float a, float b, float c, out float zmin)
//{
//
//}
//
//float4 SHDering(float4 L0L1)
//{
//    float3 dir = OptimalLinearDirection(L0L1.yzw);
//
//    float4 window = 1;
//
//    float dirLen = length(dir);
//
//    float4 outL0L1 = L0L1;
//    if (dirLen > 0)
//    {
//        // Now normalized
//        float3 optLinearDirNormalized = dir / dirLen;
//        float rotMat[9];
//
//        // Generate
//        float3 T, B;
//        GetOrthoBasis(optLinearDirNormalized, T, B);
//
//        float r11 = T.x;
//        float r21 = T.y;
//        float r31 = T.z;
//        float r12 = B.x;
//        float r22 = B.y;
//        float r32 = B.z;
//        float r13 = N.x;
//        float r23 = N.y;
//        float r33 = N.z;
//
//        rotMat[0] = r22;
//        rotMat[1] = -r32;
//        rotMat[2] = r12;
//
//        rotMat[3] = -r23;
//        rotMat[4] = r33;
//        rotMat[5] = -r13;
//
//        rotMat[6] = r21;
//        rotMat[7] = -r31;
//        rotMat[8] = r11;
//
//        outL0L1.x = L0L1.x;
//        float3 L1 = L0L1.yzw;
//        outL0L1.y = dot(L1, float3(rotMat[0], rotMat[1], rotMat[2]));
//        outL0L1.z = dot(L1, float3(rotMat[3], rotMat[4], rotMat[5]));
//        outL0L1.w = dot(L1, float3(rotMat[6], rotMat[7], rotMat[8]));
//    }
//
//    float2 ZH;
//    ZH.x = outL0L1.x; // For L0
//    ZH.y = outL0L1.z; // For L1
//
//    const float dcVal = 0.2820947917738781f;
//    const float lVal = 0.4886025119029199f;
//
//    float a = 0; // for l2
//    float c = 0; // for L2
//
//    // It is essentially a linear eq for L1.
//    float b = lVal * ZH.y;
//
//
//
//
//}
