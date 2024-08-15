// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef SPEEDTREE_WIND_INCLUDED
#define SPEEDTREE_WIND_INCLUDED

#include "SpeedTreeCommon.hlsl"

///////////////////////////////////////////////////////////////////////
//  Wind Info

CBUFFER_START(SpeedTreeWind)
float4 _ST_WindVector;
float4 _ST_WindGlobal;
float4 _ST_WindBranch;
float4 _ST_WindBranchTwitch;
float4 _ST_WindBranchWhip;
float4 _ST_WindBranchAnchor;
float4 _ST_WindBranchAdherences;
float4 _ST_WindTurbulences;
float4 _ST_WindLeaf1Ripple;
float4 _ST_WindLeaf1Tumble;
float4 _ST_WindLeaf1Twitch;
float4 _ST_WindLeaf2Ripple;
float4 _ST_WindLeaf2Tumble;
float4 _ST_WindLeaf2Twitch;
float4 _ST_WindFrondRipple;
float4 _ST_WindAnimation;
CBUFFER_END

CBUFFER_START(SpeedTreeWindHistory)
float4 _ST_WindVectorHistory;
float4 _ST_WindGlobalHistory;
float4 _ST_WindBranchHistory;
float4 _ST_WindBranchTwitchHistory;
float4 _ST_WindBranchWhipHistory;
float4 _ST_WindBranchAnchorHistory;
float4 _ST_WindBranchAdherencesHistory;
float4 _ST_WindTurbulencesHistory;
float4 _ST_WindLeaf1RippleHistory;
float4 _ST_WindLeaf1TumbleHistory;
float4 _ST_WindLeaf1TwitchHistory;
float4 _ST_WindLeaf2RippleHistory;
float4 _ST_WindLeaf2TumbleHistory;
float4 _ST_WindLeaf2TwitchHistory;
float4 _ST_WindFrondRippleHistory;
float4 _ST_WindAnimationHistory;
CBUFFER_END

#define ST_WIND_QUALITY_NONE 0
#define ST_WIND_QUALITY_FASTEST 1
#define ST_WIND_QUALITY_FAST 2
#define ST_WIND_QUALITY_BETTER 3
#define ST_WIND_QUALITY_BEST 4
#define ST_WIND_QUALITY_PALM 5

///////////////////////////////////////////////////////////////////////
//  GetCBuffer_*() functions
float4 GetCBuffer_WindVector(bool bHistory) { return bHistory ? _ST_WindVectorHistory : _ST_WindVector; }
float4 GetCBuffer_WindGlobal(bool bHistory) { return bHistory ? _ST_WindGlobalHistory : _ST_WindGlobal; }
float4 GetCBuffer_WindBranch(bool bHistory) { return bHistory ? _ST_WindBranchHistory : _ST_WindBranch; }
float4 GetCBuffer_WindBranchTwitch(bool bHistory) { return bHistory ? _ST_WindBranchTwitchHistory : _ST_WindBranchTwitch; }
float4 GetCBuffer_WindBranchWhip(bool bHistory) { return bHistory ? _ST_WindBranchWhipHistory : _ST_WindBranchWhip; }
float4 GetCBuffer_WindBranchAnchor(bool bHistory) { return bHistory ? _ST_WindBranchAnchorHistory : _ST_WindBranchAnchor; }
float4 GetCBuffer_WindBranchAdherences(bool bHistory) { return bHistory ? _ST_WindBranchAdherencesHistory : _ST_WindBranchAdherences; }
float4 GetCBuffer_WindTurbulences(bool bHistory) { return bHistory ? _ST_WindTurbulencesHistory : _ST_WindTurbulences; }
float4 GetCBuffer_WindLeaf1Ripple(bool bHistory) { return bHistory ? _ST_WindLeaf1RippleHistory : _ST_WindLeaf1Ripple; }
float4 GetCBuffer_WindLeaf1Tumble(bool bHistory) { return bHistory ? _ST_WindLeaf1TumbleHistory : _ST_WindLeaf1Tumble; }
float4 GetCBuffer_WindLeaf1Twitch(bool bHistory) { return bHistory ? _ST_WindLeaf1TwitchHistory : _ST_WindLeaf1Twitch; }
float4 GetCBuffer_WindLeaf2Ripple(bool bHistory) { return bHistory ? _ST_WindLeaf2RippleHistory : _ST_WindLeaf2Ripple; }
float4 GetCBuffer_WindLeaf2Tumble(bool bHistory) { return bHistory ? _ST_WindLeaf2TumbleHistory : _ST_WindLeaf2Tumble; }
float4 GetCBuffer_WindLeaf2Twitch(bool bHistory) { return bHistory ? _ST_WindLeaf2TwitchHistory : _ST_WindLeaf2Twitch; }
float4 GetCBuffer_WindFrondRipple(bool bHistory) { return bHistory ? _ST_WindFrondRippleHistory : _ST_WindFrondRipple; }
float4 GetCBuffer_WindAnimation(bool bHistory) { return bHistory ? _ST_WindAnimationHistory : _ST_WindAnimation; }


///////////////////////////////////////////////////////////////////////
//  UnpackNormalFromFloat

float3 UnpackNormalFromFloat(float fValue)
{
    float3 vDecodeKey = float3(16.0, 1.0, 0.0625);

    // decode into [0,1] range
    float3 vDecodedValue = frac(fValue / vDecodeKey);

    // move back into [-1,1] range & normalize
    return (vDecodedValue * 2.0 - 1.0);
}


///////////////////////////////////////////////////////////////////////
//  CubicSmooth

float4 CubicSmooth(float4 vData)
{
    return vData * vData * (3.0 - 2.0 * vData);
}


///////////////////////////////////////////////////////////////////////
//  TriangleWave

float4 TriangleWave(float4 vData)
{
    return abs((frac(vData + 0.5) * 2.0) - 1.0);
}


///////////////////////////////////////////////////////////////////////
//  TrigApproximate

float4 TrigApproximate(float4 vData)
{
    return (CubicSmooth(TriangleWave(vData)) - 0.5) * 2.0;
}


///////////////////////////////////////////////////////////////////////
//  RotationMatrix
//
//  Constructs an arbitrary axis rotation matrix

float3x3 RotationMatrix(float3 vAxis, float fAngle)
{
    // compute sin/cos of fAngle
    float2 vSinCos;
#ifdef OPENGL
    vSinCos.x = sin(fAngle);
    vSinCos.y = cos(fAngle);
#else
    sincos(fAngle, vSinCos.x, vSinCos.y);
#endif

    const float c = vSinCos.y;
    const float s = vSinCos.x;
    const float t = 1.0 - c;
    const float x = vAxis.x;
    const float y = vAxis.y;
    const float z = vAxis.z;

    return float3x3(t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c);
}


///////////////////////////////////////////////////////////////////////
//  mul_float3x3_float3x3

float3x3 mul_float3x3_float3x3(float3x3 mMatrixA, float3x3 mMatrixB)
{
    return mul(mMatrixA, mMatrixB);
}


///////////////////////////////////////////////////////////////////////
//  mul_float3x3_float3

float3 mul_float3x3_float3(float3x3 mMatrix, float3 vVector)
{
    return mul(mMatrix, vVector);
}


///////////////////////////////////////////////////////////////////////
//  cross()'s parameters are backwards in GLSL

#define wind_cross(a, b) cross((a), (b))


///////////////////////////////////////////////////////////////////////
//  Roll

float Roll(float fCurrent,
    float fMaxScale,
    float fMinScale,
    float fSpeed,
    float fRipple,
    float3 vPos,
    float fTime,
    float3 vRotatedWindVector)
{
    float fWindAngle = dot(vPos, -vRotatedWindVector) * fRipple;
    float fAdjust = TrigApproximate(float4(fWindAngle + fTime * fSpeed, 0.0, 0.0, 0.0)).x;
    fAdjust = (fAdjust + 1.0) * 0.5;

    return lerp(fCurrent * fMinScale, fCurrent * fMaxScale, fAdjust);
}


///////////////////////////////////////////////////////////////////////
//  Twitch

float Twitch(float3 vPos, float fAmount, float fSharpness, float fTime)
{
    const float c_fTwitchFudge = 0.87;
    float4 vOscillations = TrigApproximate(float4(fTime + (vPos.x + vPos.z), c_fTwitchFudge * fTime + vPos.y, 0.0, 0.0));

    //float fTwitch = sin(fFreq1 * fTime + (vPos.x + vPos.z)) * cos(fFreq2 * fTime + vPos.y);
    float fTwitch = vOscillations.x * vOscillations.y * vOscillations.y;
    fTwitch = (fTwitch + 1.0) * 0.5;

    return fAmount * pow(saturate(fTwitch), fSharpness);
}


///////////////////////////////////////////////////////////////////////
//  Oscillate
//
//  This function computes an oscillation value and whip value if necessary.
//  Whip and oscillation are combined like this to minimize calls to
//  TrigApproximate( ) when possible.

float Oscillate(float3 vPos,
    float fTime,
    float fOffset,
    float fWeight,
    float fWhip,
    bool bWhip,
    bool bRoll,
    bool bComplex,
    float fTwitch,
    float fTwitchFreqScale,
    inout float4 vOscillations,
    float3 vRotatedWindVector,
    bool bHistory)
{
    float fOscillation = 1.0;
    if (bComplex)
    {
        if (bWhip)
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * fTwitchFreqScale + fOffset, fTwitchFreqScale * 0.5 * (fTime + fOffset), fTime + fOffset + (1.0 - fWeight)));
        else
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * fTwitchFreqScale + fOffset, fTwitchFreqScale * 0.5 * (fTime + fOffset), 0.0));

        float fFineDetail = vOscillations.x;
        float fBroadDetail = vOscillations.y * vOscillations.z;

        float fTarget = 1.0;
        float fAmount = fBroadDetail;
        if (fBroadDetail < 0.0)
        {
            fTarget = -fTarget;
            fAmount = -fAmount;
        }

        fBroadDetail = lerp(fBroadDetail, fTarget, fAmount);
        fBroadDetail = lerp(fBroadDetail, fTarget, fAmount);

        fOscillation = fBroadDetail * fTwitch * (1.0 - GetCBuffer_WindVector(bHistory).w) + fFineDetail * (1.0 - fTwitch);

        if (bWhip)
            fOscillation *= 1.0 + (vOscillations.w * fWhip);
    }
    else
    {
        if (bWhip)
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * 0.689 + fOffset, 0.0, fTime + fOffset + (1.0 - fWeight)));
        else
            vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * 0.689 + fOffset, 0.0, 0.0));

        fOscillation = vOscillations.x + vOscillations.y * vOscillations.x;

        if (bWhip)
            fOscillation *= 1.0 + (vOscillations.w * fWhip);
    }

    //if (bRoll)
    //{
    //  fOscillation = Roll(fOscillation, _ST_WindRollingBranches.x, _ST_WindRollingBranches.y, _ST_WindRollingBranches.z, _ST_WindRollingBranches.w, vPos.xyz, fTime + fOffset, vRotatedWindVector);
    //}

    return fOscillation;
}


///////////////////////////////////////////////////////////////////////
//  Turbulence

float Turbulence(float fTime, float fOffset, float fGlobalTime, float fTurbulence)
{
    const float c_fTurbulenceFactor = 0.1;

    float4 vOscillations = TrigApproximate(float4(fTime * c_fTurbulenceFactor + fOffset, fGlobalTime * fTurbulence * c_fTurbulenceFactor + fOffset, 0.0, 0.0));

    return 1.0 - (vOscillations.x * vOscillations.y * vOscillations.x * vOscillations.y * fTurbulence);
}


///////////////////////////////////////////////////////////////////////
//  GlobalWind
//
//  This function positions any tree geometry based on their untransformed
//  position and 4 wind floats.

float3 GlobalWind(float3 vPos, float3 vInstancePos, bool bPreserveShape, float3 vRotatedWindVector, float time, bool bHistory)
{
    // WIND_LOD_GLOBAL may be on, but if the global wind effect (WIND_EFFECT_GLOBAL_ST_Wind)
    // was disabled for the tree in the Modeler, we should skip it
    const float4 windGlobal = GetCBuffer_WindGlobal(bHistory);
    const float4 windBranchAdherences = GetCBuffer_WindBranchAdherences(bHistory);

    float fLength = 1.0;
    if (bPreserveShape)
        fLength = length(vPos.xyz);

    // compute how much the height contributes
#ifdef SPEEDTREE_Z_UP
    float fAdjust = max(vPos.z - (1.0 / windGlobal.z) * 0.25, 0.0) * windGlobal.z;
#else
    float fAdjust = max(vPos.y - (1.0 / windGlobal.z) * 0.25, 0.0) * windGlobal.z;
#endif
    if (fAdjust != 0.0)
        fAdjust = pow(abs(fAdjust), windGlobal.w);

    // primary oscillation
    float4 vOscillations = TrigApproximate(float4(vInstancePos.x + time, vInstancePos.y + time * 0.8, 0.0, 0.0));
    float fOsc = vOscillations.x + (vOscillations.y * vOscillations.y);
    float fMoveAmount = windGlobal.y * fOsc;

    // move a minimum amount based on direction adherence
    fMoveAmount += windBranchAdherences.x / windGlobal.z;

    // adjust based on how high up the tree this vertex is
    fMoveAmount *= fAdjust;

    // xy component
#ifdef SPEEDTREE_Z_UP
    vPos.xy += vRotatedWindVector.xy * fMoveAmount;
#else
    vPos.xz += vRotatedWindVector.xz * fMoveAmount;
#endif

    if (bPreserveShape)
        vPos.xyz = normalize(vPos.xyz) * fLength;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  SimpleBranchWind

float3 SimpleBranchWind(float3 vPos,
    float3 vInstancePos,
    float fWeight,
    float fOffset,
    float fTime,
    float fDistance,
    float fTwitch,
    float fTwitchScale,
    float fWhip,
    bool bWhip,
    bool bRoll,
    bool bComplex,
    float3 vRotatedWindVector, 
    bool bHistory)
{
    // turn the offset back into a nearly normalized vector
    float3 vWindVector = UnpackNormalFromFloat(fOffset);
    vWindVector = vWindVector * fWeight;

    // try to fudge time a bit so that instances aren't in sync
    fTime += vInstancePos.x + vInstancePos.y;

    // oscillate
    float4 vOscillations;
    float fOsc = Oscillate(vPos, fTime, fOffset, fWeight, fWhip, bWhip, bRoll, bComplex, fTwitch, fTwitchScale, vOscillations, vRotatedWindVector, bHistory);

    vPos.xyz += vWindVector * fOsc * fDistance;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  DirectionalBranchWind

float3 DirectionalBranchWind(float3 vPos,
    float3 vInstancePos,
    float fWeight,
    float fOffset,
    float fTime,
    float fDistance,
    float fTurbulence,
    float fAdherence,
    float fTwitch,
    float fTwitchScale,
    float fWhip,
    bool bWhip,
    bool bRoll,
    bool bComplex,
    bool bTurbulence,
    float3 vRotatedWindVector,
    bool bHistory)
{
    // turn the offset back into a nearly normalized vector
    float3 vWindVector = UnpackNormalFromFloat(fOffset);
    vWindVector = vWindVector * fWeight;

    // try to fudge time a bit so that instances aren't in sync
    fTime += vInstancePos.x + vInstancePos.y;

    // oscillate
    float4 vOscillations;
    float fOsc = Oscillate(vPos, fTime, fOffset, fWeight, fWhip, bWhip, false, bComplex, fTwitch, fTwitchScale, vOscillations, vRotatedWindVector, bHistory);

    vPos.xyz += vWindVector * fOsc * fDistance;

    // add in the direction, accounting for turbulence
    float fAdherenceScale = 1.0;
    if (bTurbulence)
        fAdherenceScale = Turbulence(fTime, fOffset, GetCBuffer_WindAnimation(bHistory).x, fTurbulence);

    if (bWhip)
        fAdherenceScale += vOscillations.w * GetCBuffer_WindVector(bHistory).w * fWhip;

    //if (bRoll)
    //  fAdherenceScale = Roll(fAdherenceScale, _ST_WindRollingBranches.x, _ST_WindRollingBranches.y, _ST_WindRollingBranches.z, _ST_WindRollingBranches.w, vPos.xyz, fTime + fOffset, vRotatedWindVector);

    vPos.xyz += vRotatedWindVector * fAdherence * fAdherenceScale * fWeight;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  DirectionalBranchWindFrondStyle

float3 DirectionalBranchWindFrondStyle(float3 vPos,
    float3 vInstancePos,
    float fWeight,
    float fOffset,
    float fTime,
    float fDistance,
    float fTurbulence,
    float fAdherence,
    float fTwitch,
    float fTwitchScale,
    float fWhip,
    bool bWhip,
    bool bRoll,
    bool bComplex,
    bool bTurbulence,
    float3 vRotatedWindVector,
    float3 vRotatedBranchAnchor,
    bool bHistory)
{
    // turn the offset back into a nearly normalized vector
    float3 vWindVector = UnpackNormalFromFloat(fOffset);
    vWindVector = vWindVector * fWeight;

    // try to fudge time a bit so that instances aren't in sync
    fTime += vInstancePos.x + vInstancePos.y;

    // oscillate
    float4 vOscillations;
    float fOsc = Oscillate(vPos, fTime, fOffset, fWeight, fWhip, bWhip, false, bComplex, fTwitch, fTwitchScale, vOscillations, vRotatedWindVector, bHistory);

    vPos.xyz += vWindVector * fOsc * fDistance;

    // add in the direction, accounting for turbulence
    float fAdherenceScale = 1.0;
    if (bTurbulence)
        fAdherenceScale = Turbulence(fTime, fOffset, GetCBuffer_WindAnimation(bHistory).x, fTurbulence);

    //if (bRoll)
    //  fAdherenceScale = Roll(fAdherenceScale, _ST_WindRollingBranches.x, _ST_WindRollingBranches.y, _ST_WindRollingBranches.z, _ST_WindRollingBranches.w, vPos.xyz, fTime + fOffset, vRotatedWindVector);

    if (bWhip)
        fAdherenceScale += vOscillations.w * GetCBuffer_WindVector(bHistory).w * fWhip;

    float3 vWindAdherenceVector = vRotatedBranchAnchor - vPos.xyz;
    vPos.xyz += vWindAdherenceVector * fAdherence * fAdherenceScale * fWeight;

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  BranchWind

// Apply only to better, best, palm winds
float3 BranchWind(bool isPalmWind, float3 vPos, float3 vInstancePos, float4 vWindData, float3 vRotatedWindVector, float3 vRotatedBranchAnchor, bool bHistory)
{
    const float4 windBranch = GetCBuffer_WindBranch(bHistory);
    const float4 windBranchTwitch = GetCBuffer_WindBranchTwitch(bHistory);
    const float4 windBranchAdherences = GetCBuffer_WindBranchAdherences(bHistory);
    const float4 windBarnchWhip = GetCBuffer_WindBranchWhip(bHistory);
    const float4 windTurbulences = GetCBuffer_WindTurbulences(bHistory);
    const bool bWhip = isPalmWind;
    const bool bRoll = false;
    const bool bComplex = true;
    
    if (isPalmWind)
    {
        const bool bTurbulence = true;
        vPos = DirectionalBranchWindFrondStyle(
            vPos, 
            vInstancePos, 
            vWindData.x, vWindData.y, 
            windBranch.x, windBranch.y, 
            windTurbulences.x, windBranchAdherences.y, 
            windBranchTwitch.x, windBranchTwitch.y, 
            windBarnchWhip.x, 
            bWhip, 
            bRoll, 
            bComplex, 
            bTurbulence, 
            vRotatedWindVector, 
            vRotatedBranchAnchor,
            bHistory
        );
    }
    else
    {
        vPos = SimpleBranchWind(
            vPos, 
            vInstancePos, 
            vWindData.x, vWindData.y, 
            windBranch.x, windBranch.y, 
            windBranchTwitch.x, windBranchTwitch.y, 
            windBarnchWhip.x, 
            bWhip, 
            bRoll, 
            bComplex, 
            vRotatedWindVector,
            bHistory
        );
    }

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  LeafRipple

float3 LeafRipple(float3 vPos,
    inout float3 vDirection,
    float fScale,
    float fPackedRippleDir,
    float fTime,
    float fAmount,
    bool bDirectional,
    float fTrigOffset)
{
    // compute how much to move
    float4 vInput = float4(fTime + fTrigOffset, 0.0, 0.0, 0.0);
    float fMoveAmount = fAmount * TrigApproximate(vInput).x;

    if (bDirectional)
    {
        vPos.xyz += vDirection.xyz * fMoveAmount * fScale;
    }
    else
    {
        float3 vRippleDir = UnpackNormalFromFloat(fPackedRippleDir);
        vPos.xyz += vRippleDir * fMoveAmount * fScale;
    }

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  LeafTumble

float3 LeafTumble(float3 vPos,
    inout float3 vDirection,
    float fScale,
    float3 vAnchor,
    float3 vGrowthDir,
    float fTrigOffset,
    float fTime,
    float fFlip,
    float fTwist,
    float fAdherence,
    float3 vTwitch,
    float4 vRoll,
    bool bTwitch,
    bool bRoll,
    float3 vRotatedWindVector)
{
    // compute all oscillations up front
    float3 vFracs = frac((vAnchor + fTrigOffset) * 30.3);
    float fOffset = vFracs.x + vFracs.y + vFracs.z;
    float4 vOscillations = TrigApproximate(float4(fTime + fOffset, fTime * 0.75 - fOffset, fTime * 0.01 + fOffset, fTime * 1.0 + fOffset));

    // move to the origin and get the growth direction
    float3 vOriginPos = vPos.xyz - vAnchor;
    float fLength = length(vOriginPos);

    // twist
    float fOsc = vOscillations.x + vOscillations.y * vOscillations.y;
    float3x3 matTumble = RotationMatrix(vGrowthDir, fScale * fTwist * fOsc);

    // with wind
    float3 vAxis = wind_cross(vGrowthDir.xyz, vRotatedWindVector.xyz);
    float fDot = clamp(dot(vRotatedWindVector, vGrowthDir), -1.0, 1.0);
#ifdef SPEEDTREE_Z_UP
    vAxis.z += fDot;
#else
    vAxis.y += fDot;
#endif
    vAxis = normalize(vAxis);

    float fAngle = acos(fDot);

    float fAdherenceScale = 1.0;
    //if (bRoll)
    //{
    //  fAdherenceScale = Roll(fAdherenceScale, vRoll.x, vRoll.y, vRoll.z, vRoll.w, vAnchor.xyz, fTime, vRotatedWindVector);
    //}

    fOsc = vOscillations.y - vOscillations.x * vOscillations.x;

    float fTwitch = 0.0;
    if (bTwitch)
        fTwitch = Twitch(vAnchor.xyz, vTwitch.x, vTwitch.y, vTwitch.z + fOffset);

    matTumble = mul_float3x3_float3x3(matTumble, RotationMatrix(vAxis, fScale * (fAngle * fAdherence * fAdherenceScale + fOsc * fFlip + fTwitch)));

    vDirection = mul_float3x3_float3(matTumble, vDirection);
    vOriginPos = mul_float3x3_float3(matTumble, vOriginPos);

    vOriginPos = normalize(vOriginPos) * fLength;

    return (vOriginPos + vAnchor);
}


///////////////////////////////////////////////////////////////////////
//  LeafWind
//  Optimized (for instruction count) version. Assumes leaf 1 and 2 have the same options

float3 LeafWind(bool isBestWind,
    bool bLeaf2,
    float3 vPos,
    inout float3 vDirection,
    float fScale,
    float3 vAnchor,
    float fPackedGrowthDir,
    float fPackedRippleDir,
    float fRippleTrigOffset,
    float3 vRotatedWindVector,
    bool bHistory)
{
    const float4 windLeaf2Ripple = GetCBuffer_WindLeaf2Ripple(bHistory);
    const float4 windLeaf1Ripple = GetCBuffer_WindLeaf1Ripple(bHistory);
    const float4 windLeaf1Tumble = GetCBuffer_WindLeaf1Tumble(bHistory);
    const float4 windLeaf2Tumble = GetCBuffer_WindLeaf2Tumble(bHistory);
    const float4 windLeaf1Twitch = GetCBuffer_WindLeaf2Twitch(bHistory);
    const float4 windLeaf2Twitch = GetCBuffer_WindLeaf2Twitch(bHistory);

    vPos = LeafRipple(vPos, vDirection, fScale, fPackedRippleDir,
        (bLeaf2 ? windLeaf2Ripple.x : windLeaf1Ripple.x),
        (bLeaf2 ? windLeaf2Ripple.y : windLeaf1Ripple.y),
        false, fRippleTrigOffset);

    if (isBestWind)
    {
        float3 vGrowthDir = UnpackNormalFromFloat(fPackedGrowthDir);
        vPos = LeafTumble(vPos, vDirection, fScale, vAnchor, vGrowthDir, fPackedGrowthDir,
            (bLeaf2 ? windLeaf2Tumble.x : windLeaf1Tumble.x),
            (bLeaf2 ? windLeaf2Tumble.y : windLeaf1Tumble.y),
            (bLeaf2 ? windLeaf2Tumble.z : windLeaf1Tumble.z),
            (bLeaf2 ? windLeaf2Tumble.w : windLeaf1Tumble.w),
            (bLeaf2 ? windLeaf2Twitch.xyz : windLeaf1Twitch.xyz),
            0.0f,
            true,
            true,
            vRotatedWindVector);
    }

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  RippleFrondOneSided

float3 RippleFrondOneSided(float3 vPos,
    inout float3 vDirection,
    float fU,
    float fV,
    float fRippleScale,
    bool bHistory
#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
    , float3 vBinormal
    , float3 vTangent
#endif
)
{
    float fOffset = 0.0;
    if (fU < 0.5)
        fOffset = 0.75;

    const float4 windFrondRipple = GetCBuffer_WindFrondRipple(bHistory);

    float4 vOscillations = TrigApproximate(float4((windFrondRipple.x + fV) * windFrondRipple.z + fOffset, 0.0, 0.0, 0.0));

    float fAmount = fRippleScale * vOscillations.x * windFrondRipple.y;
    float3 vOffset = fAmount * vDirection;
    vPos.xyz += vOffset;

#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
    vTangent.xyz = normalize(vTangent.xyz + vOffset * windFrondRipple.w);
    float3 vNewNormal = normalize(wind_cross(vBinormal.xyz, vTangent.xyz));
    if (dot(vNewNormal, vDirection.xyz) < 0.0)
        vNewNormal = -vNewNormal;
    vDirection.xyz = vNewNormal;
#endif

    return vPos;
}

///////////////////////////////////////////////////////////////////////
//  RippleFrondTwoSided

float3 RippleFrondTwoSided(float3 vPos,
    inout float3 vDirection,
    float fU,
    float fLengthPercent,
    float fPackedRippleDir,
    float fRippleScale,
    bool bHistory
#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
    , float3 vBinormal
    , float3 vTangent
#endif
)
{
    const float4 windFrondRipple = GetCBuffer_WindFrondRipple(bHistory);
    float4 vOscillations = TrigApproximate(float4(windFrondRipple.x * fLengthPercent * windFrondRipple.z, 0.0, 0.0, 0.0));

    float3 vRippleDir = UnpackNormalFromFloat(fPackedRippleDir);

    float fAmount = fRippleScale * vOscillations.x * windFrondRipple.y;
    float3 vOffset = fAmount * vRippleDir;

    vPos.xyz += vOffset;

#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
    vTangent.xyz = normalize(vTangent.xyz + vOffset * windFrondRipple.w);
    float3 vNewNormal = normalize(wind_cross(vBinormal.xyz, vTangent.xyz));
    if (dot(vNewNormal, vDirection.xyz) < 0.0)
        vNewNormal = -vNewNormal;
    vDirection.xyz = vNewNormal;
#endif

    return vPos;
}


///////////////////////////////////////////////////////////////////////
//  RippleFrond

float3 RippleFrond(float3 vPos,
    inout float3 vDirection,
    float fU,
    float fV,
    float fPackedRippleDir,
    float fRippleScale,
    float fLenghtPercent,
    bool bHistory
#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
    , float3 vBinormal
    , float3 vTangent
#endif
)
{
    return RippleFrondOneSided(vPos,
        vDirection,
        fU,
        fV,
        fRippleScale,
        bHistory
#ifdef WIND_EFFECT_FROND_RIPPLE_ADJUST_LIGHTING
        , vBinormal
        , vTangent
#endif
    );
}

///////////////////////////////////////////////////////////////////////
//  SpeedTreeWind


float3 SpeedTreeWind(
    float3 vPos, 
    float3 vNormal, 
    float4 vTexcoord0, 
    float4 vTexcoord1, 
    float4 vTexcoord2, 
    float4 vTexcoord3, 
    int iWindQuality, 
    bool bBillboard,
    bool bCrossfade,
    bool bHistory
)
{
    float3 vReturnPos = vPos;

    // check wind enabled & data available
    const float3 windVector = GetCBuffer_WindVector(bHistory).xyz;
    float3 rotatedWindVector = TransformWorldToObjectDir(windVector);
    float windLength = length(rotatedWindVector);
    bool bWindEnabled = (iWindQuality > 0) && (length(windVector) > 1.0e-5);
    if (!bWindEnabled)
    {
        return vReturnPos; // sanity check that wind data is available
    }

    bool leafTwo = false;
    int geometryType = GetGeometryType(vTexcoord3, leafTwo);
    
    rotatedWindVector /= windLength;
    float4x4 matObjectToWorld = GetObjectToWorldMatrix();
    float3 treePos = GetAbsolutePositionWS(float3(matObjectToWorld[0].w, matObjectToWorld[1].w, matObjectToWorld[2].w));
    float globalWindTime = GetCBuffer_WindGlobal(bHistory).x;

    // BILLBOARD WIND =======================================================================================================================
    if(bBillboard) 
    {
        vReturnPos = GlobalWind(vReturnPos, treePos, true, rotatedWindVector, globalWindTime, bHistory);
        return vReturnPos;
    }

    // 3D GEOMETRY WIND =====================================================================================================================    
    // leaf
    bool bDoLeafWind = ((iWindQuality == ST_WIND_QUALITY_FAST) || (iWindQuality == ST_WIND_QUALITY_BETTER) || (iWindQuality == ST_WIND_QUALITY_BEST))
                        && geometryType > ST_GEOM_TYPE_FROND;
    if (bDoLeafWind)
    {
        float3 anchor = float3(vTexcoord1.zw, vTexcoord2.w);
        float leafWindTrigOffset = anchor.x + anchor.y;
        bool bBestWind = (iWindQuality == ST_WIND_QUALITY_BEST);

        vReturnPos -= anchor; // remove anchor position
        vReturnPos = LeafWind(bBestWind, leafTwo, vReturnPos, vNormal, vTexcoord3.x, float3(0, 0, 0), vTexcoord3.y, vTexcoord3.z, leafWindTrigOffset, rotatedWindVector, bHistory);
        vReturnPos += anchor; // move back out to anchor
    }

    // frond wind (palm-only)
    bool bDoPalmWind = iWindQuality == ST_WIND_QUALITY_PALM && geometryType == ST_GEOM_TYPE_FROND;
    if (bDoPalmWind)
    {
        vReturnPos = RippleFrond(vReturnPos, vNormal, vTexcoord0.x, vTexcoord0.y, vTexcoord3.x, vTexcoord3.y, vTexcoord3.z, bHistory);
    }

    // branch wind (applies to all 3D geometry)
    bool bDoBranchWind = (iWindQuality == ST_WIND_QUALITY_BETTER) || (iWindQuality == ST_WIND_QUALITY_BEST) || (iWindQuality == ST_WIND_QUALITY_PALM);
    if (bDoBranchWind)
    {
        const float4 windBranchAnchorHistory = GetCBuffer_WindBranchAnchor(bHistory);
        float3 rotatedBranchAnchor = TransformWorldToObjectDir(windBranchAnchorHistory.xyz) * windBranchAnchorHistory.w;
        vReturnPos = BranchWind(bDoPalmWind, vReturnPos, treePos, float4(vTexcoord0.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor, bHistory);
    }

    // global wind
    vReturnPos = GlobalWind(vReturnPos, treePos, true, rotatedWindVector, globalWindTime, bHistory);
    return vReturnPos;
}


// This version is used by ShaderGraph
void SpeedTreeWind_float(float3 vPos, float3 vNormal, float4 vTexcoord0, float4 vTexcoord1, float4 vTexcoord2, float4 vTexcoord3, int iWindQuality, bool bBillboard, bool bCrossfade, bool bHistory, out float3 outPos)
{
    outPos = vPos;

    // determine geometry type
    bool leafTwo = false;
    int geometryType = GetGeometryType(vTexcoord3, leafTwo);
    
    // apply 3D SpeedTree FX
    if (!bBillboard) 
    {
        // lod transition
        if (!bCrossfade)
        {
            outPos = ApplySmoothLODTransition(vPos, vTexcoord2.xyz);
        }

        // leaf facing
        if (geometryType == ST_GEOM_TYPE_FACINGLEAF)
        {
            float3 anchor = float3(vTexcoord1.zw, vTexcoord2.w);
            outPos = DoLeafFacing(outPos, anchor);
        }
    }

    // do wind
    if (iWindQuality != ST_WIND_QUALITY_NONE)
    {
        outPos = SpeedTreeWind(outPos, vNormal, vTexcoord0, vTexcoord1, vTexcoord2, vTexcoord3, iWindQuality, bBillboard, bCrossfade, bHistory);
    }
}
#endif // SPEEDTREE_WIND_INCLUDED
