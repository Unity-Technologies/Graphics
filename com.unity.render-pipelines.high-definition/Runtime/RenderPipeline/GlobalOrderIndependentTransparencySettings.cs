using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public enum OITLightingMode
    {
        ForwardFast,
        DeferredSSTracing
    }

    [Serializable]
    public struct GlobalOrderIndependentTransparencySettings
    {
        internal static GlobalOrderIndependentTransparencySettings NewDefault() => new GlobalOrderIndependentTransparencySettings()
        {
            enabled = true,
            memoryBudget = 16.0f,
            oitLightingMode = OITLightingMode.ForwardFast,
            maxHiZMip = 4,
            enableAccumulation = false,
            accumulationCoef = 0.5f
        };

        public bool enabled;
        public float memoryBudget;
        public OITLightingMode oitLightingMode;
        public int maxHiZMip;
        public bool enableAccumulation;
        [Range(0.0f, 1.0f)]
        public float accumulationCoef;
    }
}
