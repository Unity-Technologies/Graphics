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
        int m_MaxDensityVolumeSize;
        int m_MaxDensityVolumeOnScreen;

        DensityVolumeManager()
        {
            var settings = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;
            m_MaxDensityVolumeSize = (int)settings.maxDensityVolumeSize;
            m_MaxDensityVolumeOnScreen = settings.maxDensityVolumesOnScreen;
            m_Volumes = new List<DensityVolume>();
            Debug.Log(m_MaxDensityVolumeSize);
            Debug.Log(m_MaxDensityVolumeOnScreen);
            volumeAtlas = new Texture3DAtlas(densityVolumeAtlasFormat, m_MaxDensityVolumeSize, m_MaxDensityVolumeOnScreen);
        }

        public void RegisterVolume(DensityVolume volume)
        {
            m_Volumes.Add(volume);

            if (volume.parameters.volumeMask != null)
            {
                if (volumeAtlas.IsTextureValid(volume.parameters.volumeMask))
                {
                    if (!volumeAtlas.AddTexture(volume.parameters.volumeMask))
                        Debug.LogError($"No more space in the density volume atlas, consider increasing the max density volume on screen in the HDRP asset.");
                }
            }
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

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateDensityVolumeAtlas)))
            {
                volumeAtlas.Update(cmd);
            }

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
