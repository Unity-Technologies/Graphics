using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public struct GlobalOrderIndependentTransparencySettings
    {
        internal static GlobalOrderIndependentTransparencySettings NewDefault() => new GlobalOrderIndependentTransparencySettings()
        {
            enabled = true,
            memoryBudget = 16.0f
        };

        public bool enabled;
        public float memoryBudget;
    }
}
