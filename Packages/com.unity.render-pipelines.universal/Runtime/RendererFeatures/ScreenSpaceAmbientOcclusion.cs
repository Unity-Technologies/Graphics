using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Shader keyword constants for Screen Space Ambient Occlusion.
    /// </summary>
    internal static class ScreenSpaceAmbientOcclusionKeywords
    {
        internal const string k_GTAOModeKeyword = "_GTAO_MODE";
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
        internal const string k_TemporalFilteringKeyword = "_TEMPORAL_FILTERING";
    }

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

#if MODERN_SSAO
        [SerializeField] internal ScreenSpaceAmbientOcclusionMode Mode = ScreenSpaceAmbientOcclusionMode.Standard;

        // GTAO Mode Parameters
        [SerializeField] internal int GTAOMinimumRadiusInPixels = 40;
        [SerializeField] internal bool UseComputeShader = false;
        [SerializeField] internal bool GTAOTemporalFilterEnabled = false;
        [SerializeField] internal float GTAOGhostingMitigation = 0.5f;
        [SerializeField] internal float GTAOHistoryLength = 0.9f;
        [SerializeField] internal int GTAODirectionCount = 2;
        [SerializeField] internal int GTAOStepCount = 4;
        [SerializeField] internal ScreenSpaceAmbientOcclusionSpatialFilter GTAOSpatialFilter = ScreenSpaceAmbientOcclusionSpatialFilter.Bilateral;

        internal bool NeedsComputeShader => Mode == ScreenSpaceAmbientOcclusionMode.GTAO && UseComputeShader;
        internal bool IsTemporalFilterActive => NeedsComputeShader && GTAOTemporalFilterEnabled;
#endif

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
    [Scripting.APIUpdating.MovedFrom(false, sourceClassName: "ScreenSpaceAmbientOcclusionPersistentResources")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: SSAO Shader", Order = 1000)]
    [Categorization.ElementInfo(Order = 0), HideInInspector]
    class ScreenSpaceAmbientOcclusionCoreResources : IRenderPipelineResources
    {
        [SerializeField]
        [ResourcePath("Shaders/Utils/ScreenSpaceAmbientOcclusion.shader"), FormerlySerializedAs("m_Shader")]
        Shader m_RasterizationShader;

        public Shader RasterizationShader
        {
            get => m_RasterizationShader;
            set => this.SetValueAndNotify(ref m_RasterizationShader, value);
        }

#if MODERN_SSAO
        [SerializeField]
        [ResourcePath("Shaders/Utils/GTAO.compute")]
        ComputeShader m_GTAOComputeShader;

        public ComputeShader GTAOComputeShader
        {
            get => m_GTAOComputeShader;
            set => this.SetValueAndNotify(ref m_GTAOComputeShader, value);
        }
#endif

        public bool isAvailableInPlayerBuild => true;

        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>Current version of the resource container. Used only for upgrading a project.</summary>
        public int version => m_Version;
    }

    [Serializable]
    [Scripting.APIUpdating.MovedFrom(false, sourceClassName: "ScreenSpaceAmbientOcclusionDynamicResources")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: SSAO Noise Textures", Order = 1000)]
    [Categorization.ElementInfo(Order = 0), HideInInspector]
    class ScreenSpaceAmbientOcclusionBlueNoiseResources : IRenderPipelineResources
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
    [URPHelpURL("urp/post-processing-ssao")]
    public class ScreenSpaceAmbientOcclusion : ScriptableRendererFeature
    {
        // Serialized Fields
        [SerializeField] private ScreenSpaceAmbientOcclusionSettings m_Settings = new ScreenSpaceAmbientOcclusionSettings();

        // Private Fields
        private AAOPass m_AAOPass = null;
#if MODERN_SSAO
        private GTAOPass m_GTAOPass = null;
#endif
        private Shader m_RasterizationShader;
        private Texture2D[] m_BlueNoise256Textures;

        // Internal
#if MODERN_SSAO
        [Obsolete("Configuring SSAO via the renderer feature settings is deprecated. Use the ScreenSpaceAmbientOcclusionVolumeOverride volume component instead.", false)]
#endif
        internal ref ScreenSpaceAmbientOcclusionSettings settings => ref m_Settings;

        private struct FeatureSettings
        {
            public bool afterOpaque;
            public bool isDepthNormalsSource;
            public RenderPassEvent passEvent;
            public ScriptableRenderPassInput requirements;
            public ScreenSpaceAmbientOcclusionSettings.DepthSource effectiveDepthSource;
        }

        /// <inheritdoc/>
        public override void Create()
        {
            TryPrepareResources();

            // Create the passes
            if (m_RasterizationShader != null)
            {
                m_AAOPass = new AAOPass(m_RasterizationShader, m_BlueNoise256Textures);
#if MODERN_SSAO
                m_GTAOPass = new GTAOPass(m_RasterizationShader, m_BlueNoise256Textures);
#endif
            }

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

            if (m_AAOPass == null)
                return;

#if MODERN_SSAO
            // To preserve backward compatibility with existing renderer feature setups, the volume only takes over when any parameter overriden.
            var ssaoVolume = VolumeManager.instance.stack.GetComponent<ScreenSpaceAmbientOcclusionVolumeOverride>();
            bool useVolume = ssaoVolume.AnyPropertiesIsOverridden();
#endif

            bool usesDeferred = renderer is UniversalRenderer { usesDeferredLighting: true };
            bool shouldAdd;
            FeatureSettings resolvedSettings;
            ScriptableRenderPass activePass;

#if MODERN_SSAO
            if (useVolume)
            {
                resolvedSettings = ResolveFeatureSettings(ssaoVolume, usesDeferred);
                bool useGTAO = !ssaoVolume.IsStandardMode();

                if (useGTAO)
                {
                    shouldAdd = m_GTAOPass.Setup(ssaoVolume);
                    activePass = m_GTAOPass;
                }
                else
                {
                    shouldAdd = m_AAOPass.Setup(ssaoVolume, usesDeferred);
                    activePass = m_AAOPass;
                }
            }
            else
#endif
            {
                resolvedSettings = ResolveFeatureSettings(m_Settings, usesDeferred);
                shouldAdd = m_AAOPass.Setup(m_Settings, resolvedSettings.effectiveDepthSource);
                activePass = m_AAOPass;
            }

            if (!shouldAdd)
                return;

            if (renderer is UniversalRenderer universalRenderer && universalRenderer.useTileOnlyMode)
            {
                if (!RenderingUtils.IsCompatibleWithTileOnlyMode(resolvedSettings.requirements, resolvedSettings.passEvent))
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
                if (resolvedSettings.afterOpaque && resolvedSettings.isDepthNormalsSource)
                {
                    Debug.LogErrorFormat(
                        "Screen Space Ambient Occlusion \"{0}\": the current settings are not compatible with Tile-Only Mode. Open the Universal Renderer \"{1}\" in the Inspector for more information.",
                        name, renderer.name);
                    return;
                }
            }

            activePass.renderPassEvent = resolvedSettings.passEvent;
            activePass.ConfigureInput(resolvedSettings.requirements);
            renderer.EnqueuePass(activePass);
        }

#if MODERN_SSAO
        private static FeatureSettings ResolveFeatureSettings(ScreenSpaceAmbientOcclusionVolumeOverride volume, bool usesDeferred)
        {
            var featureSettings = new FeatureSettings();

            if (!volume.IsActive())
                return featureSettings;

            featureSettings.afterOpaque = volume.afterOpaque;
            featureSettings.passEvent = usesDeferred
                ? (featureSettings.afterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingPrePasses)
                : (featureSettings.afterOpaque ? RenderPassEvent.BeforeRenderingTransparents : RenderPassEvent.AfterRenderingPrePasses + 1);

            featureSettings.requirements = ScriptableRenderPassInput.Depth;
            bool isGTAO = !volume.IsStandardMode();
            featureSettings.isDepthNormalsSource = isGTAO || usesDeferred || volume.depthSource == ScreenSpaceAmbientOcclusionDepthSource.DepthNormals;
            if (featureSettings.isDepthNormalsSource)
                featureSettings.requirements |= ScriptableRenderPassInput.Normal;
            if (isGTAO && volume.useComputeShader && volume.temporalFilter)
                featureSettings.requirements |= ScriptableRenderPassInput.Motion;

            return featureSettings;
        }
#endif

        private static FeatureSettings ResolveFeatureSettings(ScreenSpaceAmbientOcclusionSettings settings, bool usesDeferred)
        {
            var featureSettings = new FeatureSettings();
            featureSettings.afterOpaque = settings.AfterOpaque;
            featureSettings.effectiveDepthSource = usesDeferred ? ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals : settings.Source;

            if (usesDeferred)
                featureSettings.passEvent = featureSettings.afterOpaque ? RenderPassEvent.AfterRenderingOpaques : RenderPassEvent.AfterRenderingPrePasses;
            else
                featureSettings.passEvent = featureSettings.afterOpaque ? RenderPassEvent.BeforeRenderingTransparents : RenderPassEvent.AfterRenderingPrePasses + 1;

            featureSettings.requirements = featureSettings.effectiveDepthSource == ScreenSpaceAmbientOcclusionSettings.DepthSource.Depth
                ? ScriptableRenderPassInput.Depth
                : ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;

            featureSettings.isDepthNormalsSource = featureSettings.effectiveDepthSource == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
            return featureSettings;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            m_AAOPass?.Dispose();
            m_AAOPass = null;
#if MODERN_SSAO
            m_GTAOPass?.Dispose();
            m_GTAOPass = null;
#endif
        }

        void TryPrepareResources()
        {
            if (m_RasterizationShader == null)
            {
                if (!GraphicsSettings.TryGetRenderPipelineSettings<ScreenSpaceAmbientOcclusionCoreResources>(out var ssaoCoreResources))
                {
                    Debug.LogErrorFormat(
                        $"Couldn't find the required resources for the {nameof(ScreenSpaceAmbientOcclusion)} render feature. If this exception appears in the Player, make sure at least one {nameof(ScreenSpaceAmbientOcclusion)} render feature is enabled or adjust your stripping settings.");
                }

                m_RasterizationShader = ssaoCoreResources.RasterizationShader;
            }

            if (m_BlueNoise256Textures == null || m_BlueNoise256Textures.Length == 0)
            {
                if (GraphicsSettings.TryGetRenderPipelineSettings<ScreenSpaceAmbientOcclusionBlueNoiseResources>(out var ssaoBlueNoiseResources))
                    m_BlueNoise256Textures = ssaoBlueNoiseResources.BlueNoise256Textures;
            }
        }
    }
}
