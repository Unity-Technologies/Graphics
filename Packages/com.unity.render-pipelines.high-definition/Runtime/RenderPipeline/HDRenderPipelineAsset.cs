using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System.Linq;
using UnityEditorInternal;
#endif
namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// High Definition Render Pipeline asset.
    /// </summary>
    [HDRPHelpURLAttribute("HDRP-Asset")]
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
                    maxPlanarReflectionProbePerCamera = currentPlatformRenderPipelineSettings.lightLoopSettings.maxPlanarReflectionOnScreen,
                    maxActivePlanarReflectionProbe = 512,
                    planarReflectionProbeSize = (int)PlanarReflectionAtlasResolution.Resolution512,
                    maxActiveReflectionProbe = 512,
                    reflectionProbeSize = (int)currentPlatformRenderPipelineSettings.lightLoopSettings.reflectionCubemapSize
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
            => globalSettings.renderingLayerMaskNames;

        /// <summary>Names used for display of rendering layer masks with a prefix.</summary>
        public override string[] prefixedRenderingLayerMaskNames
            => globalSettings.prefixedRenderingLayerMaskNames;

        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] lightLayerNames => globalSettings.lightLayerNames;

        /// <summary>
        /// Names used for display of decal layers.
        /// </summary>
        public string[] decalLayerNames => globalSettings.decalLayerNames;

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
