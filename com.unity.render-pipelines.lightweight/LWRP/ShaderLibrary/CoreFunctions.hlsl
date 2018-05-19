#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED

float3 GetCameraPositionWS()
{
    return _WorldSpaceCameraPos;
}

float4x4 GetWorldToViewMatrix()
{
    return UNITY_MATRIX_V;
}

float4x4 GetObjectToWorldMatrix()
{
    return UNITY_MATRIX_M;
}

float4x4 GetWorldToObjectMatrix()
{
    return UNITY_MATRIX_I_M;
}

// Transform to homogenous clip space
float4x4 GetWorldToHClipMatrix()
{
    return UNITY_MATRIX_VP;
}

real GetOddNegativeScale()
{
    return unity_WorldTransformParams.w;
}

float3 TransformWorldToView(float3 positionWS)
{
    return mul(GetWorldToViewMatrix(), real4(positionWS, 1.0)).xyz;
}

float3 TransformObjectToWorld(float3 positionOS)
{
    return mul(GetObjectToWorldMatrix(), real4(positionOS, 1.0)).xyz;
}

float3 TransformWorldToObject(float3 positionWS)
{
    return mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
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

// Transforms normal from object to world space
real3 TransformObjectToWorldNormal(real3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return UnityObjectToWorldDir(normalOS);
#else
    // Normal need to be multiply by inverse transpose
    // mul(IT_M, norm) => mul(norm, I_M) => {dot(norm, I_M.col0), dot(norm, I_M.col1), dot(norm, I_M.col2)}
    return normalize(mul(normalOS, (real3x3)GetWorldToObjectMatrix()));
#endif
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

real3x3 CreateWorldToTangent(real3 normal, real3 tangent, real flipSign)
{
    // For odd-negative scale transforms we need to flip the sign
    real sgn = flipSign * GetOddNegativeScale();
    real3 bitangent = cross(normal, tangent) * sgn;

    return real3x3(tangent, bitangent, normal);
}

real3 TransformTangentToWorld(real3 dirTS, real3x3 worldToTangent)
{
    // Use transpose transformation to go from tangent to world as the matrix is orthogonal
    return mul(dirTS, worldToTangent);
}

real3 TransformWorldToTangent(real3 dirWS, real3x3 worldToTangent)
{
    return mul(worldToTangent, dirWS);
}

real3 TransformTangentToObject(real3 dirTS, real3x3 worldToTangent)
{
    // Use transpose transformation to go from tangent to world as the matrix is orthogonal
    real3 normalWS = mul(dirTS, worldToTangent);
    return mul((real3x3)GetWorldToObjectMatrix(), normalWS);
}

real3 TransformObjectToTangent(real3 dirOS, real3x3 worldToTangent)
{
    return mul(worldToTangent, TransformObjectToWorldDir(dirOS));
}

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
