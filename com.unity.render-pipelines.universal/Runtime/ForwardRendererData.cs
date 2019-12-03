#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom("UnityEngine.Rendering.LWRP")]
    public class ForwardRendererData : ScriptableRendererData
    {
#if UNITY_EDITOR
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateForwardRendererAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                var instance = CreateInstance<ForwardRendererData>();
                AssetDatabase.CreateAsset(instance, pathName);
                ResourceReloader.ReloadAllNullIn(instance, UniversalRenderPipelineAsset.packagePath);
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/Forward Renderer", priority = CoreUtils.assetCreateMenuPriority2)]
        static void CreateForwardRendererData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateForwardRendererAsset>(), "CustomForwardRendererData.asset", null, null);
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

            [Reload("Shaders/Utils/FallbackError.shader")]
            public Shader fallbackErrorPS;
        }

        [Reload("Runtime/Data/PostProcessData.asset")]
        public PostProcessData postProcessData = null;

        public ShaderResources shaders = null;

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;
        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData();
        [SerializeField] bool m_ShadowTransparentReceive = true;

        protected override ScriptableRenderer Create()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
                ResourceReloader.TryReloadAllNullIn(postProcessData, UniversalRenderPipelineAsset.packagePath);
            }
#endif
            return new ForwardRenderer(this);
        }

        internal LayerMask opaqueLayerMask => m_OpaqueLayerMask;

        public LayerMask transparentLayerMask => m_TransparentLayerMask;

        public StencilStateData defaultStencilState => m_DefaultStencilState;

        public bool shadowTransparentReceive => m_ShadowTransparentReceive;

        protected override void OnEnable()
        {
            base.OnEnable();

            // Upon asset creation, OnEnable is called and `shaders` reference is not yet initialized
            // We need to call the OnEnable for data migration when updating from old versions of UniversalRP that
            // serialized resources in a different format. Early returning here when OnEnable is called
            // upon asset creation is fine because we guarantee new assets get created with all resources initialized.
            if (shaders == null)
                return;

#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
            ResourceReloader.TryReloadAllNullIn(postProcessData, UniversalRenderPipelineAsset.packagePath);
#endif
        }
    }
}
