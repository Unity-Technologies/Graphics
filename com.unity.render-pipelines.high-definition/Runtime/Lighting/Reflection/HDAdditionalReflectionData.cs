namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Additional component used to store settings for HDRP's reflection probes.
    /// </summary>
    [HDRPHelpURLAttribute("Reflection-Probe")]
    [AddComponentMenu("")] // Hide in menu
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ReflectionProbe))]
    public sealed partial class HDAdditionalReflectionData : HDProbe, IAdditionalData
    {
        void Awake()
        {
            type = ProbeSettings.ProbeType.ReflectionProbe;
            k_ReflectionProbeMigration.Migrate(this);
        }
    }

    /// <summary>
    /// Utilities for reflection probes.
    /// </summary>
    public static class HDAdditionalReflectionDataExtensions
    {
        /// <summary>
        /// Requests that Unity renders the passed in Reflection Probe during the next update.
        /// </summary>
        /// <remarks>
        /// If you call this method for a Reflection Probe using <see cref="ProbeSettings.RealtimeMode.OnDemand"/> mode, Unity renders the probe the next time the probe influences a Camera rendering.
        ///
        /// If the Reflection Probe doesn't have an attached <see cref="HDAdditionalReflectionData"/> component, calling this function has no effect.
        ///
        /// Note: If any part of a Camera's frustum intersects a Reflection Probe's influence volume, the Reflection Probe influences the Camera.
        /// </remarks>
        /// <param name="probe">The Reflection Probe to request a render for.</param>
        public static void RequestRenderNextUpdate(this ReflectionProbe probe)
        {
            var add = probe.GetComponent<HDAdditionalReflectionData>();
            if (add != null && !add.Equals(null))
                add.RequestRenderNextUpdate();
        }
    }
}
