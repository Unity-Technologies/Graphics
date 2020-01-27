using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This defines the ray tracing feature sets that should be used.
    /// </summary>
    public enum RayTracingTier
    {
        Tier1 = 1 << 0,
        Tier2 = 1 << 1
    }

    [Serializable]
    public sealed class RayTracingTierParameter : VolumeParameter<RayTracingTier>
    {
        public RayTracingTierParameter(RayTracingTier value, bool overrideState = false) : base(value, overrideState) { }
    }
}
