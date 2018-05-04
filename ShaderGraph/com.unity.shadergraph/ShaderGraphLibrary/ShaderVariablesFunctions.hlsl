#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED

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

float GetOddNegativeScale()
{
    return unity_WorldTransformParams.w;
}

float3 TransformWorldToView(float3 positionWS)
{
    return mul(GetWorldToViewMatrix(), float4(positionWS, 1.0)).xyz;
}

float3 TransformObjectToWorld(float3 positionOS)
{
    return mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
}

float3 TransformWorldToObject(float3 positionWS)
{
    return mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
}

float3 TransformObjectToWorldDir(float3 dirOS)
{
    // Normalize to support uniform scaling
    return normalize(mul((float3x3)GetObjectToWorldMatrix(), dirOS));
}

float3 TransformWorldToObjectDir(float3 dirWS)
{
    // Normalize to support uniform scaling
    return normalize(mul((float3x3)GetWorldToObjectMatrix(), dirWS));
}

// Transforms normal from object to world space
float3 TransformObjectToWorldNormal(float3 normalOS)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
    return UnityObjectToWorldDir(normalOS);
#else
    // Normal need to be multiply by inverse transpose
    // mul(IT_M, norm) => mul(norm, I_M) => {dot(norm, I_M.col0), dot(norm, I_M.col1), dot(norm, I_M.col2)}
    return normalize(mul(normalOS, (float3x3)GetWorldToObjectMatrix()));
#endif
}

// Tranforms position from world space to homogenous space
float4 TransformWorldToHClip(float3 positionWS)
{
    return mul(GetWorldToHClipMatrix(), float4(positionWS, 1.0));
}

float3 GetAbsolutePositionWS(float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionWS += _WorldSpaceCameraPos;
#endif
    return positionWS;
}

float3 GetCameraRelativePositionWS(float3 positionWS)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    positionWS -= _WorldSpaceCameraPos;
#endif
    return positionWS;
}

// Note: '_WorldSpaceCameraPos' is set by the legacy Unity code.
float3 GetPrimaryCameraPosition()
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    return float3(0, 0, 0);
#else
    return _WorldSpaceCameraPos;
#endif
}

// Could be e.g. the position of a primary camera or a shadow-casting light.
float3 GetCurrentViewPosition()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    return GetPrimaryCameraPosition();
#else
    // This is a generic solution.
    // However, for the primary camera, using '_WorldSpaceCameraPos' is better for cache locality,
    // and in case we enable camera-relative rendering, we can statically set the position is 0.
    return UNITY_MATRIX_I_V._14_24_34;
#endif
}

// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    return (unity_OrthoParams.w == 0);
#else
    // TODO: set 'unity_OrthoParams' during the shadow pass.
    return UNITY_MATRIX_P[3][3] == 0;
#endif
}

// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        float3 V = GetCurrentViewPosition() - positionWS;
        return normalize(V);
    }
    else
    {
        // Orthographic
        return -GetViewForwardDir();
    }
}

float3x3 CreateWorldToTangent(float3 normal, float3 tangent, float flipSign)
{
    // For odd-negative scale transforms we need to flip the sign
    float sgn = flipSign * GetOddNegativeScale();
    float3 bitangent = cross(normal, tangent) * sgn;

    return float3x3(tangent, bitangent, normal);
}

float3 TransformTangentToWorld(float3 dirTS, float3x3 worldToTangent)
{
    // Use transpose transformation to go from tangent to world as the matrix is orthogonal
    return mul(dirTS, worldToTangent);
}

float3 TransformWorldToTangent(float3 dirWS, float3x3 worldToTangent)
{
    return mul(worldToTangent, dirWS);
}

float3 TransformTangentToObject(float3 dirTS, float3x3 worldToTangent)
{
    // Use transpose transformation to go from tangent to world as the matrix is orthogonal
    float3 normalWS = mul(dirTS, worldToTangent);
    return mul((float3x3)GetWorldToObjectMatrix(), normalWS);
}

float3 TransformObjectToTangent(float3 dirOS, float3x3 worldToTangent)
{
    return mul(worldToTangent, TransformObjectToWorldDir(dirOS));
}

// UNITY_MATRIX_V defines a right-handed view space with the Z axis pointing towards the viewer.
// This function reverses the direction of the Z axis (so that it points forward),
// making the view space coordinate system left-handed.
void GetLeftHandedViewSpaceMatrices(out float4x4 viewMatrix, out float4x4 projMatrix)
{
    viewMatrix = UNITY_MATRIX_V;
    viewMatrix._31_32_33_34 = -viewMatrix._31_32_33_34;

    projMatrix = UNITY_MATRIX_P;
    projMatrix._13_23_33_43 = -projMatrix._13_23_33_43;
}

#if UNITY_REVERSED_Z
    #if SHADER_API_OPENGL || SHADER_API_GLES || SHADER_API_GLES3
        //GL with reversed z => z clip range is [near, -far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(-(coord), 0)
    #else
        //D3d with reversed Z => z clip range is [near, 0] -> remapping to [0, far]
        //max is required to protect ourselves from near plane not being correct/meaningfull in case of oblique matrices.
        #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) max(((1.0-(coord)/_ProjectionParams.y)*_ProjectionParams.z),0)
    #endif
#elif UNITY_UV_STARTS_AT_TOP
    //D3d without reversed z => z clip range is [0, far] -> nothing to do
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#else
    //Opengl => z clip range is [-near, far] -> should remap in theory but dont do it in practice to save some perf (range is close enough)
    #define UNITY_Z_0_FAR_FROM_CLIPSPACE(coord) (coord)
#endif

half3 SampleSH(half3 normalWS)
{
    // LPPV is not supported in Ligthweight Pipeline
    real4 SHCoefficients[7];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;
    SHCoefficients[3] = unity_SHBr;
    SHCoefficients[4] = unity_SHBg;
    SHCoefficients[5] = unity_SHBb;
    SHCoefficients[6] = unity_SHC;

    return max(half3(0, 0, 0), SampleSH9(SHCoefficients, normalWS));
}

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
