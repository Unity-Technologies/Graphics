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

        internal List<ProbeVolumeHandle> CollectVolumesToRender()
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
            return m_VolumeHandles;
        }

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

            ReleaseVolumeFromAtlas(new ProbeVolumeHandle(this, index));
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
            var volumesCurrent = (volumesSelected.Count > 0) ? volumesSelected : m_Volumes;
            foreach (var volume in m_Volumes)
            {
                volume.OnBakeCompleted();
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
#endif
        
        void IProbeVolumeList.ReleaseRemovedVolumesFromAtlas() { }
        int IProbeVolumeList.GetVolumeCount() => m_Volumes.Count;
        bool IProbeVolumeList.IsAssetCompatible(int i) => m_Volumes[i].IsAssetCompatible();
        bool IProbeVolumeList.IsDataAssigned(int i) => m_Volumes[i].IsDataAssigned();
        bool IProbeVolumeList.IsDataUpdated(int i) => m_Volumes[i].GetDataIsUpdated();
        Vector3 IProbeVolumeList.GetPosition(int i) => m_Volumes[i].transform.position;
        Quaternion IProbeVolumeList.GetRotation(int i) => m_Volumes[i].transform.rotation;
        ref ProbeVolumeArtistParameters IProbeVolumeList.GetParameters(int i) => ref m_Volumes[i].parameters;
        int IProbeVolumeList.GetAtlasID(int i) => m_Volumes[i].GetAtlasID();
        int IProbeVolumeList.GetBakeID(int i) => m_Volumes[i].GetBakeID();
        ProbeVolume.ProbeVolumeAtlasKey IProbeVolumeList.ComputeProbeVolumeAtlasKey(int i) => m_Volumes[i].ComputeProbeVolumeAtlasKey();
        ProbeVolume.ProbeVolumeAtlasKey IProbeVolumeList.GetProbeVolumeAtlasKeyPrevious(int i) => m_Volumes[i].GetProbeVolumeAtlasKeyPrevious();
        void IProbeVolumeList.SetProbeVolumeAtlasKeyPrevious(int i, ProbeVolume.ProbeVolumeAtlasKey key) => m_Volumes[i].SetProbeVolumeAtlasKeyPrevious(key);
        int IProbeVolumeList.GetDataSHL01Length(int i) => m_Volumes[i].GetPayload().dataSHL01.Length;
        int IProbeVolumeList.GetDataSHL2Length(int i) => m_Volumes[i].GetPayload().dataSHL2.Length;
        int IProbeVolumeList.GetDataOctahedralDepthLength(int i) => m_Volumes[i].GetPayload().dataOctahedralDepth.Length;
        void IProbeVolumeList.SetDataSHL01(int i, ComputeBuffer buffer) => buffer.SetData(m_Volumes[i].GetPayload().dataSHL01);
        void IProbeVolumeList.SetDataSHL2(int i, ComputeBuffer buffer) => buffer.SetData(m_Volumes[i].GetPayload().dataSHL2);
        void IProbeVolumeList.SetDataValidity(int i, ComputeBuffer buffer) => buffer.SetData(m_Volumes[i].GetPayload().dataValidity);
        void IProbeVolumeList.SetDataOctahedralDepth(int i, ComputeBuffer buffer) => buffer.SetData(m_Volumes[i].GetPayload().dataOctahedralDepth);
    }
}
