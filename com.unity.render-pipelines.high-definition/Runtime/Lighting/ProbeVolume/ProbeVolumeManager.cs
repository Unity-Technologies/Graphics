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

        public Texture2DAtlas volumeAtlas = null;

        private ProbeVolumeManager()
        {
            volumes = new List<ProbeVolume>();
            volumeAtlas = new Texture2DAtlas(1024, 1024, GraphicsFormat.B10G11R11_UFloatPack32);
        }

        private List<ProbeVolume> volumes = null;
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

        public List<ProbeVolume> PrepareProbeVolumeData(CommandBuffer cmd, Camera currentCam)
        {
            foreach (ProbeVolume volume in volumes)
            {
                volume.PrepareParameters();
            }

            if (volumesIsDirty)
            {
                foreach (ProbeVolume volume in volumes)
                {
                    if (volume.ProbeVolumeTexture != null)
                    {
                        volumeAtlas.AddTexture(cmd, ref volume.parameters.scaleBias, volume.ProbeVolumeTexture);
                    }
                }

                volumesIsDirty = false;
            }

            return volumes;
        }
    }
}
