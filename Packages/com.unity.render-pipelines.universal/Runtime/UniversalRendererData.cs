#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Defines if Unity will copy the depth that can be bound in shaders as _CameraDepthTexture after the opaques pass or after the transparents pass.
    /// </summary>
    public enum CopyDepthMode
    {
        /// <summary>Depth will be copied after the opaques pass</summary>
        AfterOpaques = 0,
        // Reserve space for after transparent mode
        // AfterTransparents = 1,
        /// <summary>Depth will be written by a depth prepass</summary>
        ForcePrepass = 2
    }

    [Serializable, ReloadGroup, ExcludeFromPreset]
    public class UniversalRendererData : ScriptableRendererData, ISerializationCallbackReceiver
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
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUniversalRendererAsset>(), "New Custom Universal Renderer Data.asset", null, null);
        }

#endif

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [Obsolete("Obsolete, this feature will be supported by new 'ScreenSpaceShadows' renderer feature")]
            public Shader screenSpaceShadowPS;

            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;

            [Reload("Shaders/Utils/StencilDeferred.shader")]
            public Shader stencilDeferredPS;

            [Reload("Shaders/Utils/FallbackError.shader")]
            public Shader fallbackErrorPS;

            [Reload("Shaders/Utils/MaterialError.shader")]
            public Shader materialErrorPS;

            // Core blitter shaders, adapted from HDRP
            // TODO: move to core and share with HDRP
            [Reload("Shaders/Utils/CoreBlit.shader"), SerializeField]
            internal Shader coreBlitPS;
            [Reload("Shaders/Utils/CoreBlitColorAndDepth.shader"), SerializeField]
            internal Shader coreBlitColorAndDepthPS;


            [Reload("Shaders/CameraMotionVectors.shader")]
            public Shader cameraMotionVector;

            [Reload("Shaders/ObjectMotionVectors.shader")]
            public Shader objectMotionVector;
        }

        public PostProcessData postProcessData = null;

#if ENABLE_VR && ENABLE_XR_MODULE
        [Reload("Runtime/Data/XRSystemData.asset")]
        public XRSystemData xrSystemData = null;
#endif

        public ShaderResources shaders = null;

        const int k_LatestAssetVersion = 2;
        [SerializeField] int m_AssetVersion = 0;
        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;
        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData() { passOperation = StencilOp.Replace }; // This default state is compatible with deferred renderer.
        [SerializeField] bool m_ShadowTransparentReceive = true;
        [SerializeField] RenderingMode m_RenderingMode = RenderingMode.Forward;
        [SerializeField] DepthPrimingMode m_DepthPrimingMode = DepthPrimingMode.Disabled; // Default disabled because there are some outstanding issues with Text Mesh rendering.
        [SerializeField] CopyDepthMode m_CopyDepthMode = CopyDepthMode.AfterOpaques;
#if UNITY_EDITOR
        // Do not strip accurateGbufferNormals on Mobile Vulkan as some GPUs do not support R8G8B8A8_SNorm, which then force us to use accurateGbufferNormals
        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.Vulkan)]
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings._GBUFFER_NORMALS_OCT)]
#endif
        [SerializeField] bool m_AccurateGbufferNormals = false;
        //[SerializeField] bool m_TiledDeferredShading = false;
#if UNITY_EDITOR
        // NOTE: Atm clustered enabled only makes sense when rendering mode is forward. This filter attribute
        // only considers this boolean here. So it is up to the rest of the system to ensure that rendering mode is forward.
        // Maybe it would make sense to merge clustered state into the RenderingMode enum by adding ForwardPlus etc there?
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: ShaderKeywordStrings.ClusteredRendering)]
        [ShaderKeywordFilter.RemoveIf(true, overridePriority: true, keywordNames: new string[] {ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel})]
#endif
        [SerializeField] bool m_ClusteredRendering = false;
        const TileSize k_DefaultTileSize = TileSize._32;
        [SerializeField] TileSize m_TileSize = k_DefaultTileSize;
        [SerializeField] IntermediateTextureMode m_IntermediateTextureMode = IntermediateTextureMode.Always;

        protected override ScriptableRenderer Create()
        {
            if (!Application.isPlaying)
            {
                ReloadAllNullProperties();
            }
            return new UniversalRenderer(this);
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
        /// Use Octaedron Octahedron normal vector encoding for gbuffer normals.
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

        /*
        public bool tiledDeferredShading
        {
            get => m_TiledDeferredShading;
            set
            {
                SetDirty();
                m_TiledDeferredShading = value;
            }
        }
        */

        internal bool clusteredRendering
        {
            get => m_ClusteredRendering;
            set
            {
                SetDirty();
                m_ClusteredRendering = value;
            }
        }

        internal TileSize tileSize
        {
            get => m_TileSize;
            set
            {
                Assert.IsTrue(value.IsValid());
                SetDirty();
                m_TileSize = value;
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

        protected override void OnValidate()
        {
            base.OnValidate();
            if (!m_TileSize.IsValid())
            {
                m_TileSize = k_DefaultTileSize;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // Upon asset creation, OnEnable is called and `shaders` reference is not yet initialized
            // We need to call the OnEnable for data migration when updating from old versions of UniversalRP that
            // serialized resources in a different format. Early returning here when OnEnable is called
            // upon asset creation is fine because we guarantee new assets get created with all resources initialized.
            if (shaders == null)
                return;

            ReloadAllNullProperties();
        }

        private void ReloadAllNullProperties()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#if ENABLE_VR && ENABLE_XR_MODULE
            ResourceReloader.TryReloadAllNullIn(xrSystemData, UniversalRenderPipelineAsset.packagePath);
#endif
#endif
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_AssetVersion = k_LatestAssetVersion;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_AssetVersion <= 0)
            {
                // Default to old intermediate texture mode for compatibility reason.
                m_IntermediateTextureMode = IntermediateTextureMode.Always;
            }

            if (m_AssetVersion <= 1)
            {
                // To avoid breaking existing projects, keep the old AfterOpaques behaviour. The new AfterTransparents default will only apply to new projects.
                m_CopyDepthMode = CopyDepthMode.AfterOpaques;
            }

            m_AssetVersion = k_LatestAssetVersion;
        }
    }
}
