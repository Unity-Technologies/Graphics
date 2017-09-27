namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum ShadowCascades
    {
        NO_CASCADES = 1,
        TWO_CASCADES = 2,
        FOUR_CASCADES = 4,
    }

    public enum ShadowType
    {
        NO_SHADOW = 0,
        HARD_SHADOWS,
        SOFT_SHADOWS,
    }

    public enum ShadowResolution
    {
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048
    }

    public enum MSAAQuality
    {
        Disabled = 1,
        _2x = 2,
        _4x = 4,
        _8x = 8
    }

    public class LightweightPipelineAsset : RenderPipelineAsset
    {
        public static readonly string m_SimpleLightShaderPath = "ScriptableRenderPipeline/LightweightPipeline/Standard (Simple Lighting)";
        public static readonly string m_PBSShaderPath = "ScriptableRenderPipeline/LightweightPipeline/Standard (Simple Lighting)";
        public static readonly string m_BlitShaderPath = "Hidden/ScriptableRenderPipeline/LightweightPipeline/Blit";
        private static readonly string m_PipelineFolder = "Assets/ScriptableRenderPipeline/LightweightPipeline";
        private static readonly string m_AssetName = "LightweightPipelineAsset.asset";

        [SerializeField] private int m_MaxPixelLights = 1;
        [SerializeField] private bool m_SupportsVertexLight = true;
        [SerializeField] private MSAAQuality m_MSAA = MSAAQuality.Disabled;
        [SerializeField] private float m_RenderScale = 1.0f;
        [SerializeField] private ShadowType m_ShadowType = ShadowType.HARD_SHADOWS;
        [SerializeField] private ShadowResolution m_ShadowAtlasResolution = ShadowResolution._1024;
        [SerializeField] private float m_ShadowNearPlaneOffset = 2.0f;
        [SerializeField] private float m_ShadowDistance = 50.0f;
        [SerializeField] private ShadowCascades m_ShadowCascades = ShadowCascades.NO_CASCADES;
        [SerializeField] private float m_Cascade2Split = 0.25f;
        [SerializeField] private Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
        [SerializeField] private bool m_LinearRendering = true;
        [SerializeField] private Texture2D m_AttenuationTexture;

        [SerializeField] private Material m_DefaultDiffuseMaterial;
        [SerializeField] private Material m_DefaultParticleMaterial;
        [SerializeField] private Material m_DefaultLineMaterial;
        [SerializeField] private Material m_DefaultSpriteMaterial;
        [SerializeField] private Material m_DefaultUIMaterial;
        [SerializeField] private Shader m_DefaultShader;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("RenderPipeline/LightweightPipeline/Create Pipeline Asset", false, 15)]
        static void CreateLightweightPipeline()
        {
            var instance = ScriptableObject.CreateInstance<LightweightPipelineAsset>();

            string[] paths = m_PipelineFolder.Split('/');
            string currentPath = paths[0];
            for (int i = 1; i < paths.Length; ++i)
            {
                string folder = currentPath + "/" + paths[i];
                if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                    UnityEditor.AssetDatabase.CreateFolder(currentPath, paths[i]);

                currentPath = folder;
            }

            UnityEditor.AssetDatabase.CreateAsset(instance, m_PipelineFolder + "/" + m_AssetName);
        }
#endif

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new LightweightPipeline(this);
        }

        void OnValidate()
        {
            DestroyCreatedInstances();
        }

        public bool AreShadowsEnabled()
        {
            return ShadowSetting != ShadowType.NO_SHADOW;
        }

        public int MaxSupportedPixelLights
        {
            get { return m_MaxPixelLights; }
            private set { m_MaxPixelLights = value; }
        }

        public bool SupportsVertexLight
        {
            get { return m_SupportsVertexLight; }
            private set { m_SupportsVertexLight = value; }
        }

        public int MSAASampleCount
        {
            get { return (int)m_MSAA; }
            set { m_MSAA = (MSAAQuality)value; }
        }

        public float RenderScale
        {
            get { return m_RenderScale; }
            set { m_RenderScale = value; }
        }

        public ShadowType ShadowSetting
        {
            get { return m_ShadowType; }
            private set { m_ShadowType = value; }
        }

        public int ShadowAtlasResolution
        {
            get { return (int)m_ShadowAtlasResolution; }
            private set { m_ShadowAtlasResolution = (ShadowResolution)value; }
        }

        public float ShadowNearOffset
        {
            get { return m_ShadowNearPlaneOffset; }
            private set { m_ShadowNearPlaneOffset = value; }
        }

        public float ShadowDistance
        {
            get { return m_ShadowDistance; }
            private set { m_ShadowDistance = value; }
        }

        public int CascadeCount
        {
            get { return (int)m_ShadowCascades; }
            private set { m_ShadowCascades = (ShadowCascades)value; }
        }

        public float Cascade2Split
        {
            get { return m_Cascade2Split; }
            private set { m_Cascade2Split = value; }
        }

        public Vector3 Cascade4Split
        {
            get { return m_Cascade4Split; }
            private set { m_Cascade4Split = value; }
        }

        public bool ForceLinearRendering
        {
            get { return m_LinearRendering; }
            set { m_LinearRendering = value; }
        }

        public Texture2D AttenuationTexture
        {
            get { return m_AttenuationTexture; }
            set { m_AttenuationTexture = value; }
        }

        public override Material GetDefaultMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultParticleMaterial()
        {
            return m_DefaultParticleMaterial;
        }

        public override Material GetDefaultLineMaterial()
        {
            return m_DefaultLineMaterial;
        }

        public override Material GetDefaultTerrainMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultUIMaterial()
        {
            return m_DefaultUIMaterial;
        }

        public override Material GetDefaultUIOverdrawMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefaultUIETC1SupportedMaterial()
        {
            return m_DefaultDiffuseMaterial;
        }

        public override Material GetDefault2DMaterial()
        {
            return m_DefaultSpriteMaterial;
        }
        public override Shader GetDefaultShader()
        {
            return m_DefaultShader;
        }

        public Shader BlitShader
        {
            get { return Shader.Find(LightweightPipelineAsset.m_BlitShaderPath); }
        }
    }
}
