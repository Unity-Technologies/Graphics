using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeVolumeManager : IProbeVolumeList
    {
        static private ProbeVolumeManager _instance = null;

        internal static ProbeVolumeManager manager
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ProbeVolumeManager();
                }
                return _instance;
            }
        }
        private ProbeVolumeManager()
        {
            m_Volumes = new List<ProbeVolume>();
            volumesSelected = new List<ProbeVolume>();

        #if UNITY_EDITOR
            SubscribeBakingAPI();
        #endif
        }

        ~ProbeVolumeManager()
        {
        #if UNITY_EDITOR
            UnsubscribeBakingAPI();
        #endif
        }

        List<ProbeVolume> m_Volumes = null;
        protected internal List<ProbeVolume> volumesSelected = null;

        List<IProbeVolumeList> m_AdditionalProbeLists = new List<IProbeVolumeList>();
        List<ProbeVolumeHandle> m_VolumeHandles = new List<ProbeVolumeHandle>();

        internal void UpdateVolumesToRender()
        {
            m_VolumeHandles.Clear();
            var count = m_Volumes.Count;
            for (int i = 0; i < count; i++)
                m_VolumeHandles.Add(new ProbeVolumeHandle(this, i));
            foreach (var list in m_AdditionalProbeLists)
            {
                list.ReleaseRemovedVolumesFromAtlas();
                count = list.GetVolumeCount();
                for (int i = 0; i < count; i++)
                    m_VolumeHandles.Add(new ProbeVolumeHandle(list, i));
            }
        }

        internal List<ProbeVolumeHandle> GetVolumesToRender() => m_VolumeHandles;

        internal void RegisterVolume(ProbeVolume volume)
        {
            if (m_Volumes.Contains(volume))
                return;

            m_Volumes.Add(volume);
        }
        internal void DeRegisterVolume(ProbeVolume volume)
        {
            var index = m_Volumes.IndexOf(volume);
            if (index == -1)
                return;

            var handle = new ProbeVolumeHandle(this, index);
            ReleaseVolumeFromAtlas(handle);
            volume.CleanupBuffers();
            ProbeVolumeDynamicGI.instance.CleanupPropagation(handle);

            m_Volumes.RemoveAt(index);
        }

        public void ReleaseVolumeFromAtlas(ProbeVolumeHandle volume)
        {
            if (RenderPipelineManager.currentPipeline is HDRenderPipeline hdrp)
                hdrp.ReleaseProbeVolumeFromAtlas(volume);
        }

        public void AddProbeList(IProbeVolumeList list)
        {
            m_AdditionalProbeLists.Add(list);
        }

        public void RemoveProbeList(IProbeVolumeList list)
        {
            m_AdditionalProbeLists.Remove(list);
        }

#if UNITY_EDITOR
        bool IProbeVolumeList.IsHiddenInScene(int i) => UnityEditor.SceneVisibilityManager.instance.IsHidden(m_Volumes[i].gameObject);

        void SubscribeBakingAPI()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted += OnProbesBakeCompleted;
            UnityEditor.Lightmapping.bakeCompleted += OnBakeCompleted;

            UnityEditor.Lightmapping.lightingDataCleared += OnLightingDataCleared;
            UnityEditor.Lightmapping.lightingDataAssetCleared += OnLightingDataAssetCleared;
        }

        void UnsubscribeBakingAPI()
        {
            if (ShaderConfig.s_ProbeVolumesEvaluationMode == ProbeVolumesEvaluationModes.Disabled)
                return;

            UnityEditor.Experimental.Lightmapping.additionalBakedProbesCompleted -= OnProbesBakeCompleted;
            UnityEditor.Lightmapping.bakeCompleted -= OnBakeCompleted;

            UnityEditor.Lightmapping.lightingDataCleared -= OnLightingDataCleared;
            UnityEditor.Lightmapping.lightingDataAssetCleared -= OnLightingDataAssetCleared;
        }

        void OnProbesBakeCompleted()
        {
            var volumesCurrent = (volumesSelected.Count > 0) ? volumesSelected : m_Volumes;
            foreach (var volume in volumesCurrent)
            {
                volume.OnProbesBakeCompleted();
            }
        }

        void OnBakeCompleted()
        {
            foreach (var volume in m_Volumes)
            {
                var index = m_Volumes.IndexOf(volume);
                if (index == -1)
                    continue;

                volume.OnBakeCompleted();

                // cleanup buffers
                var handle = new ProbeVolumeHandle(this, index);
                volume.CleanupBuffers();
                ProbeVolumeDynamicGI.instance.CleanupPropagation(handle);
            }

            if (volumesSelected.Count > 0)
            {
                // Go through and reenable all non-selected volumes now so that any following bakes will bake everything.
                foreach (ProbeVolume v in m_Volumes)
                {
                    if (volumesSelected.Contains(v))
                        continue;

                    v.ForceBakingEnabled();
                }

                volumesSelected.Clear();
            }
        }

        void OnLightingDataCleared()
        {
            volumesSelected.Clear();

            foreach (var volume in m_Volumes)
            {
                volume.OnLightingDataCleared();
            }
        }

        void OnLightingDataAssetCleared()
        {
            foreach (var volume in m_Volumes)
            {
                volume.OnLightingDataAssetCleared();
            }
        }

        internal static void BakeSelected()
        {
            manager.volumesSelected.Clear();

            foreach (GameObject go in UnityEditor.Selection.gameObjects)
            {
                ProbeVolume probeVolume = go.GetComponent<ProbeVolume>();
                if (probeVolume)
                    manager.volumesSelected.Add(probeVolume);
            }

            foreach (ProbeVolume v in manager.m_Volumes)
            {
                if (manager.volumesSelected.Contains(v))
                    continue;

                v.ForceBakingDisabled();
            }

            UnityEditor.Lightmapping.BakeAsync();
        }

        internal void DebugDrawNeighborhood(ProbeVolume probeVolume, Camera camera)
        {
            var index = m_Volumes.IndexOf(probeVolume);
            if (index != -1)
                ProbeVolumeDynamicGI.instance.DebugDrawNeighborhood(new ProbeVolumeHandle(this, index), camera);
        }
#endif

        void IProbeVolumeList.ReleaseRemovedVolumesFromAtlas() { }
        int IProbeVolumeList.GetVolumeCount() => m_Volumes.Count;
        bool IProbeVolumeList.IsAssetCompatible(int i) => m_Volumes[i].IsAssetCompatible();
        bool IProbeVolumeList.IsDataAssigned(int i) => m_Volumes[i].IsDataAssigned();
        bool IProbeVolumeList.IsDataUpdated(int i) => m_Volumes[i].GetDataIsUpdated();
        void IProbeVolumeList.SetDataUpdated(int i, bool value) => m_Volumes[i].SetDataIsUpdated(value);

        Vector3 IProbeVolumeList.GetPosition(int i) => m_Volumes[i].transform.position;
        ProbeVolumeArtistParameters IProbeVolumeList.GetParameters(int i) => m_Volumes[i].parameters;
        ProbeVolume.ProbeVolumeAtlasKey IProbeVolumeList.ComputeProbeVolumeAtlasKey(int i) => m_Volumes[i].ComputeProbeVolumeAtlasKey();
        int IProbeVolumeList.GetDataSHL01Length(int i) => m_Volumes[i].GetPayload().dataSHL01.Length;
        int IProbeVolumeList.GetDataSHL2Length(int i) => m_Volumes[i].GetPayload().dataSHL2.Length;
        int IProbeVolumeList.GetDataOctahedralDepthLength(int i) => m_Volumes[i].GetPayload().dataOctahedralDepth.Length;
        void IProbeVolumeList.SetDataOctahedralDepth(int i, ComputeBuffer buffer) => buffer.SetData(m_Volumes[i].GetPayload().dataOctahedralDepth);
        void IProbeVolumeList.EnsureVolumeBuffers(int i) => m_Volumes[i].EnsureVolumeBuffers();
        ref ProbeVolumePipelineData IProbeVolumeList.GetPipelineData(int i) => ref m_Volumes[i].pipelineData;

        // Dynamic GI
        int IProbeVolumeList.GetProbeVolumeEngineDataIndex(int i) => m_Volumes[i].m_ProbeVolumeEngineDataIndex;
        OrientedBBox IProbeVolumeList.GetProbeVolumeEngineDataBoundingBox(int i) => m_Volumes[i].m_BoundingBox;
        ProbeVolumeEngineData IProbeVolumeList.GetProbeVolumeEngineData(int i) => m_Volumes[i].m_EngineData;
        void IProbeVolumeList.ClearProbeVolumeEngineData(int i) => m_Volumes[i].ClearProbeVolumeEngineData();
        void IProbeVolumeList.SetProbeVolumeEngineData(int i, int dataIndex, in OrientedBBox box, in ProbeVolumeEngineData data) => m_Volumes[i].SetProbeVolumeEngineData(dataIndex, in box, in data);
        OrientedBBox IProbeVolumeList.ConstructOBBEngineData(int i, Vector3 camOffset)  => m_Volumes[i].ConstructOBBEngineData(camOffset);
        ref ProbePropagationBuffers IProbeVolumeList.GetPropagationBuffers(int i) => ref m_Volumes[i].m_PropagationBuffers;

        void IProbeVolumeList.SetLastSimulatedFrame(int i, int simulationFrameTick) => m_Volumes[i].SetLastSimulatedFrame(simulationFrameTick);
        int IProbeVolumeList.GetLastSimulatedFrame(int i) => m_Volumes[i].GetLastSimulatedFrame();

        bool IProbeVolumeList.HasNeighbors(int i)
        {
            bool hasNeighbors = false;
            if (m_Volumes[i].probeVolumeAsset != null)
            {
                var neighborAxis = m_Volumes[i].probeVolumeAsset.payload.neighborAxis;
                hasNeighbors = neighborAxis != null && neighborAxis.Length > 0;
            }
            return hasNeighbors;
        }

        int IProbeVolumeList.GetHitNeighborAxisLength(int i) => m_Volumes[i].GetPayload().hitNeighborAxis.Length;
        int IProbeVolumeList.GetNeighborAxisLength(int i) => m_Volumes[i].GetPayload().neighborAxis.Length;
        void IProbeVolumeList.SetHitNeighborAxis(int i, ComputeBuffer buffer) => buffer.SetData(m_Volumes[i].GetPayload().hitNeighborAxis);
        void IProbeVolumeList.SetNeighborAxis(int i, ComputeBuffer buffer) => buffer.SetData(m_Volumes[i].GetPayload().neighborAxis);
    }
}
