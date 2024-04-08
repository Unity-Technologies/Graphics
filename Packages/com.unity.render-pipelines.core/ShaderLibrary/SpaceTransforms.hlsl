#ifndef UNITY_SPACE_TRANSFORMS_INCLUDED
#define UNITY_SPACE_TRANSFORMS_INCLUDED

#if SHADER_API_MOBILE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_SWITCH
#pragma warning (disable : 3205) // conversion of larger type to smaller
#endif

// Caution: For HDRP, adding a function in this file requires adding the appropriate #define in PickingSpaceTransforms.hlsl

// Return the PreTranslated ObjectToWorld Matrix (i.e matrix with _WorldSpaceCameraPos apply to it if we use camera relative rendering)
float4x4 GetObjectToWorldMatrix()
{
    return UNITY_MATRIX_M;
}

float4x4 GetWorldToObjectMatrix()
{
    return UNITY_MATRIX_I_M;
}

float4x4 GetPrevObjectToWorldMatrix()
{
    return UNITY_PREV_MATRIX_M;
}

float4x4 GetPrevWorldToObjectMatrix()
{
    return UNITY_PREV_MATRIX_I_M;
}

float4x4 GetWorldToViewMatrix()
{
    return UNITY_MATRIX_V;
}

float4x4 GetViewToWorldMatrix()
{
    return UNITY_MATRIX_I_V;
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
    positionRWS += _WorldSpaceCameraPos.xyz;
#endif
    return positionRWS;
}

// This function return the camera relative position in WS
float3 GetCameraRelativePositionWS(float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionWS -= _WorldSpaceCameraPos.xyz;
#endif
    return positionWS;
}

real GetOddNegativeScale()
{
    // FIXME: We should be able to just return unity_WorldTransformParams.w, but it is not
    // properly set at the moment, when doing ray-tracing; once this has been fixed in cpp,
    // we can revert back to the former implementation.
    return unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0;
}

float3 TransformObjectToWorld(float3 positionOS)
{
    #if defined(SHADER_STAGE_RAY_TRACING)
    return mul(ObjectToWorld3x4(), float4(positionOS, 1.0)).xyz;
    #else
    return mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
    #endif
}

float3 TransformWorldToObject(float3 positionWS)
{
    #if defined(SHADER_STAGE_RAY_TRACING)
    return mul(WorldToObject3x4(), float4(positionWS, 1.0)).xyz;
    #else
    return mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
    #endif
}

float3 TransformWorldToView(float3 positionWS)
{
    return mul(GetWorldToViewMatrix(), float4(positionWS, 1.0)).xyz;
}

float3 TransformViewToWorld(float3 positionVS)
{
    return mul(GetViewToWorldMatrix(), float4(positionVS, 1.0)).xyz;
}

// Transforms position from object space to homogenous space
float4 TransformObjectToHClip(float3 positionOS)
{
    // More efficient than computing M*VP matrix product
    return mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)));
}

// Transforms position from world space to homogenous space
float4 TransformWorldToHClip(float3 positionWS)
{
    return mul(GetWorldToHClipMatrix(), float4(positionWS, 1.0));
}

// Transforms position from view space to homogenous space
float4 TransformWViewToHClip(float3 positionVS)
{
    return mul(GetViewToHClipMatrix(), float4(positionVS, 1.0));
}

// Normalize to support uniform scaling
float3 TransformObjectToWorldDir(float3 dirOS, bool doNormalize = true)
{
    #ifndef SHADER_STAGE_RAY_TRACING
    float3 dirWS = mul((float3x3)GetObjectToWorldMatrix(), dirOS);
    #else
    float3 dirWS = mul((float3x3)ObjectToWorld3x4(), dirOS);
    #endif
    if (doNormalize)
        return SafeNormalize(dirWS);

    return dirWS;
}

// Normalize to support uniform scaling
float3 TransformWorldToObjectDir(float3 dirWS, bool doNormalize = true)
{
    #ifndef SHADER_STAGE_RAY_TRACING
    float3 dirOS = mul((float3x3)GetWorldToObjectMatrix(), dirWS);
    #else
    float3 dirOS = mul((float3x3)WorldToObject3x4(), dirWS);
    #endif
    if (doNormalize)
        return normalize(dirOS);

    return dirOS;
}

// Transforms vector from world space to view space
real3 TransformWorldToViewDir(real3 dirWS, bool doNormalize = false)
{
    float3 dirVS = mul((real3x3)GetWorldToViewMatrix(), dirWS).xyz;
    if (doNormalize)
        return normalize(dirVS);

    return dirVS;
}

// Transforms vector from view space to world space
real3 TransformViewToWorldDir(real3 dirVS, bool doNormalize = false)
{
    float3 dirWS = mul((real3x3)GetViewToWorldMatrix(), dirVS).xyz;
    if (doNormalize)
        return normalize(dirWS);

    return dirWS;
}

// Transforms normal from world space to view space
real3 TransformWorldToViewNormal(real3 normalWS, bool doNormalize = false)
{
    // assuming view matrix is uniformly scaled, we can use direction transform
    return TransformWorldToViewDir(normalWS, doNormalize);
}

// Transforms normal from view space to world space
real3 TransformViewToWorldNormal(real3 normalVS, bool doNormalize = false)
{
    // assuming view matrix is uniformly scaled, we can use direction transform
    return TransformViewToWorldDir(normalVS, doNormalize);
}

// Transforms vector from world space to homogenous space
real3 TransformWorldToHClipDir(real3 directionWS, bool doNormalize = false)
{
    float3 dirHCS = mul((real3x3)GetWorldToHClipMatrix(), directionWS).xyz;
    if (doNormalize)
        return normalize(dirHCS);

    return dirHCS;
}

// Transforms normal from object to world space
float3 TransformObjectToWorldNormal(float3 normalOS, bool doNormalize = true)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return TransformObjectToWorldDir(normalOS, doNormalize);
#else
    // Normal need to be multiply by inverse transpose
    float3 normalWS = mul(normalOS, (float3x3)GetWorldToObjectMatrix());
    if (doNormalize)
        return SafeNormalize(normalWS);

    return normalWS;
#endif
}

// Transforms normal from world to object space
float3 TransformWorldToObjectNormal(float3 normalWS, bool doNormalize = true)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return TransformWorldToObjectDir(normalWS, doNormalize);
#else
    // Normal need to be multiply by inverse transpose
    float3 normalOS = mul(normalWS, (float3x3)GetObjectToWorldMatrix());
    if (doNormalize)
        return SafeNormalize(normalOS);

    return normalOS;
#endif
}

real3x3 CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)
{
    // For odd-negative scale transforms we need to flip the sign
    real sgn = flipSign * GetOddNegativeScale();
    real3 bitangent = cross(normal, tangent) * sgn;

    return real3x3(tangent, bitangent, normal);
}

// this function is intended to work on Normals (handles non-uniform scale)
// tangentToWorld is the matrix representing the transformation of a normal from tangent to world space
real3 TransformTangentToWorld(float3 normalTS, real3x3 tangentToWorld, bool doNormalize = false)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    real3 result = mul(normalTS, tangentToWorld);
    if (doNormalize)
        return SafeNormalize(result);
    return result;
}

// this function is intended to work on Normals (handles non-uniform scale)
// This function does the exact inverse of TransformTangentToWorld() and is
// also decribed within comments in mikktspace.h and it follows implicitly
// from the scalar triple product (google it).
// tangentToWorld is the matrix representing the transformation of a normal from tangent to world space
real3 TransformWorldToTangent(real3 normalWS, real3x3 tangentToWorld, bool doNormalize = true)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    float3 row0 = tangentToWorld[0];
    float3 row1 = tangentToWorld[1];
    float3 row2 = tangentToWorld[2];

    // these are the columns of the inverse matrix but scaled by the determinant
    float3 col0 = cross(row1, row2);
    float3 col1 = cross(row2, row0);
    float3 col2 = cross(row0, row1);

    float determinant = dot(row0, col0);

    // inverse transposed but scaled by determinant
    // Will remove transpose part by using matrix as the first arg in the mul() below
    // this makes it the exact inverse of what TransformTangentToWorld() does.
    real3x3 matTBN_I_T = real3x3(col0, col1, col2);
    real3 result = mul(matTBN_I_T, normalWS);
    if (doNormalize)
    {
        float sgn = determinant < 0.0 ? (-1.0) : 1.0;
        return SafeNormalize(sgn * result);
    }
    else
        return result / determinant;
}

// this function is intended to work on Vectors/Directions
// tangentToWorld is the matrix representing the transformation of a normal from tangent to world space
real3 TransformWorldToTangentDir(real3 dirWS, real3x3 tangentToWorld, bool doNormalize = false)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    real3 result = mul(tangentToWorld, dirWS);
    if (doNormalize)
        return SafeNormalize(result);
    return result;
}

// this function is intended to work on Vectors/Directions
// This function does the exact inverse of TransformWorldToTangentDir()
// tangentToWorld is the matrix representing the transformation of a normal from tangent to world space
real3 TransformTangentToWorldDir(real3 dirWS, real3x3 tangentToWorld, bool doNormalize = false)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    float3 row0 = tangentToWorld[0];
    float3 row1 = tangentToWorld[1];
    float3 row2 = tangentToWorld[2];

    // these are the columns of the inverse matrix but scaled by the determinant
    float3 col0 = cross(row1, row2);
    float3 col1 = cross(row2, row0);
    float3 col2 = cross(row0, row1);

    float determinant = dot(row0, col0);

    // inverse transposed but scaled by determinant
    // Will remove transpose part by using matrix as the second arg in the mul() below
    // this makes it the exact inverse of what TransformWorldToTangentDir() does.
    real3x3 matTBN_I_T = real3x3(col0, col1, col2);
    real3 result = mul(dirWS, matTBN_I_T);
    if (doNormalize)
    {
        float sgn = determinant < 0.0 ? (-1.0) : 1.0;
        return SafeNormalize(sgn * result);
    }
    else
        return result / determinant;
}

// tangentToWorld is the matrix representing the transformation of a normal from tangent to world space
real3 TransformTangentToObject(real3 dirTS, real3x3 tangentToWorld)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly
    real3 normalWS = TransformTangentToWorld(dirTS, tangentToWorld);
    return TransformWorldToObjectNormal(normalWS);
}

// tangentToWorld is the matrix representing the transformation of a normal from tangent to world space
real3 TransformObjectToTangent(real3 dirOS, real3x3 tangentToWorld)
{
    // Note matrix is in row major convention with left multiplication as it is build on the fly

    // don't normalize, as normalWS will be normalized after TransformWorldToTangent
    float3 normalWS = TransformObjectToWorldNormal(dirOS, false);

    // transform from world to tangent
    return TransformWorldToTangent(normalWS, tangentToWorld);
}

#if SHADER_API_MOBILE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_SWITCH
#pragma warning (enable : 3205) // conversion of larger type to smaller
#endif

#endif
