using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class DensityVolumeManager
    {
        public static readonly GraphicsFormat densityVolumeAtlasFormat = GraphicsFormat.R8G8B8A8_UNorm;

        static DensityVolumeManager m_Manager;
        public static DensityVolumeManager manager
        {
            get
            {
                if (m_Manager == null)
                    m_Manager = new DensityVolumeManager();
                return m_Manager;
            }
        }

        public Texture3DAtlas volumeAtlas = null;

        List<DensityVolume> m_Volumes = null;

        DensityVolumeManager()
        {
            var settings = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;
            m_Volumes = new List<DensityVolume>();
            volumeAtlas = new Texture3DAtlas(densityVolumeAtlasFormat, (int)settings.maxDensityVolumeSize, settings.maxDensityVolumesOnScreen);
        }


        public void RegisterVolume(DensityVolume volume)
        {
            m_Volumes.Add(volume);

            if (volume.parameters.volumeMask != null)
                volumeAtlas.AddTexture(volume.parameters.volumeMask);
        }

        public void DeRegisterVolume(DensityVolume volume)
        {
            if (m_Volumes.Contains(volume))
                m_Volumes.Remove(volume);

            if (volume.parameters.volumeMask != null)
                volumeAtlas.RemoveTexture(volume.parameters.volumeMask);
        }

        public bool ContainsVolume(DensityVolume volume) => m_Volumes.Contains(volume);

        public List<DensityVolume> PrepareDensityVolumeData(CommandBuffer cmd, HDCamera currentCam, float time)
        {
            //Update volumes
            bool animate = currentCam.animateMaterials;
            foreach (DensityVolume volume in m_Volumes)
                volume.PrepareParameters(animate, time);

            volumeAtlas.Update(cmd);

            return m_Volumes;
        }

        private void VolumeAtlasRefresh()
        {
            volumeAtlas.ClearTextures();
            foreach (DensityVolume volume in m_Volumes)
            {
                if (volume.parameters.volumeMask != null)
                {
                    volumeAtlas.AddTexture(volume.parameters.volumeMask);
                }
            }
        }

        // TODO: we'll need to remove texture index
        // private void AtlasUpdated()
        // {
        //     foreach (DensityVolume volume in volumes)
        //     {
        //         volume.parameters.textureIndex = volumeAtlas.GetTextureIndex(volume.parameters.volumeMask);
        //     }
        // }
    }
}
