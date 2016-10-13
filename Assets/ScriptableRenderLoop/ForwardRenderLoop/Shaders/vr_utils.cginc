// Copyright (c) Valve Corporation, All rights reserved. ======================================================================================================

#ifndef VALVE_VR_UTILS_INCLUDED
#define VALVE_VR_UTILS_INCLUDED

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
#include "UnityShaderVariables.cginc"

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
#define g_vCameraPositionWs ( _WorldSpaceCameraPos )
//#define g_flTime ( _Time.y )
float g_flTime = 0.0; // Set by ValveCamera.cs

//float3 g_vMiddleEyePositionWs;
//float4x4 g_matWorldToProjectionMultiview[ 2 ];
//float4 g_vCameraPositionWsMultiview[ 2 ];

//---------------------------------------------------------------------------------------------------------------------------------------------------------
float3 ScreenSpaceDither( float2 vScreenPos )
{
    //if ( Blink( 1.5 ) )
    //  return 0.0;

    //if ( ( int )vScreenPos.y == 840 )
    //  return 0.3;
    //if ( vScreenPos.y < 840 )
    //  return 0.0;

    float3 vDither = dot( float2( 171.0, 231.0 ), vScreenPos.xy + g_flTime.xx ).xxx;
    vDither.rgb = frac( vDither.rgb / float3( 103.0, 71.0, 97.0 ) ) - float3( 0.5, 0.5, 0.5 );
    return ( vDither.rgb / 255.0 ) * 0.375;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Used to blink shader code to see before/after during development. Meant to be used like this: if ( Blink( 1.0 ) )
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float Blink( float flNumSeconds )
{
    return step( 0.5, frac( g_flTime * 0.5 / flNumSeconds ) );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Tangent transform helper functions
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 Vec3WsToTs( float3 vVectorWs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
{
    float3 vVectorTs;
    vVectorTs.x = dot( vVectorWs.xyz, vTangentUWs.xyz );
    vVectorTs.y = dot( vVectorWs.xyz, vTangentVWs.xyz );
    vVectorTs.z = dot( vVectorWs.xyz, vNormalWs.xyz );
    return vVectorTs.xyz; // Return without normalizing
}

float3 Vec3WsToTsNormalized( float3 vVectorWs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
{
    return normalize( Vec3WsToTs( vVectorWs.xyz, vNormalWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz ) );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 Vec3TsToWs( float3 vVectorTs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
{
    float3 vVectorWs;
    vVectorWs.xyz = vVectorTs.x * vTangentUWs.xyz;
    vVectorWs.xyz += vVectorTs.y * vTangentVWs.xyz;
    vVectorWs.xyz += vVectorTs.z * vNormalWs.xyz;
    return vVectorWs.xyz; // Return without normalizing
}

float3 Vec3TsToWsNormalized( float3 vVectorTs, float3 vNormalWs, float3 vTangentUWs, float3 vTangentVWs )
{
    return normalize( Vec3TsToWs( vVectorTs.xyz, vNormalWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz ) );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 ComputeTangentVFromSign( float3 vNormalWs, float3 vTangentUWs, float flTangentFlip )
{
    return normalize( cross( vTangentUWs.xyz, vNormalWs.xyz ) ) * flTangentFlip;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 DecodeHemiOctahedronNormal( float2 vHemiOct )
{
    // Rotate and scale the unit square back to the center diamond
    vHemiOct.xy = ( vHemiOct.xy * 2.0 ) - 1.0;
    float2 temp = float2( vHemiOct.x + vHemiOct.y, vHemiOct.x - vHemiOct.y ) * 0.5;
    float3 v = float3( temp.xy, 1.0 - abs( temp.x ) - abs( temp.y ) );
    return normalize( v );
}

// Defines ----------------------------------------------------------------------------------------------------------------------------------------------------
#define M_PI ( 3.14159265358979323846 )
#define MOD2X_SCALAR ( 1.992156862745098 ) // 254.0/255.0 * 2.0. This maps 128 in an 8-bit texture to 1.0. We'll probably need something different for DXT textures
#define SMALL_FLOAT 1e-12

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// MAX/MIN to match src
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float MAX( float flA, float flB )
{
    return max( flA, flB );
}

float MIN( float flA, float flB )
{
    return min( flA, flB );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Normalize functions that avoid divide by 0
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 NormalizeSafe( float3 vVec )
{
    float3 vResult;

    //[flatten]
    if ( length( vVec.xyz ) == 0.0 )
    {
        vResult.xyz = float3( 0.0, 0.0, 0.0 );
    }
    else
    {
        vResult.xyz = normalize( vVec.xyz );
    }

    return vResult.xyz;
}

float2 NormalizeSafe( float2 vVec )
{
    float2 vResult;

    //[flatten]
    if ( length( vVec.xy ) == 0.0 )
    {
        vResult.xy = float2( 0.0, 0.0 );
    }
    else
    {
        vResult.xy = normalize( vVec.xy );
    }

    return vResult.xy;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float ClampToPositive( float flValue )
{
    return max( 0.0, flValue );
}

float2 ClampToPositive( float2 vValue )
{
    return max( float2( 0.0, 0.0 ), vValue.xy );
}

float3 ClampToPositive( float3 vValue )
{
    return max( float3( 0.0, 0.0, 0.0 ), vValue.xyz );
}

float4 ClampToPositive( float4 vValue )
{
    return max( float4( 0.0, 0.0, 0.0, 0.0 ), vValue.xyzw );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float LinearRamp( float flMin, float flMax, float flInput )
{
    return saturate( ( flInput - flMin ) / ( flMax - flMin ) );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float fsel( float flComparand, float flValGE, float flLT )
{
    return ( flComparand >= 0.0 ) ? flValGE : flLT;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Remap a value in the range [A,B] to [C,D].
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float RemapVal( float flOldVal, float flOldMin, float flOldMax, float flNewMin, float flNewMax )
{
    // Put the old val into 0-1 range based on the old min/max
    float flValNormalized = ( flOldVal - flOldMin ) / ( flOldMax - flOldMin );

    // Map 0-1 range into new min/max
    return ( flValNormalized * ( flNewMax - flNewMin ) ) + flNewMin;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Remap a value in the range [A,B] to [C,D]. Values <A map to C, and >B maps to D.
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float RemapValClamped( float flOldVal, float flOldMin, float flOldMax, float flNewMin, float flNewMax )
{
    // Put the old val into 0-1 range based on the old min/max
    float flValNormalized = saturate( ( flOldVal - flOldMin ) / ( flOldMax - flOldMin ) );

    // Map 0-1 range into new min/max
    return ( flValNormalized * ( flNewMax - flNewMin ) ) + flNewMin;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float4 PackToColor( float4 vValue )
{
    return ( ( vValue.xyzw * 0.5 ) + 0.5 );
}

float3 PackToColor( float3 vValue )
{
    return ( ( vValue.xyz * 0.5 ) + 0.5 );
}

float2 PackToColor( float2 vValue )
{
    return ( ( vValue.xy * 0.5 ) + 0.5 );
}

float PackToColor( float flValue )
{
    return ( ( flValue * 0.5 ) + 0.5 );
}

float4 UnpackFromColor( float4 cColor )
{
    return ( ( cColor.xyzw * 2.0 ) - 1.0 );
}

float3 UnpackFromColor( float3 cColor )
{
    return ( ( cColor.xyz * 2.0 ) - 1.0 );
}

float2 UnpackFromColor( float2 cColor )
{
    return ( ( cColor.xy * 2.0 ) - 1.0 );
}

float UnpackFromColor( float flColor )
{
    return ( ( flColor * 2.0 ) - 1.0 );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float Luminance( float3 cColor )
{
    // Formula for calculating luminance based on NTSC standard
    float3 tmpv = float3( 0.2125, 0.7154, 0.0721 );
    float flLuminance = dot( cColor.rgb, tmpv.rgb );

    // Alternate formula for calculating luminance for linear RGB space (Widely used in color hue and saturation computations)
    //float3 tmpv = float3( 0.3086, 0.6094, 0.0820 );;
    //float flLuminance = dot( cColor.rgb, tmpv.rgb );

    // Simple average
    //float3 tmpv = float3( 0.333, 0.333, 0.333 );
    //float flLuminance = dot( cColor.rgb, tmpv.rgb );

    return flLuminance;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 SaturateColor( float3 cColor, float flTargetSaturation )
{
    float lum = Luminance( cColor.rgb );
    return lerp( float3( lum, lum, lum ), cColor.rgb, flTargetSaturation.xxx );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// 2.0 gamma conversion routines - Should only be used in special cases
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float LinearToGamma20( float vLinear )
{
    return pow( vLinear, 0.5 );
}

float3 LinearToGamma20( float3 vLinear )
{
    return pow( vLinear.rgb, float3( 0.5, 0.5, 0.5 ) );
}

float4 LinearToGamma20( float4 vLinear )
{
    return float4( pow( vLinear.rgb, float3( 0.5, 0.5, 0.5 ) ), vLinear.a );
}

float Gamma20ToLinear( float vGamma )
{
    return vGamma * vGamma;
}

float3 Gamma20ToLinear( float3 vGamma )
{
    return vGamma.rgb * vGamma.rgb;
}

float4 Gamma20ToLinear( float4 vGamma )
{
    return float4( vGamma.rgb * vGamma.rgb, vGamma.a );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// 2.2 gamma conversion routines - Should only be used in special cases
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float LinearToGamma22( float vLinear )
{
    return pow( vLinear, 0.454545454545455 );
}

float3 LinearToGamma22( float3 vLinear )
{
    return pow( vLinear.rgb, float3( 0.454545454545455, 0.454545454545455, 0.454545454545455 ) );
}

float4 LinearToGamma22( float4 vLinear )
{
    return float4( pow( vLinear.rgb, float3( 0.454545454545455, 0.454545454545455, 0.454545454545455 ) ), vLinear.a );
}

float Gamma22ToLinear( float vGamma )
{
    return pow( vGamma, 2.2 );
}

float3 Gamma22ToLinear( float3 vGamma )
{
    return pow( vGamma.rgb, float3( 2.2, 2.2, 2.2 ) );
}

float4 Gamma22ToLinear( float4 vGamma )
{
    return float4( pow( vGamma.rgb, float3( 2.2, 2.2, 2.2 ) ), vGamma.a );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// sRGB gamma conversion routines
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 SrgbGammaToLinear( float3 vSrgbGammaColor )
{
    // 15 asm instructions
    float3 vLinearSegment = vSrgbGammaColor.rgb / 12.92;
    float3 vExpSegment = pow( ( ( vSrgbGammaColor.rgb / 1.055 ) + ( 0.055 / 1.055 ) ), float3( 2.4, 2.4, 2.4 ) );

    float3 vLinearColor = float3( ( vSrgbGammaColor.r <= 0.04045 ) ? vLinearSegment.r : vExpSegment.r,
                                  ( vSrgbGammaColor.g <= 0.04045 ) ? vLinearSegment.g : vExpSegment.g,
                                  ( vSrgbGammaColor.b <= 0.04045 ) ? vLinearSegment.b : vExpSegment.b );

    return vLinearColor.rgb;
}

float3 SrgbLinearToGamma( float3 vLinearColor )
{
    // 15 asm instructions
    float3 vLinearSegment = vLinearColor.rgb * 12.92;
    float3 vExpSegment = ( 1.055 * pow( vLinearColor.rgb, float3 ( 1.0 / 2.4, 1.0 / 2.4, 1.0 / 2.4 ) ) ) - 0.055;

    float3 vGammaColor = float3( ( vLinearColor.r <= 0.0031308 ) ? vLinearSegment.r : vExpSegment.r,
                                 ( vLinearColor.g <= 0.0031308 ) ? vLinearSegment.g : vExpSegment.g,
                                 ( vLinearColor.b <= 0.0031308 ) ? vLinearSegment.b : vExpSegment.b );

    return vGammaColor.rgb;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// RGBM encode/decode
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float4 RGBMEncode( float3 vLinearColor )
{
    vLinearColor.rgb = sqrt( vLinearColor.rgb );
    vLinearColor.rgb = saturate( vLinearColor.rgb * ( 1.0 / 6.0 ) );

    float4 vRGBM;

    vRGBM.a = max( max( vLinearColor.r, vLinearColor.g ), max( vLinearColor.b, 1.0 / 6.0 ) );
    vRGBM.a = ceil( vRGBM.a * 255.0 ) / 255.0;
    vRGBM.rgb = vLinearColor.rgb / vRGBM.a;

    return vRGBM;
}

float3 RGBMDecode( float4 vRGBM )
{
    float3 vLinearColor = vRGBM.rgb * 6.0 * vRGBM.a;

    vLinearColor.rgb *= vLinearColor.rgb;

    return vLinearColor.rgb;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// NOTE: All 2D normal vectors are assumed to be from a unit-length normal, so the length of xy must be <= 1.0!
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float2 UnpackNormal2D( float2 vNormal )
{
    return ( ( vNormal.xy * 2.0 ) - 1.0 );
}

float2 PackNormal2D( float2 vNormal )
{
    return ( ( vNormal.xy * 0.5 ) + 0.5 );
}

float3 UnpackNormal3D( float3 vNormal )
{
    return ( ( vNormal.xyz * 2.0 ) - 1.0 );
}

float3 PackNormal3D( float3 vNormal )
{
    return ( ( vNormal.xyz * 0.5 ) + 0.5 );
}

float3 ComputeNormalFromXY( float2 vXY )
{
    float3 vNormalTs;

    vNormalTs.xy = vXY.xy;
    vNormalTs.z = sqrt( saturate( 1.0 - dot( vNormalTs.xy, vNormalTs.xy ) ) );

    return vNormalTs.xyz;
}

float3 ComputeNormalFromRGTexture( float2 vRGPixel )
{
    float3 vNormalTs;

    vNormalTs.xy = UnpackNormal2D( vRGPixel.rg );
    vNormalTs.z = sqrt( saturate( 1.0 - dot( vNormalTs.xy, vNormalTs.xy ) ) );

    return vNormalTs.xyz;
}

float3 ComputeNormalFromDXT5Texture( float4 vDXT5Pixel )
{
    return ComputeNormalFromRGTexture( vDXT5Pixel.ag );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 ConvertSphericalToNormal( float2 vSpherical )
{
    float2 sincosTheta;
    sincos( vSpherical.x * M_PI, sincosTheta.x, sincosTheta.y );
    float2 sincosPhi = float2( sqrt( 1.0 - ( vSpherical.y * vSpherical.y ) ), vSpherical.y );

    return float3( sincosTheta.y * sincosPhi.x, sincosTheta.x * sincosPhi.x, sincosPhi.y );
}

float2 ConvertNormalToSphericalRGTexture( float3 vNormal )
{
    // TODO: atan2 isn't defined at 0,0.  Is this a problem?
    float flAtanYX = atan2( vNormal.y, vNormal.x ) / M_PI;

    return PackToColor( float2( flAtanYX, vNormal.z ) );
}

float3 ComputeNormalFromSphericalRGTexture( float2 vRGPixel )
{
    float2 vUnpackedSpherical = UnpackNormal2D( vRGPixel.rg );

    return ConvertSphericalToNormal( vUnpackedSpherical.xy );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 CalculateCameraToPositionRayWs( float3 vPositionWs )
{
    return ( vPositionWs.xyz - g_vCameraPositionWs.xyz );
}

float3 CalculateCameraToPositionDirWs( float3 vPositionWs )
{
    return normalize( CalculateCameraToPositionRayWs( vPositionWs.xyz ) );
}

float3 CalculateCameraToPositionRayTs( float3 vPositionWs, float3 vTangentUWs, float3 vTangentVWs, float3 vNormalWs )
{
    float3 vViewVectorWs = CalculateCameraToPositionRayWs( vPositionWs.xyz ); // Not normalized
    return Vec3WsToTs( vViewVectorWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz, vNormalWs.xyz ); // Not Normalized
}

float3 CalculateCameraToPositionDirTs( float3 vPositionWs, float3 vTangentUWs, float3 vTangentVWs, float3 vNormalWs )
{
    return normalize( CalculateCameraToPositionRayTs( vPositionWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz, vNormalWs.xyz ) );
}

float3 CalculateCameraToPositionRayWsMultiview( uint nView, float3 vPositionWs )
{
    // TODO!
    return CalculateCameraToPositionRayWs( vPositionWs.xyz );
    //return ( vPositionWs.xyz - g_vCameraPositionWsMultiview[ nView ].xyz );
}

float3 CalculateCameraToPositionDirWsMultiview( uint nView, float3 vPositionWs )
{
    return normalize( CalculateCameraToPositionRayWsMultiview( nView, vPositionWs.xyz ) );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// This helps the compiler reuse the output of the reverse functions above instead of duplicating the above code with camera - position
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 CalculatePositionToCameraRayWs( float3 vPositionWs )
{
    return -CalculateCameraToPositionRayWs( vPositionWs.xyz );
}

float3 CalculatePositionToCameraDirWs( float3 vPositionWs )
{
    return -CalculateCameraToPositionDirWs( vPositionWs.xyz );
}

float3 CalculatePositionToCameraRayTs( float3 vPositionWs, float3 vTangentUWs, float3 vTangentVWs, float3 vNormalWs )
{
    return -CalculateCameraToPositionRayTs( vPositionWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz, vNormalWs.xyz );
}

float3 CalculatePositionToCameraDirTs( float3 vPositionWs, float3 vTangentUWs, float3 vTangentVWs, float3 vNormalWs )
{
    return -CalculateCameraToPositionDirTs( vPositionWs.xyz, vTangentUWs.xyz, vTangentVWs.xyz, vNormalWs.xyz );
}

float3 CalculatePositionToCameraRayWsMultiview( uint nView, float3 vPositionWs )
{
    return -CalculateCameraToPositionRayWsMultiview( nView, vPositionWs.xyz );
}

float3 CalculatePositionToCameraDirWsMultiview( uint nView, float3 vPositionWs )
{
    return -CalculateCameraToPositionDirWsMultiview( nView, vPositionWs.xyz );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float3 CalculateCameraReflectionDirWs( float3 vPositionWs, float3 vNormalWs )
{
    float3 vViewVectorWs = CalculateCameraToPositionDirWs( vPositionWs.xyz );
    float3 vReflectionVectorWs = reflect( vViewVectorWs.xyz, vNormalWs.xyz );
    return vReflectionVectorWs.xyz;
}

float3 CalculateCameraReflectionDirWsMultiview( uint nView, float3 vPositionWs, float3 vNormalWs )
{
    float3 vViewVectorWs = CalculateCameraToPositionDirWsMultiview( nView, vPositionWs.xyz );
    float3 vReflectionVectorWs = reflect( vViewVectorWs.xyz, vNormalWs.xyz );
    return vReflectionVectorWs.xyz;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float CalculateDistanceToCamera( float3 vPositionWs )
{
    return length( g_vCameraPositionWs.xyz - vPositionWs.xyz );
}

#endif
