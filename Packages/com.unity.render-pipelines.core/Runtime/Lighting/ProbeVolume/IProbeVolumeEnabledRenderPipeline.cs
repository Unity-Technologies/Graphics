namespace UnityEngine.Rendering
{
    /// <summary>
    /// By implementing this interface, a render pipeline can indicate to its usage of the Adaptive Probe Volume system..
    /// </summary>
    public interface IProbeVolumeEnabledRenderPipeline
    {
        /// <summary>
        /// Indicates if this render pipeline instance supports Adaptive Probe Volume.
        /// </summary>
        bool supportProbeVolume { get; }

        /// <summary>
        /// Indicates the maximum number of SH Bands used by this render pipeline instance.
        /// </summary>
        ProbeVolumeSHBands maxSHBands { get; }

        /// <summary>
        /// Returns the projects global ProbeVolumeSceneData instance.
        /// </summary>
        [System.Obsolete("This field is no longer necessary")]
        ProbeVolumeSceneData probeVolumeSceneData { get; }
    }
}
