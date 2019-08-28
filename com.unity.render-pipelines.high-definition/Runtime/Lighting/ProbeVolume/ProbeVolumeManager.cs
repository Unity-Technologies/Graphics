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
        private ProbeVolume[] volumesArray = null;
        private bool volumesArrayIsDirty = true;

        public void RegisterVolume(ProbeVolume volume)
        {
            volumes.Add(volume);
            volumesArrayIsDirty = true;
            
            // TODO
            //Vector4f scaleBias = new Vector4f(1,1,1,1);
            //volumeAtlas.AddTexture(volume);
        }

        public void DeRegisterVolume(ProbeVolume volume)
        {
            if (!volumes.Contains(volume))
                return;
            
            // TODO
        }

        public ProbeVolume[] PrepareProbeVolumeData(CommandBuffer cmd, Camera currentCam)
        {
            foreach (ProbeVolume volume in volumes)
            {
                volume.PrepareParameters();
            }

            if (volumesArrayIsDirty)
            {
                volumesArrayIsDirty = false;
                volumesArray = volumes.ToArray();
            }

            return volumesArray;
        }
    }
}
