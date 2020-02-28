using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    public class ProbeVolumeManager
    {
        static private ProbeVolumeManager _instance = null;

        public static ProbeVolumeManager manager
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
        }

        public List<ProbeVolume> volumes = null;

        public void RegisterVolume(ProbeVolume volume)
        {
            if (volumes.Contains(volume))
                return;

            volumes.Add(volume);
        }
        public void DeRegisterVolume(ProbeVolume volume)
        {
            if (!volumes.Contains(volume))
                return;

            volumes.Remove(volume);

            HDRenderPipeline hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            if (hdrp != null)
                hdrp.ReleaseProbeVolumeFromAtlas(volume);
        }
#if UNITY_EDITOR
        public void ReactivateProbes()
        {
            foreach (ProbeVolume v in volumes)
            {
                v.EnableBaking();
            }

            UnityEditor.Lightmapping.bakeCompleted -= ReactivateProbes;
        }
        public static void BakeSingle()
        {
            List<ProbeVolume> selectedProbeVolumes = new List<ProbeVolume>();

            foreach (GameObject go in UnityEditor.Selection.gameObjects)
            {
                ProbeVolume probeVolume = go.GetComponent<ProbeVolume>();
                if (probeVolume)
                    selectedProbeVolumes.Add(probeVolume);
            }

            foreach (ProbeVolume v in manager.volumes)
            {
                if (selectedProbeVolumes.Contains(v))
                    continue;

                v.DisableBaking();
            }

            UnityEditor.Lightmapping.bakeCompleted += manager.ReactivateProbes;
            UnityEditor.Lightmapping.BakeAsync();
        }
#endif
    }
}
