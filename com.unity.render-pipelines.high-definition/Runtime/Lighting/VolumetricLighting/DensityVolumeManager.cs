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
            fluidSimVolumes = new List<FluidSimDensityVolume>(); //seongdae;fspm

            volumeAtlas = new Texture3DAtlas(TextureFormat.Alpha8, volumeTextureSize);
            volumeFluidSimAtlas = new Texture3DAtlas(TextureFormat.ARGB32, fluidSimVolumeTextureSize); //seongdae;fspm

            volumeAtlas.OnAtlasUpdated += AtlasUpdated;
            volumeFluidSimAtlas.OnAtlasUpdated += FluidSimAtlasUpdated; //seongdae;fspm
        }

        private List<DensityVolume> volumes = null;
        private List<FluidSimDensityVolume> fluidSimVolumes = null; //seongdae;fspm

        public void RegisterVolume(DensityVolume volume)
        {
            //volumes.Add(volume); //seongdae;fspm;origin
            //seongdae;fspm
            var fluidSimVolume = volume as FluidSimDensityVolume;
            if (fluidSimVolume != null)
                fluidSimVolumes.Add((FluidSimDensityVolume)volume);
            else
                volumes.Add(volume);
            //seongdae;fspm

            volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null)
            {
                volumeAtlas.AddTexture(volume.parameters.volumeMask);
            }
            //seongdae;fspm
            if (volume.parameters.volumeFluidSim != null)
            {
                volumeFluidSimAtlas.AddTexture(volume.parameters.volumeFluidSim);
            }
            //seongdae;fspm
        }

        public void DeRegisterVolume(DensityVolume volume)
        {
            //seongdae;fspm;origin
            //if (volumes.Contains(volume))
            //{
            //    volumes.Remove(volume);
            //}
            //seongdae;fspm;origin
            //seongdae;fspm
            var fluidSimVolume = volume as FluidSimDensityVolume;
            if (fluidSimVolume != null)
            {
                if (fluidSimVolumes.Contains(fluidSimVolume))
                    fluidSimVolumes.Remove(fluidSimVolume);
            }
            else
            {
                if (volumes.Contains(volume))
                    volumes.Remove(volume);
            }
            //seongdae;fspm

            volume.OnTextureUpdated -= TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null)
            {
                volumeAtlas.RemoveTexture(volume.parameters.volumeMask);
            }
            //seongdae;fspm
            if (volume.parameters.volumeFluidSim != null)
            {
                volumeFluidSimAtlas.RemoveTexture(volume.parameters.volumeFluidSim);
            }
            //seongdae;fspm

            //Upon removal we have to refresh the texture list.
            TriggerVolumeAtlasRefresh();
        }

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
        public FluidSimDensityVolume[] PrepareFluidSimDensityVolumeData(CommandBuffer cmd, Camera currentCam, float time)
        {
            return null;
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
            foreach (FluidSimDensityVolume volume in volumes)
            {
                volume.parameters.textureIndex = volumeFluidSimAtlas.GetTextureIndex(volume.parameters.volumeFluidSim);
            }
        }
        //seongdae;fspm
    }
}
