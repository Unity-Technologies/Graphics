using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    struct HDProbeCullingResults
    {
        static readonly IReadOnlyList<HDProbe> s_EmptyList = new List<HDProbe>();

        List<HDProbe> m_VisibleProbes;

        public IReadOnlyList<HDProbe> visibleProbes => m_VisibleProbes ?? s_EmptyList;

        internal void Reset()
        {
            if (m_VisibleProbes == null)
                m_VisibleProbes = new List<HDProbe>();
            else
                m_VisibleProbes.Clear();
        }

        internal void AddProbe(HDProbe visibleProbes)
        {
            Assert.IsNotNull(m_VisibleProbes);

            m_VisibleProbes.Add(visibleProbes);
        }
    }
}
