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

        public FrameSettings defaultFrameSettings = new FrameSettings(); // This are the defaultFrameSettings for all the camera and apply to sceneView

        [SerializeField]
        GlobalFrameSettings m_globalFrameSettings;
        public GlobalFrameSettings globalFrameSettings
        {
            // TODO: This function must return the current globalFrameSettings for the given platform
            // If nothing is define we return a default value
            get
            {
                if (m_globalFrameSettings == null)
                {
                    if (m_defaultGlobalFrameSettings == null)
                        m_defaultGlobalFrameSettings = ScriptableObject.CreateInstance<GlobalFrameSettings>(); // TODO: where to destroy this ?

                    m_globalFrameSettings = m_defaultGlobalFrameSettings;
                }

                return m_globalFrameSettings;
            }

            set { m_globalFrameSettings = value; }
        }

        GlobalFrameSettings m_defaultGlobalFrameSettings;

        [SerializeField]
        SubsurfaceScatteringSettings m_sssSettings;


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

        public void OnValidate()
        {
        }
    }
}
