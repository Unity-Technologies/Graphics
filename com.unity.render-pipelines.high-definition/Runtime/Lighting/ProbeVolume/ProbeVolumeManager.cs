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

        // TODO: Store payload atlas.
        // public Texture3DAtlas volumeAtlas = null;
        // private bool atlasNeedsRefresh = false;

        //TODO: hardcoded size....:-(
        // public static int volumeTextureSize = 32;

        private ProbeVolumeManager()
        {
            volumes = new List<ProbeVolume>();

            // volumeAtlas = new Texture3DAtlas(TextureFormat.Alpha8, volumeTextureSize);

            // volumeAtlas.OnAtlasUpdated += AtlasUpdated;
        }

        private List<ProbeVolume> volumes = null;
        private ProbeVolume[] volumesArray = null;
        private bool volumesArrayIsDirty = true;

        public void RegisterVolume(ProbeVolume volume)
        {
            volumes.Add(volume);
            volumesArrayIsDirty = true;

            // volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;

            // if (volume.parameters.volumeMask != null)
            // {
            //     volumeAtlas.AddTexture(volume.parameters.volumeMask);
            // }
        }

        public void DeRegisterVolume(ProbeVolume volume)
        {
            if (volumes.Contains(volume))
            {
                volumes.Remove(volume);
                volumesArrayIsDirty = true;
            }

            // volume.OnTextureUpdated -= TriggerVolumeAtlasRefresh;

            // if (volume.parameters.volumeMask != null)
            // {
            //     volumeAtlas.RemoveTexture(volume.parameters.volumeMask);
            // }

            //Upon removal we have to refresh the texture list.
            // TriggerVolumeAtlasRefresh();
        }

        public ProbeVolume[] PrepareProbeVolumeData(CommandBuffer cmd, Camera currentCam)
        {
            foreach (ProbeVolume volume in volumes)
            {
                volume.PrepareParameters();
            }

            // if (atlasNeedsRefresh)
            // {
            //     atlasNeedsRefresh = false;
            //     VolumeAtlasRefresh();
            // }

            // volumeAtlas.GenerateAtlas(cmd);

            if (volumesArrayIsDirty)
            {
                volumesArrayIsDirty = false;

                // GC.Alloc
                // List`1.ToArray()
                volumesArray = volumes.ToArray();
            }

            return volumesArray;
        }

        // private void VolumeAtlasRefresh()
        // {
        //     volumeAtlas.ClearTextures();
        //     foreach (ProbeVolume volume in volumes)
        //     {
        //         if (volume.parameters.volumeMask != null)
        //         {
        //             volumeAtlas.AddTexture(volume.parameters.volumeMask);
        //         }
        //     }
        // }

        // public void TriggerVolumeAtlasRefresh()
        // {
        //     atlasNeedsRefresh = true;
        // }

        // private void AtlasUpdated()
        // {
        //     foreach (ProbeVolume volume in volumes)
        //     {
        //         volume.parameters.textureIndex = volumeAtlas.GetTextureIndex(volume.parameters.volumeMask);
        //     }
        // }
    }
}
