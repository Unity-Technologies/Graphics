using System;
using System.Collections.Generic;
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
    public partial class HDRenderPipelineAsset : RenderPipelineAsset, IVirtualTexturingEnabledRenderPipeline
    {
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

            HDRenderPipeline.SetupDLSSFeature(HDRenderPipelineGlobalSettings.instance);
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
            => new HDRenderPipeline(this);

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

        internal HDRenderPipelineRuntimeResources renderPipelineResources
        {
            get { return globalSettings.renderPipelineResources; }
        }

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

        /// <summary>Return the current use RenderPipelineSettings (i.e for the current platform)</summary>
        public RenderPipelineSettings currentPlatformRenderPipelineSettings => m_RenderPipelineSettings;

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

        /// <summary>Names used for display of rendering layer masks.</summary>
        public override string[] renderingLayerMaskNames
            => globalSettings.renderingLayerNames;

        /// <summary>Names used for display of rendering layer masks with a prefix.</summary>
        public override string[] prefixedRenderingLayerMaskNames
            => globalSettings.prefixedRenderingLayerNames;

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
        public string[] renderingLayerNames => globalSettings.renderingLayerNames;

        /// <summary>HDRP default shader.</summary>
        public override Shader defaultShader
            => globalSettings?.renderPipelineResources?.shaders.defaultPS;

        [SerializeField]
        internal VirtualTexturingSettingsSRP virtualTexturingSettings = new VirtualTexturingSettingsSRP();


        [SerializeField] private bool m_UseRenderGraph = true;

        internal bool useRenderGraph
        {
            get => m_UseRenderGraph;
            set => m_UseRenderGraph = value;
        }

#if UNITY_EDITOR
        /// <summary>HDRP default material.</summary>
        public override Material defaultMaterial
            => globalSettings?.renderPipelineEditorResources?.materials.defaultDiffuseMat;

        // call to GetAutodeskInteractiveShaderXXX are only from within editor
        /// <summary>HDRP default autodesk interactive shader.</summary>
        public override Shader autodeskInteractiveShader
            => globalSettings?.renderPipelineEditorResources?.shaderGraphs.autodeskInteractive;

        /// <summary>HDRP default autodesk interactive transparent shader.</summary>
        public override Shader autodeskInteractiveTransparentShader
            => globalSettings?.renderPipelineEditorResources?.shaderGraphs.autodeskInteractiveTransparent;

        /// <summary>HDRP default autodesk interactive masked shader.</summary>
        public override Shader autodeskInteractiveMaskedShader
            => globalSettings?.renderPipelineEditorResources?.shaderGraphs.autodeskInteractiveMasked;

        /// <summary>HDRP default terrain detail lit shader.</summary>
        public override Shader terrainDetailLitShader
            => globalSettings?.renderPipelineEditorResources?.shaders.terrainDetailLitShader;

        /// <summary>HDRP default terrain detail grass shader.</summary>
        public override Shader terrainDetailGrassShader
            => globalSettings?.renderPipelineEditorResources?.shaders.terrainDetailGrassShader;

        /// <summary>HDRP default terrain detail grass billboard shader.</summary>
        public override Shader terrainDetailGrassBillboardShader
            => globalSettings?.renderPipelineEditorResources?.shaders.terrainDetailGrassBillboardShader;

        public override Shader defaultSpeedTree8Shader
            => globalSettings?.renderPipelineEditorResources?.shaderGraphs.defaultSpeedTree8Shader;

        // Note: This function is HD specific
        /// <summary>HDRP default Decal material.</summary>
        public Material GetDefaultDecalMaterial()
            => globalSettings?.renderPipelineEditorResources?.materials.defaultDecalMat;

        // Note: This function is HD specific
        /// <summary>HDRP default mirror material.</summary>
        public Material GetDefaultMirrorMaterial()
            => globalSettings?.renderPipelineEditorResources?.materials.defaultMirrorMat;

        /// <summary>HDRP default particles material.</summary>
        public override Material defaultParticleMaterial
            => globalSettings?.renderPipelineEditorResources?.materials.defaultParticleMat;

        /// <summary>HDRP default terrain material.</summary>
        public override Material defaultTerrainMaterial
            => globalSettings?.renderPipelineEditorResources?.materials.defaultTerrainMat;

        /// <inheritdoc/>
        public override string renderPipelineShaderTag => HDRenderPipeline.k_ShaderTagName;

        // Array structure that allow us to manipulate the set of defines that the HD render pipeline needs
        List<string> defineArray = new List<string>();

        bool UpdateDefineList(bool flagValue, string defineMacroValue)
        {
            bool macroExists = defineArray.Contains(defineMacroValue);
            if (flagValue)
            {
                if (!macroExists)
                {
                    defineArray.Add(defineMacroValue);
                    return true;
                }
            }
            else
            {
                if (macroExists)
                {
                    defineArray.Remove(defineMacroValue);
                    return true;
                }
            }
            return false;
        }

#endif
        /// <summary>
        /// Indicates if virtual texturing is currently enabled for this render pipeline instance.
        /// </summary>
        public bool virtualTexturingEnabled { get { return true; } }
    }
}
