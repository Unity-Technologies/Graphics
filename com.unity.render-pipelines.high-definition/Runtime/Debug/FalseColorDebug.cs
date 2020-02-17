using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// False Color debug mpde settings.
    /// </summary>
    [Serializable]
    public class FalseColorDebugSettings
    {
        /// <summary>Enable false color mode.</summary>
        public bool falseColor = false;

        /// <summary>False color mode threshold 0.</summary>
        public float colorThreshold0 = 0.0f;
        /// <summary>False color mode threshold 1.</summary>
        public float colorThreshold1 = 2.0f;
        /// <summary>False color mode threshold 2.</summary>
        public float colorThreshold2 = 10.0f;
        /// <summary>False color mode threshold 3.</summary>
        public float colorThreshold3 = 20.0f;
    }
}
