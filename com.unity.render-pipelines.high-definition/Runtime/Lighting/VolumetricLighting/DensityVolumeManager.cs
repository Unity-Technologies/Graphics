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

        public Texture3DAtlas volumeAtlas = null;
        public Texture3DAtlas volumeFluidSimAtlas = null; //seongdae;fspm
        private bool atlasNeedsRefresh = false;

        //TODO: hardcoded size....:-(
        public static int volumeTextureSize = 32;
        public static int fluidSimVolumeTextureSize = 64; //seongdae;fspm

        private DensityVolumeManager()
        {
            volumes = new List<DensityVolume>();
            fluidSimVolumes = new List<FluidSimVolume>(); //seongdae;fspm

            volumeAtlas = new Texture3DAtlas(TextureFormat.Alpha8, volumeTextureSize);
            volumeFluidSimAtlas = new Texture3DAtlas(TextureFormat.ARGB32, fluidSimVolumeTextureSize); //seongdae;fspm

            volumeAtlas.OnAtlasUpdated += AtlasUpdated;
            volumeFluidSimAtlas.OnAtlasUpdated += FluidSimAtlasUpdated; //seongdae;fspm
        }

        private List<DensityVolume> volumes = null;
        private List<FluidSimVolume> fluidSimVolumes = null; //seongdae;fspm

        public void RegisterVolume(DensityVolume volume)
        {
            volumes.Add(volume);

            volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null)
            {
                volumeAtlas.AddTexture(volume.parameters.volumeMask);
            }
        }

        //seongdae;fspm
        public void RegisterVolume(FluidSimVolume volume)
        {
            fluidSimVolumes.Add(volume);

            //volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeFluidSim != null)
            {
                volumeFluidSimAtlas.AddTexture(volume.parameters.volumeFluidSim);
            }
        }
        //seongdae;fspm

        public void DeRegisterVolume(DensityVolume volume)
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

            //Upon removal we have to refresh the texture list.
            TriggerVolumeAtlasRefresh();
        }

        //seongdae;fspm
        public void DeRegisterVolume(FluidSimVolume volume)
        {
            if (fluidSimVolumes.Contains(volume))
            {
                fluidSimVolumes.Remove(volume);
            }

            //volume.OnTextureUpdated -= TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeFluidSim != null)
            {
                volumeFluidSimAtlas.RemoveTexture(volume.parameters.volumeFluidSim);
            }

            //Upon removal we have to refresh the texture list.
            //TriggerVolumeAtlasRefresh();
        }
        //seongdae;fspm

        public DensityVolume[] PrepareDensityVolumeData(CommandBuffer cmd, Camera currentCam, float time)
        {
            //Update volumes
            bool animate = CoreUtils.AreAnimatedMaterialsEnabled(currentCam);
            foreach (DensityVolume volume in volumes)
            {
                volume.PrepareParameters(animate, time);
            }

            if (atlasNeedsRefresh)
            {
                atlasNeedsRefresh = false;
                VolumeAtlasRefresh();
            }

            volumeAtlas.GenerateAtlas(cmd);

            // GC.Alloc
            // List`1.ToArray()
            return volumes.ToArray();
        }

        //seongdae;fspm
        public FluidSimVolume[] PrepareFluidSimVolumeData(CommandBuffer cmd, Camera currentCam, float time)
        {
            return fluidSimVolumes.ToArray();
        }
        //seongdae;fspm

        private void VolumeAtlasRefresh()
        {
            volumeAtlas.ClearTextures();
            foreach (DensityVolume volume in volumes)
            {
                if (volume.parameters.volumeMask != null)
                {
                    volumeAtlas.AddTexture(volume.parameters.volumeMask);
                }
            }
        }

        public void TriggerVolumeAtlasRefresh()
        {
            atlasNeedsRefresh = true;
        }

        private void AtlasUpdated()
        {
            foreach (DensityVolume volume in volumes)
            {
                volume.parameters.textureIndex = volumeAtlas.GetTextureIndex(volume.parameters.volumeMask);
            }
        }

        //seongdae;fspm
        private void FluidSimAtlasUpdated()
        {
            foreach (FluidSimVolume volume in fluidSimVolumes)
            {
                volume.parameters.textureIndex = volumeFluidSimAtlas.GetTextureIndex(volume.parameters.volumeFluidSim);
            }
        }
        //seongdae;fspm
    }
}
