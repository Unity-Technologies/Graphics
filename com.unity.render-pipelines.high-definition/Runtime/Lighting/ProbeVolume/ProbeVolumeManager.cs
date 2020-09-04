using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class ProbeVolumeManager
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
            volumes = new List<ProbeVolume>();
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

        internal List<ProbeVolume> volumes = null;
        protected internal List<ProbeVolume> volumesSelected = null;

        internal void RegisterVolume(ProbeVolume volume)
        {
            if (volumes.Contains(volume))
                return;

            volumes.Add(volume);
        }
        internal void DeRegisterVolume(ProbeVolume volume)
        {
            if (!volumes.Contains(volume))
                return;

            volumes.Remove(volume);

            HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp != null)
                hdrp.ReleaseProbeVolumeFromAtlas(volume);
        }

#if UNITY_EDITOR
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
            var volumesCurrent = (volumesSelected.Count > 0) ? volumesSelected : volumes;
            foreach (var volume in volumesCurrent)
            {
                volume.OnProbesBakeCompleted();
            }
        }

        void OnBakeCompleted()
        {
            var volumesCurrent = (volumesSelected.Count > 0) ? volumesSelected : volumes;
            foreach (var volume in volumes)
            {
                volume.OnBakeCompleted();
            }

            if (volumesSelected.Count > 0)
            {
                // Go through and reenable all non-selected volumes now so that any following bakes will bake everything.
                foreach (ProbeVolume v in volumes)
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

            foreach (var volume in volumes)
            {
                volume.OnLightingDataCleared();
            }
        }

        void OnLightingDataAssetCleared()
        {
            foreach (var volume in volumes)
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

            foreach (ProbeVolume v in manager.volumes)
            {
                if (manager.volumesSelected.Contains(v))
                    continue;

                v.ForceBakingDisabled();
            }

            UnityEditor.Lightmapping.BakeAsync();
        }
#endif
    }
}
