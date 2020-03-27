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
                if (m_SkySettings == value)
                    return;

                skyParametersHash = -1;
                m_SkySettings = value;
                currentUpdateTime = 0.0f;

                if (m_SkySettings != null && (skyRenderer == null || m_SkySettings.GetSkyRendererType() != skyRenderer.GetType()))
                {
                    if (skyRenderer != null)
                    {
                        skyRenderer.Cleanup();
                    }

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
