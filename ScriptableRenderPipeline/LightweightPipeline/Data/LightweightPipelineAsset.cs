#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum ShadowCascades
    {
        NO_CASCADES = 0,
        TWO_CASCADES,
        FOUR_CASCADES,
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
        _2048 = 2048,
        _4096 = 4096
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
        public static readonly string[] m_SearchPaths = {"Assets", "Packages/com.unity.render-pipelines.lightweight"};

        // Default values set when a new LightweightPipeline asset is created
        [SerializeField] private int m_MaxPixelLights = 4;
        [SerializeField] private bool m_SupportsVertexLight = false;
        [SerializeField] private bool m_RequireCameraDepthTexture = false;
        [SerializeField] private MSAAQuality m_MSAA = MSAAQuality._4x;
        [SerializeField] private float m_RenderScale = 1.0f;
        [SerializeField] private ShadowType m_ShadowType = ShadowType.HARD_SHADOWS;
        [SerializeField] private ShadowResolution m_ShadowAtlasResolution = ShadowResolution._2048;
        [SerializeField] private float m_ShadowNearPlaneOffset = 2.0f;
        [SerializeField] private float m_ShadowDistance = 50.0f;
        [SerializeField] private ShadowCascades m_ShadowCascades = ShadowCascades.FOUR_CASCADES;
        [SerializeField] private float m_Cascade2Split = 0.25f;
        [SerializeField] private Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);

        // Resources
        [SerializeField] private Shader m_DefaultShader;
        [SerializeField] private Shader m_BlitShader;
        [SerializeField] private Shader m_CopyDepthShader;

#if UNITY_EDITOR
        [SerializeField] private Material m_DefaultMaterial;
        [SerializeField] private Material m_DefaultParticleMaterial;
        [SerializeField] private Material m_DefaultTerrainMaterial;

        [MenuItem("Assets/Create/Render Pipeline/Lightweight/Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateLightweightPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateLightweightPipelineAsset>(),
                "LightweightAsset.asset", null, null);
        }

        class CreateLightweightPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<LightweightPipelineAsset>();

                string[] guids = AssetDatabase.FindAssets("LightweightPipelineResource t:scriptableobject", m_SearchPaths);
                LightweightPipelineResource resourceAsset = null;
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    resourceAsset = AssetDatabase.LoadAssetAtPath<LightweightPipelineResource>(path);
                    if (resourceAsset != null)
                        break;
                }

                // There's currently an issue that prevents FindAssets from find resources withing the package folder.
                if (resourceAsset == null)
                {
                    string path = "Packages/com.unity.render-pipelines.lightweight/Data/LightweightPipelineResource.asset";
                    resourceAsset = AssetDatabase.LoadAssetAtPath<LightweightPipelineResource>(path);
                }

                if (resourceAsset != null)
                {
                    instance.m_DefaultMaterial = resourceAsset.DefaultMaterial;
                    instance.m_DefaultParticleMaterial = resourceAsset.DefaultParticleMaterial;
                    instance.m_DefaultTerrainMaterial = resourceAsset.DefaultTerrainMaterial;
                }

                instance.m_DefaultShader = Shader.Find(LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_PBS));
                instance.m_BlitShader = Shader.Find(LightweightShaderUtils.GetShaderPath(ShaderPathID.HIDDEN_BLIT));
                instance.m_CopyDepthShader = Shader.Find(LightweightShaderUtils.GetShaderPath(ShaderPathID.HIDDEN_DEPTH_COPY));

                AssetDatabase.CreateAsset(instance, pathName);
            }
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
            get
            {
                switch (m_ShadowCascades)
                {
                    case ShadowCascades.TWO_CASCADES:
                        return 2;
                    case ShadowCascades.FOUR_CASCADES:
                        return 4;
                    default:
                        return 1;
                }
            }
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
#if UNITY_EDITOR
            return m_DefaultMaterial;
#else
            return null;
#endif
        }

        public override Material GetDefaultParticleMaterial()
        {
#if UNITY_EDITOR
            return m_DefaultParticleMaterial;
#else
            return null;
#endif
        }

        public override Material GetDefaultLineMaterial()
        {
            return null;
        }

        public override Material GetDefaultTerrainMaterial()
        {
#if UNITY_EDITOR
            return m_DefaultTerrainMaterial;
#else
            return null;
#endif
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
