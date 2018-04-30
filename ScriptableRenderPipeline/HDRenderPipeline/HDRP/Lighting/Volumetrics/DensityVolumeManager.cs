using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class DensityVolumeManager 
    {
        static private DensityVolumeManager _instance = null;

        public static DensityVolumeManager manager
        {
            get 
            {
                if (_instance == null)
                {
                    _instance = new DensityVolumeManager();
                }
                return _instance;
            }
        }

        public VolumeTextureAtlas volumeAtlas = null;
        private bool atlasNeedsRefresh = false;

        //TODO: hardcoded size....:-(
        public static int volumeTextureSize = 32;

        private DensityVolumeManager()
        {
            volumes = new List<HomogeneousDensityVolume>();

            volumeAtlas = new VolumeTextureAtlas(TextureFormat.RGBA32, volumeTextureSize);

            volumeAtlas.OnAtlasUpdated += AtlasUpdated;
        }

        private List<HomogeneousDensityVolume> volumes = null;

        public void RegisterVolume(HomogeneousDensityVolume volume)
        {
            volumes.Add(volume);

            volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null) 
            {
                volumeAtlas.AddTexture(volume.parameters.volumeMask); 
            }
        }

        public void DeRegisterVolume(HomogeneousDensityVolume volume)
        {
            if (volumes.Contains(volume))
            {
                volumes.Remove(volume);
            }

             volume.OnTextureUpdated -= TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null) 
            {
                volumeAtlas.RemoveTexture(volume.parameters.volumeMask);
            }
        }
        
        public HomogeneousDensityVolume[] PrepareDensityVolumeData(CommandBuffer cmd)
        {
            //Update volumes
            foreach (HomogeneousDensityVolume volume in volumes )
            {
                volume.PrepareParameters();
            }

            if (atlasNeedsRefresh)
            {
                atlasNeedsRefresh = false;
                VolumeAtlasRefresh();
            }

            volumeAtlas.GenerateVolumeAtlas(cmd);

          return volumes.ToArray();
        }

        private void VolumeAtlasRefresh()
        {
            volumeAtlas.ClearTextures();
            foreach (HomogeneousDensityVolume volume in volumes )
            {
                if (volume.parameters.volumeMask != null) 
                {
                    volumeAtlas.AddTexture(volume.parameters.volumeMask);
                }
            }
        }

        private void TriggerVolumeAtlasRefresh()
        {
            atlasNeedsRefresh = true;
        }

        private void AtlasUpdated()
        {
            foreach(HomogeneousDensityVolume volume in volumes )
            {
                volume.parameters.textureIndex = volumeAtlas.GetTextureIndex(volume.parameters.volumeMask); 
            }
        }
    }
}
