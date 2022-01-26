using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    // This structure holds all the information that can be requested during the deferred water lighting
    [GenerateHLSL(PackingRules.Exact, false)]
    struct WaterSurfaceProfile
    {
        public Vector3 waterAmbientProbe;
        public float tipScatteringHeight;

        public float bodyScatteringHeight;
        public float maxRefractionDistance;
        public uint lightLayers;
        public float padding0;

        // Refraction data Data
        public Vector3 transparencyColor;
        public float outScatteringCoefficient;
    }

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

        // Maximal horizontal displacements due to each wave
        public Vector4 _WaveDisplacement;

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

        // Smoothness of the simulation foam
        public float _SimulationFoamSmoothness;
        // Controls the amount of drag of the simulation foam
        public float _JacobianDrag;
        // Amount of surface foam
        public float _SimulationFoamAmount;
        // TODO WRITE
        public float _SSSMaskCoefficient;

        // Maximal horizontal displacement
        public float _MaxWaveDisplacement;
        public float _ScatteringBlur;
        // Maximum refraction distance
        public float _MaxRefractionDistance;
        // Smoothness of the water part of the surface (non foam)
        public float _WaterSmoothness;

        // Horizontal offsets of the foam texture
        public Vector2 _FoamOffsets;
        // Tiling parameter of the foam texture
        public float _FoamTilling;
        // Attenuation of the foam due to the wind
        public float _WindFoamAttenuation;

        // Color applied to the surfaces that are through the refraction
        public Vector4 _TransparencyColor;

        public Vector4 _ScatteringColorTips;

        public float _DisplacementScattering;
        public int _WaterInitialFrame;
        public int _SurfaceIndex;
        public float _CausticsRegionSize;

        public Vector4 _ScatteringLambertLighting;

        public Vector4 _DeepFoamColor;

        public float _OutScatteringCoefficient;
        public float _FoamSmoothness;
        public float _HeightBasedScattering;
        public float _WindSpeedMultiplier;

        public Vector4 _FoamJacobianLambda;

        public Vector2 _PaddinwW0;
        public int _PaddinwW1;
        public int _WaterSampleOffset;
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

        // Number of LODs used to render infinite water surfaces
        public uint _WaterLODCount;
        // Number of water patches that need to be rendered
        public uint _NumWaterPatches;
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

        // Blend distance
        public float _CausticsPlaneBlendDistance;
        // Type of caustics that are rendered
        public int _WaterCausticsType;
        // Which decal layers should affect this surface
        public uint _WaterDecalLayer;
        // Is this surface infinite or finite
        public int _InfiniteSurface;

        // Max tessellation factor
        public float _WaterMaxTessellationFactor;
        // Distance at which the fade of the tesselation starts
        public float _WaterTessellationFadeStart;
        // Size of the range of the tessellation
        public float _WaterTessellationFadeRange;
        // Padding
        public int _PaddingWR1;
    }
}
