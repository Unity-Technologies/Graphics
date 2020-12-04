#ifndef UNIVERSAL_DEPRECATED_INCLUDED
#define UNIVERSAL_DEPRECATED_INCLUDED

// Stereo-related bits
#define SCREENSPACE_TEXTURE         TEXTURE2D_X
#define SCREENSPACE_TEXTURE_FLOAT   TEXTURE2D_X_FLOAT
#define SCREENSPACE_TEXTURE_HALF    TEXTURE2D_X_HALF

// Typo-fixes, re-route to new name for backwards compatiblity (if there are external dependencies).
#define kDieletricSpec kDielectricSpec
#define DirectBDRF     DirectBRDF

// Deprecated: not using consistent naming convention
#if defined(USING_STEREO_MATRICES)
#define unity_StereoMatrixIP unity_StereoMatrixInvP
#define unity_StereoMatrixIVP unity_StereoMatrixInvVP
#endif

// Previously used when rendering with DrawObjectsPass.
// Global object render pass data containing various settings.
// x,y,z are currently unused
// w is used for knowing whether the object is opaque(1) or alpha blended(0)
half4 _DrawObjectPassData;

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
// _AdditionalShadowsIndices was deprecated - To get the first shadow slice index for a light, use GetAdditionalLightShadowParams(lightIndex).w [see Shadows.hlsl]
#define _AdditionalShadowsIndices _AdditionalShadowParams_SSBO
// _AdditionalShadowsBuffer was deprecated - To access a shadow slice's matrix, use _AdditionalLightsWorldToShadow_SSBO[shadowSliceIndex] - To access other shadow parameters, use GetAdditionalLightShadowParams(int lightIndex) [see Shadows.hlsl]
#define _AdditionalShadowsBuffer _AdditionalLightsWorldToShadow_SSBO
#endif

// Deprecated: even when USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA is defined we do not this structure anymore, because worldToShadowMatrix and shadowParams must be stored in arrays of different sizes
// To get the first shadow slice index for a light, use GetAdditionalLightShadowParams(lightIndex).w [see Shadows.hlsl]
// To access other shadow parameters, use GetAdditionalLightShadowParams(int lightIndex)[see Shadows.hlsl]
struct ShadowData
{
    float4x4 worldToShadowMatrix; // per-shadow-slice
    float4 shadowParams;          // per-casting-light
};

// Deprecated: A confusingly named and duplicate function that scales clipspace to unity NDC range. (-w < x(-y) < w --> 0 < xy < w)
// Use GetVertexPositionInputs().positionNDC instead for vertex shader
// Or a similar function in Common.hlsl, ComputeNormalizedDeviceCoordinatesWithZ()
float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

#endif // UNIVERSAL_DEPRECATED_INCLUDED
