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
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "HDRP-Asset" + Documentation.endURL)]
    public partial class HDRenderPipelineAsset : RenderPipelineAsset, IVirtualTexturingEnabledRenderPipeline
    {
        [System.NonSerialized]
        internal bool isInOnValidateCall = false;

        HDRenderPipelineAsset()
        {
        }

        void Reset() => OnValidate();

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
#if UNITY_EDITOR
            HDDefaultSettings.Ensure();
#endif
            //Do not reconstruct the pipeline if we modify other assets.
            //OnValidate is called once at first selection of the asset.
            if (GraphicsSettings.currentRenderPipeline == this)
                base.OnValidate();

            UpdateRenderingLayerNames();

            isInOnValidateCall = false;
        }

        public HDDefaultSettings defaultSettings => HDDefaultSettings.instance;

        internal RenderPipelineResources renderPipelineResources
        {
            get { return defaultSettings.renderPipelineResources; }
            set { defaultSettings.renderPipelineResources = value; }
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
        [Obsolete("Use diffusionProfileSettingsList instead")]
        internal DiffusionProfileSettings diffusionProfileSettings;

        void UpdateRenderingLayerNames()
        {
            m_RenderingLayerNames = new string[32];

            m_RenderingLayerNames[0] = HDDefaultSettings.instance.lightLayerName0;
            m_RenderingLayerNames[1] = HDDefaultSettings.instance.lightLayerName1;
            m_RenderingLayerNames[2] = HDDefaultSettings.instance.lightLayerName2;
            m_RenderingLayerNames[3] = HDDefaultSettings.instance.lightLayerName3;
            m_RenderingLayerNames[4] = HDDefaultSettings.instance.lightLayerName4;
            m_RenderingLayerNames[5] = HDDefaultSettings.instance.lightLayerName5;
            m_RenderingLayerNames[6] = HDDefaultSettings.instance.lightLayerName6;
            m_RenderingLayerNames[7] = HDDefaultSettings.instance.lightLayerName7;

            m_RenderingLayerNames[8]  = HDDefaultSettings.instance.decalLayerName0;
            m_RenderingLayerNames[9]  = HDDefaultSettings.instance.decalLayerName1;
            m_RenderingLayerNames[10] = HDDefaultSettings.instance.decalLayerName2;
            m_RenderingLayerNames[11] = HDDefaultSettings.instance.decalLayerName3;
            m_RenderingLayerNames[12] = HDDefaultSettings.instance.decalLayerName4;
            m_RenderingLayerNames[13] = HDDefaultSettings.instance.decalLayerName5;
            m_RenderingLayerNames[14] = HDDefaultSettings.instance.decalLayerName6;
            m_RenderingLayerNames[15] = HDDefaultSettings.instance.decalLayerName7;

            // Unused
            for (int i = 16; i < m_RenderingLayerNames.Length; ++i)
            {
                m_RenderingLayerNames[i] = string.Format("Unused {0}", i);
            }
        }

        // HDRP use GetRenderingLayerMaskNames to create its light linking system
        // Mean here we define our name for light linking.
        [System.NonSerialized]
        string[] m_RenderingLayerNames;
        string[] renderingLayerNames
        {
            get
            {
                if (m_RenderingLayerNames == null)
                {
                    UpdateRenderingLayerNames();
                }

                return m_RenderingLayerNames;
            }
        }

        /// <summary>Names used for display of rendering layer masks.</summary>
        public override string[] renderingLayerMaskNames
            => renderingLayerNames;

        [System.NonSerialized]
        string[] m_LightLayerNames = null;
        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] lightLayerNames
        {
            get
            {
                if (m_LightLayerNames == null)
                {
                    m_LightLayerNames = new string[8];
                }

                for (int i = 0; i < 8; ++i)
                {
                    m_LightLayerNames[i] = renderingLayerNames[i];
                }

                return m_LightLayerNames;
            }
        }

        //TODOJENNY - move to Default Settings

        [System.NonSerialized]
        string[] m_DecalLayerNames = null;
        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] decalLayerNames
        {
            get
            {
                if (m_DecalLayerNames == null)
                {
                    m_DecalLayerNames = new string[8];
                }

                for (int i = 0; i < 8; ++i)
                {
                    m_DecalLayerNames[i] = renderingLayerNames[i + 8];
                }

                return m_DecalLayerNames;
            }
        }

        /// <summary>HDRP default shader.</summary>
        public override Shader defaultShader
            => defaultSettings.renderPipelineResources?.shaders.defaultPS;

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
            => defaultSettings.renderPipelineEditorResources?.materials.defaultDiffuseMat;

        // call to GetAutodeskInteractiveShaderXXX are only from within editor
        /// <summary>HDRP default autodesk interactive shader.</summary>
        public override Shader autodeskInteractiveShader
            => defaultSettings.renderPipelineEditorResources.shaderGraphs.autodeskInteractive;

        /// <summary>HDRP default autodesk interactive transparent shader.</summary>
        public override Shader autodeskInteractiveTransparentShader
            => defaultSettings.renderPipelineEditorResources.shaderGraphs.autodeskInteractiveTransparent;

        /// <summary>HDRP default autodesk interactive masked shader.</summary>
        public override Shader autodeskInteractiveMaskedShader
            => defaultSettings.renderPipelineEditorResources.shaderGraphs.autodeskInteractiveMasked;

        /// <summary>HDRP default terrain detail lit shader.</summary>
        public override Shader terrainDetailLitShader
            => defaultSettings.renderPipelineEditorResources.shaders.terrainDetailLitShader;

        /// <summary>HDRP default terrain detail grass shader.</summary>
        public override Shader terrainDetailGrassShader
            => defaultSettings.renderPipelineEditorResources.shaders.terrainDetailGrassShader;

        /// <summary>HDRP default terrain detail grass billboard shader.</summary>
        public override Shader terrainDetailGrassBillboardShader
            => defaultSettings.renderPipelineEditorResources.shaders.terrainDetailGrassBillboardShader;

        // Note: This function is HD specific
        /// <summary>HDRP default Decal material.</summary>
        public Material GetDefaultDecalMaterial()
            => defaultSettings.renderPipelineEditorResources.materials.defaultDecalMat;

        // Note: This function is HD specific
        /// <summary>HDRP default mirror material.</summary>
        public Material GetDefaultMirrorMaterial()
            => defaultSettings.renderPipelineEditorResources.materials.defaultMirrorMat;

        /// <summary>HDRP default particles material.</summary>
        public override Material defaultParticleMaterial
            => defaultSettings.renderPipelineEditorResources.materials.defaultParticleMat;

        /// <summary>HDRP default terrain material.</summary>
        public override Material defaultTerrainMaterial
            => defaultSettings.renderPipelineEditorResources.materials.defaultTerrainMat;

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

        // This function allows us to raise or remove some preprocessing defines based on the render pipeline settings
        internal void EvaluateSettings()
        {
            // Grab the current set of defines and split them
            string currentDefineList = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone);
            defineArray.Clear();
            defineArray.AddRange(currentDefineList.Split(';'));

            // Update all the individual defines
            bool needUpdate = false;
            needUpdate |= UpdateDefineList(HDRenderPipeline.GatherRayTracingSupport(currentPlatformRenderPipelineSettings), "ENABLE_RAYTRACING");

            // Only set if it changed
            if (needUpdate)
            {
                UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(UnityEditor.BuildTargetGroup.Standalone, string.Join(";", defineArray.ToArray()));
            }
        }

#endif

        /// <summary>
        /// Indicates if virtual texturing is currently enabled for this render pipeline instance.
        /// </summary>
        public bool virtualTexturingEnabled { get { return true; } }
    }
}
