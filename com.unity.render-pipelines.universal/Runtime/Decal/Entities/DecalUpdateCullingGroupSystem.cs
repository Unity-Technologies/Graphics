using System;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Contains culling results.
    /// </summary>
    internal class DecalCulledChunk : DecalChunk
    {
        public Vector3 cameraPosition;
        public ulong sceneCullingMask;
        public int cullingMask;

        public CullingGroup cullingGroups;
        public int[] visibleDecalIndexArray;
        public NativeArray<int> visibleDecalIndices;
        public int visibleDecalCount;

        public override void RemoveAtSwapBack(int entityIndex)
        {
            RemoveAtSwapBack(ref visibleDecalIndexArray, entityIndex, count);
            RemoveAtSwapBack(ref visibleDecalIndices, entityIndex, count);
            count--;
        }

        public override void SetCapacity(int newCapacity)
        {
            ArrayExtensions.ResizeArray(ref visibleDecalIndexArray, newCapacity);
            visibleDecalIndices.ResizeArray(newCapacity);
            if (cullingGroups == null)
                cullingGroups = new CullingGroup();
            capacity = newCapacity;
        }

        public override void Dispose()
        {
            if (capacity == 0)
                return;

            visibleDecalIndices.Dispose();
            visibleDecalIndexArray = null;
            count = 0;
            capacity = 0;
            cullingGroups.Dispose();
            cullingGroups = null;
        }
    }

    /// <summary>
    /// Issues culling job with <see cref="CullingGroup"/>.
    /// </summary>
    internal class DecalUpdateCullingGroupSystem
    {
        /// <summary>
        /// Provides acces to the bounding distance.
        /// </summary>
        public float boundingDistance
        {
            get { return m_BoundingDistance[0]; }
            set { m_BoundingDistance[0] = value; }
        }

        private float[] m_BoundingDistance = new float[1];
        private Camera m_Camera;
        private DecalEntityManager m_EntityManager;
        private ProfilingSampler m_Sampler;

        public DecalUpdateCullingGroupSystem(DecalEntityManager entityManager, float drawDistance)
        {
            m_EntityManager = entityManager;
            m_BoundingDistance[0] = drawDistance;
            m_Sampler = new ProfilingSampler("DecalUpdateCullingGroupsSystem.Execute");
        }

        public void Execute(Camera camera)
        {
            using (new ProfilingScope(m_Sampler))
            {
                m_Camera = camera;
                for (int i = 0; i < m_EntityManager.chunkCount; ++i)
                    Execute(m_EntityManager.cachedChunks[i], m_EntityManager.culledChunks[i], m_EntityManager.culledChunks[i].count);
            }
        }

        public void Execute(DecalCachedChunk cachedChunk, DecalCulledChunk culledChunk, int count)
        {
            cachedChunk.currentJobHandle.Complete();

            CullingGroup cullingGroup = culledChunk.cullingGroups;
            cullingGroup.targetCamera = m_Camera;
            cullingGroup.SetDistanceReferencePoint(m_Camera.transform.position);
            cullingGroup.SetBoundingDistances(m_BoundingDistance);
            cachedChunk.boundingSpheres.CopyTo(cachedChunk.boundingSphereArray);
            cullingGroup.SetBoundingSpheres(cachedChunk.boundingSphereArray);
            cullingGroup.SetBoundingSphereCount(count);

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
