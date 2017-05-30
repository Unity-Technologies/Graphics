using UnityEngine.Rendering;
using System;
using System.Linq;
using UnityEngine.Experimental.PostProcessing;
using UnityEngine.Experimental.Rendering.HDPipeline.TilePass;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // This HDRenderPipeline assume linear lighting. Don't work with gamma.
    public class HDRenderPipelineAsset : RenderPipelineAsset
    {
#if UNITY_EDITOR
        const string k_HDRenderPipelinePath = "Assets/ScriptableRenderPipeline/HDRenderPipeline/HDRenderPipelineAsset.asset";

        [MenuItem("RenderPipeline/HDRenderPipeline/Create Pipeline Asset")]
        static void CreateHDRenderPipeline()
        {
            var instance = CreateInstance<HDRenderPipelineAsset>();
            AssetDatabase.CreateAsset(instance, k_HDRenderPipelinePath);

            // If it exist, load renderPipelineResources
            instance.renderPipelineResources = AssetDatabase.LoadAssetAtPath<RenderPipelineResources>(RenderPipelineResources.renderPipelineResourcesPath);
        }

        [UnityEditor.MenuItem("HDRenderPipeline/Add \"Additional Light Data\" (if not present)")]
        static void AddAdditionalLightData()
        {
            Light[] lights = FindObjectsOfType(typeof(Light)) as Light[];

            foreach (Light light in lights)
            {
                // Do not add a component if there already is one.
                if (light.GetComponent<AdditionalLightData>() == null)
                {
                    light.gameObject.AddComponent<AdditionalLightData>();
                }
            }
        }
#endif

        private HDRenderPipelineAsset()
        { }

        protected override IRenderPipeline InternalCreatePipeline()
        {
            return new HDRenderPipeline(this);
        }

        [SerializeField]
        private RenderPipelineResources m_RenderPipelineResources;
        public RenderPipelineResources renderPipelineResources
        {
            get { return m_RenderPipelineResources; }
            set { m_RenderPipelineResources = value; }
        }

        // NOTE: All those properties are public because of how HDRenderPipelineInspector retrieve those properties via serialization/reflection
        // Doing it this way allow to change parameters name and still retrieve correct serialized value

        // Debugging (Not persistent)
        public DebugDisplaySettings debugDisplaySettings = new DebugDisplaySettings();

        // Renderer Settings (per project)
        public RenderingSettings renderingSettings = new RenderingSettings();
        public SubsurfaceScatteringSettings sssSettings = new SubsurfaceScatteringSettings();
        public TileSettings tileSettings = new TileSettings();

        // TODO: Following two settings need to be update to the serialization/reflection way like above
        [SerializeField]
        ShadowSettings m_ShadowSettings = ShadowSettings.Default;
        [SerializeField]
        TextureSettings m_TextureSettings = TextureSettings.Default;
        
        public ShadowSettings shadowSettings
        {
            get { return m_ShadowSettings; }
        }

        public TextureSettings textureSettings
        {
            get { return m_TextureSettings; }
            set { m_TextureSettings = value; }
        }

        // NOTE: Following settings are Asset so they need to be serialized as usual. no reflection/serialization here

        // Renderer Settings (per "scene")
        [SerializeField]
        private CommonSettings.Settings m_CommonSettings = CommonSettings.Settings.s_Defaultsettings;
        [SerializeField]
        private SkySettings m_SkySettings;

        public CommonSettings.Settings commonSettingsToUse
        {
            get
            {
                if (CommonSettingsSingleton.overrideSettings)
                    return CommonSettingsSingleton.overrideSettings.settings;

                return m_CommonSettings;
            }
        }

        public SkySettings skySettings
        {
            get { return m_SkySettings; }
            set { m_SkySettings = value; }
        }

        public SkySettings skySettingsToUse
        {
            get
            {
                if (SkySettingsSingleton.overrideSettings)
                    return SkySettingsSingleton.overrideSettings;

                return m_SkySettings;
            }
        }

        [SerializeField]
        Material m_DefaultDiffuseMaterial;
        [SerializeField]
        Shader m_DefaultShader;

        public Material DefaultDiffuseMaterial
        {
            get { return m_DefaultDiffuseMaterial; }
            private set { m_DefaultDiffuseMaterial = value; }
        }

        public Shader DefaultShader
        {
            get { return m_DefaultShader; }
            private set { m_DefaultShader = value; }
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

        public void ApplyDebugDisplaySettings()
        {
            m_ShadowSettings.enabled = debugDisplaySettings.lightingDebugSettings.enableShadows;

            LightingDebugSettings lightingDebugSettings = debugDisplaySettings.lightingDebugSettings;
            Vector4 debugAlbedo = new Vector4(lightingDebugSettings.debugLightingAlbedo.r, lightingDebugSettings.debugLightingAlbedo.g, lightingDebugSettings.debugLightingAlbedo.b, 0.0f);
            Vector4 debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);

            Shader.SetGlobalInt("_DebugViewMaterial", (int)debugDisplaySettings.GetDebugMaterialIndex());
            Shader.SetGlobalInt("_DebugLightingMode", (int)debugDisplaySettings.GetDebugLightingMode());
            Shader.SetGlobalVector("_DebugLightingAlbedo", debugAlbedo);
            Shader.SetGlobalVector("_DebugLightingSmoothness", debugSmoothness);
        }

        public void UpdateCommonSettings()
        {
            var commonSettings = commonSettingsToUse;

            m_ShadowSettings.directionalLightCascadeCount = commonSettings.shadowCascadeCount;
            m_ShadowSettings.directionalLightCascades = new Vector3(commonSettings.shadowCascadeSplit0, commonSettings.shadowCascadeSplit1, commonSettings.shadowCascadeSplit2);
            m_ShadowSettings.maxShadowDistance = commonSettings.shadowMaxDistance;
            m_ShadowSettings.directionalLightNearPlaneOffset = commonSettings.shadowNearPlaneOffset;
        }

        public void OnValidate()
        {
            debugDisplaySettings.OnValidate();
            sssSettings.OnValidate();
        }

        void OnEnable()
        {
            debugDisplaySettings.RegisterDebug();
        }
    }
}
