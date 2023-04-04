using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// System used for skipping culling. It is used with <see cref="Graphics.DrawMesh"/> as it already handles culling.
    /// </summary>
    internal class DecalSkipCulledSystem
    {
        private DecalEntityManager m_EntityManager;
        private ProfilingSampler m_Sampler;
        private Camera m_Camera;


        public DecalSkipCulledSystem(DecalEntityManager entityManager)
        {
            m_EntityManager = entityManager;
            m_Sampler = new ProfilingSampler("DecalSkipCulledSystem.Execute");
        }

        public void Execute(Camera camera)
        {
            using (new ProfilingScope(m_Sampler))
            {
                m_Camera = camera;
                for (int i = 0; i < m_EntityManager.chunkCount; ++i)
                    Execute(m_EntityManager.culledChunks[i], m_EntityManager.culledChunks[i].count);
            }
        }

        private void Execute(DecalCulledChunk culledChunk, int count)
        {
            if (count == 0)
                return;

            culledChunk.currentJobHandle.Complete();

            for (int i = 0; i < count; ++i)
                culledChunk.visibleDecalIndices[i] = i;
            culledChunk.visibleDecalCount = count;
            culledChunk.cameraPosition = m_Camera.transform.position;
            culledChunk.cullingMask = m_Camera.cullingMask;
#if UNITY_EDITOR
            culledChunk.sceneCullingMask = GetSceneCullingMaskFromCamera(m_Camera);
#endif
        }

        internal static UInt64 GetSceneCullingMaskFromCamera(Camera camera)
        {
#if UNITY_EDITOR
            if (camera.overrideSceneCullingMask != 0)
                return camera.overrideSceneCullingMask;

            if (camera.scene.IsValid())
                return UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(camera.scene);

            switch (camera.cameraType)
            {
                case CameraType.SceneView:
                    return UnityEditor.SceneManagement.SceneCullingMasks.MainStageSceneViewObjects;
                default:
                    return UnityEditor.SceneManagement.SceneCullingMasks.GameViewObjects;
            }
#else
            return 0;
#endif
        }
    }
}
