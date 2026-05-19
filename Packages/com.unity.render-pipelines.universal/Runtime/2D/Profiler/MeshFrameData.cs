#if ENABLE_PROFILER && PROFILER_INSTALLED
using System;
using System.Collections.Generic;
using Unity.Profiling;

namespace UnityEngine.Rendering.Universal.U2D.Profiler
{
    [Serializable]
    struct MeshFrameData
    {
        public EntityId gameObjectEntityId;
        public int triangleCount;
        public int vertexCount;
    }

    class MeshFrameDataProfilerEmitter
    {
        List<MeshFrameData> m_FrameDataList = new List<MeshFrameData>();
        int m_TriangleCount = 0;
        int m_Tag;
        ProfilerCounterValue<int> m_MeshCounter;

        public MeshFrameDataProfilerEmitter(int tag, ProfilerCounterValue<int> meshCounter)
        {
            m_Tag = tag;
            m_MeshCounter = meshCounter;
        }

        public void Capture(GameObject go, Mesh mesh)
        {
            if (!UnityEngine.Profiling.Profiler.enabled)
                return;

            // Don't add duplicates
            for (int i = 0; i < m_FrameDataList.Count; ++i)
            {
                if (m_FrameDataList[i].gameObjectEntityId == go.GetEntityId())
                    return;
            }    

            if (mesh != null)
            {
                var meshData = Mesh.AcquireReadOnlyMeshData(mesh);
                var triangleCount = 0;
                var vertexCount = 0;
                for (int i = 0; i < meshData.Length; i++)
                {
                    var m = meshData[i];
                    int indexCount = 0;

                    if (m.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16)
                    {
                        var indexData = m.GetIndexData<ushort>();
                        indexCount = indexData.Length;
                    }
                    else
                    {
                        var indexData = m.GetIndexData<uint>();
                        indexCount = indexData.Length;
                    }
                    triangleCount += indexCount / 3;
                    vertexCount += m.vertexCount;
                }
                m_TriangleCount += triangleCount;
                var frameData = new MeshFrameData()
                {
                    gameObjectEntityId = go.GetEntityId(),
                    triangleCount = triangleCount,
                    vertexCount = vertexCount,
                };
                m_FrameDataList.Add(frameData);
                meshData.Dispose();
            }
        }

        public void Emit()
        {
            if (!UnityEngine.Profiling.Profiler.enabled)
                return;
            UnityEngine.Profiling.Profiler.EmitFrameMetaData(ProfilerMarkers.k_2DGraphicProfilerProjectId, m_Tag, m_FrameDataList.ToArray());
            m_MeshCounter.Value = m_TriangleCount;
            m_TriangleCount = 0;
            m_FrameDataList.Clear();
        }
    }
}
#endif
