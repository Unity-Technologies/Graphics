using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Provides the global shader variables of the URP 3D renderer that every later pass of the camera reads, as the
    /// calling pass reads them on the render graph execute timeline.
    /// </summary>
    /// <remarks>
    /// The values are staged. URP keeps your changes from one pass to the next, and sends them to the GPU only when you
    /// call <see cref="IGlobalShaderVariablesGlobalUploader.Push"/>. For changes to the calling pass only, use
    /// <see cref="UniversalGlobalShaderData.Get"/> instead.
    /// </remarks>
    public interface IUniversalGlobalShaderVariablesUploader : IGlobalShaderVariablesOnly3DUploader, IGlobalShaderVariablesCommonUploader
    {}

    /// <summary>
    /// Provides the global shader variables of the URP 2D renderer that every later pass of the camera reads, as the
    /// calling pass reads them on the render graph execute timeline.
    /// </summary>
    /// <remarks>
    /// The values are staged. URP keeps your changes from one pass to the next, and sends them to the GPU only when you
    /// call <see cref="IGlobalShaderVariablesGlobalUploader.Push"/>. For changes to the calling pass only, use
    /// <see cref="Universal2DGlobalShaderData.Get"/> instead.
    /// </remarks>
    public interface IUniversal2DGlobalShaderVariablesUploader : IGlobalShaderVariablesOnly2DUploader, IGlobalShaderVariablesCommonUploader
    {}

    /// <summary>
    /// The global shader variables shared by URP 2D and 3D, with the methods that push and reset them.
    /// </summary>
    /// <remarks>
    /// Use this interface in passes that run with both the URP 3D and 2D renderers. The objects that
    /// <see cref="UniversalGlobalShaderData.GetGlobal"/> and <see cref="Universal2DGlobalShaderData.GetGlobal"/> return
    /// implement it.
    /// </remarks>
    public interface IGlobalShaderVariablesCommonUploader : IGlobalShaderVariablesBaseUploader, IGlobalShaderVariablesGlobalUploader
    {}

    /// <summary>
    /// Pushes and resets the staged global shader variables, which every later pass of the camera reads.
    /// </summary>
    /// <remarks>
    /// A pass that pushes changes global state: it must call <see cref="UnityEngine.Rendering.RenderGraphModule.IBaseRenderGraphBuilder.AllowGlobalStateModification"/>
    /// with true, which also keeps the render graph from culling it.
    /// </remarks>
    public interface IGlobalShaderVariablesGlobalUploader
    {
        /// <summary>
        /// Sends the variables changed since the last push to the GPU. They stay set for every later pass of the camera.
        /// </summary>
        /// <param name="cmd">The command buffer of the pass.</param>
        /// <exception cref="InvalidOperationException">Thrown when there is something to push and the pass does not allow global
        /// state modification, which the render graph checks when UNITY_ENABLE_CHECKS is defined. Also thrown while a temporary
        /// copy that was changed or pushed is open.</exception>
        void Push(IBaseCommandBuffer cmd);

        /// <summary>
        /// Sets the variables URP fills for the camera back to the values it filled, without pushing them.
        /// </summary>
        void ResetToDefault();

        /// <summary>
        /// Sets the variables URP fills for the camera back to the values it filled, and pushes them.
        /// </summary>
        /// <param name="cmd">The command buffer of the pass.</param>
        /// <exception cref="InvalidOperationException">Thrown when there is something to push and the pass does not allow global
        /// state modification, which the render graph checks when UNITY_ENABLE_CHECKS is defined. Also thrown while a temporary
        /// copy that was changed or pushed is open.</exception>
        void PushDefault(IBaseCommandBuffer cmd);
    }

    /// <summary>
    /// A temporary copy of the global shader variables that applies to the current pass only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="UniversalGlobalShaderData.Get"/> and <see cref="Universal2DGlobalShaderData.Get"/> methods return this
    /// copy. Its values start as the staged ones. Disposing it restores what was bound before it.
    /// </para>
    /// <para>
    /// Disposing the copy records the restore on the command buffer of its last push. A copy that never pushed has nothing
    /// to put back. Get() returns the same object every time, so don't use a copy after disposing it.
    /// </para>
    /// <para>
    /// Push what you changed through GetGlobal() before calling Get(). With loose uniforms, the first push of the copy also
    /// sends it, and it stays after the copy is disposed. With the persistent constant buffer, it only applies while the
    /// copy is bound.
    /// </para>
    /// <para>
    /// Only one temporary copy can be open at a time. Dispose it before the render function returns: calling Get() again,
    /// or pushing through GetGlobal(), while a copy that was changed or pushed is open throws an InvalidOperationException.
    /// A copy that was only read can stay open. URP closes a copy still open at the end of the camera, and logs that
    /// exception if the copy was changed or pushed.
    /// </para>
    /// </remarks>
    /// <seealso cref="UniversalGlobalShaderData"/>
    /// <seealso cref="Universal2DGlobalShaderData"/>
    /// <seealso cref="IGlobalShaderVariablesGlobalUploader"/>
    public interface IGlobalShaderVariablesTemporaryUploader : IDisposable
    {
        /// <summary>
        /// Sends the temporary values to the GPU for every shader of the pass, until the temporary copy is disposed.
        /// </summary>
        /// <remarks>
        /// This changes global state: the pass must call <see cref="UnityEngine.Rendering.RenderGraphModule.IBaseRenderGraphBuilder.AllowGlobalStateModification"/>
        /// with true. Not for async compute passes: use <see cref="PushToCompute"/> there, see its remarks.
        /// </remarks>
        /// <param name="cmd">The command buffer of the pass.</param>
        /// <exception cref="ObjectDisposedException">Thrown when the temporary copy was disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when there is something to push and the pass does not allow global
        /// state modification. The render graph checks it when UNITY_ENABLE_CHECKS is defined.</exception>
        void Push(IBaseCommandBuffer cmd);

        /// <summary>
        /// Gives the temporary values to one compute shader, until the temporary copy is disposed.
        /// </summary>
        /// <remarks>
        /// With the persistent constant buffer, only <paramref name="computeShader"/> reads these values: the other shaders
        /// keep reading the staged values and the pass changes no global state, so this suits async compute passes. With
        /// loose uniforms, the default and the only mode on OpenGL ES 3 (except on WebGL) and OpenGL Core, a compute shader
        /// reads the global value of each variable it doesn't set itself, so URP pushes the values globally instead, like
        /// <see cref="Push"/>, until the temporary copy is disposed. A pass that calls this must therefore call
        /// <see cref="UnityEngine.Rendering.RenderGraphModule.IBaseRenderGraphBuilder.AllowGlobalStateModification"/> with true.
        /// </remarks>
        /// <param name="cmd">The command buffer of the pass, a compute or unsafe one.</param>
        /// <param name="computeShader">The compute shader that reads the temporary values.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="computeShader"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the temporary copy was disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown with loose uniforms when there is something to push and the
        /// pass does not allow global state modification.</exception>
        void PushToCompute(IComputeCommandBuffer cmd, ComputeShader computeShader);

        /// <summary>
        /// Sets the variables URP fills for the camera back to the values it filled, in this copy only, without pushing them.
        /// </summary>
        void ResetToDefault();
    }

    /// <summary>
    /// A temporary copy of the URP 3D global shader variables.
    /// </summary>
    /// <remarks>
    /// For details, refer to <see cref="IGlobalShaderVariablesTemporaryUploader"/>.
    /// </remarks>
    public interface IUniversalGlobalShaderVariablesTemporaryUploader :
        IGlobalShaderVariablesBaseUploader, IGlobalShaderVariablesOnly3DUploader, IGlobalShaderVariablesTemporaryUploader
    {}

    /// <summary>
    /// A temporary copy of the URP 2D global shader variables.
    /// </summary>
    /// <remarks>
    /// For details, refer to <see cref="IGlobalShaderVariablesTemporaryUploader"/>.
    /// </remarks>
    public interface IUniversal2DGlobalShaderVariablesTemporaryUploader :
        IGlobalShaderVariablesBaseUploader, IGlobalShaderVariablesOnly2DUploader, IGlobalShaderVariablesTemporaryUploader
    {}

    /// <summary>
    /// The global shader variables shared by URP 2D and 3D, declared in GlobalShaderVariables.hlsl. Each property is
    /// named after its shader variable.
    /// </summary>
    /// <remarks>
    /// In the time variables, t is Time.time (Time.realtimeSinceStartup in Edit mode), dt is Time.deltaTime and smoothdt
    /// is Time.smoothDeltaTime.
    /// _DitheringTextureInvSize is read-only: it describes the dithering texture URP binds.
    /// </remarks>
    public interface IGlobalShaderVariablesBaseUploader
    {
        /// <summary>The time, scaled: [t/20, t, t*2, t*3].</summary>
        Vector4 _Time { get; set; }
        /// <summary>Sine of the time: [sin(t/8), sin(t/4), sin(t/2), sin(t)].</summary>
        Vector4 _SinTime { get; set; }
        /// <summary>Cosine of the time: [cos(t/8), cos(t/4), cos(t/2), cos(t)].</summary>
        Vector4 _CosTime { get; set; }
        /// <summary>Delta time: [dt, 1/dt, smoothdt, 1/smoothdt].</summary>
        Vector4 unity_DeltaTime { get; set; }
        /// <summary>Time with its sine and cosine: [t, sin(t), cos(t), 0].</summary>
        Vector4 _TimeParameters { get; set; }
        /// <summary>The time parameters of the previous frame: [t, sin(t), cos(t), 0].</summary>
        Vector4 _LastTimeParameters { get; set; }
        /// <summary>Size of the camera viewport in pixels, before render scale: [width, height, 1 + 1/width, 1 + 1/height].</summary>
        Vector4 _ScreenParams { get; set; }
        /// <summary>Values used to linearize the Z buffer: [1 - far/near, far/near, x/far, y/far], or [-1 + far/near, 1, x/far, 1/far] with a reversed Z buffer.</summary>
        Vector4 _ZBufferParams { get; set; }
        /// <summary>Half extents of the orthographic view: [orthographicSize * aspect, orthographicSize, unused, 1 if orthographic else 0].</summary>
        Vector4 unity_OrthoParams { get; set; }
        /// <summary>Scale of the current (xy) and previous (zw) viewports relative to the RTHandle reference size. URP fills (1, 1, 1, 1).</summary>
        Vector4 _RTHandleScale { get; set; }
        /// <summary>The screen parameters after render scale and dynamic resolution, in the same layout as _ScreenParams.</summary>
        Vector4 _ScaledScreenParams { get; set; }
        /// <summary>Size of the camera target after render scale and dynamic resolution: [width, height, 1/width, 1/height]. After upscaling, URP sets it to the upscaled size, but PushDefault() and ResetToDefault() put back the size before upscaling.</summary>
        Vector4 _ScreenSize { get; set; }
        /// <summary>Screen size that shaders read instead of _ScreenSize when the camera uses a screen coordinates override (SCREEN_COORD_OVERRIDE), from UniversalAdditionalCameraData.screenSizeOverride.</summary>
        Vector4 _ScreenSizeOverride { get; set; }
        /// <summary>Scale (xy) and bias (zw) applied to screen coordinates when the camera uses a screen coordinates override, from UniversalAdditionalCameraData.screenCoordScaleBias.</summary>
        Vector4 _ScreenCoordScaleBias { get; set; }
        /// <summary>Ambient sky color of the gradient ambient mode.</summary>
        Vector4 unity_AmbientSky { get; set; }
        /// <summary>Ambient equator color of the gradient ambient mode.</summary>
        Vector4 unity_AmbientEquator { get; set; }
        /// <summary>Ambient ground color of the gradient ambient mode.</summary>
        Vector4 unity_AmbientGround { get; set; }
        /// <summary>Texture mip bias: [mip bias, 2 ^ mip bias].</summary>
        Vector2 _GlobalMipBias { get; set; }
        /// <summary>Inverse width of the LOD cross fade dithering texture.</summary>
        float _DitheringTextureInvSize { get; }

        // Environment
        /// <summary>Color of the glossy reflections of materials with environment reflections off: the constant term of the ambient probe times the reflection intensity.</summary>
        Vector4 _GlossyEnvironmentColor { get; set; }
        /// <summary>Shadow color of the subtractive mixed lighting mode.</summary>
        Vector4 _SubtractiveShadowColor { get; set; }
        /// <summary>HDR decode instructions of the default reflection cubemap.</summary>
        Vector4 _GlossyEnvironmentCubeMap_HDR { get; set; }
    }

    /// <summary>
    /// The global shader variables only URP 3D fills, declared in GlobalShaderVariables.hlsl. Each property
    /// is named after its shader variable.
    /// </summary>
    /// <remarks>
    /// The objects that <see cref="UniversalGlobalShaderData.Get"/> and <see cref="UniversalGlobalShaderData.GetGlobal"/>
    /// return implement this interface. The URP 2D renderer doesn't fill these variables.
    /// The read-only properties describe what URP binds or configures for the camera: the light lists, the shadow maps
    /// and their PCSS filtering, the light cookies, the probe volumes and the rendering layers.
    /// </remarks>
    public interface IGlobalShaderVariablesOnly3DUploader
    {
        // Main Light
        /// <summary>World to light matrix of the main light cookie.</summary>
        Matrix4x4 _MainLightWorldToLight { get; set; }
        /// <summary>Direction towards the main light with w = 0 for a directional light, position with w = 1 otherwise.</summary>
        Vector4 _MainLightPosition { get; set; }
        /// <summary>Color of the main light times its intensity, in rgb. w is 0 when the main light uses subtractive mixed lighting, 1 otherwise.</summary>
        Vector4 _MainLightColor { get; set; }
        /// <summary>Occlusion probe channels of the main light.</summary>
        Vector4 _MainLightOcclusionProbes { get; set; }
        /// <summary>Rendering layer mask of the main light.</summary>
        uint _MainLightLayerMask { get; set; }

        // Forward+
        /// <summary>Forward+ parameters: [zBinScale, zBinOffset, lightCount, directionalLightCount].</summary>
        Vector4 _FPParams0 { get; }
        /// <summary>Forward+ parameters: [tileScale.x, tileScale.y, tileCountX, wordsPerTile].</summary>
        Vector4 _FPParams1 { get; }
        /// <summary>Forward+ parameters: [zBinCount, tileCount, tileCountY, 0].</summary>
        Vector4 _FPParams2 { get; }

        // Motion Vectors
        /// <summary>Non-jittered view projection matrix of the camera.</summary>
        Matrix4x4 _NonJitteredViewProjMatrix { get; set; }
        /// <summary>Non-jittered view projection matrix of the camera in the previous frame.</summary>
        Matrix4x4 _PrevViewProjMatrix { get; set; }

        // Additional Lights
        /// <summary>Number of additional lights the shaders may read, in x.</summary>
        Vector4 _AdditionalLightsCount { get; }

        // Main Light Shadows
        /// <summary>The world to shadow matrices of the main light, one per cascade plus a trailing no-op matrix for indices past the last cascade.</summary>
        MainLightShadowMatrices _MainLightWorldToShadow { get; set; }
        /// <summary>Bounding sphere of the first cascade, xyz = center and w = radius.</summary>
        Vector4 _CascadeShadowSplitSpheres0 { get; set; }
        /// <summary>Bounding sphere of the second cascade, xyz = center and w = radius.</summary>
        Vector4 _CascadeShadowSplitSpheres1 { get; set; }
        /// <summary>Bounding sphere of the third cascade, xyz = center and w = radius.</summary>
        Vector4 _CascadeShadowSplitSpheres2 { get; set; }
        /// <summary>Bounding sphere of the fourth cascade, xyz = center and w = radius.</summary>
        Vector4 _CascadeShadowSplitSpheres3 { get; set; }
        /// <summary>Squared radii of the four cascade bounding spheres.</summary>
        Vector4 _CascadeShadowSplitSphereRadii { get; set; }
        /// <summary>Soft shadow sampling offsets, xy = offset0 and zw = offset1.</summary>
        Vector4 _MainLightShadowOffset0 { get; }
        /// <summary>Soft shadow sampling offsets, xy = offset2 and zw = offset3.</summary>
        Vector4 _MainLightShadowOffset1 { get; }
        /// <summary>xy = 1/width and 1/height, zw = width and height of the main light shadow map.</summary>
        Vector4 _MainLightShadowmapSize { get; }

        // Main Light Shadows, PCSS
        /// <summary>For each cascade: x = depth2RadialScale, y = radial2DepthScale, z = maxSampleZDistance, w = maxPCSSOffset.</summary>
        MainLightShadowCascadeVectors _MainLightPCSSParams0 { get; }
        /// <summary>For each cascade: x = minFilterSizeTexels, y = minFilterRadial2DepthScale, z = blockerRadial2DepthScale, w = blockerClumpSampleExponent.</summary>
        MainLightShadowCascadeVectors _MainLightPCSSParams1 { get; }
        /// <summary>For each cascade: atlas tile offset (xy) and scale (zw), in UV.</summary>
        MainLightShadowCascadeVectors _MainLightPCSSAtlas { get; set; }
        /// <summary>x = tile texel size, y = blocker sample count, z = filter sample count, w = temporal frame index.</summary>
        Vector4 _MainLightPCSSScalars { get; }

        // Additional Lights Shadows
        /// <summary>Soft shadow sampling offsets, xy = offset0 and zw = offset1.</summary>
        Vector4 _AdditionalShadowOffset0 { get; }
        /// <summary>Soft shadow sampling offsets, xy = offset2 and zw = offset3.</summary>
        Vector4 _AdditionalShadowOffset1 { get; }
        /// <summary>xy = 1/width and 1/height, zw = width and height of the additional lights shadow map.</summary>
        Vector4 _AdditionalShadowmapSize { get; }

        // Misc
        /// <summary>How shaders read the main light cookie: 0 = RGB, 1 = alpha, 2 = red, -1 when the main light has no cookie.</summary>
        float _MainLightCookieTextureFormat { get; }
        /// <summary>How shaders read the additional lights cookie atlas: 0 = RGB, 1 = alpha, 2 = red.</summary>
        float _AdditionalLightsCookieAtlasTextureFormat { get; }
        /// <summary>Highest value a rendering layer mask can hold, given the configured mask size.</summary>
        uint _RenderingLayerMaxInt { get; }
        /// <summary>1 when Adaptive Probe Volume data is loaded and usable, 0 otherwise.</summary>
        uint _EnableProbeVolumes { get; }
    }

    /// <summary>
    /// The global shader variables only URP 2D fills.
    /// </summary>
    /// <remarks>
    /// The objects that <see cref="Universal2DGlobalShaderData.Get"/> and <see cref="Universal2DGlobalShaderData.GetGlobal"/>
    /// return implement this interface. It has no members yet, because the URP 2D renderer currently fills only the
    /// variables in <see cref="IGlobalShaderVariablesBaseUploader"/>.
    /// </remarks>
    // Empty while that struct is disabled, kept so the 2D renderer keeps its own uploader surface.
    public interface IGlobalShaderVariablesOnly2DUploader
    {
        // URP 2D-only globals, empty so disabled.
        // Vector4 _URPDummy2D { get; set; }
    }
}