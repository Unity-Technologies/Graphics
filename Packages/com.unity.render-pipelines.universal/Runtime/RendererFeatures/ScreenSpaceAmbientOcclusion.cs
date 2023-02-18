using System;
#if UNITY_EDITOR
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ScreenSpaceAmbientOcclusionSettings
    {
        // Parameters
        [SerializeField] internal AOMethodOptions AOMethod = AOMethodOptions.BlueNoise;
        [SerializeField] internal bool Downsample = false;
        [SerializeField] internal bool AfterOpaque = false;
        [SerializeField] internal DepthSource Source = DepthSource.DepthNormals;
        [SerializeField] internal NormalQuality NormalSamples = NormalQuality.Medium;
        [SerializeField] internal float Intensity = 3.0f;
        [SerializeField] internal float DirectLightingStrength = 0.25f;
        [SerializeField] internal float Radius = 0.035f;
        [SerializeField] internal AOSampleOption Samples = AOSampleOption.Medium;
        [SerializeField] internal BlurQualityOptions BlurQuality = BlurQualityOptions.High;
        [SerializeField] internal float Falloff = 100f;

        // Legacy. Kept to migrate users over to use Samples instead.
        [SerializeField] internal int SampleCount = -1;

        // Enums
        internal enum DepthSource
        {
            Depth = 0,
            DepthNormals = 1
        }

        internal enum NormalQuality
        {
            Low,
            Medium,
            High
        }

        internal enum AOSampleOption
        {
            High,   // 12 Samples
            Medium, // 8 Samples
            Low,    // 4 Samples
        }

        internal enum AOMethodOptions
        {
            BlueNoise,
            InterleavedGradient,
        }

        internal enum BlurQualityOptions
        {
            High,   // Bilateral
            Medium, // Gaussian
            Low,    // Kawase
        }
    }

    [DisallowMultipleRendererFeature("Screen Space Ambient Occlusion")]
    [Tooltip("The Ambient Occlusion effect darkens creases, holes, intersections and surfaces that are close to each other.")]
    [URPHelpURL("post-processing-ssao")]
    internal class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField] private ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

        [Reload("Textures/BlueNoise256/LDR_LLL1_{0}.png", 0, 7)]
        public Texture2D[] m_BlueNoise256Textures;

        [SerializeField]
        [HideInInspector]
        [Reload("Shaders/Utils/ScreenSpaceAmbientOcclusion.shader")]
        private Shader m_Shader;

        // Private Fields
        private Material m_Material;
        private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;

        // Internal / Constants
        internal ref ScreenSpaceAmbientOcclusionSettings settings => ref m_Settings;
        internal const string k_AOInterleavedGradientKeyword = "_INTERLEAVED_GRADIENT";
        internal const string k_AOBlueNoiseKeyword = "_BLUE_NOISE";
        internal const string k_OrthographicCameraKeyword = "_ORTHOGRAPHIC";
        internal const string k_SourceDepthLowKeyword = "_SOURCE_DEPTH_LOW";
        internal const string k_SourceDepthMediumKeyword = "_SOURCE_DEPTH_MEDIUM";
        internal const string k_SourceDepthHighKeyword = "_SOURCE_DEPTH_HIGH";
        internal const string k_SourceDepthNormalsKeyword = "_SOURCE_DEPTH_NORMALS";
        internal const string k_SampleCountLowKeyword = "_SAMPLE_COUNT_LOW";
        internal const string k_SampleCountMediumKeyword = "_SAMPLE_COUNT_MEDIUM";
        internal const string k_SampleCountHighKeyword = "_SAMPLE_COUNT_HIGH";

        /// <inheritdoc/>
        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
            // Create the pass...
            if (m_SSAOPass == null)
                m_SSAOPass = new ScreenSpaceAmbientOcclusionPass();

            // Check for previous version of SSAO
            if (m_Settings.SampleCount > 0)
            {
                m_Settings.AOMethod = ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;

                if (m_Settings.SampleCount > 11)
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High;
                else if (m_Settings.SampleCount > 8)
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium;
                else
                    m_Settings.Samples = ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Low;

                m_Settings.SampleCount = -1;
            }
        }

        /// <inheritdoc/>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterials())
            {
                Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.", GetType().Name, name);
                return;
            }

            bool shouldAdd = m_SSAOPass.Setup(ref m_Settings, ref renderer, ref m_Material, ref m_BlueNoise256Textures);
            if (shouldAdd)
                renderer.EnqueuePass(m_SSAOPass);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            m_SSAOPass?.Dispose();
            m_SSAOPass = null;
            CoreUtils.Destroy(m_Material);
        }

        private bool GetMaterials()
        {
            if (m_Material == null && m_Shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
            return m_Material != null;
        }


    }
}
