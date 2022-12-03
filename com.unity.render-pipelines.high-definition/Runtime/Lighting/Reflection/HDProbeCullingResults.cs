using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    class HDProbeCullingResults
    {
        static readonly IReadOnlyList<HDProbe> s_EmptyList = new List<HDProbe>();

        List<HDProbe> m_VisibleProbes = new List<HDProbe>();

        public IReadOnlyList<HDProbe> visibleProbes => m_VisibleProbes;

        internal void Reset()
        {
            m_VisibleProbes.Clear();
        }

        internal void AddProbe(HDProbe visibleProbes)
        {
            Assert.IsNotNull(m_VisibleProbes);

            m_VisibleProbes.Add(visibleProbes);
        }
    }
}
