using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionProbeCullResults
    {
        int[] m_PlanarReflectionProbeIndices;
        PlanarReflectionProbe[] m_VisiblePlanarReflectionProbes;

        public int visiblePlanarReflectionProbeCount { get; private set; }
        public PlanarReflectionProbe[] visiblePlanarReflectionProbes { get { return m_VisiblePlanarReflectionProbes; } }

        internal ReflectionProbeCullResults(ReflectionSystemParameters parameters)
        {
            Assert.IsTrue(parameters.maxPlanarReflectionProbes >= 0, "Maximum number of planar reflection probe must be positive");

            visiblePlanarReflectionProbeCount = 0;

            m_PlanarReflectionProbeIndices = new int[parameters.maxPlanarReflectionProbes];
            m_VisiblePlanarReflectionProbes = new PlanarReflectionProbe[parameters.maxPlanarReflectionProbes];
        }

        public void CullPlanarReflectionProbes(CullingGroup cullingGroup, PlanarReflectionProbe[] planarReflectionProbes)
        {
            visiblePlanarReflectionProbeCount = cullingGroup.QueryIndices(true, m_PlanarReflectionProbeIndices, 0);
            for (var i = 0; i < visiblePlanarReflectionProbeCount; ++i)
                m_VisiblePlanarReflectionProbes[i] = planarReflectionProbes[m_PlanarReflectionProbeIndices[i]];
        }
    }
}
