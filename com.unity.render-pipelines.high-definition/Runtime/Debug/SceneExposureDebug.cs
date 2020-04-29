using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Color Picker Debug Mode.
    /// </summary>
    [GenerateHLSL]
    public enum ExposureDebugMode
    {
        /// <summary>No exposure debug.</summary>
        None,
        /// <summary>TODO_FCC ADD TODO.</summary>
        SceneEV100Values,
        /// <summary>TODO_FCC ADD TODO</summary>
        HistogramView,
        /// <summary>TODO_FCC Visualize metering</summary>
        MeteringWeighted,

    }

    /// <summary>
    /// Exposure debug settings.
    /// </summary>
    [Serializable]
    public class ExposureDebugSettings
    {
        /// <summary>
        /// Exposure picker mode.
        /// </summary>
        public ExposureDebugMode debugMode = ExposureDebugMode.None;
    }
}
