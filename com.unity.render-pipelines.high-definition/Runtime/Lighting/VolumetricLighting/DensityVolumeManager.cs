using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    public class DensityVolumeManager
    {
        static private DensityVolumeManager _instance = null;
        public delegate void ComputeShaderParamsDelegate(DensityVolume volume, ComputeShader shader, CommandBuffer cmd);
        internal static Dictionary<DensityVolume, ComputeShaderParamsDelegate> ComputeShaderParams = new Dictionary<DensityVolume, ComputeShaderParamsDelegate>();
        public static void AddComputeShaderCallback(DensityVolume volume, ComputeShaderParamsDelegate callback)
        {
            ComputeShaderParams[volume] = callback;
        }

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
        private bool atlasNeedsRefresh = false;

        //TODO: hardcoded size....:-(
        public static int volumeTextureSize = 48;

        private DensityVolumeManager()
        {
            volumes = new List<DensityVolume>();

            volumeAtlas = new Texture3DAtlas(TextureFormat.Alpha8, volumeTextureSize);

            volumeAtlas.OnAtlasUpdated += AtlasUpdated;
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

        public List<DensityVolume> PrepareDensityVolumeData(CommandBuffer cmd, HDCamera currentCam, float time, ComputeShader blit3dShader)
        {
            //Update volumes
            bool animate = currentCam.animateMaterials;
            foreach (DensityVolume volume in volumes)
            {
                volume.PrepareParameters(animate, time);
            }

            if (atlasNeedsRefresh)
            {
                atlasNeedsRefresh = false;
                VolumeAtlasRefresh();
            }

            volumeAtlas.UpdateAtlas(cmd, blit3dShader);

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

        private void AtlasUpdated()
        {
            //foreach (DensityVolume volume in volumes)
            //{
            //    volume.parameters.textureIndex = volumeAtlas.GetTextureIndex(volume.parameters.volumeMask);
            //}
        }
    }
}
