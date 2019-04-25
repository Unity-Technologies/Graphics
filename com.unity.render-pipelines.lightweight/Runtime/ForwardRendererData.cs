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
            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [Reload("Shaders/Utils/ScreenSpaceShadows.shader")]
            public Shader screenSpaceShadowPS;
        
            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;

            [Reload("Shaders/PostProcessing/StopNaN.shader")]
            public Shader stopNanPS;

            [Reload("Shaders/PostProcessing/SubpixelMorphologicalAntialiasing.shader")]
            public Shader subpixelMorphologicalAntialiasingPS;

            [Reload("Shaders/PostProcessing/PaniniProjection.shader")]
            public Shader paniniProjectionPS;

            [Reload("Shaders/PostProcessing/LutBuilderLdr.shader")]
            public Shader lutBuilderLdrPS;

            [Reload("Shaders/PostProcessing/LutBuilderHdr.shader")]
            public Shader lutBuilderHdrPS;

            [Reload("Shaders/PostProcessing/Bloom.shader")]
            public Shader bloomPS;

            [Reload("Shaders/PostProcessing/UberPost.shader")]
            public Shader uberPostPS;
        }

        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            // Pre-baked noise
            [Reload("Textures/BlueNoise16/L/LDR_LLL1_{0}.png", 0, 32)]
            public Texture2D[] blueNoise16LTex;

            // Post-processing
            [Reload(new[]
            {
                "Textures/FilmGrain/Thin01.png",
                "Textures/FilmGrain/Thin02.png",
                "Textures/FilmGrain/Medium01.png",
                "Textures/FilmGrain/Medium02.png",
                "Textures/FilmGrain/Medium03.png",
                "Textures/FilmGrain/Medium04.png",
                "Textures/FilmGrain/Medium05.png",
                "Textures/FilmGrain/Medium06.png",
                "Textures/FilmGrain/Large01.png",
                "Textures/FilmGrain/Large02.png"
            })]
            public Texture2D[] filmGrainTex;

            [Reload("Textures/SMAA/AreaTex.tga")]
            public Texture2D smaaAreaTex;

            [Reload("Textures/SMAA/SearchTex.tga")]
            public Texture2D smaaSearchTex;
        }

        public ShaderResources shaders;
        public TextureResources textures;

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
