namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // The HDRenderPipeline assumes linear lighting. Doesn't work with gamma.
    public class HDRenderPipelineAsset : RenderPipelineAsset
    {
        HDRenderPipelineAsset()
        {
        }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new HDRenderPipeline(this);
        }

        [SerializeField]
        RenderPipelineResources m_RenderPipelineResources;
        public RenderPipelineResources renderPipelineResources
        {
            get { return m_RenderPipelineResources; }
            set { m_RenderPipelineResources = value; }
        }

        // To be able to turn on/off FrameSettings properties at runtime for debugging purpose without affecting the original one
        // we create a runtime copy (m_effectiveFrameSettings that is used, and any parametrization is done on serialized frameSettings)
        public FrameSettings defaultFrameSettings = new FrameSettings(); // This are the defaultFrameSettings for all the camera and apply to sceneView, public to be visible in the inspector
        // Not serialized, not visible, the settings effectively used
        FrameSettings m_defaultEffectiveFrameSettings = new FrameSettings();

        public FrameSettings GetEffectiveDefaultFrameSettings()
        {
            return m_defaultEffectiveFrameSettings;
        }

        public void OnValidate()
        {
            // Modification of defaultFrameSettings in the inspector will call OnValidate().
            // We do a copy of the settings to those effectively used
            defaultFrameSettings.CopyTo(m_defaultEffectiveFrameSettings);
        }

        // Store the various GlobalFrameSettings for each platform (for now only one)
        public GlobalFrameSettings globalFrameSettings = new GlobalFrameSettings();

        // Return the current use GlobalFrameSettings (i.e for the current platform)
        public GlobalFrameSettings GetGlobalFrameSettings()
        {
            return globalFrameSettings;
        }

        [SerializeField]
        public SubsurfaceScatteringSettings sssSettings;

        // Default Material / Shader
        [SerializeField]
        Material m_DefaultDiffuseMaterial;
        [SerializeField]
        Shader m_DefaultShader;

        public Material defaultDiffuseMaterial
        {
            get { return m_DefaultDiffuseMaterial; }
            set { m_DefaultDiffuseMaterial = value; }
        }

        public Shader defaultShader
        {
            get { return m_DefaultShader; }
            set { m_DefaultShader = value; }
        }

        public override Shader GetDefaultShader()
        {
            return m_DefaultShader;
        }

        public override Material GetDefaultMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultParticleMaterial()
        {
            return null;
        }

        public override Material GetDefaultLineMaterial()
        {
            return null;
        }

        public override Material GetDefaultTerrainMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIOverdrawMaterial()
        {
            return null;
        }

        public override Material GetDefaultUIETC1SupportedMaterial()
        {
            return null;
        }

        public override Material GetDefault2DMaterial()
        {
            return null;
        }
    }
}
