using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System.Linq;
using UnityEditorInternal;
// TODO @ SHADERS: Enable as many of the rules (currently commented out) as make sense
//                 once the setting asset aggregation behavior is finalized.  More fine tuning
//                 of these rules is also desirable (current rules have been interpreted from
//                 the variant stripping logic)
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif
namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// High Definition Render Pipeline asset.
    /// </summary>
    [HDRPHelpURLAttribute("HDRP-Asset")]
#if UNITY_EDITOR
    // [ShaderKeywordFilter.ApplyRulesIfTagsEqual("RenderPipeline", "HDRenderPipeline")]
#endif
    public partial class HDRenderPipelineAsset : RenderPipelineAsset<HDRenderPipeline>, IVirtualTexturingEnabledRenderPipeline, IProbeVolumeEnabledRenderPipeline, IGPUResidentRenderPipeline, IRenderGraphEnabledRenderPipeline, ISTPEnabledRenderPipeline
    {
        /// <inheritdoc/>
        public override string renderPipelineShaderTag => HDRenderPipeline.k_ShaderTagName;

        [System.NonSerialized]
        internal bool isInOnValidateCall = false;

        HDRenderPipelineAsset()
        {
        }

        void OnEnable()
        {
            ///////////////////////////
            // This is not optimal.
            // When using AssetCache, this is not called. [TODO: fix it]
            // It will be called though if you import it.
            Migrate();
            ///////////////////////////

            // Initialize the low res cloud tracing default value. This is to avoid upgrading the whole hdrp asset
            if (m_RenderPipelineSettings.dynamicResolutionSettings.lowResVolumetricCloudsMinimumThreshold == 0.0f)
                m_RenderPipelineSettings.dynamicResolutionSettings.lowResVolumetricCloudsMinimumThreshold = 50.0f;

            HDDynamicResolutionPlatformCapabilities.SetupFeatures();
        }

        void Reset()
        {
#if UNITY_EDITOR
            // we need to ensure we have a global settings asset to be able to create an HDRP Asset
            HDRenderPipelineGlobalSettings.Ensure(canCreateNewAsset: true);
#endif
            OnValidate();
        }

        /// <summary>
        /// Ensures Global Settings are ready and registered into GraphicsSettings
        /// </summary>
        protected override void EnsureGlobalSettings()
        {
            base.EnsureGlobalSettings();

#if UNITY_EDITOR
            HDRenderPipelineGlobalSettings.Ensure();
#endif
        }

        /// <summary>
        /// CreatePipeline implementation.
        /// </summary>
        /// <returns>A new HDRenderPipeline instance.</returns>
        protected override RenderPipeline CreatePipeline()
        {
            var renderPipeline = new HDRenderPipeline(this);

            IGPUResidentRenderPipeline.ReinitializeGPUResidentDrawer();

            return renderPipeline;
        }

        /// <summary>
        /// OnValidate implementation.
        /// </summary>
        protected override void OnValidate()
        {
            isInOnValidateCall = true;

            //Do not reconstruct the pipeline if we modify other assets.
            //OnValidate is called once at first selection of the asset.
            if (GraphicsSettings.currentRenderPipeline == this)
                base.OnValidate();

            isInOnValidateCall = false;
        }

        HDRenderPipelineGlobalSettings globalSettings => HDRenderPipelineGlobalSettings.instance;

        internal bool frameSettingsHistory { get; set; } = false;

        internal ReflectionSystemParameters reflectionSystemParameters
        {
            get
            {
                return new ReflectionSystemParameters
                {
                    maxActivePlanarReflectionProbe = 512,
                    maxActiveEnvReflectionProbe = 512
                };
            }
        }

        // Note: having m_RenderPipelineSettings serializable allows it to be modified in editor.
        // And having it private with a getter property force a copy.
        // As there is no setter, it thus cannot be modified by code.
        // This ensure immutability at runtime.

        // Store the various RenderPipelineSettings for each platform (for now only one)
        [SerializeField, FormerlySerializedAs("renderPipelineSettings")]
        RenderPipelineSettings m_RenderPipelineSettings = RenderPipelineSettings.NewDefault();

        /// <summary>
        /// Settings currently used by HDRP.
        /// Note that setting this property has a significant cost as it will cause the whole pipeline to be rebuilt from scratch.
        /// </summary>
        public RenderPipelineSettings currentPlatformRenderPipelineSettings { get => m_RenderPipelineSettings ; set { m_RenderPipelineSettings = value; OnValidate(); } }

        internal void TurnOffRayTracing()
        {
            m_RenderPipelineSettings.supportRayTracing = false;
        }

        [SerializeField]
        internal bool allowShaderVariantStripping = true;
        [SerializeField]
        internal bool enableSRPBatcher = true;

        /// <summary>Available material quality levels for this asset.</summary>
        [FormerlySerializedAs("materialQualityLevels")]
        public MaterialQuality availableMaterialQualityLevels = (MaterialQuality)(-1);

        [SerializeField, FormerlySerializedAs("m_CurrentMaterialQualityLevel")]
        private MaterialQuality m_DefaultMaterialQualityLevel = MaterialQuality.High;

        /// <summary>Default material quality level for this asset.</summary>
        public MaterialQuality defaultMaterialQualityLevel { get => m_DefaultMaterialQualityLevel; }

        [SerializeField]
        [Obsolete("Use HDRP Global Settings' diffusionProfileSettingsList instead")]
        internal DiffusionProfileSettings diffusionProfileSettings;

        [SerializeField]
        private VolumeProfile m_VolumeProfile;

        /// <summary>
        /// A volume profile that can be used to override global default volume profile values. This provides a way e.g.
        /// to have different volume default values per quality level without having to place global volumes in scenes.
        /// </summary>
        public VolumeProfile volumeProfile
        {
            get => m_VolumeProfile;
            set => m_VolumeProfile = value;
        }

        static string[] s_Names;
        static int[] s_Values;

        /// <summary>Names used for display of rendering layer masks.</summary>
        [Obsolete("This property is obsolete. Use RenderingLayerMask API and Tags & Layers project settings instead. #from(23.3)", false)]
        public override string[] renderingLayerMaskNames => UnityEngine.RenderingLayerMask.GetDefinedRenderingLayerNames();

        /// <summary>Names used for display of rendering layer masks with a prefix.</summary>
        [Obsolete("This property is obsolete. Use RenderingLayerMask API and Tags & Layers project settings instead. #from(23.3)", false)]
        public override string[] prefixedRenderingLayerMaskNames
            => Array.Empty<string>();

        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        [Obsolete("Use renderingLayerNames")]
        public string[] lightLayerNames => renderingLayerNames;

        /// <summary>
        /// Names used for display of decal layers.
        /// </summary>
        [Obsolete("Use renderingLayerNames")]
        public string[] decalLayerNames => renderingLayerNames;

        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        [Obsolete("This property is obsolete. Use RenderingLayerMask API and Tags & Layers project settings instead. #from(23.3)", false)]
        public string[] renderingLayerNames => UnityEngine.RenderingLayerMask.GetDefinedRenderingLayerNames();

        [SerializeField]
        internal VirtualTexturingSettingsSRP virtualTexturingSettings = new VirtualTexturingSettingsSRP();


        [SerializeField] private bool m_UseRenderGraph = true;

        internal bool useRenderGraph
        {
            get => m_UseRenderGraph;
            set => m_UseRenderGraph = value;
        }

        /// <inheritdoc/>
        public bool isImmediateModeSupported => true;

        [SerializeField] private CustomPostProcessVolumeComponentList m_CompositorCustomVolumeComponentsList = new(CustomPostProcessInjectionPoint.BeforePostProcess);

        internal CustomPostProcessVolumeComponentList compositorCustomVolumeComponentsList =>
            m_CompositorCustomVolumeComponentsList;

        /// <summary>
        /// Indicates if virtual texturing is currently enabled for this render pipeline instance.
        /// </summary>
        public bool virtualTexturingEnabled { get { return true; } }

        /// <summary>
        /// Indicates if this render pipeline instance supports Adaptive Probe Volume.
        /// </summary>
        public bool supportProbeVolume
        {
            get => currentPlatformRenderPipelineSettings.supportProbeVolume;
        }

        /// <summary>
        /// Indicates the maximum number of SH Bands used by this render pipeline instance.
        /// </summary>
        public ProbeVolumeSHBands maxSHBands
        {
            get
            {
                if (currentPlatformRenderPipelineSettings.supportProbeVolume)
                    return currentPlatformRenderPipelineSettings.probeVolumeSHBands;
                else
                    return ProbeVolumeSHBands.SphericalHarmonicsL1;
            }
        }


        /// <summary>
        /// Global settings struct for GPU Resident Drawer
        /// </summary>
        GPUResidentDrawerSettings IGPUResidentRenderPipeline.gpuResidentDrawerSettings => new()
        {
            mode = m_RenderPipelineSettings.gpuResidentDrawerSettings.mode,
            supportDitheringCrossFade = QualitySettings.enableLODCrossFade,
            enableOcclusionCulling = m_RenderPipelineSettings.gpuResidentDrawerSettings.enableOcclusionCullingInCameras,
            allowInEditMode = true,
            smallMeshScreenPercentage = m_RenderPipelineSettings.gpuResidentDrawerSettings.smallMeshScreenPercentage,
#if UNITY_EDITOR
            pickingShader = Shader.Find("Hidden/HDRP/BRGPicking"),
#endif
            loadingShader = Shader.Find("Hidden/HDRP/MaterialLoading"),
            errorShader = Shader.Find("Hidden/HDRP/MaterialError"),
        };

        /// <summary>
        /// GPUResidentDrawerMode configured on this pipeline asset
        /// </summary>
        public GPUResidentDrawerMode gpuResidentDrawerMode
        {
            get => m_RenderPipelineSettings.gpuResidentDrawerSettings.mode;
            set
            {
                if (value == m_RenderPipelineSettings.gpuResidentDrawerSettings.mode)
                    return;

                m_RenderPipelineSettings.gpuResidentDrawerSettings.mode = value;
                OnValidate();
            }
        }

		/// <summary>
        /// Returns the projects global ProbeVolumeSceneData instance.
        /// </summary>
        [Obsolete("This property is no longer necessary.")]
        public ProbeVolumeSceneData probeVolumeSceneData => null;

        /// <summary>
        /// Returns true if STP is used by the current dynamic resolution settings
        /// </summary>
        public bool isStpUsed
        {
            get
            {
                return m_RenderPipelineSettings.dynamicResolutionSettings.advancedUpscalersByPriority.Contains(AdvancedUpscalers.STP);
            }
        }
    }
}
