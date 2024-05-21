using System;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesClouds
    {
        // The highest altitude clouds can reach in meters
        public float _HighestCloudAltitude;
        // The lowest altitude clouds can reach in meters
        public float _LowestCloudAltitude;
        // The closest distance to the cloud layer
        public float _CloudNearPlane;
        // Render the clouds in camera space
        public float _CameraSpace;

        // Maximal primary steps that a ray can do
        public int _NumPrimarySteps;
        // Maximal number of light steps a ray can do
        public int _NumLightSteps;

        // The size of the shadow region (meters)
        public Vector2 _ShadowRegionSize;

        // Direction of the wind
        public Vector2 _WindDirection;
        // Displacement vector of the wind
        public Vector2 _WindVector;

        // Offset Applied when applying the shaping (X,Z)
        public Vector2 _ShapeNoiseOffset;
        // Displacement of the wind vertically for the shaping
        public float _VerticalShapeWindDisplacement;
        // Displacement of the wind vertically for the erosion
        public float _VerticalErosionWindDisplacement;

        // Vertical shape noise offset
        public float _VerticalShapeNoiseOffset;
        // Wind speed controllers
        public float _LargeWindSpeed;
        public float _MediumWindSpeed;
        public float _SmallWindSpeed;

        // Color * intensity of the directional light
        public Vector4 _SunLightColor;

        // Direction to the sun
        public Vector4 _SunDirection;

        // Controls the tiling of the cloud map
        public Vector4 _CloudMapTiling;

        // Factor for the multi scattering
        public float _MultiScattering;
        // Controls the strength of the powder effect intensity
        public float _PowderEffectIntensity;
        // NormalizationFactor
        public float _NormalizationFactor;
        // Global multiplier to the density
        public float _DensityMultiplier;

        // Controls the amount of shaping
        public float _ShapeFactor;
        // Multiplier to shape tiling
        public float _ShapeScale;
        //  Controls the amount of micro details
        public float _MicroErosionFactor;
        // Multiplier to micro details tiling
        public float _MicroErosionScale;

        // Strength of the erosion occlusion
        public float _ErosionOcclusion;
        // Controls the amount of erosion
        public float _ErosionFactor;
        // Multiplier to erosion tiling
        public float _ErosionScale;
        // Modifier of the history accumulation
        public float _CloudHistoryInvalidation;

        // Scattering Tint
        public Vector4 _ScatteringTint;

        // Resolution of the final size of the effect
        public Vector4 _FinalScreenSize;
        // Half/ Intermediate resolution
        public Vector4 _IntermediateScreenSize;
        // Quarter/Trace resolution
        public Vector4 _TraceScreenSize;
        // Resolution of the history buffer size
        public Vector2 _HistoryViewportScale;
        // Offset in depth pyramid
        public Vector2Int _ReprojDepthMipOffset;

        // Flag that defines if the clouds should be evaluated at full resolution
        public int _LowResolutionEvaluation;
        // Flag that defines if the we should enable integration, checkerboard rendering, etc.
        public int _EnableIntegration;
        // Flag that allows us to know if the scene depth is available
        public int _ValidSceneDepth;
        // Defines the ratio between intermediate res and output res
        public uint _IntermediateResolutionScale;

        // Frame index for the accumulation
        public int _AccumulationFrameIndex;
        // Index for which of the 4 local pixels should be evaluated
        public int _SubPixelIndex;
        // Factor to decode previous depth from history buffer
        public float _NearPlaneReprojection;
        // Max step size for raymarching
        public float _MaxStepSize;

        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float _CloudsPixelCoordToViewDirWS[ShaderConfig.k_XRMaxViewsForCBuffer * 16];
        [HLSLArray(ShaderConfig.k_XRMaxViewsForCBuffer, typeof(Matrix4x4))]
        public fixed float _CameraPrevViewProjection[ShaderConfig.k_XRMaxViewsForCBuffer * 16];

        // Controls the intensity of the wind distortion at high altitudes
        public float _AltitudeDistortion;
        // Internal parameters that compensates the erosion factor to match between the different erosion noises
        public float _ErosionFactorCompensation;
        // Fast tone mapping settings
        public int _EnableFastToneMapping;
        // Maximal temporal accumulation
        public float _TemporalAccumulationFactor;

        // Fade in parameters
        public float _FadeInStart;
        public float _FadeInDistance;
        // Flag that allows to know if we should be using the improved transmittance blending
        public float _ImprovedTransmittanceBlend;
        public float _PaddingVC0;

        [HLSLArray(3 * 4, typeof(Vector4))]
        public fixed float _DistanceBasedWeights[12 * 4];
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCloudsShadows
    {
        // Intensity of the volumetric clouds shadow
        public float _ShadowIntensity;
        public float _PaddingVCS0;
        // The resolution of the shadow cookie to fill
        public int _ShadowCookieResolution;
        public float _PaddingVCS1;

        // World Camera Position used as the constant buffer has not been injected yet when this data is required, last channel is unused.
        public float4 _CloudShadowSunOrigin;

        // Right direction of the sun
        public float4 _CloudShadowSunRight;

        // Up direction of the sun
        public float4 _CloudShadowSunUp;

        // Forward direction of the sun
        public float4 _CloudShadowSunForward;

        // Camera position in planet space
        public float4 _CameraPositionPS;
    }
}
