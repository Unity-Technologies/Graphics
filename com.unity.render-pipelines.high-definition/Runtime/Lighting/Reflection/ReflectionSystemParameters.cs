namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Holds settings for the reflection system.
    /// </summary>
    struct ReflectionSystemParameters
    {
        public static ReflectionSystemParameters Default = new ReflectionSystemParameters
        {
            maxPlanarReflectionProbePerCamera = 128,
            maxActivePlanarReflectionProbe = 512,
            planarReflectionProbeSize = 128,
            maxActiveEnvReflectionProbe = 512
        };

        /// <summary>
        /// Maximum number of planar reflection that can be found in a cull result.
        /// </summary>
        public int maxPlanarReflectionProbePerCamera;

        /// <summary>
        /// Maximum number of active planar reflection in the world.
        /// </summary>
        public int maxActivePlanarReflectionProbe;

        /// <summary>
        /// Size of the planar probe textures.
        /// </summary>
        public int planarReflectionProbeSize;

        /// <summary>
        /// Maximum number of active non planar reflection in the world.
        /// </summary>
        public int maxActiveEnvReflectionProbe;
    }
}
