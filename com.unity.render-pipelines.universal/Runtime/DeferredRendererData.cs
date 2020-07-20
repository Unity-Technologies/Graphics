#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, ReloadGroup]
    public class DeferredRendererData : ScriptableRendererData
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateDeferredRendererAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<DeferredRendererData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, UniversalRenderPipelineAsset.packagePath);
                Selection.activeObject = instance;
            }
        }

        // Uncomment to expose Deferred Render Asset menu.
        //[MenuItem("Assets/Create/Rendering/Universal Render Pipeline/Deferred Renderer", priority = CoreUtils.assetCreateMenuPriority2)]
        static void CreateDeferredRendererData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateDeferredRendererAsset>(), "CustomDeferredRendererData.asset", null, null);
        }
#endif

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [Reload("Shaders/Utils/ScreenSpaceShadows.shader")]
            public Shader screenSpaceShadowPS;
        
            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;

            [Reload("Shaders/Utils/TileDepthInfo.shader")]
            public Shader tileDepthInfoPS;

            [Reload("Shaders/Utils/TileDeferred.shader")]
            public Shader tileDeferredPS;

            [Reload("Shaders/Utils/StencilDeferred.shader")]
            public Shader stencilDeferredPS;

            [Reload("Shaders/Utils/FallbackError.shader")]
            public Shader fallbackErrorPS;
        }

        [Reload("Runtime/Data/PostProcessData.asset")]
        public PostProcessData postProcessData = null;

#if ENABLE_VR && ENABLE_XR_MODULE
        [Reload("Runtime/Data/XRSystemData.asset")]
        public XRSystemData xrSystemData = null;
#endif

        public ShaderResources shaders = null;

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;
        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData() { passOperation = StencilOp.Replace };
        [SerializeField] bool m_ShadowTransparentReceive = true;
        [SerializeField] bool m_PreferDepthPrepass = false;
        [SerializeField] bool m_AccurateGbufferNormals = false;
        //[SerializeField] bool m_TiledDeferredShading = false;

        protected override ScriptableRenderer Create()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResourceReloader.ReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
                ResourceReloader.ReloadAllNullIn(postProcessData, UniversalRenderPipelineAsset.packagePath);
#if ENABLE_VR && ENABLE_XR_MODULE
                ResourceReloader.TryReloadAllNullIn(xrSystemData, UniversalRenderPipelineAsset.packagePath);
#endif
            }
#endif
            return new DeferredRenderer(this);
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

        public bool preferDepthPrepass
        {
            get => m_PreferDepthPrepass;
            set
            {
                SetDirty();
                m_PreferDepthPrepass = value;
            }
        }

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

        protected override void OnEnable()
        {
            base.OnEnable();

            // Upon asset creation, OnEnable is called and `shaders` reference is not yet initialized
            // We need to call the OnEnable for data migration when updating from old versions of LWRP that
            // serialized resources in a different format. Early returning here when OnEnable is called
            // upon asset creation is fine because we guarantee new assets get created with all resources initialized.
            if (shaders == null)
                return;

#if UNITY_EDITOR
            ResourceReloader.ReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
            ResourceReloader.ReloadAllNullIn(postProcessData, UniversalRenderPipelineAsset.packagePath);
#if ENABLE_VR && ENABLE_XR_MODULE
            ResourceReloader.TryReloadAllNullIn(xrSystemData, UniversalRenderPipelineAsset.packagePath);
#endif
#endif
        }
    }
}
