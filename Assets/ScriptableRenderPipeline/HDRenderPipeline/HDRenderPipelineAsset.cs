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

        // Renderer Settings
        public RenderingSettings renderingSettings = new RenderingSettings();
        public SubsurfaceScatteringSettings sssSettings = new SubsurfaceScatteringSettings();
        public TileSettings tileSettings = new TileSettings();

        // Shadow Settings
        public ShadowInitParameters shadowInitParams = new ShadowInitParameters();

        // Texture Settings
        public TextureSettings textureSettings = new TextureSettings();

        // Default Material / Shader

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


        public void OnValidate()
        {
            sssSettings.OnValidate();
        }
    }
}
