namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    enum PbrSkyConfig
    {
        // Tiny
        GroundIrradianceTableSize = 256, // <N, L>

        // 32 MiB
        InScatteredRadianceTableSizeX = 128, // <N, V>
        InScatteredRadianceTableSizeY = 32,  // height
        InScatteredRadianceTableSizeZ = 16,  // AzimuthAngle(L) w.r.t. the view vector
        InScatteredRadianceTableSizeW = 64,  // <N, L>,

        // 4 KiB
        MultiScatteringLutWidth = 32,
        MultiScatteringLutHeight = 32,
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)ConstantRegister.PBRSky)]
    unsafe struct ShaderVariablesPhysicallyBasedSky
    {
        // All the distance-related entries use SI units (meter, 1/meter, etc).
        public float _PlanetaryRadius;
        public float _RcpPlanetaryRadius;
        public float _AtmosphericDepth;
        public float _RcpAtmosphericDepth;

        public float _AtmosphericRadius;
        public float _AerosolAnisotropy;
        public float _AerosolPhasePartConstant;
        public float _Unused;

        public float _AirDensityFalloff;
        public float _AirScaleHeight;
        public float _AerosolDensityFalloff;
        public float _AerosolScaleHeight;

        public Vector4 _AirSeaLevelExtinction;
        public Vector4 _AirSeaLevelScattering;
        public Vector4 _AerosolSeaLevelScattering;
        public Vector4 _GroundAlbedo;
        public Vector4 _PlanetCenterPosition; // Not used during the precomputation, but needed to apply the atmospheric effect
        public Vector4 _HorizonTint;
        public Vector4 _ZenithTint;

        public float _AerosolSeaLevelExtinction;
        public float _IntensityMultiplier;
        public float _ColorSaturation;
        public float _AlphaSaturation;

        public float _AlphaMultiplier;
        public float _HorizonZenithShiftPower;
        public float _HorizonZenithShiftScale;
        public float _Unused2;
    }
}
