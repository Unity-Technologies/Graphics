using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEditorInternal;
#endif
using System.ComponentModel;
using System.Linq;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The default color buffer format in HDR (only).
    /// Affects camera rendering and postprocessing color buffers.
    /// </summary>
    public enum HDRColorBufferPrecision
    {
        /// <summary> Typically R11G11B10f for faster rendering. Recommend for mobile.
        /// R11G11B10f can cause a subtle blue/yellow banding in some rare cases due to lower precision of the blue component.</summary>
        [Tooltip("Use 32-bits per pixel for HDR rendering.")]
        _32Bits,
        /// <summary>Typically R16G16B16A16f for better quality. Can reduce banding at the cost of memory and performance.</summary>
        [Tooltip("Use 64-bits per pixel for HDR rendering.")]
        _64Bits,
    }

    /// <summary>
    /// Options for setting MSAA Quality.
    /// This defines how many samples URP computes per pixel for evaluating the effect.
    /// </summary>
    public enum MsaaQuality
    {
        /// <summary>
        /// Disables MSAA.
        /// </summary>
        Disabled = 1,

        /// <summary>
        /// Use this for 2 samples per pixel.
        /// </summary>
        _2x = 2,

        /// <summary>
        /// Use this for 4 samples per pixel.
        /// </summary>
        _4x = 4,

        /// <summary>
        /// Use this for 8 samples per pixel.
        /// </summary>
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




    /// <summary>
    /// Defines if profiling is logged or not. This enum is not longer in use, use the Profiler instead.
    /// </summary>
    [Obsolete("PipelineDebugLevel is replaced to use the profiler and has no effect.", false)]
    public enum PipelineDebugLevel
    {
        /// <summary>
        /// Disabled logging for profiling.
        /// </summary>
        Disabled,
        /// <summary>
        /// Enabled logging for profiling.
        /// </summary>
        Profiling,
    }

    /// <summary>
    /// Options to select the type of Renderer to use.
    /// </summary>
    public enum RendererType
    {
        /// <summary>
        /// Use this for Custom Renderer.
        /// </summary>
        Custom,

        /// <summary>
        /// Use this for Universal Renderer.
        /// </summary>
        UniversalRenderer,

        /// <summary>
        /// Use this for 2D Renderer.
        /// </summary>
        _2DRenderer,
        /// <summary>
        /// This name was used before the Universal Renderer was implemented.
        /// </summary>
        [Obsolete("ForwardRenderer has been renamed (UnityUpgradable) -> UniversalRenderer", true)]
        ForwardRenderer = UniversalRenderer,
    }


    /// <summary>
    /// Defines the upscaling filter selected by the user the universal render pipeline asset.
    /// </summary>
    public enum UpscalingFilterSelection
    {
        /// <summary>
        /// Indicates that URP will select an appropriate upscaling filter automatically.
        /// </summary>
        [InspectorName("Automatic")]
        Auto,

        /// <summary>
        /// Indicates that Bilinear filtering will be used when performing upscaling.
        /// </summary>
        [InspectorName("Bilinear")]
        Linear,

        /// <summary>
        /// Indicates that Nearest-Neighbour filtering will be used when performing upscaling.
        /// </summary>
        [InspectorName("Nearest-Neighbor")]
        Point,
        [InspectorName("FidelityFX Super Resolution 1.0")]
        FSR
    }

    /// <summary>
    /// The asset that contains the URP setting.
    /// You can use this asset as a graphics quality level.
    /// <see cref="RenderPipelineAsset"\>
    /// <see cref="UniversalRenderPipeline"/>
    /// </summary>
    [URPHelpURL("universalrp-asset")]
    public partial class UniversalRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
        Shader m_DefaultShader;
        internal ScriptableRenderer[] m_Renderers = new ScriptableRenderer[1];

        // Default values set when a new UniversalRenderPipeline asset is created
        [SerializeField] int k_AssetVersion = 10;
        [SerializeField] int k_AssetPreviousVersion = 10;

        // Deprecated settings for upgrading sakes
        [SerializeField] RendererType m_RendererType = RendererType.UniversalRenderer;
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SerializeReference] internal ScriptableRendererData m_RendererData = null;

        // Renderer settings
        [SerializeReference] internal ScriptableRendererData[] m_RendererDataList = { new UniversalRendererData() };
        [SerializeField] internal int m_DefaultRendererIndex = 0;

        // Quality settings
        [SerializeField] bool m_SupportsHDR = true;
        [SerializeField] HDRColorBufferPrecision m_HDRColorBufferPrecision = HDRColorBufferPrecision._32Bits;
        [SerializeField] MsaaQuality m_MSAA = MsaaQuality.Disabled;

        [SerializeField] float m_RenderScale = 1.0f;
        [SerializeField] UpscalingFilterSelection m_UpscalingFilter = UpscalingFilterSelection.Auto;
        [SerializeField] bool m_FsrOverrideSharpness = false;
        [SerializeField] float m_FsrSharpness = FSRUtils.kDefaultSharpnessLinear;
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
            {
                instance.m_RendererDataList[0] = rendererData;
                instance.m_RendererDataList[0].Awake();
                instance.m_RendererDataList[0].OnEnable();
            }

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
                AssetDatabase.CreateAsset(Create(CreateRendererData(RendererType.UniversalRenderer)), pathName);
            }
        }

        [MenuItem("Assets/Create/Rendering/URP Asset (with Universal Renderer)", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        static void CreateUniversalPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUniversalPipelineAsset>(),
                "New Universal Render Pipeline Asset.asset", null, null);
        }

        internal static ScriptableRendererData CreateRendererData(RendererType type)
        {
            switch (type)
            {
                case RendererType.UniversalRenderer:
                default:
                {
                    var rendererData = new UniversalRendererData();
                    if (UniversalRenderPipelineGlobalSettings.instance.postProcessData == null)
                        UniversalRenderPipelineGlobalSettings.instance.postProcessData = PostProcessData.GetDefaultPostProcessData();
                    ResourceReloader.ReloadAllNullIn(rendererData, packagePath);
                    return rendererData;
                }
                // 2D renderer is experimental
                case RendererType._2DRenderer:
                {
                    var rendererData = new Renderer2DData();
                    if (UniversalRenderPipelineGlobalSettings.instance.postProcessData == null)
                        UniversalRenderPipelineGlobalSettings.instance.postProcessData = PostProcessData.GetDefaultPostProcessData();
                    ResourceReloader.ReloadAllNullIn(rendererData, packagePath);
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
        /*
        /// <summary>
        /// Use this class to initialize the rendererData element that is required by the renderer.
        /// </summary>
        /// <param name="type">The <c>RendererType</c> of the new renderer that is initialized within this asset.</param>
        /// <returns></returns>
        /// <see cref="RendererType"/>
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
        */

        /// <summary>
        /// Creates a <c>UniversalRenderPipeline</c> from the <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <returns>Returns a <c>UniversalRenderPipeline</c> created from this UniversalRenderPipelineAsset.</returns>
        /// <see cref="RenderPipeline"/>
        protected override RenderPipeline CreatePipeline()
        {
            if (m_RendererDataList == null)
                m_RendererDataList = new ScriptableRendererData[1];

            // If no default data we can't create pipeline instance
            if (m_RendererDataList[m_DefaultRendererIndex] == null || m_RendererDataList.Length <= 0)
            {
                // If previous version and current version are miss-matched then we are waiting for the upgrader to kick in
                if (k_AssetPreviousVersion != k_AssetVersion)
                    return null;

                //if (m_RendererDataList[m_DefaultRendererIndex].GetType().ToString()
                //    .Contains("Universal.ForwardRendererData"))
                //    return null;

                Debug.LogError(
                    $"Default Renderer is missing, make sure there is a Renderer assigned as the default on the current Universal RP asset:{UniversalRenderPipeline.asset.name}",
                    this);
                return null;
            }

            DestroyRenderers();
            var pipeline = new UniversalRenderPipeline(this);
            CreateRenderers();
            return pipeline;
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

        void Awake()
        {
            foreach (var renderer in m_RendererDataList)
                renderer.Awake();
        }

        /// <inheritdoc/>
        protected override void OnValidate()
        {
            foreach (var renderer in m_RendererDataList)
                renderer.OnValidate();
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnValidate();
        }

        void OnEnable()
        {
            foreach (var renderer in m_RendererDataList)
                renderer.OnEnable();
        }

        /// <inheritdoc/>
        protected override void OnDisable()
        {
            foreach (var renderer in m_RendererDataList)
                renderer.OnDisable();
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnDisable();
        }

        void CreateRenderers()
        {
            if (m_Renderers != null)
            {
                for (int i = 0; i < m_Renderers.Length; ++i)
                {
                    if (m_Renderers[i] != null)
                        Debug.LogError($"Creating renderers but previous instance wasn't properly destroyed: m_Renderers[{i}]");
                }
            }

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
            {
                DestroyRenderers();
                CreateRenderers();
            }

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

        /// <summary>
        /// When true, the pipeline creates a depth texture that can be read in shaders. The depth texture can be accessed as _CameraDepthTexture. This setting can be overridden per camera.
        /// </summary>
        [Obsolete("Moved to ScriptableRendererData")]
        public bool supportsCameraDepthTexture
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// When true, the pipeline creates a texture that contains a copy of the color buffer after rendering opaque objects. This texture can be accessed in shaders as _CameraOpaqueTexture. This setting can be overridden per camera.
        /// </summary>
        [Obsolete("Moved to ScriptableRendererData")]
        public bool supportsCameraOpaqueTexture
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Returns the downsampling method used when copying the camera color texture after rendering opaques.
        /// </summary>
        [Obsolete("Moved to ScriptableRendererData")]
        public Downsampling opaqueDownsampling
        {
            get { return Downsampling.None; }
            set { }
        }

        /// <summary>
        /// This settings controls if the asset <c>UniversalRenderPipelineAsset</c> supports terrain holes.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/terrain-PaintHoles.html"/>
        [Obsolete("Moved to UniversalRenderPipelineGlobalSettings")]
        public bool supportsTerrainHoles
        {
            get { return false; }
        }

        /// <summary>
        /// Returns the active store action optimization value.
        /// </summary>
        /// <returns>Returns the active store action optimization value.</returns>
        [Obsolete("Moved to UniversalRenderPipelineGlobalSettings")]
        public StoreActionsOptimization storeActionsOptimization
        {
            get { return StoreActionsOptimization.Auto; }
            set { }
        }

        /// <summary>
        /// When enabled, the camera renders to HDR buffers. This setting can be overridden per camera.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/HDR.html"/>
        public bool supportsHDR
        {
            get { return m_SupportsHDR; }
            set { m_SupportsHDR = value; }
        }

        /// <summary>
        /// Graphics format requested for HDR color buffers.
        /// </summary>
        public HDRColorBufferPrecision hdrColorBufferPrecision
        {
            get { return m_HDRColorBufferPrecision; }
            set { m_HDRColorBufferPrecision = value; }
        }

        /// <summary>
        /// Specifies the msaa sample count used by this <c>UniversalRenderPipelineAsset</c>
        /// </summary>
        /// <see cref="SampleCount"/>
        public int msaaSampleCount
        {
            get { return (int)m_MSAA; }
            set { m_MSAA = (MsaaQuality)value; }
        }

        float ValidateRenderScale(float value)
        {
            return Mathf.Max(UniversalRenderPipeline.minRenderScale, Mathf.Min(value, UniversalRenderPipeline.maxRenderScale));
        }

        /// <summary>
        /// Specifies the render scale which scales the render target resolution used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        public float renderScale
        {
            get { return m_RenderScale; }
            set { m_RenderScale = ValidateRenderScale(value); }
        }

        /// <summary>
        /// Returns the upscaling filter desired by the user
        /// Note: Filter selections differ from actual filters in that they may include "meta-filters" such as
        ///       "Automatic" which resolve to an actual filter at a later time.
        /// </summary>
        public UpscalingFilterSelection upscalingFilter
        {
            get { return m_UpscalingFilter; }
            set { m_UpscalingFilter = value; }
        }

        /// <summary>
        /// If this property is set to true, the value from the fsrSharpness property will control the intensity of the
        /// sharpening filter associated with FidelityFX Super Resolution.
        /// </summary>
        public bool fsrOverrideSharpness
        {
            get { return m_FsrOverrideSharpness; }
            set { m_FsrOverrideSharpness = value; }
        }

        /// <summary>
        /// Controls the intensity of the sharpening filter associated with FidelityFX Super Resolution.
        /// A value of 1.0 produces maximum sharpness while a value of 0.0 disables the sharpening filter entirely.
        ///
        /// Note: This value only has an effect when the fsrOverrideSharpness property is set to true.
        /// </summary>
        public float fsrSharpness
        {
            get { return m_FsrSharpness; }
            set { m_FsrSharpness = value; }
        }

        /// <summary>
        /// Specifies the <c>LightRenderingMode</c> for the main light used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <see cref="LightRenderingMode"/>
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public LightRenderingMode mainLightRenderingMode
        {
            get { return LightRenderingMode.Disabled; }
        }

        /// <summary>
        /// Specifies if objects lit by main light cast shadows.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public bool supportsMainLightShadows
        {
            get { return false; }
        }

        /// <summary>
        /// Returns the main light shadowmap resolution used for this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int mainLightShadowmapResolution
        {
            get { return (int)ShadowResolution._256; }
        }

        /// <summary>
        /// Specifies the <c>LightRenderingMode</c> for the additional lights used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <see cref="LightRenderingMode"/>
        [Obsolete("Moved to UniversalRendererData")]
        public LightRenderingMode additionalLightsRenderingMode
        {
            get { return LightRenderingMode.Disabled; }
        }

        /// <summary>
        /// Specifies the maximum amount of per-object additional lights which can be used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int maxAdditionalLightsCount
        {
            get { return 0; }
            set { }
        }

        /// <summary>
        /// Specifies if objects lit by additional lights cast shadows.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public bool supportsAdditionalLightShadows
        {
            get { return false; }
        }

        /// <summary>
        /// Additional light shadows are rendered into a single shadow map atlas texture. This setting controls the resolution of the shadow map atlas texture.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int additionalLightsShadowmapResolution
        {
            get { return (int)ShadowResolution._256; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "Low" in the UniversalRenderPipeline asset.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int additionalLightsShadowResolutionTierLow
        {
            get { return (int)ShadowResolution._256; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "Medium" in the UniversalRenderPipeline asset.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int additionalLightsShadowResolutionTierMedium
        {
            get { return (int)ShadowResolution._256; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "High" in the UniversalRenderPipeline asset.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int additionalLightsShadowResolutionTierHigh
        {
            get { return (int)ShadowResolution._256; }
        }

        /// <summary>
        /// Specifies if this <c>UniversalRenderPipelineAsset</c> should use Probe blending for the reflection probes in the scene.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public bool reflectionProbeBlending
        {
            get { return false; }
        }

        /// <summary>
        /// Specifies if this <c>UniversalRenderPipelineAsset</c> should allow box projection for the reflection probes in the scene.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public bool reflectionProbeBoxProjection
        {
            get { return false; }
        }

        /// <summary>
        /// Controls the maximum distance at which shadows are visible.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public float shadowDistance
        {
            get { return 0; }
            set { }
        }

        /// <summary>
        /// Returns the number of shadow cascades.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int shadowCascadeCount
        {
            get { return 0; }
            set { }
        }

        /// <summary>
        /// Returns the split value.
        /// </summary>
        /// <returns>Returns a Float with the split value.</returns>
        [Obsolete("Moved to UniversalRendererData")]
        public float cascade2Split
        {
            get { return 0f; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector2 with the split values.</returns>
        [Obsolete("Moved to UniversalRendererData")]
        public Vector2 cascade3Split
        {
            get { return Vector2.zero; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector3 with the split values.</returns>
        [Obsolete("Moved to UniversalRendererData")]
        public Vector3 cascade4Split
        {
            get { return Vector3.zero; }
        }

        /// <summary>
        /// Last cascade fade distance in percentage.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public float cascadeBorder
        {
            get { return 0f; }
            set { }
        }

        /// <summary>
        /// The Shadow Depth Bias, controls the offset of the lit pixels.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public float shadowDepthBias
        {
            get { return 0f; }
            set { }
        }

        /// <summary>
        /// Controls the distance at which the shadow casting surfaces are shrunk along the surface normal.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public float shadowNormalBias
        {
            get { return 0f; }
            set { }
        }

        /// <summary>
        /// Supports Soft Shadows controls the Soft Shadows.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public bool supportsSoftShadows
        {
            get { return false; }
        }

        /// <summary>
        /// Specifies if this <c>UniversalRenderPipelineAsset</c> should use dynamic batching.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/DrawCallBatching.html"/>
        public bool supportsDynamicBatching
        {
            get { return m_SupportsDynamicBatching; }
            set { m_SupportsDynamicBatching = value; }
        }

        /// <summary>
        /// Previously returned the debug level for this Render Pipeline Asset but is now deprecated. Replaced to use the profiler and is no longer used.
        /// </summary>
        [Obsolete("PipelineDebugLevel is deprecated and replaced to use the profiler. Calling debugLevel is not necessary.", false)]
        public PipelineDebugLevel debugLevel
        {
            get => PipelineDebugLevel.Disabled;
        }

        /// <summary>
        /// Specifies if SRPBacher is used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/SRPBatcher.html"/>
        public bool useSRPBatcher
        {
            get { return m_UseSRPBatcher; }
            set { m_UseSRPBatcher = value; }
        }


        /// <summary>
        /// Returns the selected ColorGradingMode in the URP Asset.
        /// <see cref="ColorGradingMode"/>
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineGlobalSettings")]
        public ColorGradingMode colorGradingMode
        {
            get { return ColorGradingMode.LowDynamicRange; }
            set { }
        }

        /// <summary>
        /// Specifies the color grading LUT (lookup table) size in the URP Asset.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineGlobalSettings")]
        public int colorGradingLutSize
        {
            get { return 0; }
            set { }
        }

        /// <summary>
        /// Returns true if fast approximation functions are used when converting between the sRGB and Linear color spaces, false otherwise.
        /// </summary>
        [Obsolete("Moved to UniversalRenderPipelineGlobalSettings")]
        public bool useFastSRGBLinearConversion
        {
            get { return false; }
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


        /// <summary>
        /// Set to true to enable a conservative method for calculating the size and position of the minimal enclosing sphere around the frustum cascade corner points for shadow culling.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public bool conservativeEnclosingSphere
        {
            get { return false; }
            set { }
        }

        /// <summary>
        /// Set the number of iterations to reduce the cascade culling enlcosing sphere to be closer to the absolute minimun enclosing sphere, but will also require more CPU computation for increasing values.
        /// This parameter is used only when conservativeEnclosingSphere is set to true. Default value is 64.
        /// </summary>
        [Obsolete("Moved to UniversalRendererData")]
        public int numIterationsEnclosingSphere
        {
            get { return 0; }
            set { }
        }

        /// <inheritdoc/>
        public override Material defaultMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Standard); }
        }

        /// <inheritdoc/>
        public override Material defaultParticleMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        /// <inheritdoc/>
        public override Material defaultLineMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        /// <inheritdoc/>
        public override Material defaultTerrainMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Terrain); }
        }

        /// <inheritdoc/>
        public override Material defaultUIMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        /// <inheritdoc/>
        public override Material defaultUIOverdrawMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public Material decalMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Decal); }
        }

        /// <summary>
        /// Returns the default shader for the specified renderer. When creating new objects in the editor, the materials of those objects will use the selected default shader.
        /// </summary>
        /// <returns>Returns the default shader for the specified renderer.</returns>
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
        /// <inheritdoc/>
        public override Shader autodeskInteractiveShader
        {
            get { return editorResources?.shaders.autodeskInteractivePS; }
        }

        /// <inheritdoc/>
        public override Shader autodeskInteractiveTransparentShader
        {
            get { return editorResources?.shaders.autodeskInteractiveTransparentPS; }
        }

        /// <inheritdoc/>
        public override Shader autodeskInteractiveMaskedShader
        {
            get { return editorResources?.shaders.autodeskInteractiveMaskedPS; }
        }

        /// <inheritdoc/>
        public override Shader terrainDetailLitShader
        {
            get { return editorResources?.shaders.terrainDetailLitPS; }
        }

        /// <inheritdoc/>
        public override Shader terrainDetailGrassShader
        {
            get { return editorResources?.shaders.terrainDetailGrassPS; }
        }

        /// <inheritdoc/>
        public override Shader terrainDetailGrassBillboardShader
        {
            get { return editorResources?.shaders.terrainDetailGrassBillboardPS; }
        }

        /// <inheritdoc/>
        public override Shader defaultSpeedTree7Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree7PS; }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void OnBeforeSerialize()
        {
            foreach (var renderer in m_RendererDataList)
                renderer.OnBeforeSerialize();
        }

        /// <inheritdoc/>
        public void OnAfterDeserialize()
        {
            foreach (var renderer in m_RendererDataList)
                renderer.OnAfterDeserialize();
            // TODO fix the upgrade.
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

            if (k_AssetVersion < 10)
            {
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 10;
            }

#if UNITY_EDITOR
            //if (k_AssetPreviousVersion != k_AssetVersion)
            //{
            //    EditorApplication.delayCall += () => UpgradeAsset(this.GetInstanceID());
            //}
#endif
        }
        /*

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

            if (asset.k_AssetPreviousVersion < 10)
            {
                UniversalRenderPipelineGlobalSettings.Ensure().shaderVariantLogLevel = (Rendering.ShaderVariantLogLevel) asset.m_ShaderVariantLogLevel;
                asset.k_AssetPreviousVersion = 10;
            }

            EditorUtility.SetDirty(asset);
        }

#endif
        */

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
