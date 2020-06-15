namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    public enum PbrSkyConfig
    {
        // Tiny
        GroundIrradianceTableSize = 256, // <N, L>

        // 32 MiB
        InScatteredRadianceTableSizeX = 128, // <N, V>
        InScatteredRadianceTableSizeY = 32,  // height
        InScatteredRadianceTableSizeZ = 16,  // AzimuthAngle(L) w.r.t. the view vector
        InScatteredRadianceTableSizeW = 64,  // <N, L>,
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

        public Vector3 _AirSeaLevelExtinction;
        public float _AerosolSeaLevelExtinction;

        public Vector3 _AirSeaLevelScattering;
        public float _IntensityMultiplier;

        public Vector3 _AerosolSeaLevelScattering;
        public float _ColorSaturation;

        public Vector3 _GroundAlbedo;
        public float _AlphaSaturation;

        public Vector3 _PlanetCenterPosition; // Not used during the precomputation, but needed to apply the atmospheric effect
        public float _AlphaMultiplier;

        public Vector3 _HorizonTint;
        public float _HorizonZenithShiftPower;

        public Vector3 _ZenithTint;
        public float _HorizonZenithShiftScale;
    }
}
