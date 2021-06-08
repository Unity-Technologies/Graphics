using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class LocalVolumetricFogManager
    {
        public static readonly GraphicsFormat localVolumetricFogAtlasFormat = GraphicsFormat.R8G8B8A8_UNorm;

        static LocalVolumetricFogManager m_Manager;
        public static LocalVolumetricFogManager manager
        {
            get
            {
                if (m_Manager == null)
                    m_Manager = new LocalVolumetricFogManager();
                return m_Manager;
            }
        }

        Texture3DAtlas m_VolumeAtlas = null;
        public Texture3DAtlas volumeAtlas
        {
            get
            {
                if (m_VolumeAtlas == null && HDRenderPipeline.isReady)
                {
                    var settings = HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.lightLoopSettings;

                    // Prevent allocating too big textures:
                    int elementCount = Texture3DAtlas.GetMaxElementCountForWeightInByte(
                        HDRenderPipeline.k_MaxCacheSize,
                        (int)settings.maxLocalVolumetricFogSize,
                        settings.maxLocalVolumetricFogOnScreen,
                        localVolumetricFogAtlasFormat,
                        true
                    );

                    elementCount = Mathf.Clamp(elementCount, 1, settings.maxLocalVolumetricFogOnScreen);

                    m_VolumeAtlas = new Texture3DAtlas(localVolumetricFogAtlasFormat, (int)settings.maxLocalVolumetricFogSize, elementCount);

                    // When HDRP is initialized and this atlas created, some Local Volumetric Fog may have been initialized before so we add them here.
                    foreach (var volume in m_Volumes)
                    {
                        if (volume.parameters.volumeMask != null)
                            AddTextureIntoAtlas(volume.parameters.volumeMask);
                    }
                }

                return m_VolumeAtlas;
            }
        }

        List<LocalVolumetricFog> m_Volumes = null;

        LocalVolumetricFogManager()
        {
            m_Volumes = new List<LocalVolumetricFog>();
        }

        public void RegisterVolume(LocalVolumetricFog volume)
        {
            m_Volumes.Add(volume);

            // In case the Local Volumetric Fog format is not support (which is impossible because all HDRP target supports R8G8B8A8_UNorm)
            // we avoid doing operations on the atlas.
            // This happens in the CI on linux when an editor using OpenGL is building a player for Vulkan.
            if (!SystemInfo.IsFormatSupported(localVolumetricFogAtlasFormat, FormatUsage.LoadStore))
                return;

            if (volume.parameters.volumeMask != null && volumeAtlas != null)
            {
                if (volumeAtlas.IsTextureValid(volume.parameters.volumeMask))
                {
                    AddTextureIntoAtlas(volume.parameters.volumeMask);
                }
            }
        }

        internal void AddTextureIntoAtlas(Texture volumeTexture)
        {
            if (!volumeAtlas.AddTexture(volumeTexture))
                Debug.LogError($"No more space in the Local Volumetric Fog atlas, consider increasing the max Local Volumetric Fog on screen in the HDRP asset.");
        }

        public void DeRegisterVolume(LocalVolumetricFog volume)
        {
            if (m_Volumes.Contains(volume))
                m_Volumes.Remove(volume);

            // In case the Local Volumetric Fog format is not support (which is impossible because all HDRP target supports R8G8B8A8_UNorm)
            // we avoid doing operations on the atlas.
            // This happens in the CI on linux when an editor using OpenGL is building a player for Vulkan.
            if (!SystemInfo.IsFormatSupported(localVolumetricFogAtlasFormat, FormatUsage.LoadStore))
                return;

            if (volume.parameters.volumeMask != null)
            {
                // Avoid to alloc the atlas to remove a texture if it's not allocated yet.
                if (m_VolumeAtlas != null)
                    volumeAtlas.RemoveTexture(volume.parameters.volumeMask);
            }
        }

        public bool ContainsVolume(LocalVolumetricFog volume) => m_Volumes.Contains(volume);

        public List<LocalVolumetricFog> PrepareLocalVolumetricFogData(CommandBuffer cmd, HDCamera currentCam)
        {
            //Update volumes
            float time = currentCam.time;
            foreach (LocalVolumetricFog volume in m_Volumes)
                volume.PrepareParameters(time);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateLocalVolumetricFogAtlas)))
            {
                volumeAtlas.Update(cmd);
            }

            return m_Volumes;
        }

        // Note that this function will not release the manager itself as it have to live outside of HDRP to handle Local Volumetric Fog components
        internal void ReleaseAtlas()
        {
            // Release the atlas so next time the manager is used, it is reallocated with new HDRP settings.
            if (m_VolumeAtlas != null)
            {
                volumeAtlas.Release();
                m_VolumeAtlas = null;
            }
        }
    }
}
