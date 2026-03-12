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

    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: SSAO Shader", Order = 1000)]
    [Categorization.ElementInfo(Order = 0), HideInInspector]
    class ScreenSpaceAmbientOcclusionPersistentResources : IRenderPipelineResources
    {
        [SerializeField]
        [ResourcePath("Shaders/Utils/ScreenSpaceAmbientOcclusion.shader")]
        Shader m_Shader;

        public Shader Shader
        {
            get => m_Shader;
            set => this.SetValueAndNotify(ref m_Shader, value);
        }

        public bool isAvailableInPlayerBuild => true;

        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>Current version of the resource container. Used only for upgrading a project.</summary>
        public int version => m_Version;
    }

    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: SSAO Noise Textures", Order = 1000)]
    [Categorization.ElementInfo(Order = 0), HideInInspector]
    class ScreenSpaceAmbientOcclusionDynamicResources : IRenderPipelineResources
    {
        [SerializeField]
        [ResourceFormattedPaths("Textures/BlueNoise256/LDR_LLL1_{0}.png", 0, 7)]
        Texture2D[] m_BlueNoise256Textures;

        public Texture2D[] BlueNoise256Textures
        {
            get => m_BlueNoise256Textures;
            set => this.SetValueAndNotify(ref m_BlueNoise256Textures, value);
        }

        public bool isAvailableInPlayerBuild => true;

        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>Current version of the resource container. Used only for upgrading a project.</summary>
        public int version => m_Version;
    }


    /// <summary>
    /// The class for the SSAO renderer feature.
    /// </summary>
    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [DisallowMultipleRendererFeature("Screen Space Ambient Occlusion")]
    [Tooltip("The Ambient Occlusion effect darkens creases, holes, intersections and surfaces that are close to each other.")]
    [URPHelpURL("post-processing-ssao")]
    public class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField] private ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

        // Private Fields
        private Material m_Material;
        private ScreenSpaceAmbientOcclusionPass m_SSAOPass = null;
        private Shader m_Shader;
        private Texture2D[] m_BlueNoise256Textures;

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
            if (UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
                return;

            if (!TryPrepareResources())
                return;

            bool usesDeferred = renderer is UniversalRenderer { usesDeferredLighting: true };
            ScriptableRenderPassInput requirements;
            RenderPassEvent passEvent;
            if (usesDeferred)
            {
                passEvent = m_Settings.AfterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingPrePasses;
                requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            }
            else
            {
                passEvent = m_Settings.AfterOpaque ? RenderPassEvent.BeforeRenderingTransparents : RenderPassEvent.AfterRenderingPrePasses + 1;
                requirements = m_Settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth ? ScriptableRenderPassInput.Depth : ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            }

            if (renderer is UniversalRenderer universalRenderer && universalRenderer.useTileOnlyMode)
            {
                if (!RenderingUtils.IsCompatibleWithTileOnlyMode(requirements, passEvent))
                {
                    Debug.LogErrorFormat(
                        "Screen Space Ambient Occlusion \"{0}\": the current settings are not compatible with Tile-Only Mode. Open the Universal Renderer \"{1}\" in the Inspector for more information.",
                        name, renderer.name);
                    return;
                }

                // SSAO-specific: In Tile-Only Mode with After Opaque, Render Graph merges the Blit SSAO pass with
                // earlier passes that use the backbuffer (e.g. TopLeft UV origin). The merged pass then stores the
                // SSAO occlusion texture with that same origin. Later, the occlusion texture is sampled as a texture
                // (blur, etc.) which expects a different UV origin (e.g. BottomLeft), causing "Texture attachment
                // Backbuffer depth with uv origin X does not match with texture attachment _SSAO_OcclusionTexture0
                // with uv origin Y". Pass merging is currently too aggressive here, so disallow After Opaque with
                // Depth Normals in Tile-Only Mode.
                if (m_Settings.AfterOpaque && m_Settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals)
                {
                    Debug.LogErrorFormat(
                        "Screen Space Ambient Occlusion \"{0}\": the current settings are not compatible with Tile-Only Mode. Open the Universal Renderer \"{1}\" in the Inspector for more information.",
                        name, renderer.name);
                    return;
                }
            }

            m_SSAOPass.renderPassEvent = passEvent;
            m_SSAOPass.ConfigureInput(requirements);
            var effectiveDepthSource = usesDeferred ? ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals : m_Settings.Source;
            bool shouldAdd = m_SSAOPass.Setup(m_Settings, effectiveDepthSource, m_Material, m_BlueNoise256Textures);
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

        bool TryPrepareResources()
        {
            if (m_Shader == null)
            {
                if (!GraphicsSettings.TryGetRenderPipelineSettings<ScreenSpaceAmbientOcclusionPersistentResources>(out var ssaoPersistentResources))
                {
                    Debug.LogErrorFormat(
                        $"Couldn't find the required resources for the {nameof(ScreenSpaceAmbientOcclusion)} render feature. If this exception appears in the Player, make sure at least one {nameof(ScreenSpaceAmbientOcclusion)} render feature is enabled or adjust your stripping settings.");
                    return false;
                }

                m_Shader = ssaoPersistentResources.Shader;
            }

            if (m_Settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise && (m_BlueNoise256Textures == null || m_BlueNoise256Textures.Length == 0))
            {
                if (!GraphicsSettings.TryGetRenderPipelineSettings<ScreenSpaceAmbientOcclusionDynamicResources>(out var ssaoDynamicResources))
                {
                    Debug.LogErrorFormat($"Couldn't load {nameof(ScreenSpaceAmbientOcclusionDynamicResources.BlueNoise256Textures)}. If this exception appears in the Player, please check the SSAO options for {nameof(ScreenSpaceAmbientOcclusion)} or adjust your stripping settings");
                    return false;
                }

                m_BlueNoise256Textures = ssaoDynamicResources.BlueNoise256Textures;
            }

            if (m_Material == null && m_Shader != null)
                m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

            if (m_Material == null)
            {
                Debug.LogError($"{GetType().Name}.AddRenderPasses(): Missing material. {name} render pass will not be added.");
                return false;
            }

            return true;

        }
    }
}
