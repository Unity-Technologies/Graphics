using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesClouds
    {
        // Maximal ray marching distance
        public float _MaxRayMarchingDistance;
        // The highest altitude clouds can reach in meters
        public float _HighestCloudAltitude;
        // The lowest altitude clouds can reach in meters
        public float _LowestCloudAltitude;
        // Radius of the earth so that the dome falls exactly at the horizon
        public float _EarthRadius;

        // Stores (_HighestCloudAltitude + _EarthRadius)^2 and (_LowestCloudAltitude + _EarthRadius)^2
        public Vector2 _CloudRangeSquared;
        // Maximal primary steps that a ray can do
        public int _NumPrimarySteps;
        // Maximal number of light steps a ray can do
        public int _NumLightSteps;

        // Controls the tiling of the cloud map
        public Vector4 _CloudMapTiling;

        // Direction of the wind
        public Vector2 _WindDirection;
        // Displacement vector of the wind
        public Vector2 _WindVector;

        // Wind speed controllers
        public float _LargeWindSpeed;
        public float _MediumWindSpeed;
        public float _SmallWindSpeed;
        // Flag that tells us if we should apply the exposure to the sun light color (in case no directional is specified)
        public int _ExposureSunColor;

        // Color * intensity of the directional light
        public Vector4 _SunLightColor;
 
        // Direction to the sun
        public Vector4 _SunDirection;

        // Is the current sun a physically based one
        public int _PhysicallyBasedSun;
        // Factor for the multi scattering
        public float _MultiScattering;
        // Defines how we blend the forward and backward HG function
        public float _ScatteringDirection;
        // Controls the strength of the powder effect intensity
        public float _PowderEffectIntensity;

        // NormalizationFactor
        public float _NormalizationFactor;
        // Maximal cloud distance
        public float _MaxCloudDistance;
        // Global multiplier to the density
        public float _DensityMultiplier;
        // Controls the amount of low frenquency noise
        public float _ShapeFactor;

        // Controls the forward eccentricity of the clouds
        public float _ErosionFactor;
        // Multiplier to shape tiling
        public float _ShapeScale;
        // Multiplier to erosion tiling
        public float _ErosionScale;
        // Maximal temporal accumulation
        public float _TemporalAccumulationFactor;

        // Scattering Tint
        public Vector4 _ScatteringTint;

        // Resolution of the final size of the effect
        public Vector4 _FinalScreenSize;
        // Half/ Intermediate resolution
        public Vector4 _IntermediateScreenSize;
        // Quarter/Trace resolution
        public Vector4 _TraceScreenSize;
        // Resolution of the history buffer size
        public Vector2 _HistoryViewportSize;
        // Resolution of the history depth buffer
        public Vector2 _HistoryBufferSize;

        // MipOffset of the first depth mip
        public Vector2 _DepthMipOffset;
        // Frame index for the accumulation
        public int _AccumulationFrameIndex;
        // Index for which of the 4 local pixels should be evaluated
        public int _SubPixelIndex;

        [HLSLArray(7, typeof(Vector4))]
        public fixed float _AmbientProbeCoeffs[7 * 4];  // 3 bands of SH, packed, rescaled and convolved with the phase function

        // Right direction of the sun
        public Vector4 _SunRight;

        // Up direction of the sun
        public Vector4 _SunUp;

        // Intensity of the volumetric clouds shadow
        public float _ShadowIntensity;
        // Fallback intensity used when the shadow is not defined
        public float _ShadowFallbackValue;
        // The resolution of the shadow cookie to fill
        public int _ShadowCookieResolution;
        // Offset applied of the plane receiving the center of the shadow
        public float _ShadowPlaneOffset;

        // The size of the shadow region (meters)
        public Vector2 _ShadowRegionSize;

        // Padding
        public float _Padding0;
        public float _Padding1;

    }
}
