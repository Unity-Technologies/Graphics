#if UNITY_EDITOR
using System;
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
        _256 = 256,
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

    public enum Downsampling
    {
        None = 0,
        _2xBilinear,
        _4xBox,
        _4xBilinear
    }

    public enum DefaultMaterialType
    {
        Standard = 0,
        Particle,
        Terrain,
        UnityBuiltinDefault
    }

    public class LightweightPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
        public static readonly string s_SearchPathProject = "Assets";
        public static readonly string s_SearchPathPackage = "Packages/com.unity.render-pipelines.lightweight";

        Shader m_DefaultShader;

        // Default values set when a new LightweightPipeline asset is created
        [SerializeField] int k_AssetVersion = 3;
        [SerializeField] int m_MaxPixelLights = 4;
        [SerializeField] bool m_SupportsVertexLight = false;
        [SerializeField] bool m_RequireDepthTexture = false;
        [SerializeField] bool m_RequireSoftParticles = false;
        [SerializeField] bool m_RequireOpaqueTexture = false;
        [SerializeField] Downsampling m_OpaqueDownsampling = Downsampling._2xBilinear;
        [SerializeField] bool m_SupportsHDR = false;
        [SerializeField] MSAAQuality m_MSAA = MSAAQuality._4x;
        [SerializeField] float m_RenderScale = 1.0f;
        [SerializeField] bool m_SupportsDynamicBatching = true;

        [SerializeField] bool m_DirectionalShadowsSupported = true;
        [SerializeField] ShadowResolution m_ShadowAtlasResolution = ShadowResolution._2048;
        [SerializeField] float m_ShadowDistance = 50.0f;
        [SerializeField] ShadowCascades m_ShadowCascades = ShadowCascades.FOUR_CASCADES;
        [SerializeField] float m_Cascade2Split = 0.25f;
        [SerializeField] Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
        [SerializeField] bool m_LocalShadowsSupported = true;
        [SerializeField] ShadowResolution m_LocalShadowsAtlasResolution = ShadowResolution._512;
        [SerializeField] bool m_SoftShadowsSupported = false;

        [SerializeField] LightweightPipelineResources m_ResourcesAsset;
        [SerializeField] XRGraphicsConfig m_SavedXRConfig = XRGraphicsConfig.s_DefaultXRConfig;

        // Deprecated
        [SerializeField] ShadowType m_ShadowType = ShadowType.HARD_SHADOWS;

#if UNITY_EDITOR
        [NonSerialized]
        LightweightPipelineEditorResources m_EditorResourcesAsset;

        [MenuItem("Assets/Create/Rendering/Lightweight Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateLightweightPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateLightweightPipelineAsset>(),
                "LightweightAsset.asset", null, null);
        }

        //[MenuItem("Assets/Create/Rendering/Lightweight Pipeline Resources", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateLightweightPipelineResources()
        {
            var instance = CreateInstance<LightweightPipelineResources>();
            AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(LightweightPipelineResources).Name));
        }

        //[MenuItem("Assets/Create/Rendering/Lightweight Pipeline Editor Resources", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateLightweightPipelineEditorResources()
        {
            var instance = CreateInstance<LightweightPipelineEditorResources>();
            AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(LightweightPipelineEditorResources).Name));
        }

        class CreateLightweightPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<LightweightPipelineAsset>();
                instance.m_EditorResourcesAsset = LoadResourceFile<LightweightPipelineEditorResources>();
                instance.m_ResourcesAsset = LoadResourceFile<LightweightPipelineResources>();
                AssetDatabase.CreateAsset(instance, pathName);
            }
        }

        static T LoadResourceFile<T>() where T : ScriptableObject
        {
            T resourceAsset = null;
            var guids = AssetDatabase.FindAssets(typeof(T).Name + " t:scriptableobject", new[] {s_SearchPathProject});
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                resourceAsset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (resourceAsset != null)
                    break;
            }

            // There's currently an issue that prevents FindAssets from find resources withing the package folder.
            if (resourceAsset == null)
            {
                string path = s_SearchPathPackage + "/LWRP/Data/" + typeof(T).Name + ".asset";
                resourceAsset = AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return resourceAsset;
        }

        LightweightPipelineEditorResources editorResources
        {
            get
            {
                if (m_EditorResourcesAsset == null)
                    m_EditorResourcesAsset = LoadResourceFile<LightweightPipelineEditorResources>();

                return m_EditorResourcesAsset;
            }
        }
#endif
        LightweightPipelineResources resources
        {
            get
            {
#if UNITY_EDITOR
                if (m_ResourcesAsset == null)
                    m_ResourcesAsset = LoadResourceFile<LightweightPipelineResources>();
#endif
                return m_ResourcesAsset;
            }
        }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new LightweightPipeline(this);
        }

        Material GetMaterial(DefaultMaterialType materialType)
        {
#if UNITY_EDITOR
            if (editorResources == null)
                return null;

            switch (materialType)
            {
                case DefaultMaterialType.Standard:
                    return editorResources.DefaultMaterial;

                case DefaultMaterialType.Particle:
                    return editorResources.DefaultParticleMaterial;

                case DefaultMaterialType.Terrain:
                    return editorResources.DefaultTerrainMaterial;

                // Unity Builtin Default
                default:
                    return null;
            }
#else
            return null;
#endif
        }
        
        public int GetAssetVersion()
        {
            return k_AssetVersion;
        }

        public int maxPixelLights
        {
            get { return m_MaxPixelLights; }
        }

        public bool supportsVertexLight
        {
            get { return m_SupportsVertexLight; }
        }

        public bool supportsCameraDepthTexture
        {
            get { return m_RequireDepthTexture; }
        }

        public bool supportsSoftParticles
        {
            get { return m_RequireSoftParticles; }
        }

        public bool supportsCameraOpaqueTexture
        {
            get { return m_RequireOpaqueTexture; }
        }

        public Downsampling opaqueDownsampling
        {
            get { return m_OpaqueDownsampling; }
        }

        public bool supportsHDR
        {
            get { return m_SupportsHDR; }
        }

        public int msaaSampleCount
        {
            get { return (int)m_MSAA; }
            set { m_MSAA = (MSAAQuality)value; }
        }

        public float renderScale
        {
            get { return m_RenderScale; }
            set { m_RenderScale = value; }
        }

        public bool supportsDynamicBatching
        {
            get { return m_SupportsDynamicBatching; }
        }

        public bool supportsDirectionalShadows
        {
            get { return m_DirectionalShadowsSupported; }
        }

        public int directionalShadowAtlasResolution
        {
            get { return (int)m_ShadowAtlasResolution; }
        }

        public float shadowDistance
        {
            get { return m_ShadowDistance; }
            set { m_ShadowDistance = value; }
        }

        public int cascadeCount
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

        public float cascade2Split
        {
            get { return m_Cascade2Split; }
        }

        public Vector3 cascade4Split
        {
            get { return m_Cascade4Split; }
        }

        public bool supportsLocalShadows
        {
            get { return m_LocalShadowsSupported; }
        }

        public int localShadowAtlasResolution
        {
            get { return (int)m_LocalShadowsAtlasResolution; }
        }
        public bool supportsSoftShadows
        {
            get { return m_SoftShadowsSupported; }
        }

        public override Material GetDefaultMaterial()
        {
            return GetMaterial(DefaultMaterialType.Standard);
        }

        public override Material GetDefaultParticleMaterial()
        {
            return GetMaterial(DefaultMaterialType.Particle);
        }

        public override Material GetDefaultLineMaterial()
        {
            return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
        }

        public override Material GetDefaultTerrainMaterial()
        {
            return GetMaterial(DefaultMaterialType.Terrain);
        }

        public override Material GetDefaultUIMaterial()
        {
            return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
        }

        public override Material GetDefaultUIOverdrawMaterial()
        {
            return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
        }

        public override Material GetDefaultUIETC1SupportedMaterial()
        {
            return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
        }

        public override Material GetDefault2DMaterial()
        {
            return GetMaterial(DefaultMaterialType.UnityBuiltinDefault);
        }

        public override Shader GetDefaultShader()
        {
            if (m_DefaultShader == null)
                m_DefaultShader = Shader.Find(LightweightShaderUtils.GetShaderPath(ShaderPathID.STANDARD_PBS));
            return m_DefaultShader;
        }

        public Shader blitShader
        {
            get { return resources != null ? resources.BlitShader : null; }
        }

        public Shader copyDepthShader
        {
            get { return resources != null ? resources.CopyDepthShader : null; }
        }

        public Shader screenSpaceShadowShader
        {
            get { return resources != null ? resources.ScreenSpaceShadowShader : null; }
        }

        public Shader samplingShader
        {
            get { return resources != null ? resources.SamplingShader : null; }
        }

        public XRGraphicsConfig savedXRGraphicsConfig
        {
            get { return m_SavedXRConfig; }
            set { m_SavedXRConfig = value;  }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (k_AssetVersion < 3)
            {
                k_AssetVersion = 3;
                m_SoftShadowsSupported = (m_ShadowType == ShadowType.SOFT_SHADOWS);
            }
        }
    }
}
