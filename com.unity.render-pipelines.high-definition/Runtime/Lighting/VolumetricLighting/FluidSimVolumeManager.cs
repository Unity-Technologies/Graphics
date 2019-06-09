using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class FluidSimVolumeManager
    {
        static private FluidSimVolumeManager _instance = null;

        public static FluidSimVolumeManager manager
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new FluidSimVolumeManager();
                }
                return _instance;
            }
        }

        public Texture3DAtlas volumeAtlas = null; //seongdae;fspm
        private bool atlasNeedsRefresh = false;

        //TODO: hardcoded size....:-(
        public static int fluidSimVolumeTextureSize = 256; //seongdae;fspm

        private FluidSimVolumeManager()
        {
            volumes = new List<FluidSimVolume>(); //seongdae;fspm

            volumeAtlas = new Texture3DAtlas(TextureFormat.RGBA32, fluidSimVolumeTextureSize); //seongdae;fspm

            volumeAtlas.OnAtlasUpdated += FluidSimAtlasUpdated; //seongdae;fspm
        }

        private List<FluidSimVolume> volumes = null; //seongdae;fspm

        public void RegisterVolume(FluidSimVolume volume)
        {
            volumes.Add(volume);

            //volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;

            if (volume.parameters.initialStateTexture != null)
            {
                volumeAtlas.AddTexture(volume.parameters.initialStateTexture);
            }
        }

        public void DeRegisterVolume(FluidSimVolume volume)
        {
            if (volumes.Contains(volume))
            {
                volumes.Remove(volume);
            }

            //volume.OnTextureUpdated -= TriggerVolumeAtlasRefresh;

            if (volume.parameters.initialStateTexture != null)
            {
                volumeAtlas.RemoveTexture(volume.parameters.initialStateTexture);
            }

            //Upon removal we have to refresh the texture list.
            //TriggerVolumeAtlasRefresh();
        }

        public FluidSimVolume[] PrepareFluidSimVolumeData(CommandBuffer cmd, Camera currentCam, float time)
        {
            return volumes.ToArray();
        }

        public void TriggerVolumeAtlasRefresh()
        {
            atlasNeedsRefresh = true;
        }

        private void FluidSimAtlasUpdated()
        {
            foreach (FluidSimVolume volume in volumes)
            {
                volume.parameters.textureIndex = volumeAtlas.GetTextureIndex(volume.parameters.initialStateTexture);
            }
        }
    }
}
