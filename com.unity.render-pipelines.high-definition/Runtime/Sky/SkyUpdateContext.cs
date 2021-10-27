using System;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class SkyUpdateContext
    {
        SkySettings m_SkySettings;
        public SkyRenderer skyRenderer { get; private set; }
        public int cachedSkyRenderingContextId = -1;

        CloudSettings m_CloudSettings;
        public CloudRenderer cloudRenderer { get; private set; }

        public int skyParametersHash = -1;
        public float currentUpdateTime = 0.0f;

        VolumetricClouds m_VolumetricClouds;

        public bool settingsHadBigDifferenceWithPrev { get; private set; }

        public SkySettings skySettings
        {
            get { return m_SkySettings; }
            set
            {
                // We cleanup the renderer first here because in some cases, after scene unload, the skySettings field will be "null" because the object got destroyed.
                // In this case, the renderer might stay allocated until a non null value is set. To avoid a lingering allocation, we cleanup first before anything else.
                // So next frame after scene unload, renderer will be freed.
                if (skyRenderer != null && (value == null || value.GetSkyRendererType() != skyRenderer.GetType()))
                {
                    skyRenderer.Cleanup();
                    skyRenderer = null;
                }

                if (m_SkySettings == null)
                    settingsHadBigDifferenceWithPrev = true;
                else
                    settingsHadBigDifferenceWithPrev = m_SkySettings.SignificantlyDivergesFrom(value);

                if (m_SkySettings == value)
                    return;

                skyParametersHash = -1;
                m_SkySettings = value;
                currentUpdateTime = 0.0f;

                if (m_SkySettings != null && skyRenderer == null)
                {
                    var rendererType = m_SkySettings.GetSkyRendererType();
                    skyRenderer = (SkyRenderer)Activator.CreateInstance(rendererType);
                    skyRenderer.Build();
                }
            }
        }

        public CloudSettings cloudSettings
        {
            get { return m_CloudSettings; }
            set
            {
                if (cloudRenderer != null && (value == null || value.GetCloudRendererType() != cloudRenderer.GetType()))
                {
                    cloudRenderer.Cleanup();
                    cloudRenderer = null;
                }

                if (m_CloudSettings == value)
                    return;

                skyParametersHash = -1;
                m_CloudSettings = value;

                if (m_CloudSettings != null && cloudRenderer == null)
                {
                    var rendererType = m_CloudSettings.GetCloudRendererType();
                    cloudRenderer = (CloudRenderer)Activator.CreateInstance(rendererType);
                    cloudRenderer.Build();
                }
            }
        }

        public VolumetricClouds volumetricClouds
        {
            get { return m_VolumetricClouds; }
            set
            {
                if (m_VolumetricClouds == value)
                    return;

                m_VolumetricClouds = value;

                if (m_CloudSettings != null && cloudRenderer == null)
                {
                    var rendererType = m_CloudSettings.GetCloudRendererType();
                    cloudRenderer = (CloudRenderer)Activator.CreateInstance(rendererType);
                    cloudRenderer.Build();
                }
            }
        }

        public void Cleanup()
        {
            if (skyRenderer != null)
                skyRenderer.Cleanup();

            if (cloudRenderer != null)
                cloudRenderer.Cleanup();

            HDRenderPipeline hdrp = HDRenderPipeline.currentPipeline;
            if (hdrp != null)
                hdrp.skyManager.ReleaseCachedContext(cachedSkyRenderingContextId);
        }

        public bool IsValid()
        {
            // We need to check m_SkySettings because it can be "nulled" when destroying the volume containing the settings (as it's a ScriptableObject) without the context knowing about it.
            return m_SkySettings != null;
        }

        public bool HasClouds()
        {
            return m_CloudSettings != null;
        }

        public bool HasVolumetricClouds()
        {
            return m_VolumetricClouds != null;
        }
    }
}
