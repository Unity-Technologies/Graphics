using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Gives the passes of the URP 2D renderer access to the URP global shader variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Get it from the frame data while recording, keep it in the pass data, and call <see cref="Get"/> or
    /// <see cref="GetGlobal"/> from the render function: the variables are only available while the render graph
    /// executes. Get() changes them for the calling pass only, GetGlobal() for every later pass of the camera. A pass that
    /// pushes them must call
    /// <see cref="UnityEngine.Rendering.RenderGraphModule.IBaseRenderGraphBuilder.AllowGlobalStateModification"/> with true.
    /// </para>
    /// <para>
    /// Outside URP camera rendering, for example in an editor tool that draws URP shaders with a command buffer, the
    /// buffer URP bound for its last camera stays bound while Use Persistent Global Constant Buffer is on, so SetGlobal*
    /// doesn't reach these variables. Unbind it first, then set the variables your shaders read, as in the example.
    /// URP binds its buffer again for its next camera. URP doesn't update the loose uniform values in this mode, so the
    /// shaders read what you set, or stale values.
    /// </para>
    /// <para>
    /// This API is experimental and might change in future versions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>This sample code sets a variable for URP shaders drawn outside URP camera rendering:</para>
    /// <code>
    /// cmd.SetGlobalConstantBuffer((GraphicsBuffer)null, Shader.PropertyToID("GlobalShaderVariables"), 0, 0);
    /// cmd.SetGlobalVector("_ScreenSize", new Vector4(width, height, 1f / width, 1f / height));
    /// </code>
    /// </example>
    /// <seealso cref="IUniversal2DGlobalShaderVariablesTemporaryUploader"/>
    /// <seealso cref="IUniversal2DGlobalShaderVariablesUploader"/>
    /// <seealso cref="UnityEngine.Rendering.Universal.RenderingCapabilitiesSettings.useGlobalConstantBuffer"/>
    // Lives in the URP Runtime assembly (Unity.RenderPipelines.Universal.Runtime) rather than the URP 2D one
    // (Unity.RenderPipelines.Universal.2D.Runtime), like Universal2DResourceData: passes shared by both renderers need to
    // reach it, and the 2D assembly references this one, not the reverse.
    public class Universal2DGlobalShaderData : ContextItem
    {
        // Owned and created by the renderer (so its persistent constant buffer survives across frames); this per-frame
        // ContextItem only references it, set once per frame via Set().
        private GlobalShaderVariablesUploader m_Uploader;

        internal void Set(GlobalShaderVariablesUploader uploader) => m_Uploader = uploader;

        /// <summary>
        /// Creates a temporary copy of the URP global shader variables that applies to the calling pass only.
        /// </summary>
        /// <remarks>
        /// Disposing the copy restores the values that were bound before you called this method. Dispose it before the
        /// render function returns, typically with a using statement. To make changes that persist for the later passes
        /// of the camera, use <see cref="GetGlobal"/> instead.
        /// </remarks>
        /// <returns>The temporary copy of the global shader variables.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called outside the render graph execution of the camera,
        /// for example while the render graph records, or while a temporary copy that was changed or pushed is open.</exception>
        public IUniversal2DGlobalShaderVariablesTemporaryUploader Get() => GetUploader().Get();

        /// <summary>
        /// Gets the global shader variables at the point of the execute timeline where the calling pass runs. What
        /// the pass pushes through them stays set for the later passes of the camera.
        /// </summary>
        /// <remarks>
        /// Changes that you push through the returned object stay set for every later pass of the camera. To go back to the
        /// values URP filled for the camera, call <see cref="IGlobalShaderVariablesGlobalUploader.PushDefault"/> on the
        /// returned object. For changes to the calling pass only, use <see cref="Get"/> instead.
        /// </remarks>
        /// <returns>The staged global shader variables.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called outside the render graph execution of the camera,
        /// for example while the render graph records.</exception>
        public IUniversal2DGlobalShaderVariablesUploader GetGlobal() => GetUploader();

        private GlobalShaderVariablesUploader GetUploader()
        {
            if (m_Uploader == null)
                throw new InvalidOperationException(
                    "The global shader variables uploader is only available while the render graph executes. " +
                    "Access it from SetRenderFunc().");

            return m_Uploader;
        }

        /// <summary>
        /// Clears the reference to the global shader variables so the frame data can reuse this item.
        /// </summary>
        /// <remarks>
        /// The render pipeline calls this method when it resets the frame data. You don't need to call it yourself.
        /// </remarks>
        public override void Reset()
        {
            m_Uploader = null;
        }
    }
}