using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Gives the passes of the URP 3D renderer access to the URP global shader variables.
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
    /// buffer URP bound for its last camera stays bound while **Use Persistent Global Constant Buffer** is enabled, so
    /// SetGlobal* doesn't reach these variables. Unbind it first, then set the variables your shaders read, as in the
    /// example that draws a mesh into a render texture. URP binds its buffer again for its next camera. URP doesn't
    /// update the loose uniform values in this mode, so the shaders read what you set, or stale values.
    /// </para>
    /// <para>
    /// This API is experimental and might change in future versions.
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>This sample code draws the opaque objects again into a half-resolution texture, for example as the source
    /// of a glow effect. The shaders of the pass read the screen size of the half-resolution texture, and the shaders
    /// of later passes read the screen size of the camera. The renderer feature adds the pass to the renderer.</para>
    ///
    /// <code source="../../DocCodeSamples.Tests/GlobalShaderVariablesSamples.cs" region="temporary-copy"/>
    /// <para>This sample code draws a mesh with a URP material into a render texture outside URP camera rendering, for
    /// example to make a preview in a Unity Editor tool. It unbinds the URP global constant buffer, then sets the
    /// variables that the shaders read.</para>
    ///
    /// <code source="../../DocCodeSamples.Tests/GlobalShaderVariablesSamples.cs" region="outside-camera-rendering"/>
    /// </example>
    /// <seealso cref="IUniversalGlobalShaderVariablesTemporaryUploader"/>
    /// <seealso cref="IUniversalGlobalShaderVariablesUploader"/>
    /// <seealso cref="UnityEngine.Rendering.Universal.RenderingCapabilitiesSettings.useGlobalConstantBuffer"/>
    public class UniversalGlobalShaderData : ContextItem
    {
        // Owned and created by the renderer
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
        /// <example>
        /// <para>This sample code draws the opaque objects again into a half-resolution texture, for example as the
        /// source of a glow effect. The shaders of the pass read the screen size of the half-resolution texture, and
        /// the shaders of later passes read the screen size of the camera. The renderer feature adds the pass to the
        /// renderer.</para>
        ///
        /// <code source="../../DocCodeSamples.Tests/GlobalShaderVariablesSamples.cs" region="temporary-copy"/>
        /// </example>
        /// <returns>The temporary copy of the global shader variables.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called outside the render graph execution of the camera,
        /// for example while the render graph records, or while a temporary copy that was changed or pushed is open.</exception>
        public IUniversalGlobalShaderVariablesTemporaryUploader Get() => GetUploader().Get();

        /// <summary>
        /// Gets the global shader variables at the point of the execute timeline where the calling pass runs. What
        /// the pass pushes through them stays set for the later passes of the camera.
        /// </summary>
        /// <remarks>
        /// Changes that you push through the returned object stay set for every later pass of the camera. To go back to the
        /// values URP filled for the camera, call <see cref="IGlobalShaderVariablesGlobalUploader.PushDefault"/> on the
        /// returned object. For changes to the calling pass only, use <see cref="Get"/> instead.
        /// </remarks>
        /// <example>
        /// <para>This sample code makes texture sampling sharper for every later pass of the camera, for example when
        /// the camera renders at a low render scale and an upscaler restores the resolution. A negative mip bias
        /// selects more detailed mipmaps.</para>
        ///
        /// <code source="../../DocCodeSamples.Tests/GlobalShaderVariablesSamples.cs" region="global-variables"/>
        /// </example>
        /// <returns>The staged global shader variables.</returns>
        /// <exception cref="InvalidOperationException">Thrown when called outside the render graph execution of the camera,
        /// for example while the render graph records.</exception>
        public IUniversalGlobalShaderVariablesUploader GetGlobal() => GetUploader();

        // URP's own passes write the variables that are read-only on the public interfaces through it.
        internal GlobalShaderVariablesUploader GetUploader()
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