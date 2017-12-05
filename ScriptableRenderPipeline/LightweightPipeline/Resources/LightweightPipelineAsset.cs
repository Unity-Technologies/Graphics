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
        public static readonly string m_SimpleLightShaderPath = "LightweightPipeline/Standard (Simple Lighting)";
        public static readonly string m_StandardShaderPath = "LightweightPipeline/Standard (Physically Based)";
        public static readonly string[] m_SearchPaths = {"Assets", "Packages/com.unity.render-pipelines"};

        // Default values set when a new LightweightPipeline asset is created
        [SerializeField] private int m_MaxPixelLights = 4;
        [SerializeField] private bool m_SupportsVertexLight = false;
        [SerializeField] private bool m_RequireCameraDepthTexture = false;
        [SerializeField] private MSAAQuality m_MSAA = MSAAQuality._4x;
        [SerializeField] private float m_RenderScale = 1.0f;
        [SerializeField] private ShadowType m_ShadowType = ShadowType.HARD_SHADOWS;
        [SerializeField] private ShadowResolution m_ShadowAtlasResolution = ShadowResolution._1024;
        [SerializeField] private float m_ShadowNearPlaneOffset = 2.0f;
        [SerializeField] private float m_ShadowDistance = 50.0f;
        [SerializeField] private ShadowCascades m_ShadowCascades = ShadowCascades.NO_CASCADES;
        [SerializeField] private float m_Cascade2Split = 0.25f;
        [SerializeField] private Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);

        // Resources
        [SerializeField] private Shader m_DefaultShader;
        [SerializeField] private Shader m_BlitShader;
        [SerializeField] private Shader m_CopyDepthShader;

        [SerializeField] private Material m_DefaultMaterial;
        [SerializeField] private Material m_DefaultParticleMaterial;
        [SerializeField] private Material m_DefaultTerrainMaterial;

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Assets/Create/Render Pipeline/Lightweight/Render Pipeline", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateLightweightPipeline()
        {
            var instance = CreateInstance<LightweightPipelineAsset>();

            string[] guids = UnityEditor.AssetDatabase.FindAssets("LightweightPipelineResource t:scriptableobject", m_SearchPaths);
            LightweightPipelineResource resourceAsset = null;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                resourceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LightweightPipelineResource>(path);
                if (resourceAsset != null)
                    break;
            }

            // There's currently an issue that prevents FindAssets from find resources withing the package folder.
            if (resourceAsset == null)
            {
                string path = "Packages/com.unity.render-pipelines.lightweight/Resources/LightweightPipelineResource.asset";
                resourceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<LightweightPipelineResource>(path);
            }

            if (resourceAsset != null)
            {
                instance.m_DefaultMaterial = resourceAsset.DefaultMaterial;
                instance.m_DefaultParticleMaterial = resourceAsset.DefaultParticleMaterial;
                instance.m_DefaultTerrainMaterial = resourceAsset.DefaultTerrainMaterial;
            }

            instance.m_DefaultShader = Shader.Find(m_StandardShaderPath);
            instance.m_BlitShader = Shader.Find("Hidden/LightweightPipeline/Blit");
            instance.m_CopyDepthShader = Shader.Find("Hidden/LightweightPipeline/CopyDepth");

            string assetPath = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Lightweight Asset", "LightweightAsset", "asset",
                "Please enter a file name to save the asset to");

            if (assetPath.Length > 0)
                UnityEditor.AssetDatabase.CreateAsset(instance, assetPath);
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

        public int MaxPixelLights
        {
            get { return m_MaxPixelLights; }
        }

        public bool SupportsVertexLight
        {
            get { return m_SupportsVertexLight; }
        }

        public bool RequireCameraDepthTexture
        {
            get { return m_RequireCameraDepthTexture; }
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

        public override Material GetDefaultMaterial()
        {
            return m_DefaultMaterial;
        }

        public override Material GetDefaultParticleMaterial()
        {
            return m_DefaultParticleMaterial;
        }

        public override Material GetDefaultLineMaterial()
        {
            return null;
        }

        public override Material GetDefaultTerrainMaterial()
        {
            return m_DefaultTerrainMaterial;
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
        public override Shader GetDefaultShader()
        {
            return m_DefaultShader;
        }

        public Shader BlitShader
        {
            get { return m_BlitShader; }
        }

        public Shader CopyDepthShader
        {
            get { return m_CopyDepthShader; }
        }
    }
}
