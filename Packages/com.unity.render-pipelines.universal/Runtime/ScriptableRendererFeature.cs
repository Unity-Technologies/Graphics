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
    public abstract class ScriptableRendererFeature : ScriptableObject, IDisposable
    {
        [SerializeField, HideInInspector] private bool m_Active = true;
        /// <summary>
        /// Returns the state of the ScriptableRenderFeature (true: the feature is active, false: the feature is inactive). Use the method ScriptableRenderFeature.SetActive to change the value of this variable.
        /// </summary>
        public bool isActive => m_Active;

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

        /// <summary>
        /// Callback after render targets are initialized. This allows for accessing targets from renderer after they are created and ready.
        /// </summary>
        /// <param name="renderer">Renderer used for adding render passes.</param>
        /// <param name="renderingData">Rendering state. Use this to setup render passes.</param>
        public virtual void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) { }

        void OnEnable()
        {
            Create();
        }

        void OnValidate()
        {
            Create();
        }

        /// <summary>
        /// Override this method and return true if the feature should use the Native RenderPass API
        /// </summary>
        internal virtual bool SupportsNativeRenderPass()
        {
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
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
