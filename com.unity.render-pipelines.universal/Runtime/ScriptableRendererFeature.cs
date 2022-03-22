using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// You can add a <c>ScriptableRendererFeature</c> to the <c>ScriptableRenderer</c>. Use this scriptable renderer feature to inject render passes into the renderer.
    /// </summary>
    /// <seealso cref="ScriptableRenderer"/>
    /// <seealso cref="ScriptableRenderPass"/>
    [Serializable]
    public abstract class ScriptableRendererFeature : IDisposable
    {
        /// <summary>
        /// Name of the ScriptableRendererFeature
        /// </summary>
        public string name = "Custom Renderer Feature";
        /// <summary>
        /// Returns the state of the ScriptableRenderFeature (true: the feature is active, false: the feature is inactive). Use the method ScriptableRenderFeature.SetActive to change the value of this variable.
        /// </summary>
        [HideInInspector]
        public bool isActive = true;

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
        /*
        void OnEnable()
        {
            Create();
        }

        void OnValidate()
        {
            Create();
        }*/

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
            isActive = active;
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

    /// <summary>
    /// You can add a <c>ScriptableRendererFeature</c> to the <c>ScriptableRenderer</c>. Use this scriptable renderer feature to inject render passes into the renderer.
    /// </summary>
    /// <seealso cref="ScriptableRenderer"/>
    /// <seealso cref="ScriptableRenderPass"/>
    [ExcludeFromPreset, Obsolete("ScriptableRendererFeature no longer uses ScriptableObject in order to avoid sub assets.")]
    public abstract class ScriptableRendererFeatureAssetLegacy : ScriptableObject, IDisposable
    {
#pragma warning disable 414 // Never used warning - Needed to show in editor.
        [SerializeField, HideInInspector] private bool m_Active = true;
#pragma warning restore 414 // Never used warning

        /// <summary>
        /// Function is called to convert the asset settings into the URP asset such that setting wont be lost.
        /// </summary>
        /// <returns> Returns a new renderer feature version without sciptable object inheritance. </returns>
        public abstract ScriptableRendererFeature UpgradeToRendererFeatureWithoutAsset();

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
