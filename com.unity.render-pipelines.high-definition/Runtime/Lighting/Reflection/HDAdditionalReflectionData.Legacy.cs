namespace UnityEngine.Experimental.Rendering.HDPipeline
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
                if(m_LegacyProbe == null || m_LegacyProbe.Equals(null))
                {
                    m_LegacyProbe = GetComponent<ReflectionProbe>();
                }
                return m_LegacyProbe;
            }
        }

        public override void PrepareCulling()
        {
            base.PrepareCulling();
            var influence = settings.influence;
            var tr = transform;
            var position = tr.position;
            var cubeProbe = reflectionProbe;
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
        }
    }
}
