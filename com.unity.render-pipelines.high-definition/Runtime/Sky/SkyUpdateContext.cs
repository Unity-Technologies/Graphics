using System;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class SkyUpdateContext
    {
        SkySettings         m_SkySettings;
        public SkyRenderer  skyRenderer { get; private set; }
        public int          cachedSkyRenderingContextId = -1;

        public int          skyParametersHash = -1;
        public float        currentUpdateTime = 0.0f;

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

        public void Cleanup()
        {
            if (skyRenderer != null)
            {
                skyRenderer.Cleanup();
            }

            HDRenderPipeline hdrp = HDRenderPipeline.currentPipeline;
            if (hdrp != null)
                hdrp.skyManager.ReleaseCachedContext(cachedSkyRenderingContextId);
        }

        public bool IsValid()
        {
            // We need to check m_SkySettings because it can be "nulled" when destroying the volume containing the settings (as it's a ScriptableObject) without the context knowing about it.
            return m_SkySettings != null;
        }
    }
}
