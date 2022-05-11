using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Monitor debug settings
    /// </summary>
    [Serializable]
    public class MonitorsDebugSettings
    {
        /// <summary>
        /// Size ratio of the monitors
        /// </summary>
        public float monitorsSize = 0.3f;

        /// <summary>
        /// Toggles the waveform monitor
        /// </summary>
        public bool waveformToggle;

        /// <summary>
        /// The exposure multiplier applied to the waveform values.
        /// Must be positive.
        /// </summary>
        public float waveformExposure = 0.12f;

        /// <summary>
        /// Toggles parade mode for the waveform monitor
        /// </summary>
        public bool waveformParade;

        /// <summary>
        /// Toggles the vectorscope monitor
        /// </summary>
        public bool vectorscopeToggle;

        /// <summary>
        /// The exposure multiplier applied to the vectorscope values.
        /// Must be positive.
        /// </summary>
        public float vectorscopeExposure = 0.12f;
    }
}
