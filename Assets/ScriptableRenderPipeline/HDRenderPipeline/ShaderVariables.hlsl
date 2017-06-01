// UNITY_SHADER_NO_UPGRADE

#ifndef UNITY_SHADER_VARIABLES_INCLUDED
#define UNITY_SHADER_VARIABLES_INCLUDED

// CAUTION:
// Currently the shaders compiler always include regualr Unity shaderVariables, so I get a conflict here were UNITY_SHADER_VARIABLES_INCLUDED is already define, this need to be fixed.
// As I haven't change the variables name yet, I simply don't define anything, and I put the transform function at the end of the file outside the guard header.
// This need to be fixed.

#define UNITY_MATRIX_M unity_ObjectToWorld

// These are updated per eye in VR
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_MATRIX_VP unity_MatrixVP

#ifdef UNITY_SINGLE_PASS_STEREO
    #define UNITY_MATRIX_MVP mul(unity_MatrixVP, unity_ObjectToWorld)
#else
    #define UNITY_MATRIX_MVP glstate_matrix_mvp
#endif

// These use the camera center position in VR
#define UNITY_MATRIX_MV glstate_matrix_modelview0
#define UNITY_MATRIX_T_MV glstate_matrix_transpose_modelview0
#define UNITY_MATRIX_IT_MV glstate_matrix_invtrans_modelview0


// ----------------------------------------------------------------------------


CBUFFER_START(UnityPerCamera)
// Time (t = time since current level load) values from Unity
float4 _Time; // (t/20, t, t*2, t*3)
float4 _SinTime; // sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime; // cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 unity_DeltaTime; // dt, 1/dt, smoothdt, 1/smoothdt

#if !defined(USING_STEREO_MATRICES)
float3 _WorldSpaceCameraPos;
#endif

// x = 1 or -1 (-1 if projection is flipped)
// y = near plane
// z = far plane
// w = 1/far plane
float4 _ProjectionParams;

// x = width
// y = height
// z = 1 + 1.0/width
// w = 1 + 1.0/height
float4 _ScreenParams;

// Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
// x = 1-far/near
// y = far/near
// z = x/far
// w = y/far
float4 _ZBufferParams;

// x = orthographic camera's width
// y = orthographic camera's height
// z = unused
// w = 1.0 if camera is ortho, 0.0 if perspective
float4 unity_OrthoParams;
CBUFFER_END


CBUFFER_START(UnityPerCameraRare)
float4 unity_CameraWorldClipPlanes[6];

#if !defined(USING_STEREO_MATRICES)
// Projection matrices of the camera. Note that this might be different from projection matrix
// that is set right now, e.g. while rendering shadows the matrices below are still the projection
// of original camera.
float4x4 unity_CameraProjection;
float4x4 unity_CameraInvProjection;
float4x4 unity_WorldToCamera;
float4x4 unity_CameraToWorld;
#endif
CBUFFER_END

// ----------------------------------------------------------------------------

CBUFFER_START(UnityPerDraw : register(b0))
#ifdef UNITY_USE_PREMULTIPLIED_MATRICES
    float4x4 glstate_matrix_mvp;
    float4x4 glstate_matrix_modelview0;
    float4x4 glstate_matrix_invtrans_modelview0;
#endif

    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4 unity_LODFade; // x is the fade value ranging within [0,1]. y is x quantized into 16 levels
    float4 unity_WorldTransformParams; // w is usually 1.0, or -1.0 for odd-negative scale transforms

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;

    // SH lighting environment
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

    // x = Disabled(0)/Enabled(1)
    // y = Computation are done in global space(0) or local space(1)
    // z = Texel size on U texture coordinate
    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float3 unity_ProbeVolumeSizeInv;
    float3 unity_ProbeVolumeMin;

CBUFFER_END

#if defined(USING_STEREO_MATRICES)
CBUFFER_START(UnityStereoGlobals)
float4x4 unity_StereoMatrixP[2];
float4x4 unity_StereoMatrixV[2];
float4x4 unity_StereoMatrixInvV[2];
float4x4 unity_StereoMatrixVP[2];

float4x4 unity_StereoCameraProjection[2];
float4x4 unity_StereoCameraInvProjection[2];
float4x4 unity_StereoWorldToCamera[2];
float4x4 unity_StereoCameraToWorld[2];

float3 unity_StereoWorldSpaceCameraPos[2];
float4 unity_StereoScaleOffset[2];
CBUFFER_END

#ifdef UNITY_SUPPORT_MULTIVIEW
#define unity_StereoEyeIndex UNITY_VIEWID
UNITY_DECLARE_MULTIVIEW(2);
#else
CBUFFER_START(UnityStereoEyeIndex)
int unity_StereoEyeIndex;
CBUFFER_END
#endif

#endif

CBUFFER_START(UnityPerDrawRare)
float4x4 glstate_matrix_transpose_modelview0;
CBUFFER_END


// ----------------------------------------------------------------------------

CBUFFER_START(UnityPerFrame)

float4 glstate_lightmodel_ambient;
float4 unity_AmbientSky;
float4 unity_AmbientEquator;
float4 unity_AmbientGround;
float4 unity_IndirectSpecColor;

#if !defined(USING_STEREO_MATRICES)
float4x4 glstate_matrix_projection;
float4x4 unity_MatrixV;
float4x4 unity_MatrixInvV;
float4x4 unity_MatrixVP;
int unity_StereoEyeIndex;
#endif

float4 unity_ShadowColor;

CBUFFER_END

TEXTURE2D_FLOAT(_MainDepthTexture);
SAMPLER2D(sampler_MainDepthTexture);

// Main lightmap
TEXTURE2D(unity_Lightmap);
SAMPLER2D(samplerunity_Lightmap);
// Dual or directional lightmap (always used with unity_Lightmap, so can share sampler)
TEXTURE2D(unity_LightmapInd);

// Dynamic GI lightmap
TEXTURE2D(unity_DynamicLightmap);
SAMPLER2D(samplerunity_DynamicLightmap);

TEXTURE2D(unity_DynamicDirectionality);

// TODO: Change code here so probe volume use only one transform instead of all this parameters!
TEXTURE3D_FLOAT(unity_ProbeVolumeSH);
SAMPLER3D(samplerunity_ProbeVolumeSH);

CBUFFER_START(UnityVelocityPass)
    float4x4 _NonJitteredVP;
    float4x4 _PreviousVP;
    float4x4 _PreviousM;
    bool _HasLastPositionData;
    bool _ForceNoMotion;
    float _MotionVectorDepthBias;
CBUFFER_END

// ----------------------------------------------------------------------------

// TODO: move this to constant buffer by Pass
float4   _ScreenSize;
float4x4 _ViewProjMatrix; // Looks like using UNITY_MATRIX_VP in pixel shader doesn't work ??? need to setup my own...
float4x4 _InvViewProjMatrix;
float4x4 _InvProjMatrix;
float4   _InvProjParam;

float4x4 GetWorldToViewMatrix()
{
    return UNITY_MATRIX_V;
}

float4x4 GetObjectToWorldMatrix()
{
    return unity_ObjectToWorld;
}

float4x4 GetWorldToObjectMatrix()
{
    return unity_WorldToObject;
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

float3 GetCurrentCameraPosition()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_DEPTH_ONLY)
    return _WorldSpaceCameraPos;
#else
    // TEMP: this is rather expensive. Then again, we need '_WorldSpaceCameraPos'
    // to represent the position of the primary (scene view) camera in order to
    // have identical tessellation levels for both the scene view and shadow views.
    // Otherwise, depth comparisons become meaningless!
    float4x4 trViewMat = transpose(GetWorldToViewMatrix());
    float3   rotCamPos = trViewMat[3].xyz;
   return mul((float3x3)trViewMat, -rotCamPos);
#endif
}

// Returns the forward direction of the current camera in the world space.
float3 GetCameraForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}

// Returns 'true' if the current camera performs a perspective projection.
bool IsPerspectiveCamera()
{
#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_DEPTH_ONLY)
    return (unity_OrthoParams.w == 0);
#else
    // TODO: set 'unity_OrthoParams' during the shadow pass.
    return (GetWorldToHClipMatrix()[3].x != 0 ||
            GetWorldToHClipMatrix()[3].y != 0 ||
            GetWorldToHClipMatrix()[3].z != 0 ||
            GetWorldToHClipMatrix()[3].w != 1);
#endif
}

// Computes the world space view direction (pointing towards the camera).
float3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    if (IsPerspectiveCamera())
    {
        // Perspective
        float3 V = GetCurrentCameraPosition() - positionWS;
        return normalize(V);
    }
    else
    {
        // Orthographic
        return -GetCameraForwardDir();
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
    return mul((float3x3)unity_WorldToObject, normalWS);
}

float3 TransformObjectToTangent(float3 dirOS, float3x3 worldToTangent)
{
    return mul(worldToTangent, TransformObjectToWorldDir(dirOS));
}
#endif // UNITY_SHADER_VARIABLES_INCLUDED
