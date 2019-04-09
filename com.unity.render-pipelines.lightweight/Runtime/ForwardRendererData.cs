#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;

namespace UnityEngine.Rendering.LWRP
{    
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
                Selection.activeObject = instance;
            }
        }

        [MenuItem("Assets/Create/Rendering/Lightweight Render Pipeline/Forward Renderer", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateForwardRendererData()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateForwardRendererAsset>(), "CustomForwardRendererData.asset", null, null);
        }
#endif

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [SerializeField, Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            [SerializeField, Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [SerializeField, Reload("Shaders/Utils/ScreenSpaceShadows.shader")]
            public Shader screenSpaceShadowPS;
        
            [SerializeField, Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;

            [SerializeField, Reload("Shaders/PostProcessing/StopNaN.shader")]
            public Shader stopNanPS;

            [SerializeField, Reload("Shaders/PostProcessing/PaniniProjection.shader")]
            public Shader paniniProjectionPS;

            [SerializeField, Reload("Shaders/PostProcessing/LutBuilderLdr.shader")]
            public Shader lutBuilderLdrPS;

            [SerializeField, Reload("Shaders/PostProcessing/LutBuilderHdr.shader")]
            public Shader lutBuilderHdrPS;

            [SerializeField, Reload("Shaders/PostProcessing/Bloom.shader")]
            public Shader bloomPS;

            [SerializeField, Reload("Shaders/PostProcessing/UberPost.shader")]
            public Shader uberPostPS;
        }

        public ShaderResources shaders;

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;

        [SerializeField] StencilStateData m_DefaultStencilState = null;

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            ResourceReloader.ReloadAllNullIn(this, LightweightRenderPipelineAsset.packagePath);
        }
#endif

        protected override ScriptableRenderer Create() => new ForwardRenderer(this);

        internal LayerMask opaqueLayerMask => m_OpaqueLayerMask;

        public LayerMask transparentLayerMask => m_TransparentLayerMask;

        public StencilStateData defaultStencilState => m_DefaultStencilState;
    }
}
