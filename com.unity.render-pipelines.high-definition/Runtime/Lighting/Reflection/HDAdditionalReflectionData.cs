namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Additional component used to store settings for HDRP's reflection probes.
    /// </summary>
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Reflection-Probe" + Documentation.endURL)]
//forest-begin: Explicit reflection probe tracking
	[ExecuteInEditMode]
//forest-end:
    [RequireComponent(typeof(ReflectionProbe))]
    public sealed partial class HDAdditionalReflectionData : HDProbe
    {
        void Awake()
        {
            type = ProbeSettings.ProbeType.ReflectionProbe;
            k_ReflectionProbeMigration.Migrate(this);
        }
//forest-begin: Explicit reflection probe tracking
		static public System.Collections.Generic.List<ReflectionProbe> s_ActiveReflectionProbes = new System.Collections.Generic.List<ReflectionProbe>();

		void OnEnable()
        {
			s_ActiveReflectionProbes.Add(GetComponent<ReflectionProbe>());
        }

		void OnDisable()
        {
			s_ActiveReflectionProbes.Remove(GetComponent<ReflectionProbe>());
		}
//forest-end:
    }

    /// <summary>
    /// Utilities for reflection probes.
    /// </summary>
    public static class HDAdditionalReflectionDataExtensions
    {
        /// <summary>
        /// Request to render this probe next update.
        ///
        /// Call this method with the mode <see cref="ProbeSettings.RealtimeMode.OnDemand"/> and the probe will
        /// be rendered the next time it will influence a camera rendering.
        ///
        /// If the probe don't have a <see cref="HDAdditionalReflectionData"/> component, nothing is done.
        /// </summary>
        /// <param name="probe">The probe to request a render.</param>
        public static void RequestRenderNextUpdate(this ReflectionProbe probe)
        {
            var add = probe.GetComponent<HDAdditionalReflectionData>();
            if (add != null && !add.Equals(null))
                add.RequestRenderNextUpdate();
        }
    }
}
