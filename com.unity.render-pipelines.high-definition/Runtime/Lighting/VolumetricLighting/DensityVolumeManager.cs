using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class DensityVolumeManager
    {
        static private DensityVolumeManager _instance = null;
        public delegate void ComputeShaderParamsDelegate(List<DensityVolume> volumes, CommandBuffer cmd, RTHandle atlas);
        public static ComputeShaderParamsDelegate DynamicDensityVolumeCallback;

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

        public DensityVolumeAtlas volumeAtlas = null;
        private bool atlasNeedsRefresh = false;

        private DensityVolumeManager()
        {
            volumes = new List<DensityVolume>();

            volumeAtlas = new DensityVolumeAtlas();
        }

        private List<DensityVolume> volumes = null;

        public void RegisterVolume(DensityVolume volume)
        {
            volumes.Add(volume);

            volume.OnTextureUpdated += TriggerVolumeAtlasRefresh;
            volumeAtlas.AddVolume(volume);
        }

        public void DeRegisterVolume(DensityVolume volume)
        {
            if (volumes.Contains(volume))
            {
                volumes.Remove(volume);
            }

            volume.OnTextureUpdated -= TriggerVolumeAtlasRefresh;

            if (volume.parameters.volumeMask != null)
            {
                volumeAtlas.RemoveVolume(volume);
            }

            //Upon removal we have to refresh the texture list.
            TriggerVolumeAtlasRefresh();
        }

        public bool ContainsVolume(DensityVolume volume) => volumes.Contains(volume);

        public List<DensityVolume> PrepareDensityVolumeData(CommandBuffer cmd, HDCamera currentCam, ComputeShader blit3dShader, Vector3Int atlasResolution)
        {
            //Update volumes
            float time = currentCam.time;
            foreach (DensityVolume volume in volumes)
            {
                volume.PrepareParameters(time);
            }

            if (atlasNeedsRefresh)
            {
                atlasNeedsRefresh = false;
                VolumeAtlasRefresh();
            }

            volumeAtlas.UpdateAtlas(cmd, blit3dShader, atlasResolution);

            return volumes;
        }

        private void VolumeAtlasRefresh()
        {
            volumeAtlas.ClearVolumes();
            foreach (DensityVolume volume in volumes)
            {
                volumeAtlas.AddVolume(volume);
            }
        }

        public void TriggerVolumeAtlasRefresh()
        {
            atlasNeedsRefresh = true;
        }
    }
}
