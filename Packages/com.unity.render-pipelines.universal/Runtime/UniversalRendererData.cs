#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Defines if Unity will copy the depth that can be bound in shaders as _CameraDepthTexture after the opaques pass or after the transparents pass.
    /// </summary>
    public enum CopyDepthMode
    {
        /// <summary>Depth will be copied after the opaques pass</summary>
        AfterOpaques,
        /// <summary>Depth will be copied after the transparents pass</summary>
        AfterTransparents,
        /// <summary>Depth will be written by a depth prepass</summary>
        ForcePrepass
    }

    /// <summary>
    /// Render path exposed as flags to allow compatibility to be expressed with multiple options.
    /// </summary>
    [Flags]
    public enum RenderPathCompatibility
    {
        /// <summary>Forward Rendering Path</summary>
        Forward      = 1 << 0,
        /// <summary>Deferred Rendering Path</summary>
        Deferred     = 1 << 1,
        /// <summary>Forward+ Rendering Path</summary>
        ForwardPlus  = 1 << 2,
        /// <summary>Forward+ Rendering Path</summary>
        DeferredPlus = 1 << 3,
        /// <summary>All Rendering Paths</summary>
        All         = Forward | Deferred | ForwardPlus | DeferredPlus
    }

    [AttributeUsage(AttributeTargets.Field)]
    sealed class RenderPathCompatibleAttribute : Attribute
    {
        public RenderPathCompatibility renderPath;

        public RenderPathCompatibleAttribute(RenderPathCompatibility renderPath)
        {
            this.renderPath = renderPath;
        }
    }

    /// <summary>
    /// Dept format options for the depth texture and depth attachment.
    /// Each option is marked with all the render paths its compatible with.
    /// </summary>
    public enum DepthFormat
    {
        /// <summary>
        /// Default format for Android and Switch platforms is <see cref="GraphicsFormat.D24_UNorm_S8_UInt"/> and <see cref="GraphicsFormat.D32_SFloat_S8_UInt"/> for other platforms
        /// </summary>
        [RenderPathCompatible(RenderPathCompatibility.All)]
        Default,

        /// <summary>
        /// Format containing 16 unsigned normalized bits in depth component. Corresponds to <see cref="GraphicsFormat.D16_UNorm"/>.
        /// </summary>
        [RenderPathCompatible(RenderPathCompatibility.Forward | RenderPathCompatibility.ForwardPlus)]
        Depth_16 = GraphicsFormat.D16_UNorm,

        /// <summary>
        /// Format containing 24 unsigned normalized bits in depth component. Corresponds to <see cref="GraphicsFormat.D24_UNorm"/>.
        /// </summary>
        [RenderPathCompatible(RenderPathCompatibility.Forward | RenderPathCompatibility.ForwardPlus)]
        Depth_24 = GraphicsFormat.D24_UNorm,

        /// <summary>
        /// Format containing 32 signed float bits in depth component. Corresponds to <see cref="GraphicsFormat.D32_SFloat"/>.
        /// </summary>
        [RenderPathCompatible(RenderPathCompatibility.Forward | RenderPathCompatibility.ForwardPlus)]
        Depth_32 = GraphicsFormat.D32_SFloat,

        /// <summary>
        /// Format containing 16 unsigned normalized bits in depth component and 8 unsigned integer bits in stencil. Corresponds to <see cref="GraphicsFormat.D16_UNorm_S8_UInt"/>.
        /// </summary>
        [RenderPathCompatible(RenderPathCompatibility.All)]
        Depth_16_Stencil_8 = GraphicsFormat.D16_UNorm_S8_UInt,

        /// <summary>
        /// Format containing 24 unsigned normalized bits in depth component and 8 unsigned integer bits in stencil. Corresponds to <see cref="GraphicsFormat.D24_UNorm_S8_UInt"/>.
        /// </summary>
        [RenderPathCompatible(RenderPathCompatibility.All)]
        Depth_24_Stencil_8 = GraphicsFormat.D24_UNorm_S8_UInt,

        /// <summary>
        /// Format containing 32 signed float bits in depth component and 8 unsigned integer bits in stencil. Corresponds to <see cref="GraphicsFormat.D32_SFloat_S8_UInt"/>.
        /// </summary>
        [RenderPathCompatible(RenderPathCompatibility.All)]
        Depth_32_Stencil_8 = GraphicsFormat.D32_SFloat_S8_UInt,
    }

    /// <summary>
    /// Class containing resources needed for the <c>UniversalRenderer</c>.
    /// </summary>
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [URPHelpURL("urp-universal-renderer")]
    public partial class UniversalRendererData : ScriptableRendererData, ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateUniversalRendererAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = UniversalRenderPipelineAsset.CreateRendererAsset(pathName, RendererType.UniversalRenderer, false) as UniversalRendererData;
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/URP Universal Renderer", priority = CoreUtils.Sections.section3 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 2)]
        static void CreateUniversalRendererData()
        {
            var icon = CoreUtils.GetIconForType<ScriptableRendererData>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUniversalRendererAsset>(), "New Custom Universal Renderer Data.asset", icon, null);
        }

#endif

        /// <summary>
        /// Resources needed for Post Processing.
        /// </summary>
        public PostProcessData postProcessData = null;

        const int k_LatestAssetVersion = 3;
        [SerializeField] int m_AssetVersion = 0;
        [SerializeField] LayerMask m_PrepassLayerMask = -1;
        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;
        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData() { passOperation = StencilOp.Replace }; // This default state is compatible with deferred renderer.
        [SerializeField] bool m_ShadowTransparentReceive = true;
        [SerializeField] RenderingMode m_RenderingMode = RenderingMode.Forward;
        [SerializeField] DepthPrimingMode m_DepthPrimingMode = DepthPrimingMode.Disabled; // Default disabled because there are some outstanding issues with Text Mesh rendering.
        [SerializeField] CopyDepthMode m_CopyDepthMode = CopyDepthMode.AfterTransparents;
        [SerializeField] DepthFormat m_DepthAttachmentFormat = DepthFormat.Default;
        [SerializeField] DepthFormat m_DepthTextureFormat = DepthFormat.Default;
#if UNITY_EDITOR
        // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm, which then force us to use accurateGbufferNormals
        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.Vulkan)]
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings._GBUFFER_NORMALS_OCT)]
#endif
        [SerializeField]
        bool m_AccurateGbufferNormals = false;

        [SerializeField] IntermediateTextureMode m_IntermediateTextureMode = IntermediateTextureMode.Always;

        /// <inheritdoc/>
        protected override ScriptableRenderer Create()
        {
            if (!Application.isPlaying)
            {
                ReloadAllNullProperties();
            }
            return new UniversalRenderer(this);
        }

        /// <summary>
        /// Use this to configure how to filter prepass objects.
        /// </summary>
        public LayerMask prepassLayerMask
        {
            get => m_PrepassLayerMask;
            set
            {
                SetDirty();
                m_PrepassLayerMask = value;
            }
        }

        /// <summary>
        /// Use this to configure how to filter opaque objects.
        /// </summary>
        public LayerMask opaqueLayerMask
        {
            get => m_OpaqueLayerMask;
            set
            {
                SetDirty();
                m_OpaqueLayerMask = value;
            }
        }

        /// <summary>
        /// Use this to configure how to filter transparent objects.
        /// </summary>
        public LayerMask transparentLayerMask
        {
            get => m_TransparentLayerMask;
            set
            {
                SetDirty();
                m_TransparentLayerMask = value;
            }
        }

        /// <summary>
        /// The default stencil state settings.
        /// </summary>
        public StencilStateData defaultStencilState
        {
            get => m_DefaultStencilState;
            set
            {
                SetDirty();
                m_DefaultStencilState = value;
            }
        }

        /// <summary>
        /// True if transparent objects receive shadows.
        /// </summary>
        public bool shadowTransparentReceive
        {
            get => m_ShadowTransparentReceive;
            set
            {
                SetDirty();
                m_ShadowTransparentReceive = value;
            }
        }

        /// <summary>
        /// Rendering mode.
        /// </summary>
        public RenderingMode renderingMode
        {
            get => m_RenderingMode;
            set
            {
                SetDirty();
                m_RenderingMode = value;
            }
        }

        /// <summary>
        /// Depth priming mode.
        /// </summary>
        public DepthPrimingMode depthPrimingMode
        {
            get => m_DepthPrimingMode;
            set
            {
                SetDirty();
                m_DepthPrimingMode = value;
            }
        }

        /// <summary>
        /// Copy depth mode.
        /// </summary>
        public CopyDepthMode copyDepthMode
        {
            get => m_CopyDepthMode;
            set
            {
                SetDirty();
                m_CopyDepthMode = value;
            }
        }

        /// <summary>
        /// Depth format used for CameraDepthAttachment
        /// </summary>
        public DepthFormat depthAttachmentFormat
        {
            get
            {
                if (m_DepthAttachmentFormat != DepthFormat.Default && !SystemInfo.IsFormatSupported((GraphicsFormat)m_DepthAttachmentFormat, GraphicsFormatUsage.Render))
                {
                    Debug.LogWarning("Selected Depth Attachment Format is not supported on this platform, falling back to Default");
                    return DepthFormat.Default;
                }
                return m_DepthAttachmentFormat;
            }
            set
            {
                SetDirty();
                if (renderingMode == RenderingMode.Deferred && !GraphicsFormatUtility.IsStencilFormat((GraphicsFormat)value))
                {
                    Debug.LogWarning("Depth format without stencil is not supported on Deferred renderer, falling back to Default");
                    m_DepthAttachmentFormat = DepthFormat.Default;
                }
                else
                {
                    m_DepthAttachmentFormat = value;
                }
            }
        }

        /// <summary>
        /// Depth format used for CameraDepthTexture
        /// </summary>
        public DepthFormat depthTextureFormat
        {
            get
            {
                if (m_DepthTextureFormat != DepthFormat.Default && !SystemInfo.IsFormatSupported((GraphicsFormat) m_DepthTextureFormat, GraphicsFormatUsage.Render))
                {
                    Debug.LogWarning($"Selected Depth Texture Format {m_DepthTextureFormat.ToString()} is not supported on this platform, falling back to Default");
                    return DepthFormat.Default;
                }
                return m_DepthTextureFormat;
            }
            set
            {
                SetDirty();
                m_DepthTextureFormat = value;
            }
        }

        /// <summary>
        /// Use Octahedron normal vector encoding for gbuffer normals.
        /// The overhead is negligible from desktop GPUs, while it should be avoided for mobile GPUs.
        /// </summary>
        public bool accurateGbufferNormals
        {
            get => m_AccurateGbufferNormals;
            set
            {
                SetDirty();
                m_AccurateGbufferNormals = value;
            }
        }

        /// <summary>
        /// Controls when URP renders via an intermediate texture.
        /// </summary>
        public IntermediateTextureMode intermediateTextureMode
        {
            get => m_IntermediateTextureMode;
            set
            {
                SetDirty();
                m_IntermediateTextureMode = value;
            }
        }

        /// <summary>
        /// Returns true if the renderer uses a deferred lighting pass and GBuffers.
        /// This is true for the Deferred and Deferred+ rendering paths.
        /// </summary>
        public bool usesDeferredLighting => m_RenderingMode == RenderingMode.Deferred ||
                                            m_RenderingMode == RenderingMode.DeferredPlus;

        /// <summary>
        /// Returns true if the renderer uses a spatially clustered/tiled light list.
        /// This is true for the Forward+ and Deferred+ rendering paths.
        /// </summary>
        public bool usesClusterLightLoop => m_RenderingMode == RenderingMode.ForwardPlus ||
                                            m_RenderingMode == RenderingMode.DeferredPlus;

        internal override bool stripShadowsOffVariants
        {
            get => m_StripShadowsOffVariants;
            set => m_StripShadowsOffVariants = value;
        }

        internal override bool stripAdditionalLightOffVariants
        {
            get => m_StripAdditionalLightOffVariants;
            set => m_StripAdditionalLightOffVariants = value;
        }

        [NonSerialized]
        bool m_StripShadowsOffVariants = true;
        [NonSerialized]
        bool m_StripAdditionalLightOffVariants = true;

        /// <inheritdoc/>
        protected override void OnEnable()
        {
            base.OnEnable();
            ReloadAllNullProperties();
        }

        private void ReloadAllNullProperties()
        {
            // Upon asset creation, OnEnable is called and `shaders` reference is not yet initialized
            // We need to call the OnEnable for data migration when updating from old versions of UniversalRP that
            // serialized resources in a different format. Early returning here when OnEnable is called
            // upon asset creation is fine because we guarantee new assets get created with all resources initialized.

#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);

            if (postProcessData != null)
                postProcessData.Populate();
#endif
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_AssetVersion = k_LatestAssetVersion;
        }

        /// <inheritdoc/>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_AssetVersion <= 1)
            {
                // To avoid breaking existing projects, keep the old AfterOpaques behaviour. The new AfterTransparents default will only apply to new projects.
                m_CopyDepthMode = CopyDepthMode.AfterOpaques;
            }

            if (m_AssetVersion <= 2)
            {
                m_PrepassLayerMask = m_OpaqueLayerMask;
            }

            m_AssetVersion = k_LatestAssetVersion;
        }
    }
}
