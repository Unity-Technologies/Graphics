using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal.U2D.Profiler
{
    class MeshRenderFrameDataProfilerEmitter
    {
        List<EntityId> m_FrameDataList = new List<EntityId>();
        int m_Tag;

        public MeshRenderFrameDataProfilerEmitter(int tag)
        {
            m_Tag = tag;
        }

        public void Capture(EntityId entityId)
        {
            if (!UnityEngine.Profiling.Profiler.enabled)
                return;
            m_FrameDataList.Add(entityId);
        }

        public void Emit()
        {
            if (!UnityEngine.Profiling.Profiler.enabled)
                return;
            UnityEngine.Profiling.Profiler.EmitFrameMetaData(ProfilerMarkers.k_2DGraphicProfilerProjectId, m_Tag, m_FrameDataList.ToArray());
            m_FrameDataList.Clear();
        }
    }
}
