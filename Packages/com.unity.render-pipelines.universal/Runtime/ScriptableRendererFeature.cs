using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// You can add a <c>ScriptableRendererFeature</c> to the <c>ScriptableRenderer</c>. Use this scriptable renderer feature to inject render passes into the renderer.
    /// </summary>
    /// <seealso cref="ScriptableRenderer"/>
    /// <seealso cref="ScriptableRenderPass"/>
    [ExcludeFromPreset]
    public abstract partial class ScriptableRendererFeature : ScriptableObject, IDisposable
    {
        [SerializeField, HideInInspector] private bool m_Active = true;
        /// <summary>
        /// Returns the state of the ScriptableRenderFeature (true: the feature is active, false: the feature is inactive). Use the method ScriptableRenderFeature.SetActive to change the value of this variable.
        /// </summary>
        public bool isActive => m_Active;
        
        /// <summary>
        /// Specifies whether a render pass makes use of an intermediate texture.
        /// This allows for early optimization by skipping incompatible passes.
        /// </summary>
        [Obsolete("This enum is not used. #from(6000.3)", false)]
        public enum IntermediateTextureUsage 
        {
            /// <summary>
            /// The usage is not specified. The system will attempt to run the passes and determine compatibility at execution time.
            /// </summary>
            Unknown, 
            /// <summary>
            /// The passes require or use an intermediate texture.
            /// </summary>
            Required, 
            /// <summary>
            /// The passes do not use an intermediate texture.
            /// This signals that the passes can be safely skipped if no intermediate texture is available.
            /// </summary>
            NotRequired 
        }

        /// <summary>
        /// Specifies the feature's dependency on an intermediate texture. Override this property to allow the renderer to optimize its setup by skipping the creation of render passes for features that are incompatible with the pipeline's Intermediate Texture setting.
        /// </summary>
        [Obsolete("This property is not used. #from(6000.3)", false)]
        protected virtual IntermediateTextureUsage useIntermediateTextures => IntermediateTextureUsage.Unknown;

        /// <summary>
        /// Initializes this feature's resources. This is called every time serialization happens.
        /// </summary>
        public abstract void Create();

        /// <summary>
        /// Callback before cull happens in renderer.
        /// </summary>
        /// <param name="renderer">Renderer of callback.</param>
        /// <param name="cameraData">CameraData contains all relevant render target information for the camera.</param>
        public virtual void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData) { }

        /// <summary>
        /// Injects one or multiple <c>ScriptableRenderPass</c> in the renderer.
        /// </summary>
        /// <param name="renderer">Renderer used for adding render passes.</param>
        /// <param name="renderingData">Rendering state. Use this to setup render passes.</param>
        public abstract void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData);

#if URP_COMPATIBILITY_MODE
        /// <summary>
        /// Callback after render targets are initialized. This allows for accessing targets from renderer after they are created and ready.
        /// </summary>
        /// <param name="renderer">Renderer used for adding render passes.</param>
        /// <param name="renderingData">Rendering state. Use this to setup render passes.</param>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete + " #from(6000.2)")]
        public virtual void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) { }
#endif

        void OnEnable()
        {
            // UUM-44048: If the pipeline is not created, don't call Create() as it may allocate RTHandles or do other
            // things that require the pipeline to be constructed. This is safe because once the pipeline is constructed,
            // ScriptableRendererFeature.Create() will be called by ScriptableRenderer constructor.
            if (RenderPipelineManager.currentPipeline is UniversalRenderPipeline)
                Create();
        }

        void OnValidate()
        {
            // See comment in OnEnable.
            if (RenderPipelineManager.currentPipeline is UniversalRenderPipeline)
                Create();
        }

#if URP_COMPATIBILITY_MODE
        /// <summary>
        /// Override this method and return true if the feature should use the Native RenderPass API
        /// </summary>
        internal virtual bool SupportsNativeRenderPass()
        {
            return false;
        }
#endif

        /// <summary>
        /// Override this method and return true that renderer would produce rendering layers texture.
        /// </summary>
        /// <param name="isDeferred">True if renderer is using deferred rendering mode</param>
        /// <param name="needsGBufferAccurateNormals">True if renderer has Accurate G-Buffer Normals enabled</param>
        /// <param name="atEvent">Requeted event at which rendering layers texture will be produced</param>
        /// <param name="maskSize">Requested bit size of rendering layers texture</param>
        /// <returns></returns>
        internal virtual bool RequireRenderingLayers(bool isDeferred, bool needsGBufferAccurateNormals, out RenderingLayerUtils.Event atEvent, out RenderingLayerUtils.MaskSize maskSize)
        {
            atEvent = RenderingLayerUtils.Event.DepthNormalPrePass;
            maskSize = RenderingLayerUtils.MaskSize.Bits8;
            return false;
        }

        /// <summary>
        /// Sets the state of ScriptableRenderFeature (true: the feature is active, false: the feature is inactive).
        /// If the feature is active, it is added to the renderer it is attached to, otherwise the feature is skipped while rendering.
        /// </summary>
        /// <param name="active">The true value activates the ScriptableRenderFeature and the false value deactivates it.</param>
        public void SetActive(bool active)
        {
            m_Active = active;
        }

        /// <summary>
        /// Disposable pattern implementation.
        /// Cleans up resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Called by Dispose().
        /// Override this function to clean up resources in your renderer.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
