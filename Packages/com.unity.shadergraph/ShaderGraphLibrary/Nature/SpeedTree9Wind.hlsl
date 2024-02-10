// Unity built-in shader source. Copyright (c) 2024 Unity Technologies. MIT license (see license.txt)

#ifndef SPEEDTREE_WIND_9_INCLUDED
#define SPEEDTREE_WIND_9_INCLUDED

#define SPEEDTREE_VERSION_9
#include "SpeedTreeCommon.hlsl"

//
// DATA DEFINITIONS
//
struct WindBranchState // 8 floats | 32B
{
    float3 m_vNoisePosTurbulence;
    float m_fIndependence;
    float m_fBend;
    float m_fOscillation;
    float m_fTurbulence;
    float m_fFlexibility;
};
struct WindRippleState // 8 floats | 32B
{
    float3 m_vNoisePosTurbulence;
    float m_fIndependence;
    float m_fPlanar;
    float m_fDirectional;
    float m_fFlexibility;
    float m_fShimmer;
};
struct CBufferSpeedTree9 // 44 floats | 176B
{
    float3 m_vWindDirection;
    float  m_fWindStrength;

    float3 m_vTreeExtents;
    float  m_fSharedHeightStart;

    float m_fBranch1StretchLimit;
    float m_fBranch2StretchLimit;
    float m_fWindIndependence;
    float pad1;

    WindBranchState m_sShared;
    WindBranchState m_sBranch1;
    WindBranchState m_sBranch2;
    WindRippleState m_sRipple;
};

CBUFFER_START(SpeedTreeWind)
float4 _ST_WindVector;
float4 _ST_TreeExtents_SharedHeightStart;
float4 _ST_BranchStretchLimits;
float4 _ST_Shared_NoisePosTurbulence_Independence;
float4 _ST_Shared_Bend_Oscillation_Turbulence_Flexibility;
float4 _ST_Branch1_NoisePosTurbulence_Independence;
float4 _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility;
float4 _ST_Branch2_NoisePosTurbulence_Independence;
float4 _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility;
float4 _ST_Ripple_NoisePosTurbulence_Independence;
float4 _ST_Ripple_Planar_Directional_Flexibility_Shimmer;
CBUFFER_END

CBUFFER_START(SpeedTreeWindHistory)
float4 _ST_HistoryWindVector;
float4 _ST_HistoryTreeExtents_SharedHeightStart;
float4 _ST_HistoryBranchStretchLimits;
float4 _ST_HistoryShared_NoisePosTurbulence_Independence;
float4 _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility;
float4 _ST_HistoryBranch1_NoisePosTurbulence_Independence;
float4 _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility;
float4 _ST_HistoryBranch2_NoisePosTurbulence_Independence;
float4 _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility;
float4 _ST_HistoryRipple_NoisePosTurbulence_Independence;
float4 _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer;
CBUFFER_END

#ifdef UNITY_DOTS_INSTANCING_ENABLED

#define DOTS_ST_WindVector DOTS_ST_WindParam0
#define DOTS_ST_TreeExtents_SharedHeightStart DOTS_ST_WindParam1
#define DOTS_ST_BranchStretchLimits DOTS_ST_WindParam2
#define DOTS_ST_Shared_NoisePosTurbulence_Independence DOTS_ST_WindParam3
#define DOTS_ST_Shared_Bend_Oscillation_Turbulence_Flexibility DOTS_ST_WindParam4
#define DOTS_ST_Branch1_NoisePosTurbulence_Independence DOTS_ST_WindParam5
#define DOTS_ST_Branch1_Bend_Oscillation_Turbulence_Flexibility DOTS_ST_WindParam6
#define DOTS_ST_Branch2_NoisePosTurbulence_Independence DOTS_ST_WindParam7
#define DOTS_ST_Branch2_Bend_Oscillation_Turbulence_Flexibility DOTS_ST_WindParam8
#define DOTS_ST_Ripple_NoisePosTurbulence_Independence DOTS_ST_WindParam9
#define DOTS_ST_Ripple_Planar_Directional_Flexibility_Shimmer DOTS_ST_WindParam10

#define DOTS_ST_HistoryWindVector DOTS_ST_WindHistoryParam0
#define DOTS_ST_HistoryTreeExtents_SharedHeightStart DOTS_ST_WindHistoryParam1
#define DOTS_ST_HistoryBranchStretchLimits DOTS_ST_WindHistoryParam2
#define DOTS_ST_HistoryShared_NoisePosTurbulence_Independence DOTS_ST_WindHistoryParam3
#define DOTS_ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility DOTS_ST_WindHistoryParam4
#define DOTS_ST_HistoryBranch1_NoisePosTurbulence_Independence DOTS_ST_WindHistoryParam5
#define DOTS_ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility DOTS_ST_WindHistoryParam6
#define DOTS_ST_HistoryBranch2_NoisePosTurbulence_Independence DOTS_ST_WindHistoryParam7
#define DOTS_ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility DOTS_ST_WindHistoryParam8
#define DOTS_ST_HistoryRipple_NoisePosTurbulence_Independence DOTS_ST_WindHistoryParam9
#define DOTS_ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer DOTS_ST_WindHistoryParam10

UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_WindVector)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_TreeExtents_SharedHeightStart)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_BranchStretchLimits)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Shared_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Shared_Bend_Oscillation_Turbulence_Flexibility)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch1_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch1_Bend_Oscillation_Turbulence_Flexibility)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch2_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch2_Bend_Oscillation_Turbulence_Flexibility)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Ripple_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_Ripple_Planar_Directional_Flexibility_Shimmer)

    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryWindVector)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryTreeExtents_SharedHeightStart)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranchStretchLimits)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryShared_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch1_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch2_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryRipple_NoisePosTurbulence_Independence)
    UNITY_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer)
UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)

#define _ST_WindVector                                              UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_WindVector)
#define _ST_TreeExtents_SharedHeightStart                           UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_TreeExtents_SharedHeightStart)
#define _ST_BranchStretchLimits                                     UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_BranchStretchLimits)
#define _ST_Shared_NoisePosTurbulence_Independence                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Shared_NoisePosTurbulence_Independence)
#define _ST_Shared_Bend_Oscillation_Turbulence_Flexibility          UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Shared_Bend_Oscillation_Turbulence_Flexibility)
#define _ST_Branch1_NoisePosTurbulence_Independence                 UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch1_NoisePosTurbulence_Independence)
#define _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility         UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch1_Bend_Oscillation_Turbulence_Flexibility)
#define _ST_Branch2_NoisePosTurbulence_Independence                 UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch2_NoisePosTurbulence_Independence)
#define _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility         UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Branch2_Bend_Oscillation_Turbulence_Flexibility)
#define _ST_Ripple_NoisePosTurbulence_Independence                  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Ripple_NoisePosTurbulence_Independence)
#define _ST_Ripple_Planar_Directional_Flexibility_Shimmer           UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_Ripple_Planar_Directional_Flexibility_Shimmer)

#define _ST_HistoryWindVector                                       UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryWindVector)
#define _ST_HistoryTreeExtents_SharedHeightStart                    UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryTreeExtents_SharedHeightStart)
#define _ST_HistoryBranchStretchLimits                              UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranchStretchLimits)
#define _ST_HistoryShared_NoisePosTurbulence_Independence           UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryShared_NoisePosTurbulence_Independence)
#define _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility   UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility)
#define _ST_HistoryBranch1_NoisePosTurbulence_Independence          UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch1_NoisePosTurbulence_Independence)
#define _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility)
#define _ST_HistoryBranch2_NoisePosTurbulence_Independence          UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch2_NoisePosTurbulence_Independence)
#define _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility  UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility)
#define _ST_HistoryRipple_NoisePosTurbulence_Independence           UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryRipple_NoisePosTurbulence_Independence)
#define _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer    UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, DOTS_ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer)

#endif

CBufferSpeedTree9 ReadCBuffer(bool bHistory /*must be known compile-time*/)
{
    CBufferSpeedTree9 cb;
    cb.m_vWindDirection                 = bHistory ? _ST_HistoryWindVector.xyz                    : _ST_WindVector.xyz;
    cb.m_fWindStrength                  = bHistory ? _ST_HistoryWindVector.w                      : _ST_WindVector.w;
    cb.m_vTreeExtents                   = bHistory ? _ST_HistoryTreeExtents_SharedHeightStart.xyz : _ST_TreeExtents_SharedHeightStart.xyz;
    cb.m_fSharedHeightStart             = bHistory ? _ST_HistoryTreeExtents_SharedHeightStart.w   : _ST_TreeExtents_SharedHeightStart.w;
    cb.m_fBranch1StretchLimit           = bHistory ? _ST_HistoryBranchStretchLimits.x             : _ST_BranchStretchLimits.x;
    cb.m_fBranch2StretchLimit           = bHistory ? _ST_HistoryBranchStretchLimits.y             : _ST_BranchStretchLimits.y;
    cb.m_fWindIndependence              = bHistory ? _ST_HistoryBranchStretchLimits.z             : _ST_BranchStretchLimits.z;

    // Shared Wind State
    cb.m_sShared.m_vNoisePosTurbulence  = bHistory ? _ST_HistoryShared_NoisePosTurbulence_Independence.xyz       : _ST_Shared_NoisePosTurbulence_Independence.xyz;
    cb.m_sShared.m_fIndependence        = bHistory ? _ST_HistoryShared_NoisePosTurbulence_Independence.w         : _ST_Shared_NoisePosTurbulence_Independence.w;
    cb.m_sShared.m_fBend                = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.x : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.x;
    cb.m_sShared.m_fOscillation         = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.y : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.y;
    cb.m_sShared.m_fTurbulence          = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.z : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.z;
    cb.m_sShared.m_fFlexibility         = bHistory ? _ST_HistoryShared_Bend_Oscillation_Turbulence_Flexibility.w : _ST_Shared_Bend_Oscillation_Turbulence_Flexibility.w;

    // Branch1 Wind State
    cb.m_sBranch1.m_vNoisePosTurbulence  = bHistory ? _ST_HistoryBranch1_NoisePosTurbulence_Independence.xyz       : _ST_Branch1_NoisePosTurbulence_Independence.xyz;
    cb.m_sBranch1.m_fIndependence        = bHistory ? _ST_HistoryBranch1_NoisePosTurbulence_Independence.w         : _ST_Branch1_NoisePosTurbulence_Independence.w;
    cb.m_sBranch1.m_fBend                = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.x : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.x;
    cb.m_sBranch1.m_fOscillation         = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.y : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.y;
    cb.m_sBranch1.m_fTurbulence          = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.z : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.z;
    cb.m_sBranch1.m_fFlexibility         = bHistory ? _ST_HistoryBranch1_Bend_Oscillation_Turbulence_Flexibility.w : _ST_Branch1_Bend_Oscillation_Turbulence_Flexibility.w;

    // Branch2 Wind State
    cb.m_sBranch2.m_vNoisePosTurbulence  = bHistory ? _ST_HistoryBranch2_NoisePosTurbulence_Independence.xyz       : _ST_Branch2_NoisePosTurbulence_Independence.xyz;
    cb.m_sBranch2.m_fIndependence        = bHistory ? _ST_HistoryBranch2_NoisePosTurbulence_Independence.w         : _ST_Branch2_NoisePosTurbulence_Independence.w;
    cb.m_sBranch2.m_fBend                = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.x : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.x;
    cb.m_sBranch2.m_fOscillation         = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.y : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.y;
    cb.m_sBranch2.m_fTurbulence          = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.z : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.z;
    cb.m_sBranch2.m_fFlexibility         = bHistory ? _ST_HistoryBranch2_Bend_Oscillation_Turbulence_Flexibility.w : _ST_Branch2_Bend_Oscillation_Turbulence_Flexibility.w;

    // Ripple Wind State
    cb.m_sRipple.m_vNoisePosTurbulence   = bHistory ? _ST_HistoryRipple_NoisePosTurbulence_Independence.xyz      : _ST_Ripple_NoisePosTurbulence_Independence.xyz;
    cb.m_sRipple.m_fIndependence         = bHistory ? _ST_HistoryRipple_NoisePosTurbulence_Independence.w        : _ST_Ripple_NoisePosTurbulence_Independence.w;
    cb.m_sRipple.m_fPlanar               = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.x : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.x;
    cb.m_sRipple.m_fDirectional          = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.y : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.y;
    cb.m_sRipple.m_fFlexibility          = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.z : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.z;
    cb.m_sRipple.m_fShimmer              = bHistory ? _ST_HistoryRipple_Planar_Directional_Flexibility_Shimmer.w : _ST_Ripple_Planar_Directional_Flexibility_Shimmer.w;

    // transformations : all wind vectors are in local space
    cb.m_vWindDirection  = mul((float3x3)UNITY_MATRIX_I_M, cb.m_vWindDirection); 
    return cb;
}


//
// UTILS
//
float NoiseHash(float n) { return frac(sin(n) * 1e4); }
float NoiseHash(float2 p){ return frac(1e4 * sin(17.0f * p.x + p.y * 0.1f) * (0.1f + abs(sin(p.y * 13.0f + p.x)))); }
float QNoise(float2 x)
{
    float2 i = floor(x);
    float2 f = frac(x);
    
    // four corners in 2D of a tile
    float a = NoiseHash(i);
    float b = NoiseHash(i + float2(1.0, 0.0));
    float c = NoiseHash(i + float2(0.0, 1.0));
    float d = NoiseHash(i + float2(1.0, 1.0));
    
    // same code, with the clamps in smoothstep and common subexpressions optimized away.
    float2 u = f * f * (float2(3.0, 3.0) - float2(2.0, 2.0) * f);
    
    return lerp(a, b, u.x) + (c - a) * u.y * (1.0f - u.x) + (d - b) * u.x * u.y;
}
float4 RuntimeSdkNoise2DFlat(float3 vNoisePos3d)
{
    float2 vNoisePos = vNoisePos3d.xz;

#ifdef USE_ST_NOISE_TEXTURE // test this toggle during shader perf tuning
    return texture2D(g_samNoiseKernel, vNoisePos.xy) - float4(0.5f, 0.5f, 0.5f, 0.5f);
#else
        // fallback, slower noise lookup method
        const float c_fFrequecyScale = 20.0f;
        const float c_fAmplitudeScale = 1.0f;
        const float	c_fAmplitueShift = 0.0f;

        float fNoiseX = (QNoise(vNoisePos * c_fFrequecyScale) + c_fAmplitueShift) * c_fAmplitudeScale - 0.5f;
        float fNoiseY = (QNoise(vNoisePos.yx * 0.5f * c_fFrequecyScale) + c_fAmplitueShift) * c_fAmplitudeScale;
        return float4(fNoiseX, fNoiseY, 0.0f, 0.0f);
#endif
}
float  WindUtil_Square(float  fValue) { return fValue * fValue; }
float2 WindUtil_Square(float2 fValue) { return fValue * fValue; }
float3 WindUtil_Square(float3 fValue) { return fValue * fValue; }
float4 WindUtil_Square(float4 fValue) { return fValue * fValue; }

float3 WindUtil_UnpackNormalizedFloat(float fValue)
{
    float3 vReturn = frac(float3(fValue * 0.01f, fValue, fValue * 100.0f));

    vReturn -= 0.5f;
    vReturn *= 2.0f;

    return normalize(vReturn);
}



//
// SPEEDTREE WIND 9 FUNCTIONS
//

// returns position offset (caller must apply to the vertex position)
float3 RippleWindMotion(
    float3 vUpVector,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float  fRippleWeight,
    float3 vRippleNoisePosTurbulence,
    float  fRippleIndependence,
    float  fRippleFlexibility,
    float  fRippleDirectional,
    float  fRipplePlanar
)
{
    float3 vNoisePosition = vGlobalNoisePosition + vRippleNoisePosTurbulence + vVertexPositionIn * fRippleIndependence;
    vNoisePosition += vWindDirection * (fRippleFlexibility * fRippleWeight);
    
    float4 vNoise = RuntimeSdkNoise2DFlat(vNoisePosition);

    float fRippleFactor = (vNoise.r + 0.25f) * fRippleDirectional;
    float3 vMotion = vWindDirection * fRippleFactor + vUpVector * (vNoise.g * fRipplePlanar);
    vMotion *= fRippleWeight;
    
    return vMotion;
}

// returns updated position
float3 BranchWindPosition(
    float3 vUp,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float  fPackedBranchDir,
    float  fPackedBranchNoiseOffset,
    float  fBranchWeight,
    float  fBranchStretchLimit,
    float3 vBranchNoisePosTurbulence,
    float  fBranchIndependence,
    float  fBranchTurbulence,
    float  fBranchOscillation,
    float  fBranchBend,
    float  fBranchFlexibility
)
{
    float fLength = fBranchWeight * fBranchStretchLimit;
    if (fLength <= 0.0f)
    {
        return vVertexPositionIn;
    }
    
    float3 vBranchDir = WindUtil_UnpackNormalizedFloat(fPackedBranchDir);
    float3 vBranchNoiseOffset = WindUtil_UnpackNormalizedFloat(fPackedBranchNoiseOffset);
    float3 vAnchor = vVertexPositionIn - vBranchDir * fLength;
    vVertexPositionIn -= vAnchor;

    float3 vWind = normalize(vWindDirection + vUp * WindUtil_Square(dot(vBranchDir, vWindDirection)));

    float3 vNoisePosition = vGlobalNoisePosition + vBranchNoisePosTurbulence + vBranchNoiseOffset * fBranchIndependence;
    vNoisePosition += vWind * (fBranchFlexibility * fBranchWeight);
    float4 vNoise = RuntimeSdkNoise2DFlat(vNoisePosition);

    float3 vOscillationTurbulent = cross(vWind, vBranchDir) * fBranchTurbulence;
    float3 vMotion = (vWind * vNoise.r + vOscillationTurbulent * vNoise.g) * fBranchOscillation;
    vMotion += vWind * (fBranchBend * (1.0f - vNoise.b));
    vMotion *= fBranchWeight;

    return normalize(vVertexPositionIn + vMotion) * fLength + vAnchor;
}

// returns updated position
float3 SharedWindPosition(
    float3 vUp,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float  fTreeHeight,
    float  fSharedHeightStart,
    float3 vSharedNoisePosTurbulence,
    float  fSharedTurbulence,
    float  fSharedOscillation,
    float  fSharedBend,
    float  fSharedFlexibility
)
{
    float fLengthSq = dot(vVertexPositionIn, vVertexPositionIn);
    if (fLengthSq == 0.0f)
    {
        return vVertexPositionIn;
    }
    float fLength = sqrt(fLengthSq);

    float fHeight = vVertexPositionIn.y;  // y-up
    float fMaxHeight = fTreeHeight;

    float fWeight = WindUtil_Square(max(fHeight - (fMaxHeight * fSharedHeightStart), 0.0f) / fMaxHeight);

    float3 vNoisePosition = vGlobalNoisePosition + vSharedNoisePosTurbulence;
    vNoisePosition += vWindDirection * (fSharedFlexibility * fWeight);
    float4 vNoise = RuntimeSdkNoise2DFlat(vNoisePosition);

    float3 vOscillationTurbulent = cross(vWindDirection, vUp) * fSharedTurbulence;
    float3 vMotion = (vWindDirection * vNoise.r + vOscillationTurbulent * vNoise.g) * fSharedOscillation;
    vMotion += vWindDirection * (fSharedBend * (1.0f - vNoise.b));
    vMotion *= fWeight;

    return normalize(vVertexPositionIn + vMotion) * fLength;
}

//
// CBUFFER UNPACKING
//
// structured parameter input stubs
// *Wind*()    : float / float2/3/4 inputs, meat of animation logic (above)
// *Wind*_cb() : unpacks the cb struct
// *Wind*_s()  : unpacks the structs within the cb
float3 RippleWindMotion_s(
    float3 vUpVector,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float fRippleWeight,
    in WindRippleState sRipple
)
{
    return RippleWindMotion(
        vUpVector,
        vWindDirection,
        vVertexPositionIn,
        vGlobalNoisePosition,
        fRippleWeight,
        sRipple.m_vNoisePosTurbulence,
        sRipple.m_fIndependence,
        sRipple.m_fFlexibility,
        sRipple.m_fDirectional,
        sRipple.m_fPlanar
    );
}


float3 BranchWindPosition_s(
    float3 vUp,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float fBranch2Weight,
    float fBranch2StretchLimit,
    float fPackedBranch2Dir,
    float fPackedBranch2NoiseOffset,
    in WindBranchState sBranch
)
{
    return BranchWindPosition(
        vUp,
        vWindDirection,
        vVertexPositionIn,
        vGlobalNoisePosition,
        fPackedBranch2Dir,
        fPackedBranch2NoiseOffset,
        fBranch2Weight,
        fBranch2StretchLimit,
        sBranch.m_vNoisePosTurbulence,
        sBranch.m_fIndependence,
        sBranch.m_fTurbulence,
        sBranch.m_fOscillation,
        sBranch.m_fBend,
        sBranch.m_fFlexibility
    );
}

float3 SharedWindPosition_s(
    float3 vUp,
    float3 vWindDirection,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float fTreeHeight,
    float fSharedHeightStart,
    in WindBranchState sShared
)
{
    return SharedWindPosition(
        vUp,
        vWindDirection,
        vVertexPositionIn,
        vGlobalNoisePosition,
        fTreeHeight,
        fSharedHeightStart,
        sShared.m_vNoisePosTurbulence,
        sShared.m_fTurbulence,
        sShared.m_fOscillation,
        sShared.m_fBend,
        sShared.m_fFlexibility
    );
}


// ------------------------------
float3 RippleWindMotion_cb(
    float3 vUpVector,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    float fRippleWeight,
    in CBufferSpeedTree9 cb
)
{
    return RippleWindMotion_s(
        vUpVector,
        cb.m_vWindDirection,
        vVertexPositionIn,
        vGlobalNoisePosition,
        fRippleWeight,
        cb.m_sRipple
    );
}


float3 BranchWindPosition_cb(
    float3 vUp,
    float3 vGlobalNoisePosition,
    float3 vVertexPositionIn,

    float fBranchWeight,
    float fPackedBranchDir,
    float fPackedBranchNoiseOffset,
    in CBufferSpeedTree9 cb,
    int iBranch // 1 or 2, compile time constants
)
{
    if(iBranch == 1)
    {
        return BranchWindPosition_s(
            vUp,
            cb.m_vWindDirection,
            vVertexPositionIn,
            vGlobalNoisePosition,
    
            fBranchWeight,
            cb.m_fBranch1StretchLimit,
            fPackedBranchDir,
            fPackedBranchNoiseOffset,
            cb.m_sBranch1
        );
    }
    
    return BranchWindPosition_s(
        vUp,
        cb.m_vWindDirection,
        vVertexPositionIn,
        vGlobalNoisePosition,
    
        fBranchWeight,
        cb.m_fBranch2StretchLimit,
        fPackedBranchDir,
        fPackedBranchNoiseOffset,
        cb.m_sBranch2
    );
}


float3 SharedWindPosition_cb(
    float3 vUp,
    float3 vVertexPositionIn,
    float3 vGlobalNoisePosition,

    in CBufferSpeedTree9 cb
)
{
    return SharedWindPosition_s(
        vUp,
        cb.m_vWindDirection,
        vVertexPositionIn,
        vGlobalNoisePosition,
        cb.m_vTreeExtents.y, // y-up = height
        cb.m_fSharedHeightStart,
        cb.m_sShared
    );
}


//====================================================================================================

//
//  SPEEDTREE WIND 9 ENTRY
//
float3 SpeedTree9Wind(
    float3 vPos,
    float3 vNormal,
    float4 vTexcoord ,
    float4 vTexcoord1,
    float4 vTexcoord2,
    float4 vTexcoord3,
    bool bHistory
)
{
    CBufferSpeedTree9 cb = ReadCBuffer(bHistory);
    const float fWindDirectionLengthSq = dot(cb.m_vWindDirection, cb.m_vWindDirection);
    if (fWindDirectionLengthSq == 0.0f) // check if we have valid wind vector
    {
        return vPos;
    }

    float3 vUp = normalize(mul((float3x3) UNITY_MATRIX_I_M, float3(0.0, 1.0, 0.0)));
    float3 vWindyPosition = vPos;
 
    // global noise applied to animation instances to break off synchronized
    // movement among multiple instances under wind effect.
    float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
    float3 vGlobalNoisePosition = treePos * cb.m_fWindIndependence;

    #if defined(_WIND_RIPPLE)
    {
        float fRippleWeight = vTexcoord1.w;
        float3 vMotion = RippleWindMotion_cb(
            vUp,
            vWindyPosition,
            vGlobalNoisePosition,
            fRippleWeight,
            cb
        );
        vWindyPosition += vMotion;

        #if defined(_WIND_SHIMMER)
        {
            vNormal = normalize(vNormal - (vMotion * cb.m_sRipple.m_fShimmer));
        } 
        #endif
    }
    #endif
    
    #if defined(_WIND_BRANCH2)
    {
        const int BRANCH2 = 2;
        float fBranch2Weight = vTexcoord2.z;
        float fPackedBranch2Dir = vTexcoord2.y;
        float fPackedBranch2NoiseOffset = vTexcoord2.x;
        vWindyPosition = BranchWindPosition_cb(
            vUp,
            vGlobalNoisePosition,
            vWindyPosition,

            fBranch2Weight,
            fPackedBranch2Dir,
            fPackedBranch2NoiseOffset,
            cb,
            BRANCH2
        );
    }
    #endif

    #if defined(_WIND_BRANCH1)
    {
        const int BRANCH1 = 1;
        float fBranch1Weight = vTexcoord1.z;
        float fPackedBranch1Dir = vTexcoord.w;
        float fPackedBranch1NoiseOffset = vTexcoord.z;
        vWindyPosition = BranchWindPosition_cb(
            vUp,
            vGlobalNoisePosition,
            vWindyPosition,

            fBranch1Weight,
            fPackedBranch1Dir,
            fPackedBranch1NoiseOffset,
            cb,
            BRANCH1
        );
    }
    #endif
    
    #if defined(_WIND_SHARED)
    {
        vWindyPosition = SharedWindPosition_cb(
            vUp,
            vWindyPosition,
            vGlobalNoisePosition,
            cb
        );
    }
    #endif

    return vWindyPosition;
}

// This version is used by ShaderGraph
void SpeedTree9Wind_float(
    // in
    float3 vPos,
    float3 vNormal,
    float4 vTexcoord0,
    float4 vTexcoord1,
    float4 vTexcoord2,
    float4 vTexcoord3,
    bool bHistory,

    // out
    out float3 outPos
)
{
    outPos = SpeedTree9Wind(
        vPos,
        vNormal,
        vTexcoord0,
        vTexcoord1,
        vTexcoord2,
        vTexcoord3,
        bHistory
    );
}

#endif // SPEEDTREE_WIND_9_INCLUDED
