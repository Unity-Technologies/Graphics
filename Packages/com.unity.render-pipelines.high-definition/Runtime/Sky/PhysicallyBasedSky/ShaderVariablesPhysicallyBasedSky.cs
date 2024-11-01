namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    enum PbrSkyConfig
    {
        // Tiny
        GroundIrradianceTableSize = 256, // <N, L>

        // 16 MiB
        InScatteredRadianceTableSizeX = 128, // <N, V>
        InScatteredRadianceTableSizeY = 32,  // height
        InScatteredRadianceTableSizeZ = 16,  // AzimuthAngle(L) w.r.t. the view vector
        InScatteredRadianceTableSizeW = 64,  // <N, L>,

        // 4 KiB
        MultiScatteringLutWidth = 32,
        MultiScatteringLutHeight = 32,

        // 144 KiB
        SkyViewLutWidth = 256,
        SkyViewLutHeight = 144,

        // 256 KiB
        AtmosphericScatteringLutWidth = 32,
        AtmosphericScatteringLutHeight = 32,
        AtmosphericScatteringLutDepth = 64,
    };

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)ConstantRegister.PBRSky)]
    unsafe struct ShaderVariablesPhysicallyBasedSky
    {
        // All the distance-related entries use SI units (meter, 1/meter, etc).
        public float _AtmosphericRadius;
        public float _AerosolAnisotropy;
        public float _AerosolPhasePartConstant;
        public float _AerosolSeaLevelExtinction;

        public float _AirDensityFalloff;
        public float _AirScaleHeight;
        public float _AerosolDensityFalloff;
        public float _AerosolScaleHeight;

        public Vector2 _OzoneScaleOffset;
        public float _OzoneLayerStart;
        public float _OzoneLayerEnd;

        public Vector4 _AirSeaLevelExtinction;
        public Vector4 _AirSeaLevelScattering;
        public Vector4 _AerosolSeaLevelScattering;
        public Vector4 _OzoneSeaLevelExtinction;
        public Vector4 _GroundAlbedo_PlanetRadius;
        public Vector4 _HorizonTint;
        public Vector4 _ZenithTint;

        public float _IntensityMultiplier;
        public float _ColorSaturation;
        public float _AlphaSaturation;
        public float _AlphaMultiplier;

        public float _HorizonZenithShiftPower;
        public float _HorizonZenithShiftScale;
        public uint _CelestialLightCount;
        public uint _CelestialBodyCount;

        public float _AtmosphericDepth;
        public float _RcpAtmosphericDepth;
        public float _CelestialLightExposure;
        public float _VolumetricCloudsBottomAltitude;
    }
}
