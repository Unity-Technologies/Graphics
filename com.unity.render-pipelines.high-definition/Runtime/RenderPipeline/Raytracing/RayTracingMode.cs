using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// This defines which version of an effect should be used.
    /// </summary>
    public enum RayTracingMode
    {
        Performance = 1 << 0,
        Quality = 1 << 1
    }

    [Serializable]
    public sealed class RayTracingModeParameter : VolumeParameter<RayTracingMode>
    {
        public RayTracingModeParameter(RayTracingMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
