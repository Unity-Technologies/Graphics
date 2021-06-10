namespace UnityEngine.Rendering.HighDefinition
{
    public sealed partial class HDAdditionalReflectionData
    {
        // We use the legacy ReflectionProbe for the culling system
        // So we need to update its influence (center, size) so the culling behave properly

        ReflectionProbe m_LegacyProbe;
        /// <summary>Get the sibling component ReflectionProbe</summary>
        ReflectionProbe reflectionProbe
        {
            get
            {
                if (m_LegacyProbe == null || m_LegacyProbe.Equals(null))
                {
                    m_LegacyProbe = GetComponent<ReflectionProbe>();
                }
                return m_LegacyProbe;
            }
        }

        /// <summary>
        /// Prepare the culling phase by settings the appropriate values to the legacy reflection probe component.
        /// The culling system is driven by the legacy probe's values.
        /// </summary>
        public override void PrepareCulling()
        {
            base.PrepareCulling();
            var influence = settings.influence;
            var tr = transform;
            var position = tr.position;
            var cubeProbe = reflectionProbe;

            if (cubeProbe == null || cubeProbe.Equals(null))
            {
                // case 1244047
                // This can happen when removing the component from the editor and then undo the remove.
                // The order of call maybe incorrect and the code flows here before the reflection probe
                // is restored.
                return;
            }

            switch (influence.shape)
            {
                case InfluenceShape.Box:
                    cubeProbe.size = influence.boxSize;
                    cubeProbe.center = Vector3.zero;
                    break;
                case InfluenceShape.Sphere:
                    cubeProbe.size = Vector3.one * (2 * influence.sphereRadius);
                    cubeProbe.center = Vector3.zero;
                    break;
            }

            // Reassign back the position
            // If we updated ReflectionProbe.center, it will have moved the transform
            // But we only want to update the ReflectionProbe.center property
            // So we need to restore the position after the update.
            tr.position = position;

            // Force the legacy system to not update the probe
            cubeProbe.mode = ReflectionProbeMode.Custom;
            cubeProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
#if UNITY_2020_2_OR_NEWER
            if (m_ProbeSettings.mode == ProbeSettings.Mode.Realtime)
                cubeProbe.renderDynamicObjects = true;
#endif
        }
    }
}
