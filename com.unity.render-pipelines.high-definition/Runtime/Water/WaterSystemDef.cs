using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWater
    {
        // Resolution at which the simulation is evaluated
        public uint _BandResolution;
        // Maximal wave height of the current setup
        public float _MaxWaveHeight;
        // Current simulation time
        public float _SimulationTime;
        // Controls how much the wind affect the current of the waves
        public float _DirectionDampener;

        // Individual heights of the wave bands.
        public Vector4 _WaveAmplitude;

        // Individual sizes of the wave bands
        public Vector4 _BandPatchSize;

        // Wind speed per band
        public Vector4 _WindSpeed;

        // Horizontal wind direction
        public Vector2 _WindDirection;
        // Amount of choppiness
        public float _Choppiness;
        // Delta-time since the last simulation step
        public float _DeltaTime;

        // Intensity of the surface foam
        public float _SurfaceFoamIntensity;
        // Amount of surface foam
        public float _SurfaceFoamAmount;
        // Amount of deep foam
        public float _DeepFoamAmount;
        // TODO WRITE
        public float _SSSMaskCoefficient;

        // Padding
        public Vector2 _PaddingW0;
        // Maximum refraction distance
        public float _MaxRefractionDistance;
        // Smoothness of the water part of the surface (non foam)
        public float _WaterSmoothness;

        // Horizonal offsets of the foam texture
        public Vector2 _FoamOffsets;
        // Tiling parameter of the foam texture
        public float _FoamTilling;
        // Attenuation of the foam due to the wind
        public float _WindFoamAttenuation;

        // Color applied to the surfaces that are through the refraction
        public Vector4 _TransparencyColor;

        public Vector4 _ScatteringColorTips;

        public float _DispersionAmount;
        public float _RefractionLow;
        public float _MaxAbsorptionDistance;
        public float _ScatteringBlur;


        public float _DisplacementScattering;
        public float _ScatteringIntensity;
        public float _BodyScatteringWeight;
        public float _TipScatteringWeight;

        public Vector4 _ScatteringLambertLighting;

        public Vector4 _DeepFoamColor;

        public float _OutScatteringCoefficient;
        public float _FoamSmoothness;
        public float _HeightBasedScattering;
        public float _PaddingW1;

        public Vector4 _FoamJacobianLambda;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesWaterRendering
    {
        // Horizontal size of the grid in the horizontal plane
        public Vector2 _GridSize;
        // Rotation of the water geometry
        public Vector2 _WaterRotation;

        // Offset of the current patch w/r to the origin
        public Vector4 _PatchOffset;
        // Ambient probe used to render the water
        public Vector4 _WaterAmbientProbe;

        // Resolution (in quads) of the current water patch
        public uint _GridRenderingResolution;
        // Mask that defines the tessellation pattern
        public uint _TesselationMasks;
        // Earth radius
        public float _EarthRadius;
        // Intensity of the water caustics
        public float _CausticsIntensity;

        // Scale of the current water mask
        public Vector2 _WaterMaskScale;
        // Offset of the current water mask
        public Vector2 _WaterMaskOffset;

        // Scale of the current foam mask
        public Vector2 _FoamMaskScale;
        // Offset of the current foam mask
        public Vector2 _FoamMaskOffset;

        // Offsets of the caustics pattern
        public Vector2 _CausticsOffset;
        // Tiling factor of the caustics
        public float _CausticsTiling;
        // Vertical shift on when the caustics start
        public float _CausticsPlaneOffset;
    }
}
