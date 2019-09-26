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
        private bool volumesIsDirty = true;
        public void RegisterVolume(ProbeVolume volume)
        {
            if (volumes.Contains(volume))
                return;

            volumes.Add(volume);
            volumesIsDirty = true;
        }
        public void DeRegisterVolume(ProbeVolume volume)
        {
            if (!volumes.Contains(volume))
                return;

            volumes.Remove(volume);
            volumesIsDirty = true;
        }
#if UNITY_EDITOR
        public void ReactivateProbes()
        {
            foreach (ProbeVolume v in volumes)
            {
                v.EnableBaking();
            }

            UnityEditor.Lightmapping.additionalBakedProbesCompleted -= ReactivateProbes;
        }
        public static void BakeSingle(ProbeVolume probeVolume)
        {
            if (!probeVolume)
                return;

            foreach (ProbeVolume v in manager.volumes)
            {
                if (v == probeVolume)
                    continue;

                v.DisableBaking();
            }

            UnityEditor.Lightmapping.additionalBakedProbesCompleted += manager.ReactivateProbes;
            UnityEditor.Lightmapping.BakeAsync();
        }
    }
#endif
}
