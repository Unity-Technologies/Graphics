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
            sortingEnabled = true,
        };

        public bool enabled;
        public float memoryBudget;
        public OITLightingMode oitLightingMode;
        public int maxHiZMip;
        public bool sortingEnabled;
    }
}
