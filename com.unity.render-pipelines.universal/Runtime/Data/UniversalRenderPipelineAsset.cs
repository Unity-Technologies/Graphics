using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEditorInternal;
#endif
using System.ComponentModel;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    public enum MsaaQuality
    {
        Disabled = 1,
        _2x = 2,
        _4x = 4,
        _8x = 8
    }

    internal enum DefaultMaterialType
    {
        Standard,
        Particle,
        Terrain,
        Sprite,
        UnityBuiltinDefault,
        SpriteMask,
        Decal
    }

    [Obsolete("PipelineDebugLevel is unused and has no effect.", false)]
    public enum PipelineDebugLevel
    {
        Disabled,
        Profiling,
    }

    public enum RendererType
    {
        Custom,
        UniversalRenderer,
        _2DRenderer,
        [Obsolete("ForwardRenderer has been renamed (UnityUpgradable) -> UniversalRenderer", true)]
        ForwardRenderer = UniversalRenderer,
    }

    public enum ColorGradingMode
    {
        LowDynamicRange,
        HighDynamicRange
    }

    /// <summary>
    /// Defines the update frequency for the Volume Framework.
    /// </summary>
    public enum VolumeFrameworkUpdateMode
    {
        [InspectorName("Every Frame")]
        EveryFrame = 0,
        [InspectorName("Via Scripting")]
        ViaScripting = 1,
        [InspectorName("Use Pipeline Settings")]
        UsePipelineSettings = 2,
    }

    [ExcludeFromPreset]
    public partial class UniversalRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
        Shader m_DefaultShader;
        internal ScriptableRenderer[] m_Renderers = new ScriptableRenderer[1];

        // Default values set when a new UniversalRenderPipeline asset is created
        [SerializeField] int k_AssetVersion = 9;
        [SerializeField] int k_AssetPreviousVersion = 9;

        // Deprecated settings for upgrading sakes
        [SerializeField] RendererType m_RendererType = RendererType.UniversalRenderer;
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SerializeField] internal ScriptableRendererData m_RendererData = null;

        // Renderer settings
        [SerializeField] internal ScriptableRendererData[] m_RendererDataList = new ScriptableRendererData[1];
        [SerializeField] internal int m_DefaultRendererIndex = 0;

        // Quality settings
        [SerializeField] bool m_SupportsHDR = true;
        [SerializeField] MsaaQuality m_MSAA = MsaaQuality.Disabled;
        // TODO: Shader Quality Tiers

        // Advanced settings
        [SerializeField] bool m_UseSRPBatcher = true;
        [SerializeField] bool m_SupportsDynamicBatching = false;

        // Adaptive performance settings
        [SerializeField] bool m_UseAdaptivePerformance = true;

        // Deprecated settings
        [SerializeField] ShadowQuality m_ShadowType = ShadowQuality.HardShadows;
        [SerializeField] bool m_LocalShadowsSupported = false;
        [SerializeField] ShadowResolution m_LocalShadowsAtlasResolution = ShadowResolution._256;
        [SerializeField] int m_MaxPixelLights = 0;
        [SerializeField] ShadowResolution m_ShadowAtlasResolution = ShadowResolution._256;

#if UNITY_EDITOR
        [NonSerialized]
        internal UniversalRenderPipelineEditorResources m_EditorResourcesAsset;

        public static readonly string packagePath = "Packages/com.unity.render-pipelines.universal";
        public static readonly string editorResourcesGUID = "a3d8d823eedde654bb4c11a1cfaf1abb";

        public static UniversalRenderPipelineAsset Create(ScriptableRendererData rendererData = null)
        {
            // Create Universal RP Asset
            var instance = CreateInstance<UniversalRenderPipelineAsset>();
            if (rendererData != null)
                instance.m_RendererDataList[0] = rendererData;
            else
                instance.m_RendererDataList[0] = CreateInstance<UniversalRendererData>();

            // Initialize default Renderer
            instance.m_EditorResourcesAsset = instance.editorResources;

            return instance;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateUniversalPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                //Create asset
                AssetDatabase.CreateAsset(Create(CreateRendererAsset(pathName, RendererType.UniversalRenderer)), pathName);
            }
        }

        [MenuItem("Assets/Create/Rendering/URP Asset (with Universal Renderer)", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        static void CreateUniversalPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUniversalPipelineAsset>(),
                "New Universal Render Pipeline Asset.asset", null, null);
        }

        internal static ScriptableRendererData CreateRendererAsset(string path, RendererType type, bool relativePath = true, string suffix = "Renderer")
        {
            ScriptableRendererData data = CreateRendererData(type);
            string dataPath;
            if (relativePath)
                dataPath =
                    $"{Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path))}_{suffix}{Path.GetExtension(path)}";
            else
                dataPath = path;
            AssetDatabase.CreateAsset(data, dataPath);
            ResourceReloader.ReloadAllNullIn(data, packagePath);
            return data;
        }

        static ScriptableRendererData CreateRendererData(RendererType type)
        {
            switch (type)
            {
                case RendererType.UniversalRenderer:
                default:
                {
                    var rendererData = CreateInstance<UniversalRendererData>();
                    if (UniversalRenderPipelineGlobalSettings.instance.postProcessData == null)
                        UniversalRenderPipelineGlobalSettings.instance.postProcessData = PostProcessData.GetDefaultPostProcessData();
                    return rendererData;
                }
                // 2D renderer is experimental
                case RendererType._2DRenderer:
                {
                    var rendererData = CreateInstance<Renderer2DData>();
                    if (UniversalRenderPipelineGlobalSettings.instance.postProcessData == null)
                        UniversalRenderPipelineGlobalSettings.instance.postProcessData = PostProcessData.GetDefaultPostProcessData();
                    return rendererData;
                    // Universal Renderer is the fallback renderer that works on all platforms
                }
            }
        }

        // Hide: User aren't suppose to have to create it.
        //[MenuItem("Assets/Create/Rendering/URP Editor Resources", priority = CoreUtils.Sections.section8 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreateUniversalPipelineEditorResources()
        {
            var instance = CreateInstance<UniversalRenderPipelineEditorResources>();
            ResourceReloader.ReloadAllNullIn(instance, packagePath);
            AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(UniversalRenderPipelineEditorResources).Name));
        }

        UniversalRenderPipelineEditorResources editorResources
        {
            get
            {
                if (m_EditorResourcesAsset != null && !m_EditorResourcesAsset.Equals(null))
                    return m_EditorResourcesAsset;

                string resourcePath = AssetDatabase.GUIDToAssetPath(editorResourcesGUID);
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                m_EditorResourcesAsset = objs != null && objs.Length > 0 ? objs.First() as UniversalRenderPipelineEditorResources : null;
                return m_EditorResourcesAsset;
            }
        }
#endif

        public ScriptableRendererData LoadBuiltinRendererData(RendererType type = RendererType.UniversalRenderer)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            return m_RendererDataList[0] =
                CreateRendererAsset("Assets/UniversalRenderer.asset", type, false);
#else
            m_RendererDataList[0] = null;
            return m_RendererDataList[0];
#endif
        }

        protected override RenderPipeline CreatePipeline()
        {
            if (m_RendererDataList == null)
                m_RendererDataList = new ScriptableRendererData[1];

            // If no default data we can't create pipeline instance
            if (m_RendererDataList[m_DefaultRendererIndex] == null)
            {
                // If previous version and current version are miss-matched then we are waiting for the upgrader to kick in
                if (k_AssetPreviousVersion != k_AssetVersion)
                    return null;

                if (m_RendererDataList[m_DefaultRendererIndex].GetType().ToString()
                    .Contains("Universal.ForwardRendererData"))
                    return null;

                Debug.LogError(
                    $"Default Renderer is missing, make sure there is a Renderer assigned as the default on the current Universal RP asset:{UniversalRenderPipeline.asset.name}",
                    this);
                return null;
            }

            CreateRenderers();
            return new UniversalRenderPipeline(this);
        }

        void DestroyRenderers()
        {
            if (m_Renderers == null)
                return;

            for (int i = 0; i < m_Renderers.Length; i++)
                DestroyRenderer(ref m_Renderers[i]);
        }

        void DestroyRenderer(ref ScriptableRenderer renderer)
        {
            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }
        }

        protected override void OnValidate()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnValidate();
        }

        protected override void OnDisable()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnDisable();
        }

        void CreateRenderers()
        {
            DestroyRenderers();

            if (m_Renderers == null || m_Renderers.Length != m_RendererDataList.Length)
                m_Renderers = new ScriptableRenderer[m_RendererDataList.Length];

            for (int i = 0; i < m_RendererDataList.Length; ++i)
            {
                if (m_RendererDataList[i] != null)
                    m_Renderers[i] = m_RendererDataList[i].InternalCreateRenderer();
            }
        }

        Material GetMaterial(DefaultMaterialType materialType)
        {
#if UNITY_EDITOR
            if (scriptableRendererData == null || editorResources == null)
                return null;

            var material = scriptableRendererData.GetDefaultMaterial(materialType);
            if (material != null)
                return material;

            switch (materialType)
            {
                case DefaultMaterialType.Standard:
                    return editorResources.materials.lit;

                case DefaultMaterialType.Particle:
                    return editorResources.materials.particleLit;

                case DefaultMaterialType.Terrain:
                    return editorResources.materials.terrainLit;

                case DefaultMaterialType.Decal:
                    return editorResources.materials.decal;

                // Unity Builtin Default
                default:
                    return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Returns the default renderer being used by this pipeline.
        /// </summary>
        public ScriptableRenderer scriptableRenderer
        {
            get
            {
                if (m_RendererDataList?.Length > m_DefaultRendererIndex && m_RendererDataList[m_DefaultRendererIndex] == null)
                {
                    Debug.LogError("Default renderer is missing from the current Pipeline Asset.", this);
                    return null;
                }

                if (scriptableRendererData.isInvalidated || m_Renderers[m_DefaultRendererIndex] == null)
                {
                    DestroyRenderer(ref m_Renderers[m_DefaultRendererIndex]);
                    m_Renderers[m_DefaultRendererIndex] = scriptableRendererData.InternalCreateRenderer();
                }

                return m_Renderers[m_DefaultRendererIndex];
            }
        }

        /// <summary>
        /// Returns a renderer from the current pipeline asset
        /// </summary>
        /// <param name="index">Index to the renderer. If invalid index is passed, the default renderer is returned instead.</param>
        /// <returns></returns>
        public ScriptableRenderer GetRenderer(int index)
        {
            if (index == -1)
                index = m_DefaultRendererIndex;

            if (index >= m_RendererDataList.Length || index < 0 || m_RendererDataList[index] == null)
            {
                Debug.LogWarning(
                    $"Renderer at index {index.ToString()} is missing, falling back to Default Renderer {m_RendererDataList[m_DefaultRendererIndex].name}",
                    this);
                index = m_DefaultRendererIndex;
            }

            // RendererData list differs from RendererList. Create RendererList.
            if (m_Renderers == null || m_Renderers.Length < m_RendererDataList.Length)
                CreateRenderers();

            // This renderer data is outdated or invalid, we recreate the renderer
            // so we construct all render passes with the updated data
            if (m_RendererDataList[index].isInvalidated || m_Renderers[index] == null)
            {
                DestroyRenderer(ref m_Renderers[index]);
                m_Renderers[index] = m_RendererDataList[index].InternalCreateRenderer();
            }

            return m_Renderers[index];
        }

        internal ScriptableRendererData scriptableRendererData
        {
            get
            {
                if (m_RendererDataList[m_DefaultRendererIndex] == null)
                    CreatePipeline();

                return m_RendererDataList[m_DefaultRendererIndex];
            }
        }

#if UNITY_EDITOR
        internal GUIContent[] rendererDisplayList
        {
            get
            {
                GUIContent[] list = new GUIContent[m_RendererDataList.Length + 1];
                list[0] = new GUIContent($"Default Renderer ({RendererDataDisplayName(m_RendererDataList[m_DefaultRendererIndex])})");

                for (var i = 1; i < list.Length; i++)
                {
                    list[i] = new GUIContent($"{(i - 1).ToString()}: {RendererDataDisplayName(m_RendererDataList[i - 1])}");
                }
                return list;
            }
        }

        string RendererDataDisplayName(ScriptableRendererData data)
        {
            if (data != null)
                return data.name;

            return "NULL (Missing RendererData)";
        }

#endif

        internal int[] rendererIndexList
        {
            get
            {
                int[] list = new int[m_RendererDataList.Length + 1];
                for (int i = 0; i < list.Length; i++)
                {
                    list[i] = i - 1;
                }
                return list;
            }
        }

        public bool supportsHDR
        {
            get { return m_SupportsHDR; }
            set { m_SupportsHDR = value; }
        }

        public int msaaSampleCount
        {
            get { return (int)m_MSAA; }
            set { m_MSAA = (MsaaQuality)value; }
        }

        public bool supportsDynamicBatching
        {
            get { return m_SupportsDynamicBatching; }
            set { m_SupportsDynamicBatching = value; }
        }


        [Obsolete("PipelineDebugLevel is deprecated. Calling debugLevel is not necessary.", false)]
        public PipelineDebugLevel debugLevel
        {
            get => PipelineDebugLevel.Disabled;
        }

        public bool useSRPBatcher
        {
            get { return m_UseSRPBatcher; }
            set { m_UseSRPBatcher = value; }
        }



        /// <summary>
        /// Set to true to allow Adaptive performance to modify graphics quality settings during runtime.
        /// Only applicable when Adaptive performance package is available.
        /// </summary>
        public bool useAdaptivePerformance
        {
            get { return m_UseAdaptivePerformance; }
            set { m_UseAdaptivePerformance = value; }
        }

        public override Material defaultMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Standard); }
        }

        public override Material defaultParticleMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        public override Material defaultLineMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        public override Material defaultTerrainMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Terrain); }
        }

        public override Material defaultUIMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material defaultUIOverdrawMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material defaultUIETC1SupportedMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        /// <summary>
        /// Returns the default material for the 2D renderer.
        /// </summary>
        /// <returns>Returns the material containing the default lit and unlit shader passes for sprites in the 2D renderer.</returns>
        public override Material default2DMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Sprite); }
        }

        /// <summary>
        /// Returns the default sprite mask material for the 2D renderer.
        /// </summary>
        /// <returns>Returns the material containing the default shader pass for sprite mask in the 2D renderer.</returns>
        public override Material default2DMaskMaterial
        {
            get { return GetMaterial(DefaultMaterialType.SpriteMask); }
        }

        public Material decalMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Decal); }
        }

        public override Shader defaultShader
        {
            get
            {
#if UNITY_EDITOR
                // TODO: When importing project, AssetPreviewUpdater:CreatePreviewForAsset will be called multiple time
                // which in turns calls this property to get the default shader.
                // The property should never return null as, when null, it loads the data using AssetDatabase.LoadAssetAtPath.
                // However it seems there's an issue that LoadAssetAtPath will not load the asset in some cases. so adding the null check
                // here to fix template tests.
                if (scriptableRendererData != null)
                {
                    Shader defaultShader = scriptableRendererData.GetDefaultShader();
                    if (defaultShader != null)
                        return defaultShader;
                }

                if (m_DefaultShader == null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(ShaderPathID.Lit));
                    m_DefaultShader  = AssetDatabase.LoadAssetAtPath<Shader>(path);
                }
#endif

                if (m_DefaultShader == null)
                    m_DefaultShader = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.Lit));

                return m_DefaultShader;
            }
        }

#if UNITY_EDITOR
        public override Shader autodeskInteractiveShader
        {
            get { return editorResources?.shaders.autodeskInteractivePS; }
        }

        public override Shader autodeskInteractiveTransparentShader
        {
            get { return editorResources?.shaders.autodeskInteractiveTransparentPS; }
        }

        public override Shader autodeskInteractiveMaskedShader
        {
            get { return editorResources?.shaders.autodeskInteractiveMaskedPS; }
        }

        public override Shader terrainDetailLitShader
        {
            get { return editorResources?.shaders.terrainDetailLitPS; }
        }

        public override Shader terrainDetailGrassShader
        {
            get { return editorResources?.shaders.terrainDetailGrassPS; }
        }

        public override Shader terrainDetailGrassBillboardShader
        {
            get { return editorResources?.shaders.terrainDetailGrassBillboardPS; }
        }

        public override Shader defaultSpeedTree7Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree7PS; }
        }

        public override Shader defaultSpeedTree8Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree8PS; }
        }
#endif

        /// <summary>Names used for display of rendering layer masks.</summary>
        public override string[] renderingLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.renderingLayerMaskNames;

        /// <summary>Names used for display of rendering layer masks with prefix.</summary>
        public override string[] prefixedRenderingLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.prefixedRenderingLayerMaskNames;

        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] lightLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.lightLayerNames;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (k_AssetVersion < 3)
            {
                //m_SoftShadowsSupported = (m_ShadowType == ShadowQuality.SoftShadows);
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 3;
            }

            if (k_AssetVersion < 4)
            {
                //m_AdditionalLightShadowsSupported = m_LocalShadowsSupported;
                //m_AdditionalLightsShadowmapResolution = m_LocalShadowsAtlasResolution;
                //m_AdditionalLightsPerObjectLimit = m_MaxPixelLights;
                //m_MainLightShadowmapResolution = m_ShadowAtlasResolution;
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 4;
            }

            if (k_AssetVersion < 5)
            {
                if (m_RendererType == RendererType.Custom)
                {
                    m_RendererDataList[0] = m_RendererData;
                }
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 5;
            }

            if (k_AssetVersion < 6)
            {
#pragma warning disable 618 // Obsolete warning
                // Adding an upgrade here so that if it was previously set to 2 it meant 4 cascades.
                // So adding a 3rd cascade shifted this value up 1.
                int value = (int)m_ShadowCascades;
                if (value == 2)
                {
                    //m_ShadowCascadeCount = 4;
                }
                else
                {
                    //m_ShadowCascadeCount = value + 1;
                }
                k_AssetVersion = 6;
#pragma warning restore 618 // Obsolete warning
            }

            if (k_AssetVersion < 7)
            {
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 7;
            }

            if (k_AssetVersion < 8)
            {
                k_AssetPreviousVersion = k_AssetVersion;
                //m_CascadeBorder = 0.1f; // In previous version we had this hard coded
                k_AssetVersion = 8;
            }

            if (k_AssetVersion < 9)
            {
                //bool assetContainsCustomAdditionalLightShadowResolutions =
                //    m_AdditionalLightsShadowResolutionTierHigh != AdditionalLightsDefaultShadowResolutionTierHigh ||
                //    m_AdditionalLightsShadowResolutionTierMedium != AdditionalLightsDefaultShadowResolutionTierMedium ||
                //    m_AdditionalLightsShadowResolutionTierLow != AdditionalLightsDefaultShadowResolutionTierLow;

                //if (!assetContainsCustomAdditionalLightShadowResolutions)
                //{
                //    // if all resolutions are still the default values, we assume that they have never been customized and that it is safe to upgrade them to fit better the Additional Lights Shadow Atlas size
                //    m_AdditionalLightsShadowResolutionTierHigh = (int)m_AdditionalLightsShadowmapResolution;
                //    m_AdditionalLightsShadowResolutionTierMedium = Mathf.Max(m_AdditionalLightsShadowResolutionTierHigh / 2, UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution);
                //    m_AdditionalLightsShadowResolutionTierLow = Mathf.Max(m_AdditionalLightsShadowResolutionTierMedium / 2, UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution);
                //}

                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 9;
            }

#if UNITY_EDITOR
            if (k_AssetPreviousVersion != k_AssetVersion)
            {
                EditorApplication.delayCall += () => UpgradeAsset(this.GetInstanceID());
            }
#endif
        }

#if UNITY_EDITOR
        static void UpgradeAsset(int assetInstanceID)
        {
            UniversalRenderPipelineAsset asset = EditorUtility.InstanceIDToObject(assetInstanceID) as UniversalRenderPipelineAsset;

            if (asset.k_AssetPreviousVersion < 5)
            {
                if (asset.m_RendererType == RendererType.UniversalRenderer)
                {
                    var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/UniversalRenderer.asset");
                    if (data)
                    {
                        asset.m_RendererDataList[0] = data;
                    }
                    else
                    {
                        asset.LoadBuiltinRendererData();
                    }
                    asset.m_RendererData = null; // Clears the old renderer
                }

                asset.k_AssetPreviousVersion = 5;
            }

            if (asset.k_AssetPreviousVersion < 9)
            {
                // The added feature was reverted, we keep this version to avoid breakage in case somebody already has version 7
                asset.k_AssetPreviousVersion = 9;
            }

            EditorUtility.SetDirty(asset);
        }

#endif

        /// <summary>
        /// Check to see if the RendererData list contains valid RendererData references.
        /// </summary>
        /// <param name="partial">This bool controls whether to test against all or any, if false then there has to be no invalid RendererData</param>
        /// <returns></returns>
        internal bool ValidateRendererDataList(bool partial = false)
        {
            var emptyEntries = 0;
            for (int i = 0; i < m_RendererDataList.Length; i++) emptyEntries += ValidateRendererData(i) ? 0 : 1;
            if (partial)
                return emptyEntries == 0;
            return emptyEntries != m_RendererDataList.Length;
        }

        internal bool ValidateRendererData(int index)
        {
            // Check to see if you are asking for the default renderer
            if (index == -1) index = m_DefaultRendererIndex;
            return index < m_RendererDataList.Length ? m_RendererDataList[index] != null : false;
        }
    }
}
