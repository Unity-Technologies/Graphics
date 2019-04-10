#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;

namespace UnityEngine.Rendering.LWRP
{    
    public class ForwardRendererData : ScriptableRendererData
    {
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [SerializeField, Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            [SerializeField, Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [SerializeField, Reload("Shaders/Utils/ScreenSpaceShadows.shader")]
            public Shader screenSpaceShadowPS;

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
        
            [SerializeField, Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;
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
