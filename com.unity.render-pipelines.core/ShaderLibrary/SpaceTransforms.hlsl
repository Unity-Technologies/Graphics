#ifndef UNITY_SPACE_TRANSFORMS_INCLUDED
#define UNITY_SPACE_TRANSFORMS_INCLUDED

// Return the PreTranslated ObjectToWorld Matrix (i.e matrix with _WorldSpaceCameraPos apply to it if we use camera relative rendering)
float4x4 GetObjectToWorldMatrix()
{
    return UNITY_MATRIX_M;
}

float4x4 GetWorldToObjectMatrix()
{
    return UNITY_MATRIX_I_M;
}

float4x4 GetWorldToViewMatrix()
{
    return UNITY_MATRIX_V;
}

// Transform to homogenous clip space
float4x4 GetWorldToHClipMatrix()
{
    return UNITY_MATRIX_VP;
}

// Transform to homogenous clip space
float4x4 GetViewToHClipMatrix()
{
    return UNITY_MATRIX_P;
}

// This function always return the absolute position in WS
float3 GetAbsolutePositionWS(float3 positionRWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionRWS += _WorldSpaceCameraPos;
#endif
    return positionRWS;
}

// This function return the camera relative position in WS
float3 GetCameraRelativePositionWS(float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionWS -= _WorldSpaceCameraPos;
#endif
    return positionWS;
}

real GetOddNegativeScale()
{
    return unity_WorldTransformParams.w;
}

float3 TransformObjectToWorld(float3 positionOS)
{
    return mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
}

float3 TransformWorldToObject(float3 positionWS)
{
    return mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
}

float3 TransformWorldToView(float3 positionWS)
{
    return mul(GetWorldToViewMatrix(), float4(positionWS, 1.0)).xyz;
}

// Transforms position from object space to homogenous space
float4 TransformObjectToHClip(float3 positionOS)
{
    // More efficient than computing M*VP matrix product
    return mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)));
}

// Tranforms position from world space to homogenous space
float4 TransformWorldToHClip(float3 positionWS)
{
    return mul(GetWorldToHClipMatrix(), float4(positionWS, 1.0));
}

// Tranforms position from view space to homogenous space
float4 TransformWViewToHClip(float3 positionVS)
{
    return mul(GetViewToHClipMatrix(), float4(positionVS, 1.0));
}

real3 TransformObjectToWorldDir(real3 dirOS)
{
    // Normalize to support uniform scaling
    return normalize(mul((real3x3)GetObjectToWorldMatrix(), dirOS));
}

real3 TransformWorldToObjectDir(real3 dirWS)
{
    // Normalize to support uniform scaling
    return normalize(mul((real3x3)GetWorldToObjectMatrix(), dirWS));
}

real3 TransformWorldToViewDir(real3 dirWS)
{
    return mul((real3x3)GetWorldToViewMatrix(), dirWS).xyz;
}

// Tranforms vector from world space to homogenous space
real3 TransformWorldToHClipDir(real3 directionWS)
{
    return mul((real3x3)GetWorldToHClipMatrix(), directionWS);
}

// Transforms normal from object to world space
float3 TransformObjectToWorldNormal(float3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return TransformObjectToWorldDir(normalOS);
#else
    // Normal need to be multiply by inverse transpose
    return normalize(mul(normalOS, (float3x3)GetWorldToObjectMatrix()));
#endif
}

real3x3 CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)
{
    // For odd-negative scale transforms we need to flip the sign
    real sgn = flipSign * GetOddNegativeScale();
    real3 bitangent = cross(normal, tangent) * sgn;

    return real3x3(tangent, bitangent, normal);
}

real3 TransformTangentToWorld(real3 dirTS, real3x3 tangentToWorld)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    return mul(dirTS, tangentToWorld);
}

real3 TransformWorldToTangent(real3 dirWS, real3x3 tangentToWorld)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    // Use transpose transformation to go from "tangent to world" to "world to tangent" as the matrix is orthogonal
    return mul(tangentToWorld, dirWS);
}

real3 TransformTangentToObject(real3 dirTS, real3x3 tangentToWorld)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    real3 normalWS = mul(dirTS, tangentToWorld);
    return mul((real3x3)GetWorldToObjectMatrix(), normalWS);
}

real3 TransformObjectToTangent(real3 dirOS, real3x3 tangentToWorld)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    // Use transpose transformation to go from "tangent to world" to "world to tangent" as the matrix is orthogonal
    return mul(tangentToWorld, TransformObjectToWorldDir(dirOS));
}

#endif
