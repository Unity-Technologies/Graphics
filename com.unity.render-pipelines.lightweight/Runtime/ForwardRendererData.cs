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
                ResourceReloader.ReloadAllNullIn(instance, LightweightRenderPipelineAsset.packagePath);
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
            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [Reload("Shaders/Utils/ScreenSpaceShadows.shader")]
            public Shader screenSpaceShadowPS;
        
            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;
        }

        [Reload("Runtime/Data/PostProcessData.asset")]
        public PostProcessData postProcessData;

        public ShaderResources shaders;

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;

        [SerializeField] StencilStateData m_DefaultStencilState = null;

        protected override ScriptableRenderer Create() => new ForwardRenderer(this);

        internal LayerMask opaqueLayerMask => m_OpaqueLayerMask;

        public LayerMask transparentLayerMask => m_TransparentLayerMask;

        public StencilStateData defaultStencilState => m_DefaultStencilState;

        protected override void OnEnable()
        {
            base.OnEnable();
#if UNITY_EDITOR
            foreach (var shader in shaders.GetType().GetFields())
            {
                if (shader.GetValue(shaders) == null)
                {
                    ResourceReloader.ReloadAllNullIn(this, LightweightRenderPipelineAsset.packagePath);
                    break;
                }
            }
#endif
        }
    }
}
