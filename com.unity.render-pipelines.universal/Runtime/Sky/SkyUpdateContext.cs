using System;

namespace UnityEngine.Rendering.Universal
{
    internal class SkyUpdateContext
    {
        private SkySettings m_SkySettings;
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

                m_SkySettings = value;

                if (m_SkySettings != null)
                {
                    var expectedRendererType = m_SkySettings.GetSkyRendererType();

                    bool hasSkyRendererTypeChanged = (skyRenderer == null) || (expectedRendererType != skyRenderer.GetType());
                    if (hasSkyRendererTypeChanged)
                    {
                        if (skyRenderer != null)
                            skyRenderer.Cleanup();

                        skyRenderer = Activator.CreateInstance(expectedRendererType) as SkyRenderer;
                        skyRenderer.Build();
                    }
                }
            }
        }

        public void Cleanup()
        {
            if (skyRenderer != null)
                skyRenderer.Cleanup();
        }

        public bool IsValid()
        {
            return m_SkySettings != null;
        }
    }
}
