#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Rendering.LWRP
{    
    public class ForwardRendererData : ScriptableRendererData
    {
        [SerializeField] Shader m_BlitShader = null;
        [SerializeField] Shader m_CopyDepthShader = null;
        [SerializeField] Shader m_ScreenSpaceShadowShader = null;
        [SerializeField] Shader m_SamplingShader = null;

        [SerializeField] LayerMask m_OpaqueLayerMask = -1;
        [SerializeField] LayerMask m_TransparentLayerMask = -1;

        [SerializeField] StencilStateData m_DefaultStencilState = null;

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
        
        protected override ScriptableRenderer Create()
        {
            return new ForwardRenderer(this);
        }

        internal Shader blitShader
        {
            get => m_BlitShader;
        }

        internal Shader copyDepthShader
        {
            get => m_CopyDepthShader;
        }

        internal Shader screenSpaceShadowShader
        {
            get => m_ScreenSpaceShadowShader;
        }

        internal Shader samplingShader
        {
            get => m_SamplingShader;
        }

        internal LayerMask opaqueLayerMask
        {
            get => m_OpaqueLayerMask;
        }

        public LayerMask transparentLayerMask
        {
            get => m_TransparentLayerMask;
        }

        public StencilStateData defaultStencilState
        {
            get => m_DefaultStencilState;
        }
    }
}
