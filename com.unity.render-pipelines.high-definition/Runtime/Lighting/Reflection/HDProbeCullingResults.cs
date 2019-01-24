using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public struct HDProbeCullingResults
    {
        static readonly IReadOnlyList<HDProbe> s_EmptyList = new List<HDProbe>();

        List<HDProbe> m_VisibleProbes;

        public IReadOnlyList<HDProbe> visibleProbes => m_VisibleProbes ?? s_EmptyList;
        internal List<HDProbe> writeableVisibleProbes => m_VisibleProbes;

        internal void Reset()
        {
            if (m_VisibleProbes == null)
                m_VisibleProbes = new List<HDProbe>();
            else
                m_VisibleProbes.Clear();
        }

        internal void Set(List<HDProbe> visibleProbes)
        {
            Assert.IsNotNull(m_VisibleProbes);

            m_VisibleProbes.AddRange(visibleProbes);
        }
    }
}
