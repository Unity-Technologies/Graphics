namespace UnityEngine.Rendering
{
    /// <summary>
    /// By implementing this interface, a render pipeline can indicate to its usage of the Adaptive Probe Volume system..
    /// </summary>
    public interface IProbeVolumeEnabledRenderPipeline
    {
        /// <summary>
        /// Indicates the maximum number of SH Bands used by this render pipeline instance.
        /// </summary>
        ProbeVolumeSHBands maxSHBands { get; }

        /// <summary>
        /// Returns the projects global ProbeVolumeSceneData instance.
        /// </summary>
        ProbeVolumeSceneData probeVolumeSceneData { get; }
    }
}
