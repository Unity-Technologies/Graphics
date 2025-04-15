using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;

namespace UnityEngine.Rendering.Universal
{
    static class NativeArrayExtensions
    {
        /// <summary>
        /// IMPORTANT: Make sure you do not write to the value! There are no checks for this!
        /// </summary>
        public static unsafe ref T UnsafeElementAt<T>(this NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        public static unsafe ref T UnsafeElementAtMutable<T>(this NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }

    /// <summary>
    /// Options for mixed lighting setup.
    /// </summary>
    public enum MixedLightingSetup
    {
        /// <summary>
        /// Use this to disable mixed lighting.
        /// </summary>
        None,

        /// <summary>
        /// Use this to select shadow mask.
        /// </summary>
        ShadowMask,

        /// <summary>
        /// Use this to select subtractive.
        /// </summary>
        Subtractive,
    };

    /// <summary>
    /// Enumeration that indicates what kind of image scaling is occurring if any
    /// </summary>
    internal enum ImageScalingMode
    {
        /// No scaling
        None,

        /// Upscaling to a larger image
        Upscaling,

        /// Downscaling to a smaller image
        Downscaling
    }

    /// <summary>
    /// Enumeration that indicates what kind of upscaling filter is being used
    /// </summary>
    internal enum ImageUpscalingFilter
    {
        /// Bilinear filtering
        Linear,

        /// Nearest-Neighbor filtering
        Point,

        /// FidelityFX Super Resolution
        FSR,

        /// Spatial-Temporal Post-Processing
        STP
    }

    /// <summary>
    /// Struct that flattens several rendering settings used to render a camera stack.
    /// URP builds the <c>RenderingData</c> settings from several places, including the pipeline asset, camera and light settings.
    /// The settings also might vary on different platforms and depending on if Adaptive Performance is used.
    /// </summary>
    public struct RenderingData
    {
        internal ContextContainer frameData;

        internal RenderingData(ContextContainer frameData)
        {
            this.frameData = frameData;
            cameraData = new CameraData(frameData);
            lightData = new LightData(frameData);
            shadowData = new ShadowData(frameData);
            postProcessingData = new PostProcessingData(frameData);
        }

        internal UniversalRenderingData universalRenderingData => frameData.Get<UniversalRenderingData>();

        // Non-rendergraph path only. Do NOT use with rendergraph!
        internal ref CommandBuffer commandBuffer
        {
            get
            {
                ref var cmd = ref frameData.Get<UniversalRenderingData>().m_CommandBuffer;
                if (cmd == null)
                    Debug.LogError("RenderingData.commandBuffer is null. RenderGraph does not support this property. Please use the command buffer provided by the RenderGraphContext.");

                return ref cmd;
            }
        }

        /// <summary>
        /// Returns culling results that exposes handles to visible objects, lights and probes.
        /// You can use this to draw objects with <c>ScriptableRenderContext.DrawRenderers</c>
        /// <see cref="CullingResults"/>
        /// <seealso cref="ScriptableRenderContext"/>
        /// </summary>
        public ref CullingResults cullResults => ref frameData.Get<UniversalRenderingData>().cullResults;

        /// <summary>
        /// Holds several rendering settings related to camera.
        /// <see cref="CameraData"/>
        /// </summary>
        public CameraData cameraData;

        /// <summary>
        /// Holds several rendering settings related to lights.
        /// <see cref="LightData"/>
        /// </summary>
        public LightData lightData;

        /// <summary>
        /// Holds several rendering settings related to shadows.
        /// <see cref="ShadowData"/>
        /// </summary>
        public ShadowData shadowData;

        /// <summary>
        /// Holds several rendering settings and resources related to the integrated post-processing stack.
        /// <see cref="PostProcessData"/>
        /// </summary>
        public PostProcessingData postProcessingData;

        /// <summary>
        /// True if the pipeline supports dynamic batching.
        /// This settings doesn't apply when drawing shadow casters. Dynamic batching is always disabled when drawing shadow casters.
        /// </summary>
        public ref bool supportsDynamicBatching => ref frameData.Get<UniversalRenderingData>().supportsDynamicBatching;

        /// <summary>
        /// Holds per-object data that are requested when drawing
        /// <see cref="PerObjectData"/>
        /// </summary>
        public ref PerObjectData perObjectData => ref frameData.Get<UniversalRenderingData>().perObjectData;

        /// <summary>
        /// True if post-processing effect is enabled while rendering the camera stack.
        /// </summary>
        public ref bool postProcessingEnabled => ref frameData.Get<UniversalPostProcessingData>().isEnabled;
    }

    /// <summary>
    /// Struct that holds settings related to lights.
    /// </summary>
    public struct LightData
    {
        ContextContainer frameData;

        internal LightData(ContextContainer frameData)
        {
            this.frameData = frameData;
        }

        internal UniversalLightData universalLightData => frameData.Get<UniversalLightData>();

        /// <summary>
        /// Holds the main light index from the <c>VisibleLight</c> list returned by culling. If there's no main light in the scene, <c>mainLightIndex</c> is set to -1.
        /// The main light is the directional light assigned as Sun source in light settings or the brightest directional light.
        /// <seealso cref="CullingResults"/>
        /// </summary>
        public ref int mainLightIndex => ref frameData.Get<UniversalLightData>().mainLightIndex;

        /// <summary>
        /// The number of additional lights visible by the camera.
        /// </summary>
        public ref int additionalLightsCount => ref frameData.Get<UniversalLightData>().additionalLightsCount;

        /// <summary>
        /// Maximum amount of lights that can be shaded per-object. This value only affects forward rendering.
        /// </summary>
        public ref int maxPerObjectAdditionalLightsCount => ref frameData.Get<UniversalLightData>().maxPerObjectAdditionalLightsCount;

        /// <summary>
        /// List of visible lights returned by culling.
        /// </summary>
        public ref NativeArray<VisibleLight> visibleLights => ref frameData.Get<UniversalLightData>().visibleLights;

        /// <summary>
        /// True if additional lights should be shaded in vertex shader, otherwise additional lights will be shaded per pixel.
        /// </summary>
        public ref bool shadeAdditionalLightsPerVertex => ref frameData.Get<UniversalLightData>().shadeAdditionalLightsPerVertex;

        /// <summary>
        /// True if mixed lighting is supported.
        /// </summary>
        public ref bool supportsMixedLighting => ref frameData.Get<UniversalLightData>().supportsMixedLighting;

        /// <summary>
        /// True if box projection is enabled for reflection probes.
        /// </summary>
        public ref bool reflectionProbeBoxProjection => ref frameData.Get<UniversalLightData>().reflectionProbeBoxProjection;

        /// <summary>
        /// True if blending is enabled for reflection probes.
        /// </summary>
        public ref bool reflectionProbeBlending => ref frameData.Get<UniversalLightData>().reflectionProbeBlending;

        /// <summary>
        /// True if light layers are enabled.
        /// </summary>
        public ref bool supportsLightLayers => ref frameData.Get<UniversalLightData>().supportsLightLayers;

        /// <summary>
        /// True if additional lights enabled.
        /// </summary>
        public ref bool supportsAdditionalLights => ref frameData.Get<UniversalLightData>().supportsAdditionalLights;
    }


    /// <summary>
    /// Struct that holds settings related to camera.
    /// </summary>
    public struct CameraData
    {
        ContextContainer frameData;

        internal CameraData(ContextContainer frameData)
        {
            this.frameData = frameData;
        }

        internal UniversalCameraData universalCameraData => frameData.Get<UniversalCameraData>();

        internal void SetViewAndProjectionMatrix(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            frameData.Get<UniversalCameraData>().SetViewAndProjectionMatrix(viewMatrix, projectionMatrix);
        }

        internal void SetViewProjectionAndJitterMatrix(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 jitterMatrix)
        {
            frameData.Get<UniversalCameraData>().SetViewProjectionAndJitterMatrix(viewMatrix, projectionMatrix, jitterMatrix);
        }
        // Helper function to populate builtin stereo matricies as well as URP stereo matricies
        internal void PushBuiltinShaderConstantsXR(RasterCommandBuffer cmd, bool renderIntoTexture)
        {
            frameData.Get<UniversalCameraData>().PushBuiltinShaderConstantsXR(cmd, renderIntoTexture);
        }

        /// <summary>
        /// Returns the camera view matrix.
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <returns> The camera view matrix. </returns>
        public Matrix4x4 GetViewMatrix(int viewIndex = 0)
        {
            return frameData.Get<UniversalCameraData>().GetViewMatrix(viewIndex);
        }

        /// <summary>
        /// Returns the camera projection matrix. Might be jittered for temporal features.
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <returns> The camera projection matrix. </returns>
        public Matrix4x4 GetProjectionMatrix(int viewIndex = 0)
        {
            return frameData.Get<UniversalCameraData>().GetProjectionMatrix(viewIndex);
        }

        internal Matrix4x4 GetProjectionMatrixNoJitter(int viewIndex = 0)
        {
            return frameData.Get<UniversalCameraData>().GetProjectionMatrixNoJitter(viewIndex);
        }

        /// <summary>
        /// Returns the camera GPU projection matrix. This contains platform specific changes to handle y-flip and reverse z. Includes camera jitter if required by active features.
        /// Similar to <c>GL.GetGPUProjectionMatrix</c> but queries URP internal state to know if the pipeline is rendering to render texture.
        /// For more info on platform differences regarding camera projection check: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <seealso cref="GL.GetGPUProjectionMatrix(Matrix4x4, bool)"/>
        /// <returns></returns>
        public Matrix4x4 GetGPUProjectionMatrix(int viewIndex = 0)
        {
            return frameData.Get<UniversalCameraData>().GetGPUProjectionMatrix(viewIndex);
        }

        /// <summary>
        /// Returns the camera GPU projection matrix. This contains platform specific changes to handle y-flip and reverse z. Does not include any camera jitter.
        /// Similar to <c>GL.GetGPUProjectionMatrix</c> but queries URP internal state to know if the pipeline is rendering to render texture.
        /// For more info on platform differences regarding camera projection check: https://docs.unity3d.com/Manual/SL-PlatformDifferences.html
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <seealso cref="GL.GetGPUProjectionMatrix(Matrix4x4, bool)"/>
        /// <returns></returns>
        public Matrix4x4 GetGPUProjectionMatrixNoJitter(int viewIndex = 0)
        {
            return frameData.Get<UniversalCameraData>().GetGPUProjectionMatrixNoJitter(viewIndex);
        }

        internal Matrix4x4 GetGPUProjectionMatrix(bool renderIntoTexture, int viewIndex = 0)
        {
            return frameData.Get<UniversalCameraData>().GetGPUProjectionMatrix(renderIntoTexture, viewIndex);
        }

        /// <summary>
        /// The camera component.
        /// </summary>
        public ref Camera camera => ref frameData.Get<UniversalCameraData>().camera;

        /// <summary>
        /// The camera history texture manager. Used to access camera history from a ScriptableRenderPass.
        /// </summary>
        /// <seealso cref="ScriptableRenderPass"/>
        public ref UniversalCameraHistory historyManager => ref frameData.Get<UniversalCameraData>().m_HistoryManager;

        /// <summary>
        /// The camera render type used for camera stacking.
        /// <see cref="CameraRenderType"/>
        /// </summary>
        public ref CameraRenderType renderType => ref frameData.Get<UniversalCameraData>().renderType;

        /// <summary>
        /// Controls the final target texture for a camera. If null camera will resolve rendering to screen.
        /// </summary>
        public ref RenderTexture targetTexture => ref frameData.Get<UniversalCameraData>().targetTexture;

        /// <summary>
        /// Render texture settings used to create intermediate camera textures for rendering.
        /// </summary>
        public ref RenderTextureDescriptor cameraTargetDescriptor => ref frameData.Get<UniversalCameraData>().cameraTargetDescriptor;
        internal ref Rect pixelRect => ref frameData.Get<UniversalCameraData>().pixelRect;
        internal ref bool useScreenCoordOverride => ref frameData.Get<UniversalCameraData>().useScreenCoordOverride;
        internal ref Vector4 screenSizeOverride => ref frameData.Get<UniversalCameraData>().screenSizeOverride;
        internal ref Vector4 screenCoordScaleBias => ref frameData.Get<UniversalCameraData>().screenCoordScaleBias;
        internal ref int pixelWidth => ref frameData.Get<UniversalCameraData>().pixelWidth;
        internal ref int pixelHeight => ref frameData.Get<UniversalCameraData>().pixelHeight;
        internal ref float aspectRatio => ref frameData.Get<UniversalCameraData>().aspectRatio;

        /// <summary>
        /// Render scale to apply when creating camera textures. Scaled extents are rounded down to integers.
        /// </summary>
        public ref float renderScale => ref frameData.Get<UniversalCameraData>().renderScale;
        internal ref ImageScalingMode imageScalingMode => ref frameData.Get<UniversalCameraData>().imageScalingMode;
        internal ref ImageUpscalingFilter upscalingFilter => ref frameData.Get<UniversalCameraData>().upscalingFilter;
        internal ref bool fsrOverrideSharpness => ref frameData.Get<UniversalCameraData>().fsrOverrideSharpness;
        internal ref float fsrSharpness => ref frameData.Get<UniversalCameraData>().fsrSharpness;
        internal ref HDRColorBufferPrecision hdrColorBufferPrecision => ref frameData.Get<UniversalCameraData>().hdrColorBufferPrecision;

        /// <summary>
        /// True if this camera should clear depth buffer. This setting only applies to cameras of type <c>CameraRenderType.Overlay</c>
        /// <seealso cref="CameraRenderType"/>
        /// </summary>
        public ref bool clearDepth => ref frameData.Get<UniversalCameraData>().clearDepth;

        /// <summary>
        /// The camera type.
        /// <seealso cref="UnityEngine.CameraType"/>
        /// </summary>
        public ref CameraType cameraType => ref frameData.Get<UniversalCameraData>().cameraType;

        /// <summary>
        /// True if this camera is drawing to a viewport that maps to the entire screen.
        /// </summary>
        public ref bool isDefaultViewport => ref frameData.Get<UniversalCameraData>().isDefaultViewport;

        /// <summary>
        /// True if this camera should render to high dynamic range color targets.
        /// </summary>
        public ref bool isHdrEnabled => ref frameData.Get<UniversalCameraData>().isHdrEnabled;

        /// <summary>
        /// True if this camera allow color conversion and encoding for high dynamic range displays.
        /// </summary>
        public ref bool allowHDROutput => ref frameData.Get<UniversalCameraData>().allowHDROutput;

        /// <summary>
        /// True if this camera writes the alpha channel. Requires to color target to have an alpha channel.
        /// </summary>
        public ref bool isAlphaOutputEnabled => ref frameData.Get<UniversalCameraData>().isAlphaOutputEnabled;

        /// <summary>
        /// True if this camera requires to write _CameraDepthTexture.
        /// </summary>
        public ref bool requiresDepthTexture => ref frameData.Get<UniversalCameraData>().requiresDepthTexture;

        /// <summary>
        /// True if this camera requires to copy camera color texture to _CameraOpaqueTexture.
        /// </summary>
        public ref bool requiresOpaqueTexture => ref frameData.Get<UniversalCameraData>().requiresOpaqueTexture;

        /// <summary>
        /// Returns true if post processing passes require depth texture.
        /// </summary>
        public ref bool postProcessingRequiresDepthTexture => ref frameData.Get<UniversalCameraData>().postProcessingRequiresDepthTexture;

        /// <summary>
        /// Returns true if XR rendering is enabled.
        /// </summary>
        public ref bool xrRendering => ref frameData.Get<UniversalCameraData>().xrRendering;

        internal bool requireSrgbConversion => frameData.Get<UniversalCameraData>().requireSrgbConversion;

        /// <summary>
        /// True if the camera rendering is for the scene window in the editor.
        /// </summary>
        public bool isSceneViewCamera => frameData.Get<UniversalCameraData>().isSceneViewCamera;

        /// <summary>
        /// True if the camera rendering is for the preview window in the editor.
        /// </summary>
        public bool isPreviewCamera => frameData.Get<UniversalCameraData>().isPreviewCamera;

        internal bool isRenderPassSupportedCamera => frameData.Get<UniversalCameraData>().isRenderPassSupportedCamera;

        internal bool resolveToScreen => frameData.Get<UniversalCameraData>().resolveToScreen;

        /// <summary>
        /// True if the Camera should output to an HDR display.
        /// </summary>
        public bool isHDROutputActive => frameData.Get<UniversalCameraData>().isHDROutputActive;

        /// <summary>
        /// HDR Display information about the current display this camera is rendering to.
        /// </summary>
        public HDROutputUtils.HDRDisplayInformation hdrDisplayInformation => frameData.Get<UniversalCameraData>().hdrDisplayInformation;

        /// <summary>
        /// HDR Display Color Gamut
        /// </summary>
        public ColorGamut hdrDisplayColorGamut => frameData.Get<UniversalCameraData>().hdrDisplayColorGamut;

        /// <summary>
        /// True if the Camera should render overlay UI.
        /// </summary>
        public bool rendersOverlayUI => frameData.Get<UniversalCameraData>().rendersOverlayUI;

        /// <summary>
        /// True is the handle has its content flipped on the y axis.
        /// This happens only with certain rendering APIs.
        /// On those platforms, any handle will have its content flipped unless rendering to a backbuffer, however,
        /// the scene view will always be flipped.
        /// When transitioning from a flipped space to a non-flipped space - or vice-versa - the content must be flipped
        /// in the shader:
        /// shouldPerformYFlip = IsHandleYFlipped(source) != IsHandleYFlipped(target)
        /// </summary>
        /// <param name="handle">Handle to check the flipped status on.</param>
        /// <returns>True is the content is flipped in y.</returns>
        public bool IsHandleYFlipped(RTHandle handle)
        {
            return frameData.Get<UniversalCameraData>().IsHandleYFlipped(handle);
        }

        /// <summary>
        /// True if the camera device projection matrix is flipped. This happens when the pipeline is rendering
        /// to a render texture in non OpenGL platforms. If you are doing a custom Blit pass to copy camera textures
        /// (_CameraColorTexture, _CameraDepthAttachment) you need to check this flag to know if you should flip the
        /// matrix when rendering with for cmd.Draw* and reading from camera textures.
        /// </summary>
        /// <returns> True if the camera device projection matrix is flipped. </returns>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public bool IsCameraProjectionMatrixFlipped()
        {
            return frameData.Get<UniversalCameraData>().IsCameraProjectionMatrixFlipped();
        }

        /// <summary>
        /// True if the render target's projection matrix is flipped. This happens when the pipeline is rendering
        /// to a render texture in non OpenGL platforms. If you are doing a custom Blit pass to copy camera textures
        /// (_CameraColorTexture, _CameraDepthAttachment) you need to check this flag to know if you should flip the
        /// matrix when rendering with for cmd.Draw* and reading from camera textures.
        /// </summary>
        /// <param name="color">Color render target to check whether the matrix is flipped.</param>
        /// <param name="depth">Depth render target which is used if color is null. By default <c>depth</c> is set to null.</param>
        /// <returns> True if the render target's projection matrix is flipped. </returns>
        public bool IsRenderTargetProjectionMatrixFlipped(RTHandle color, RTHandle depth = null)
        {
            return frameData.Get<UniversalCameraData>().IsRenderTargetProjectionMatrixFlipped(color, depth);
        }

        internal bool IsTemporalAAEnabled()
        {
            return frameData.Get<UniversalCameraData>().IsTemporalAAEnabled();
        }

        /// <summary>
        /// The sorting criteria used when drawing opaque objects by the internal URP render passes.
        /// When a GPU supports hidden surface removal, URP will rely on that information to avoid sorting opaque objects front to back and
        /// benefit for more optimal static batching.
        /// </summary>
        /// <seealso cref="SortingCriteria"/>
        public ref SortingCriteria defaultOpaqueSortFlags => ref frameData.Get<UniversalCameraData>().defaultOpaqueSortFlags;

        /// <summary>
        /// XRPass holds the render target information and a list of XRView.
        /// XRView contains the parameters required to render (projection and view matrices, viewport, etc)
        /// </summary>
        public XRPass xr
        {
            get => frameData.Get<UniversalCameraData>().xr;
            internal set => frameData.Get<UniversalCameraData>().xr = value;
        }

        internal XRPassUniversal xrUniversal => frameData.Get<UniversalCameraData>().xrUniversal;

        /// <summary>
        /// Maximum shadow distance visible to the camera. When set to zero shadows will be disable for that camera.
        /// </summary>
        public ref float maxShadowDistance => ref frameData.Get<UniversalCameraData>().maxShadowDistance;

        /// <summary>
        /// True if post-processing is enabled for this camera.
        /// </summary>
        public ref bool postProcessEnabled => ref frameData.Get<UniversalCameraData>().postProcessEnabled;

        /// <summary>
        /// Provides set actions to the renderer to be triggered at the end of the render loop for camera capture.
        /// </summary>
        public ref IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> captureActions => ref frameData.Get<UniversalCameraData>().captureActions;

        /// <summary>
        /// The camera volume layer mask.
        /// </summary>
        public ref LayerMask volumeLayerMask => ref frameData.Get<UniversalCameraData>().volumeLayerMask;

        /// <summary>
        /// The camera volume trigger.
        /// </summary>
        public ref Transform volumeTrigger => ref frameData.Get<UniversalCameraData>().volumeTrigger;

        /// <summary>
        /// If set to true, the integrated post-processing stack will replace any NaNs generated by render passes prior to post-processing with black/zero.
        /// Enabling this option will cause a noticeable performance impact. It should be used while in development mode to identify NaN issues.
        /// </summary>
        public ref bool isStopNaNEnabled => ref frameData.Get<UniversalCameraData>().isStopNaNEnabled;

        /// <summary>
        /// If set to true a final post-processing pass will be applied to apply dithering.
        /// This can be combined with post-processing antialiasing.
        /// <seealso cref="antialiasing"/>
        /// </summary>
        public ref bool isDitheringEnabled => ref frameData.Get<UniversalCameraData>().isDitheringEnabled;

        /// <summary>
        /// Controls the anti-alising mode used by the integrated post-processing stack.
        /// When any other value other than <c>AntialiasingMode.None</c> is chosen, a final post-processing pass will be applied to apply anti-aliasing.
        /// This pass can be combined with dithering.
        /// <see cref="AntialiasingMode"/>
        /// <seealso cref="isDitheringEnabled"/>
        /// </summary>
        public ref AntialiasingMode antialiasing => ref frameData.Get<UniversalCameraData>().antialiasing;

        /// <summary>
        /// Controls the anti-alising quality of the anti-aliasing mode.
        /// <see cref="antialiasingQuality"/>
        /// <seealso cref="AntialiasingMode"/>
        /// </summary>
        public ref AntialiasingQuality antialiasingQuality => ref frameData.Get<UniversalCameraData>().antialiasingQuality;

        /// <summary>
        /// Returns the current renderer used by this camera.
        /// <see cref="ScriptableRenderer"/>
        /// </summary>
        public ref ScriptableRenderer renderer => ref frameData.Get<UniversalCameraData>().renderer;

        /// <summary>
        /// True if this camera is resolving rendering to the final camera render target.
        /// When rendering a stack of cameras only the last camera in the stack will resolve to camera target.
        /// </summary>
        public ref bool resolveFinalTarget => ref frameData.Get<UniversalCameraData>().resolveFinalTarget;

        /// <summary>
        /// Camera position in world space.
        /// </summary>
        public ref Vector3 worldSpaceCameraPos => ref frameData.Get<UniversalCameraData>().worldSpaceCameraPos;

        /// <summary>
        /// Final background color in the active color space.
        /// </summary>
        public ref Color backgroundColor => ref frameData.Get<UniversalCameraData>().backgroundColor;

        /// <summary>
        /// Persistent TAA data, primarily for the accumulation texture.
        /// </summary>
        internal ref TaaHistory taaHistory => ref frameData.Get<UniversalCameraData>().taaHistory;

        // TAA settings.
        internal ref TemporalAA.Settings taaSettings => ref frameData.Get<UniversalCameraData>().taaSettings;

        // Post-process history reset has been triggered for this camera.
        internal bool resetHistory => frameData.Get<UniversalCameraData>().resetHistory;

        /// <summary>
        /// Camera at the top of the overlay camera stack
        /// </summary>
        public ref Camera baseCamera => ref frameData.Get<UniversalCameraData>().baseCamera;
    }

    /// <summary>
    /// Container struct for various data used for shadows in URP.
    /// </summary>
    public struct ShadowData
    {
        ContextContainer frameData;

        internal ShadowData(ContextContainer frameData)
        {
            this.frameData = frameData;
        }

        internal UniversalShadowData universalShadowData => frameData.Get<UniversalShadowData>();

        /// <summary>
        /// True if main light shadows are enabled.
        /// </summary>
        public ref bool supportsMainLightShadows => ref frameData.Get<UniversalShadowData>().supportsMainLightShadows;

        /// <summary>
        /// True if additional lights shadows are enabled in the URP Asset
        /// </summary>
        internal ref bool mainLightShadowsEnabled => ref frameData.Get<UniversalShadowData>().mainLightShadowsEnabled;

        /// <summary>
        /// The width of the main light shadow map.
        /// </summary>
        public ref int mainLightShadowmapWidth => ref frameData.Get<UniversalShadowData>().mainLightShadowmapWidth;

        /// <summary>
        /// The height of the main light shadow map.
        /// </summary>
        public ref int mainLightShadowmapHeight => ref frameData.Get<UniversalShadowData>().mainLightShadowmapHeight;

        /// <summary>
        /// The number of shadow cascades.
        /// </summary>
        public ref int mainLightShadowCascadesCount => ref frameData.Get<UniversalShadowData>().mainLightShadowCascadesCount;

        /// <summary>
        /// The split between cascades.
        /// </summary>
        public ref Vector3 mainLightShadowCascadesSplit => ref frameData.Get<UniversalShadowData>().mainLightShadowCascadesSplit;

        /// <summary>
        /// Main light last cascade shadow fade border.
        /// Value represents the width of shadow fade that ranges from 0 to 1.
        /// Where value 0 is used for no shadow fade.
        /// </summary>
        public ref float mainLightShadowCascadeBorder => ref frameData.Get<UniversalShadowData>().mainLightShadowCascadeBorder;

        /// <summary>
        /// True if additional lights shadows are enabled.
        /// </summary>
        public ref bool supportsAdditionalLightShadows => ref frameData.Get<UniversalShadowData>().supportsAdditionalLightShadows;

        /// <summary>
        /// True if additional lights shadows are enabled in the URP Asset
        /// </summary>
        internal ref bool additionalLightShadowsEnabled => ref frameData.Get<UniversalShadowData>().additionalLightShadowsEnabled;

        /// <summary>
        /// The width of the additional light shadow map.
        /// </summary>
        public ref int additionalLightsShadowmapWidth => ref frameData.Get<UniversalShadowData>().additionalLightsShadowmapWidth;

        /// <summary>
        /// The height of the additional light shadow map.
        /// </summary>
        public ref int additionalLightsShadowmapHeight => ref frameData.Get<UniversalShadowData>().additionalLightsShadowmapHeight;

        /// <summary>
        /// True if soft shadows are enabled.
        /// </summary>
        public ref bool supportsSoftShadows => ref frameData.Get<UniversalShadowData>().supportsSoftShadows;

        /// <summary>
        /// The number of bits used.
        /// </summary>
        public ref int shadowmapDepthBufferBits => ref frameData.Get<UniversalShadowData>().shadowmapDepthBufferBits;

        /// <summary>
        /// A list of shadow bias.
        /// </summary>
        public ref List<Vector4> bias => ref frameData.Get<UniversalShadowData>().bias;

        /// <summary>
        /// A list of resolution for the shadow maps.
        /// </summary>
        public ref List<int> resolution => ref frameData.Get<UniversalShadowData>().resolution;

        internal ref bool isKeywordAdditionalLightShadowsEnabled => ref frameData.Get<UniversalShadowData>().isKeywordAdditionalLightShadowsEnabled;
        internal ref bool isKeywordSoftShadowsEnabled => ref frameData.Get<UniversalShadowData>().isKeywordSoftShadowsEnabled;
        internal ref int mainLightShadowResolution => ref frameData.Get<UniversalShadowData>().mainLightShadowResolution;
        internal ref int mainLightRenderTargetWidth => ref frameData.Get<UniversalShadowData>().mainLightRenderTargetWidth;
        internal ref int mainLightRenderTargetHeight => ref frameData.Get<UniversalShadowData>().mainLightRenderTargetHeight;

        internal ref NativeArray<URPLightShadowCullingInfos> visibleLightsShadowCullingInfos => ref frameData.Get<UniversalShadowData>().visibleLightsShadowCullingInfos;
        internal ref AdditionalLightsShadowAtlasLayout shadowAtlasLayout => ref frameData.Get<UniversalShadowData>().shadowAtlasLayout;
    }

    /// <summary>
    /// Precomputed tile data.
    /// Tile left, right, bottom and top plane equations in view space.
    /// Normals are pointing out.
    /// </summary>
    public struct PreTile
    {
        /// <summary>
        /// The left plane.
        /// </summary>
        public Unity.Mathematics.float4 planeLeft;

        /// <summary>
        /// The right plane.
        /// </summary>
        public Unity.Mathematics.float4 planeRight;

        /// <summary>
        /// The bottom plane.
        /// </summary>
        public Unity.Mathematics.float4 planeBottom;

        /// <summary>
        /// The top plane.
        /// </summary>
        public Unity.Mathematics.float4 planeTop;
    }

    /// <summary>
    /// The tile data passed to the deferred shaders.
    /// </summary>
    public struct TileData
    {
        /// <summary>
        /// The tile ID.
        /// </summary>
        public uint tileID;         // 2x 16 bits

        /// <summary>
        /// The list bit mask.
        /// </summary>
        public uint listBitMask;    // 32 bits

        /// <summary>
        /// The relative light offset.
        /// </summary>
        public uint relLightOffset; // 16 bits is enough

        /// <summary>
        /// Unused variable.
        /// </summary>
        public uint unused;
    }

    /// <summary>
    /// The point/spot light data passed to the deferred shaders.
    /// </summary>
    public struct PunctualLightData
    {
        /// <summary>
        /// The world position.
        /// </summary>
        public Vector3 wsPos;

        /// <summary>
        /// The radius of the light.
        /// </summary>
        public float radius; // TODO remove? included in attenuation

        /// <summary>
        /// The color of the light.
        /// </summary>
        public Vector4 color;

        /// <summary>
        /// The attenuation of the light.
        /// </summary>
        public Vector4 attenuation; // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation (for SpotLights)

        /// <summary>
        /// The direction for spot lights.
        /// </summary>
        public Vector3 spotDirection;   // for spotLights

        /// <summary>
        /// The flags used.
        /// </summary>
        public int flags;

        /// <summary>
        /// The occlusion probe info.
        /// </summary>
        public Vector4 occlusionProbeInfo;

        /// <summary>
        /// The layer mask used.
        /// </summary>
        public uint layerMask;
    }

    internal static class ShaderPropertyId
    {
        public static readonly int glossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
        public static readonly int subtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

        public static readonly int glossyEnvironmentCubeMap = Shader.PropertyToID("_GlossyEnvironmentCubeMap");
        public static readonly int glossyEnvironmentCubeMapHDR = Shader.PropertyToID("_GlossyEnvironmentCubeMap_HDR");

        public static readonly int ambientSkyColor = Shader.PropertyToID("unity_AmbientSky");
        public static readonly int ambientEquatorColor = Shader.PropertyToID("unity_AmbientEquator");
        public static readonly int ambientGroundColor = Shader.PropertyToID("unity_AmbientGround");

        public static readonly int time = Shader.PropertyToID("_Time");
        public static readonly int sinTime = Shader.PropertyToID("_SinTime");
        public static readonly int cosTime = Shader.PropertyToID("_CosTime");
        public static readonly int deltaTime = Shader.PropertyToID("unity_DeltaTime");
        public static readonly int timeParameters = Shader.PropertyToID("_TimeParameters");
        public static readonly int lastTimeParameters = Shader.PropertyToID("_LastTimeParameters");

        public static readonly int scaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
        public static readonly int worldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
        public static readonly int screenParams = Shader.PropertyToID("_ScreenParams");
        public static readonly int alphaToMaskAvailable = Shader.PropertyToID("_AlphaToMaskAvailable");
        public static readonly int projectionParams = Shader.PropertyToID("_ProjectionParams");
        public static readonly int zBufferParams = Shader.PropertyToID("_ZBufferParams");
        public static readonly int orthoParams = Shader.PropertyToID("unity_OrthoParams");
        public static readonly int globalMipBias = Shader.PropertyToID("_GlobalMipBias");

        public static readonly int screenSize = Shader.PropertyToID("_ScreenSize");
        public static readonly int screenCoordScaleBias = Shader.PropertyToID("_ScreenCoordScaleBias");
        public static readonly int screenSizeOverride = Shader.PropertyToID("_ScreenSizeOverride");

        public static readonly int viewMatrix = Shader.PropertyToID("unity_MatrixV");
        public static readonly int projectionMatrix = Shader.PropertyToID("glstate_matrix_projection");
        public static readonly int viewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixVP");

        public static readonly int inverseViewMatrix = Shader.PropertyToID("unity_MatrixInvV");
        public static readonly int inverseProjectionMatrix = Shader.PropertyToID("unity_MatrixInvP");
        public static readonly int inverseViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixInvVP");

        public static readonly int cameraProjectionMatrix = Shader.PropertyToID("unity_CameraProjection");
        public static readonly int inverseCameraProjectionMatrix = Shader.PropertyToID("unity_CameraInvProjection");
        public static readonly int worldToCameraMatrix = Shader.PropertyToID("unity_WorldToCamera");
        public static readonly int cameraToWorldMatrix = Shader.PropertyToID("unity_CameraToWorld");

        public static readonly int shadowBias = Shader.PropertyToID("_ShadowBias");
        public static readonly int lightDirection = Shader.PropertyToID("_LightDirection");
        public static readonly int lightPosition = Shader.PropertyToID("_LightPosition");

        public static readonly int cameraWorldClipPlanes = Shader.PropertyToID("unity_CameraWorldClipPlanes");

        public static readonly int billboardNormal = Shader.PropertyToID("unity_BillboardNormal");
        public static readonly int billboardTangent = Shader.PropertyToID("unity_BillboardTangent");
        public static readonly int billboardCameraParams = Shader.PropertyToID("unity_BillboardCameraParams");

        public static readonly int previousViewProjectionNoJitter = Shader.PropertyToID("_PrevViewProjMatrix");
        public static readonly int viewProjectionNoJitter = Shader.PropertyToID("_NonJitteredViewProjMatrix");
#if ENABLE_VR && ENABLE_XR_MODULE
        public static readonly int previousViewProjectionNoJitterStereo = Shader.PropertyToID("_PrevViewProjMatrixStereo");
        public static readonly int viewProjectionNoJitterStereo = Shader.PropertyToID("_NonJitteredViewProjMatrixStereo");
#endif

        public static readonly int blitTexture = Shader.PropertyToID("_BlitTexture");
        public static readonly int blitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        public static readonly int sourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");
        public static readonly int scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");

        // This uniform is specific to the RTHandle system
        public static readonly int rtHandleScale = Shader.PropertyToID("_RTHandleScale");

        // Required for 2D Unlit Shadergraph master node as it doesn't currently support hidden properties.
        public static readonly int rendererColor = Shader.PropertyToID("_RendererColor");

        public static readonly int ditheringTexture = Shader.PropertyToID("_DitheringTexture");
        public static readonly int ditheringTextureInvSize = Shader.PropertyToID("_DitheringTextureInvSize");

        public static readonly int renderingLayerMaxInt = Shader.PropertyToID("_RenderingLayerMaxInt");
        public static readonly int renderingLayerRcpMaxInt = Shader.PropertyToID("_RenderingLayerRcpMaxInt");

        public static readonly int overlayUITexture = Shader.PropertyToID("_OverlayUITexture");
        public static readonly int hdrOutputLuminanceParams = Shader.PropertyToID("_HDROutputLuminanceParams");
        public static readonly int hdrOutputGradingParams = Shader.PropertyToID("_HDROutputGradingParams");
    }

    /// <summary>
    /// Settings used for Post Processing.
    /// </summary>
    public struct PostProcessingData
    {
        ContextContainer frameData;

        internal PostProcessingData(ContextContainer frameData)
        {
            this.frameData = frameData;
        }

        internal UniversalPostProcessingData universalPostProcessingData => frameData.Get<UniversalPostProcessingData>();

        /// <summary>
        /// The <c>ColorGradingMode</c> to use.
        /// </summary>
        /// <seealso cref="ColorGradingMode"/>
        public ref ColorGradingMode gradingMode => ref frameData.Get<UniversalPostProcessingData>().gradingMode;

        /// <summary>
        /// The size of the Look Up Table (LUT)
        /// </summary>
        public ref int lutSize => ref frameData.Get<UniversalPostProcessingData>().lutSize;

        /// <summary>
        /// True if fast approximation functions are used when converting between the sRGB and Linear color spaces, false otherwise.
        /// </summary>
        public ref bool useFastSRGBLinearConversion => ref frameData.Get<UniversalPostProcessingData>().useFastSRGBLinearConversion;

        /// <summary>
        /// Returns true if Screen Space Lens Flare are supported by this asset, false otherwise.
        /// </summary>
        public ref bool supportScreenSpaceLensFlare => ref frameData.Get<UniversalPostProcessingData>().supportScreenSpaceLensFlare;

        /// <summary>
        /// Returns true if Data Driven Lens Flare are supported by this asset, false otherwise.
        /// </summary>
        public ref bool supportDataDrivenLensFlare => ref frameData.Get<UniversalPostProcessingData>().supportDataDrivenLensFlare;
    }

    internal static class ShaderGlobalKeywords
    {
        public static GlobalKeyword MainLightShadows;
        public static GlobalKeyword MainLightShadowCascades;
        public static GlobalKeyword MainLightShadowScreen;
        public static GlobalKeyword CastingPunctualLightShadow;
        public static GlobalKeyword AdditionalLightsVertex;
        public static GlobalKeyword AdditionalLightsPixel;
        public static GlobalKeyword ForwardPlus;
        public static GlobalKeyword AdditionalLightShadows;
        public static GlobalKeyword ReflectionProbeBoxProjection;
        public static GlobalKeyword ReflectionProbeBlending;
        public static GlobalKeyword SoftShadows;
        public static GlobalKeyword SoftShadowsLow;
        public static GlobalKeyword SoftShadowsMedium;
        public static GlobalKeyword SoftShadowsHigh;
        public static GlobalKeyword MixedLightingSubtractive; // Backward compatibility
        public static GlobalKeyword LightmapShadowMixing;
        public static GlobalKeyword ShadowsShadowMask;
        public static GlobalKeyword LightLayers;
        public static GlobalKeyword RenderPassEnabled;
        public static GlobalKeyword BillboardFaceCameraPos;
        public static GlobalKeyword LightCookies;
        public static GlobalKeyword DepthNoMsaa;
        public static GlobalKeyword DepthMsaa2;
        public static GlobalKeyword DepthMsaa4;
        public static GlobalKeyword DepthMsaa8;
        public static GlobalKeyword DBufferMRT1;
        public static GlobalKeyword DBufferMRT2;
        public static GlobalKeyword DBufferMRT3;
        public static GlobalKeyword DecalNormalBlendLow;
        public static GlobalKeyword DecalNormalBlendMedium;
        public static GlobalKeyword DecalNormalBlendHigh;
        public static GlobalKeyword DecalLayers;
        public static GlobalKeyword WriteRenderingLayers;
        public static GlobalKeyword ScreenSpaceOcclusion;
        public static GlobalKeyword _SPOT;
        public static GlobalKeyword _DIRECTIONAL;
        public static GlobalKeyword _POINT;
        public static GlobalKeyword _DEFERRED_STENCIL;
        public static GlobalKeyword _DEFERRED_FIRST_LIGHT;
        public static GlobalKeyword _DEFERRED_MAIN_LIGHT;
        public static GlobalKeyword _GBUFFER_NORMALS_OCT;
        public static GlobalKeyword _DEFERRED_MIXED_LIGHTING;
        public static GlobalKeyword LIGHTMAP_ON;
        public static GlobalKeyword DYNAMICLIGHTMAP_ON;
        public static GlobalKeyword _ALPHATEST_ON;
        public static GlobalKeyword DIRLIGHTMAP_COMBINED;
        public static GlobalKeyword _DETAIL_MULX2;
        public static GlobalKeyword _DETAIL_SCALED;
        public static GlobalKeyword _CLEARCOAT;
        public static GlobalKeyword _CLEARCOATMAP;
        public static GlobalKeyword DEBUG_DISPLAY;
        public static GlobalKeyword LOD_FADE_CROSSFADE;
        public static GlobalKeyword USE_UNITY_CROSSFADE;
        public static GlobalKeyword _EMISSION;
        public static GlobalKeyword _RECEIVE_SHADOWS_OFF;
        public static GlobalKeyword _SURFACE_TYPE_TRANSPARENT;
        public static GlobalKeyword _ALPHAPREMULTIPLY_ON;
        public static GlobalKeyword _ALPHAMODULATE_ON;
        public static GlobalKeyword _NORMALMAP;
        public static GlobalKeyword _ADD_PRECOMPUTED_VELOCITY;
        public static GlobalKeyword EDITOR_VISUALIZATION;
        public static GlobalKeyword FoveatedRenderingNonUniformRaster;
        public static GlobalKeyword DisableTexture2DXArray;
        public static GlobalKeyword BlitSingleSlice;
        public static GlobalKeyword XROcclusionMeshCombined;
        public static GlobalKeyword SCREEN_COORD_OVERRIDE;
        public static GlobalKeyword DOWNSAMPLING_SIZE_2;
        public static GlobalKeyword DOWNSAMPLING_SIZE_4;
        public static GlobalKeyword DOWNSAMPLING_SIZE_8;
        public static GlobalKeyword DOWNSAMPLING_SIZE_16;
        public static GlobalKeyword EVALUATE_SH_MIXED;
        public static GlobalKeyword EVALUATE_SH_VERTEX;
        public static GlobalKeyword ProbeVolumeL1;
        public static GlobalKeyword ProbeVolumeL2;
        public static GlobalKeyword _OUTPUT_DEPTH;
        public static GlobalKeyword LinearToSRGBConversion;
        public static GlobalKeyword _ENABLE_ALPHA_OUTPUT;

        // TODO: Move following keywords to Local keywords?
        // https://docs.unity3d.com/ScriptReference/Rendering.LocalKeyword.html
        //public static GlobalKeyword TonemapACES;
        //public static GlobalKeyword TonemapNeutral;
        //public static GlobalKeyword UseFastSRGBLinearConversion;
        //public static GlobalKeyword SmaaLow;
        //public static GlobalKeyword SmaaMedium;
        //public static GlobalKeyword SmaaHigh;
        //public static GlobalKeyword PaniniGeneric;
        //public static GlobalKeyword PaniniUnitDistance;
        //public static GlobalKeyword HighQualitySampling;
        //public static GlobalKeyword BloomLQ;
        //public static GlobalKeyword BloomHQ;
        //public static GlobalKeyword BloomLQDirt;
        //public static GlobalKeyword BloomHQDirt;
        //public static GlobalKeyword UseRGBM;
        //public static GlobalKeyword Distortion;
        //public static GlobalKeyword ChromaticAberration;
        //public static GlobalKeyword HDRGrading;
        //public static GlobalKeyword FilmGrain;
        //public static GlobalKeyword Fxaa;
        //public static GlobalKeyword Dithering;
        //public static GlobalKeyword Rcas;
        //public static GlobalKeyword EasuRcasAndHDRInput;
        //public static GlobalKeyword Gamma20;
        //public static GlobalKeyword Gamma20AndHDRInput;
        //public static GlobalKeyword PointSampling;

        public static void InitializeShaderGlobalKeywords()
        {
            // Init all keywords upfront
            ShaderGlobalKeywords.MainLightShadows = GlobalKeyword.Create(ShaderKeywordStrings.MainLightShadows);
            ShaderGlobalKeywords.MainLightShadowCascades = GlobalKeyword.Create(ShaderKeywordStrings.MainLightShadowCascades);
            ShaderGlobalKeywords.MainLightShadowScreen = GlobalKeyword.Create(ShaderKeywordStrings.MainLightShadowScreen);
            ShaderGlobalKeywords.CastingPunctualLightShadow = GlobalKeyword.Create(ShaderKeywordStrings.CastingPunctualLightShadow);
            ShaderGlobalKeywords.AdditionalLightsVertex = GlobalKeyword.Create(ShaderKeywordStrings.AdditionalLightsVertex);
            ShaderGlobalKeywords.AdditionalLightsPixel = GlobalKeyword.Create(ShaderKeywordStrings.AdditionalLightsPixel);
            ShaderGlobalKeywords.ForwardPlus = GlobalKeyword.Create(ShaderKeywordStrings.ForwardPlus);
            ShaderGlobalKeywords.AdditionalLightShadows = GlobalKeyword.Create(ShaderKeywordStrings.AdditionalLightShadows);
            ShaderGlobalKeywords.ReflectionProbeBoxProjection = GlobalKeyword.Create(ShaderKeywordStrings.ReflectionProbeBoxProjection);
            ShaderGlobalKeywords.ReflectionProbeBlending = GlobalKeyword.Create(ShaderKeywordStrings.ReflectionProbeBlending);
            ShaderGlobalKeywords.SoftShadows = GlobalKeyword.Create(ShaderKeywordStrings.SoftShadows);
            ShaderGlobalKeywords.SoftShadowsLow = GlobalKeyword.Create(ShaderKeywordStrings.SoftShadowsLow);
            ShaderGlobalKeywords.SoftShadowsMedium = GlobalKeyword.Create(ShaderKeywordStrings.SoftShadowsMedium);
            ShaderGlobalKeywords.SoftShadowsHigh = GlobalKeyword.Create(ShaderKeywordStrings.SoftShadowsHigh);
            ShaderGlobalKeywords.MixedLightingSubtractive = GlobalKeyword.Create(ShaderKeywordStrings.MixedLightingSubtractive);
            ShaderGlobalKeywords.LightmapShadowMixing = GlobalKeyword.Create(ShaderKeywordStrings.LightmapShadowMixing);
            ShaderGlobalKeywords.ShadowsShadowMask = GlobalKeyword.Create(ShaderKeywordStrings.ShadowsShadowMask);
            ShaderGlobalKeywords.LightLayers = GlobalKeyword.Create(ShaderKeywordStrings.LightLayers);
            ShaderGlobalKeywords.RenderPassEnabled = GlobalKeyword.Create(ShaderKeywordStrings.RenderPassEnabled);
            ShaderGlobalKeywords.BillboardFaceCameraPos = GlobalKeyword.Create(ShaderKeywordStrings.BillboardFaceCameraPos);
            ShaderGlobalKeywords.LightCookies = GlobalKeyword.Create(ShaderKeywordStrings.LightCookies);
            ShaderGlobalKeywords.DepthNoMsaa = GlobalKeyword.Create(ShaderKeywordStrings.DepthNoMsaa);
            ShaderGlobalKeywords.DepthMsaa2 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa2);
            ShaderGlobalKeywords.DepthMsaa4 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa4);
            ShaderGlobalKeywords.DepthMsaa8 = GlobalKeyword.Create(ShaderKeywordStrings.DepthMsaa8);
            ShaderGlobalKeywords.DBufferMRT1 = GlobalKeyword.Create(ShaderKeywordStrings.DBufferMRT1);
            ShaderGlobalKeywords.DBufferMRT2 = GlobalKeyword.Create(ShaderKeywordStrings.DBufferMRT2);
            ShaderGlobalKeywords.DBufferMRT3 = GlobalKeyword.Create(ShaderKeywordStrings.DBufferMRT3);
            ShaderGlobalKeywords.DecalNormalBlendLow = GlobalKeyword.Create(ShaderKeywordStrings.DecalNormalBlendLow);
            ShaderGlobalKeywords.DecalNormalBlendMedium = GlobalKeyword.Create(ShaderKeywordStrings.DecalNormalBlendMedium);
            ShaderGlobalKeywords.DecalNormalBlendHigh = GlobalKeyword.Create(ShaderKeywordStrings.DecalNormalBlendHigh);
            ShaderGlobalKeywords.DecalLayers = GlobalKeyword.Create(ShaderKeywordStrings.DecalLayers);
            ShaderGlobalKeywords.WriteRenderingLayers = GlobalKeyword.Create(ShaderKeywordStrings.WriteRenderingLayers);
            ShaderGlobalKeywords.ScreenSpaceOcclusion = GlobalKeyword.Create(ShaderKeywordStrings.ScreenSpaceOcclusion);
            ShaderGlobalKeywords._SPOT = GlobalKeyword.Create(ShaderKeywordStrings._SPOT);
            ShaderGlobalKeywords._DIRECTIONAL = GlobalKeyword.Create(ShaderKeywordStrings._DIRECTIONAL);
            ShaderGlobalKeywords._POINT = GlobalKeyword.Create(ShaderKeywordStrings._POINT);
            ShaderGlobalKeywords._DEFERRED_STENCIL = GlobalKeyword.Create(ShaderKeywordStrings._DEFERRED_STENCIL);
            ShaderGlobalKeywords._DEFERRED_FIRST_LIGHT = GlobalKeyword.Create(ShaderKeywordStrings._DEFERRED_FIRST_LIGHT);
            ShaderGlobalKeywords._DEFERRED_MAIN_LIGHT = GlobalKeyword.Create(ShaderKeywordStrings._DEFERRED_MAIN_LIGHT);
            ShaderGlobalKeywords._GBUFFER_NORMALS_OCT = GlobalKeyword.Create(ShaderKeywordStrings._GBUFFER_NORMALS_OCT);
            ShaderGlobalKeywords._DEFERRED_MIXED_LIGHTING = GlobalKeyword.Create(ShaderKeywordStrings._DEFERRED_MIXED_LIGHTING);
            ShaderGlobalKeywords.LIGHTMAP_ON = GlobalKeyword.Create(ShaderKeywordStrings.LIGHTMAP_ON);
            ShaderGlobalKeywords.DYNAMICLIGHTMAP_ON = GlobalKeyword.Create(ShaderKeywordStrings.DYNAMICLIGHTMAP_ON);
            ShaderGlobalKeywords._ALPHATEST_ON = GlobalKeyword.Create(ShaderKeywordStrings._ALPHATEST_ON);
            ShaderGlobalKeywords.DIRLIGHTMAP_COMBINED = GlobalKeyword.Create(ShaderKeywordStrings.DIRLIGHTMAP_COMBINED);
            ShaderGlobalKeywords._DETAIL_MULX2 = GlobalKeyword.Create(ShaderKeywordStrings._DETAIL_MULX2);
            ShaderGlobalKeywords._DETAIL_SCALED = GlobalKeyword.Create(ShaderKeywordStrings._DETAIL_SCALED);
            ShaderGlobalKeywords._CLEARCOAT = GlobalKeyword.Create(ShaderKeywordStrings._CLEARCOAT);
            ShaderGlobalKeywords._CLEARCOATMAP = GlobalKeyword.Create(ShaderKeywordStrings._CLEARCOATMAP);
            ShaderGlobalKeywords.DEBUG_DISPLAY = GlobalKeyword.Create(ShaderKeywordStrings.DEBUG_DISPLAY);
            ShaderGlobalKeywords.LOD_FADE_CROSSFADE = GlobalKeyword.Create(ShaderKeywordStrings.LOD_FADE_CROSSFADE);
            ShaderGlobalKeywords.USE_UNITY_CROSSFADE = GlobalKeyword.Create(ShaderKeywordStrings.USE_UNITY_CROSSFADE);
            ShaderGlobalKeywords._EMISSION = GlobalKeyword.Create(ShaderKeywordStrings._EMISSION);
            ShaderGlobalKeywords._RECEIVE_SHADOWS_OFF = GlobalKeyword.Create(ShaderKeywordStrings._RECEIVE_SHADOWS_OFF);
            ShaderGlobalKeywords._SURFACE_TYPE_TRANSPARENT = GlobalKeyword.Create(ShaderKeywordStrings._SURFACE_TYPE_TRANSPARENT);
            ShaderGlobalKeywords._ALPHAPREMULTIPLY_ON = GlobalKeyword.Create(ShaderKeywordStrings._ALPHAPREMULTIPLY_ON);
            ShaderGlobalKeywords._ALPHAMODULATE_ON = GlobalKeyword.Create(ShaderKeywordStrings._ALPHAMODULATE_ON);
            ShaderGlobalKeywords._NORMALMAP = GlobalKeyword.Create(ShaderKeywordStrings._NORMALMAP);
            ShaderGlobalKeywords._ADD_PRECOMPUTED_VELOCITY = GlobalKeyword.Create(ShaderKeywordStrings._ADD_PRECOMPUTED_VELOCITY);
            ShaderGlobalKeywords.EDITOR_VISUALIZATION = GlobalKeyword.Create(ShaderKeywordStrings.EDITOR_VISUALIZATION);
            ShaderGlobalKeywords.FoveatedRenderingNonUniformRaster = GlobalKeyword.Create(ShaderKeywordStrings.FoveatedRenderingNonUniformRaster);
            ShaderGlobalKeywords.DisableTexture2DXArray = GlobalKeyword.Create(ShaderKeywordStrings.DisableTexture2DXArray);
            ShaderGlobalKeywords.BlitSingleSlice = GlobalKeyword.Create(ShaderKeywordStrings.BlitSingleSlice);
            ShaderGlobalKeywords.XROcclusionMeshCombined = GlobalKeyword.Create(ShaderKeywordStrings.XROcclusionMeshCombined);
            ShaderGlobalKeywords.SCREEN_COORD_OVERRIDE = GlobalKeyword.Create(ShaderKeywordStrings.SCREEN_COORD_OVERRIDE);
            ShaderGlobalKeywords.DOWNSAMPLING_SIZE_2 = GlobalKeyword.Create(ShaderKeywordStrings.DOWNSAMPLING_SIZE_2);
            ShaderGlobalKeywords.DOWNSAMPLING_SIZE_4 = GlobalKeyword.Create(ShaderKeywordStrings.DOWNSAMPLING_SIZE_4);
            ShaderGlobalKeywords.DOWNSAMPLING_SIZE_8 = GlobalKeyword.Create(ShaderKeywordStrings.DOWNSAMPLING_SIZE_8);
            ShaderGlobalKeywords.DOWNSAMPLING_SIZE_16 = GlobalKeyword.Create(ShaderKeywordStrings.DOWNSAMPLING_SIZE_16);
            ShaderGlobalKeywords.EVALUATE_SH_MIXED = GlobalKeyword.Create(ShaderKeywordStrings.EVALUATE_SH_MIXED);
            ShaderGlobalKeywords.EVALUATE_SH_VERTEX = GlobalKeyword.Create(ShaderKeywordStrings.EVALUATE_SH_VERTEX);
            ShaderGlobalKeywords.ProbeVolumeL1 = GlobalKeyword.Create(ShaderKeywordStrings.ProbeVolumeL1);
            ShaderGlobalKeywords.ProbeVolumeL2 = GlobalKeyword.Create(ShaderKeywordStrings.ProbeVolumeL2);
            ShaderGlobalKeywords._OUTPUT_DEPTH = GlobalKeyword.Create(ShaderKeywordStrings._OUTPUT_DEPTH);
            ShaderGlobalKeywords.LinearToSRGBConversion = GlobalKeyword.Create(ShaderKeywordStrings.LinearToSRGBConversion);
            ShaderGlobalKeywords._ENABLE_ALPHA_OUTPUT = GlobalKeyword.Create(ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT);
        }
    }

    /// <summary>
    /// Container class for keywords used in URP shaders.
    /// </summary>
    public static class ShaderKeywordStrings
    {
        /// <summary> Keyword used for shadows without cascades. </summary>
        public const string MainLightShadows = "_MAIN_LIGHT_SHADOWS";

        /// <summary> Keyword used for shadows with cascades. </summary>
        public const string MainLightShadowCascades = "_MAIN_LIGHT_SHADOWS_CASCADE";

        /// <summary> Keyword used for screen space shadows. </summary>
        public const string MainLightShadowScreen = "_MAIN_LIGHT_SHADOWS_SCREEN";

        /// <summary> Keyword used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias. </summary>
        public const string CastingPunctualLightShadow = "_CASTING_PUNCTUAL_LIGHT_SHADOW";

        /// <summary> Keyword used for per vertex additional lights. </summary>
        public const string AdditionalLightsVertex = "_ADDITIONAL_LIGHTS_VERTEX";

        /// <summary> Keyword used for per pixel additional lights. </summary>
        public const string AdditionalLightsPixel = "_ADDITIONAL_LIGHTS";

        /// <summary> Keyword used for Forward+. </summary>
        internal const string ForwardPlus = "_FORWARD_PLUS";

        /// <summary> Keyword used for shadows on additional lights. </summary>
        public const string AdditionalLightShadows = "_ADDITIONAL_LIGHT_SHADOWS";

        /// <summary> Keyword used for Box Projection with Reflection Probes. </summary>
        public const string ReflectionProbeBoxProjection = "_REFLECTION_PROBE_BOX_PROJECTION";

        /// <summary> Keyword used for Reflection probe blending. </summary>
        public const string ReflectionProbeBlending = "_REFLECTION_PROBE_BLENDING";

        /// <summary> Keyword used for soft shadows. </summary>
        public const string SoftShadows = "_SHADOWS_SOFT";

        /// <summary> Keyword used for low quality soft shadows. </summary>
        public const string SoftShadowsLow = "_SHADOWS_SOFT_LOW";

        /// <summary> Keyword used for medium quality soft shadows. </summary>
        public const string SoftShadowsMedium = "_SHADOWS_SOFT_MEDIUM";

        /// <summary> Keyword used for high quality soft shadows. </summary>
        public const string SoftShadowsHigh = "_SHADOWS_SOFT_HIGH";

        /// <summary> Keyword used for Mixed Lights in Subtractive lighting mode. </summary>
        public const string MixedLightingSubtractive = "_MIXED_LIGHTING_SUBTRACTIVE"; // Backward compatibility

        /// <summary> Keyword used for mixing lightmap shadows. </summary>
        public const string LightmapShadowMixing = "LIGHTMAP_SHADOW_MIXING";

        /// <summary> Keyword used for Shadowmask. </summary>
        public const string ShadowsShadowMask = "SHADOWS_SHADOWMASK";

        /// <summary> Keyword used for Light Layers. </summary>
        public const string LightLayers = "_LIGHT_LAYERS";

        /// <summary> Keyword used for RenderPass. </summary>
        public const string RenderPassEnabled = "_RENDER_PASS_ENABLED";

        /// <summary> Keyword used for Billboard cameras. </summary>
        public const string BillboardFaceCameraPos = "BILLBOARD_FACE_CAMERA_POS";

        /// <summary> Keyword used for Light Cookies. </summary>
        public const string LightCookies = "_LIGHT_COOKIES";

        /// <summary> Keyword used for no Multi Sampling Anti-Aliasing (MSAA). </summary>
        public const string DepthNoMsaa = "_DEPTH_NO_MSAA";

        /// <summary> Keyword used for Multi Sampling Anti-Aliasing (MSAA) with 2 per pixel sample count. </summary>
        public const string DepthMsaa2 = "_DEPTH_MSAA_2";

        /// <summary> Keyword used for Multi Sampling Anti-Aliasing (MSAA) with 4 per pixel sample count. </summary>
        public const string DepthMsaa4 = "_DEPTH_MSAA_4";

        /// <summary> Keyword used for Multi Sampling Anti-Aliasing (MSAA) with 8 per pixel sample count. </summary>
        public const string DepthMsaa8 = "_DEPTH_MSAA_8";

        /// <summary> Keyword used for Linear to SRGB conversions. </summary>
        public const string LinearToSRGBConversion = "_LINEAR_TO_SRGB_CONVERSION";

        /// <summary> Keyword used for less expensive Linear to SRGB conversions. </summary>
        internal const string UseFastSRGBLinearConversion = "_USE_FAST_SRGB_LINEAR_CONVERSION";

        /// <summary> Keyword used for first target in the DBuffer. </summary>
        public const string DBufferMRT1 = "_DBUFFER_MRT1";

        /// <summary> Keyword used for second target in the DBuffer. </summary>
        public const string DBufferMRT2 = "_DBUFFER_MRT2";

        /// <summary> Keyword used for third target in the DBuffer. </summary>
        public const string DBufferMRT3 = "_DBUFFER_MRT3";

        /// <summary> Keyword used for low quality normal reconstruction in Decals. </summary>
        public const string DecalNormalBlendLow = "_DECAL_NORMAL_BLEND_LOW";

        /// <summary> Keyword used for medium quality normal reconstruction in Decals. </summary>
        public const string DecalNormalBlendMedium = "_DECAL_NORMAL_BLEND_MEDIUM";

        /// <summary> Keyword used for high quality normal reconstruction in Decals. </summary>
        public const string DecalNormalBlendHigh = "_DECAL_NORMAL_BLEND_HIGH";

        /// <summary> Keyword used for Decal Layers. </summary>
        public const string DecalLayers = "_DECAL_LAYERS";

        /// <summary> Keyword used for writing Rendering Layers. </summary>
        public const string WriteRenderingLayers = "_WRITE_RENDERING_LAYERS";

        /// <summary> Keyword used for low quality Subpixel Morphological Anti-aliasing (SMAA). </summary>
        public const string SmaaLow = "_SMAA_PRESET_LOW";

        /// <summary> Keyword used for medium quality Subpixel Morphological Anti-aliasing (SMAA). </summary>
        public const string SmaaMedium = "_SMAA_PRESET_MEDIUM";

        /// <summary> Keyword used for high quality Subpixel Morphological Anti-aliasing (SMAA). </summary>
        public const string SmaaHigh = "_SMAA_PRESET_HIGH";

        /// <summary> Keyword used for generic Panini Projection. </summary>
        public const string PaniniGeneric = "_GENERIC";

        /// <summary> Keyword used for unit distance Panini Projection. </summary>
        public const string PaniniUnitDistance = "_UNIT_DISTANCE";

        /// <summary> Keyword used for low quality Bloom. </summary>
        public const string BloomLQ = "_BLOOM_LQ";

        /// <summary> Keyword used for high quality Bloom. </summary>
        public const string BloomHQ = "_BLOOM_HQ";

        /// <summary> Keyword used for low quality Bloom dirt. </summary>
        public const string BloomLQDirt = "_BLOOM_LQ_DIRT";

        /// <summary> Keyword used for high quality Bloom dirt. </summary>
        public const string BloomHQDirt = "_BLOOM_HQ_DIRT";

        /// <summary> Keyword used for Distortion. </summary>
        public const string Distortion = "_DISTORTION";

        /// <summary> Keyword used for Chromatic Aberration. </summary>
        public const string ChromaticAberration = "_CHROMATIC_ABERRATION";

        /// <summary> Keyword used for HDR Color Grading. </summary>
        public const string HDRGrading = "_HDR_GRADING";

        /// <summary> Keyword used for HDR UI Overlay compositing. </summary>
        public const string HDROverlay = "_HDR_OVERLAY";

        /// <summary> Keyword used for ACES Tonemapping. </summary>
        public const string TonemapACES = "_TONEMAP_ACES";

        /// <summary> Keyword used for Neutral Tonemapping. </summary>
        public const string TonemapNeutral = "_TONEMAP_NEUTRAL";

        /// <summary> Keyword used for Film Grain. </summary>
        public const string FilmGrain = "_FILM_GRAIN";

        /// <summary> Keyword used for Fast Approximate Anti-aliasing (FXAA). </summary>
        public const string Fxaa = "_FXAA";

        /// <summary> Keyword used for Dithering. </summary>
        public const string Dithering = "_DITHERING";

        /// <summary> Keyword used for Screen Space Occlusion, such as Screen Space Ambient Occlusion (SSAO). </summary>
        public const string ScreenSpaceOcclusion = "_SCREEN_SPACE_OCCLUSION";

        /// <summary> Keyword used for Point sampling when doing upsampling. </summary>
        public const string PointSampling = "_POINT_SAMPLING";

        /// <summary> Keyword used for Robust Contrast-Adaptive Sharpening (RCAS) when doing upsampling. </summary>
        public const string Rcas = "_RCAS";

        /// <summary> Keyword used for Robust Contrast-Adaptive Sharpening (RCAS) when doing upsampling, after EASU has ran and with HDR Dsiplay output. </summary>
        public const string EasuRcasAndHDRInput = "_EASU_RCAS_AND_HDR_INPUT";

        /// <summary> Keyword used for Gamma 2.0. </summary>
        public const string Gamma20 = "_GAMMA_20";

        /// <summary> Keyword used for Gamma 2.0 with HDR_INPUT. </summary>
        public const string Gamma20AndHDRInput = "_GAMMA_20_AND_HDR_INPUT";

        /// <summary> Keyword used for high quality sampling for Depth Of Field. </summary>
        public const string HighQualitySampling = "_HIGH_QUALITY_SAMPLING";

        /// <summary> Keyword used for Spot lights. </summary>
        public const string _SPOT = "_SPOT";

        /// <summary> Keyword used for Directional lights. </summary>
        public const string _DIRECTIONAL = "_DIRECTIONAL";

        /// <summary> Keyword used for Point lights. </summary>
        public const string _POINT = "_POINT";

        /// <summary> Keyword used for stencils when rendering with the Deferred rendering path. </summary>
        public const string _DEFERRED_STENCIL = "_DEFERRED_STENCIL";

        /// <summary> Keyword used for the first light when rendering with the Deferred rendering path. </summary>
        public const string _DEFERRED_FIRST_LIGHT = "_DEFERRED_FIRST_LIGHT";

        /// <summary> Keyword used for the main light when rendering with the Deferred rendering path. </summary>
        public const string _DEFERRED_MAIN_LIGHT = "_DEFERRED_MAIN_LIGHT";

        /// <summary> Keyword used for Accurate G-buffer normals when rendering with the Deferred rendering path. </summary>
        public const string _GBUFFER_NORMALS_OCT = "_GBUFFER_NORMALS_OCT";

        /// <summary> Keyword used for Mixed Lighting when rendering with the Deferred rendering path. </summary>
        public const string _DEFERRED_MIXED_LIGHTING = "_DEFERRED_MIXED_LIGHTING";

        /// <summary> Keyword used for Lightmaps. </summary>
        public const string LIGHTMAP_ON = "LIGHTMAP_ON";

        /// <summary> Keyword used for dynamic Lightmaps. </summary>
        public const string DYNAMICLIGHTMAP_ON = "DYNAMICLIGHTMAP_ON";

        /// <summary> Keyword used for Alpha testing. </summary>
        public const string _ALPHATEST_ON = "_ALPHATEST_ON";

        /// <summary> Keyword used for combined directional Lightmaps. </summary>
        public const string DIRLIGHTMAP_COMBINED = "DIRLIGHTMAP_COMBINED";

        /// <summary> Keyword used for 2x detail mapping. </summary>
        public const string _DETAIL_MULX2 = "_DETAIL_MULX2";

        /// <summary> Keyword used for scaled detail mapping. </summary>
        public const string _DETAIL_SCALED = "_DETAIL_SCALED";

        /// <summary> Keyword used for Clear Coat. </summary>
        public const string _CLEARCOAT = "_CLEARCOAT";

        /// <summary> Keyword used for Clear Coat maps. </summary>
        public const string _CLEARCOATMAP = "_CLEARCOATMAP";

        /// <summary> Keyword used for Debug Display. </summary>
        public const string DEBUG_DISPLAY = "DEBUG_DISPLAY";

        /// <summary> Keyword used for LOD Crossfade. </summary>
        public const string LOD_FADE_CROSSFADE = "LOD_FADE_CROSSFADE";

        /// <summary> Keyword used for LOD Crossfade with ShaderGraph shaders. </summary>
        public const string USE_UNITY_CROSSFADE = "USE_UNITY_CROSSFADE";

        /// <summary> Keyword used for Emission. </summary>
        public const string _EMISSION = "_EMISSION";

        /// <summary> Keyword used for receiving shadows. </summary>
        public const string _RECEIVE_SHADOWS_OFF = "_RECEIVE_SHADOWS_OFF";

        /// <summary> Keyword used for opaque or transparent surface types. </summary>
        public const string _SURFACE_TYPE_TRANSPARENT = "_SURFACE_TYPE_TRANSPARENT";

        /// <summary> Keyword used for Alpha premultiply. </summary>
        public const string _ALPHAPREMULTIPLY_ON = "_ALPHAPREMULTIPLY_ON";

        /// <summary> Keyword used for Alpha modulate. </summary>
        public const string _ALPHAMODULATE_ON = "_ALPHAMODULATE_ON";

        /// <summary> Keyword used for Normal maps. </summary>
        public const string _NORMALMAP = "_NORMALMAP";

        /// <summary> Keyword used for Alembic precomputed velocity. </summary>
        public const string _ADD_PRECOMPUTED_VELOCITY = "_ADD_PRECOMPUTED_VELOCITY";

        /// <summary> Keyword used for editor visualization. </summary>
        public const string EDITOR_VISUALIZATION = "EDITOR_VISUALIZATION";

        /// <summary> Keyword used for foveated rendering. </summary>
        public const string FoveatedRenderingNonUniformRaster = "_FOVEATED_RENDERING_NON_UNIFORM_RASTER";

        /// <summary> Keyword used for disabling Texture 2D Arrays. </summary>
        public const string DisableTexture2DXArray = "DISABLE_TEXTURE2D_X_ARRAY";

        /// <summary> Keyword used for Single Slice Blits. </summary>
        public const string BlitSingleSlice = "BLIT_SINGLE_SLICE";

        /// <summary> Keyword used for rendering a combined mesh for XR. </summary>
        public const string XROcclusionMeshCombined = "XR_OCCLUSION_MESH_COMBINED";

        /// <summary> Keyword used for applying scale and bias. </summary>
        public const string SCREEN_COORD_OVERRIDE = "SCREEN_COORD_OVERRIDE";

        /// <summary> Keyword used for half size downsampling. </summary>
        public const string DOWNSAMPLING_SIZE_2 = "DOWNSAMPLING_SIZE_2";

        /// <summary> Keyword used for quarter size downsampling. </summary>
        public const string DOWNSAMPLING_SIZE_4 = "DOWNSAMPLING_SIZE_4";

        /// <summary> Keyword used for eighth size downsampling. </summary>
        public const string DOWNSAMPLING_SIZE_8 = "DOWNSAMPLING_SIZE_8";

        /// <summary> Keyword used for sixteenth size downsampling. </summary>
        public const string DOWNSAMPLING_SIZE_16 = "DOWNSAMPLING_SIZE_16";

        /// <summary> Keyword used for mixed Spherical Harmonic (SH) evaluation in URP Lit shaders.</summary>
        public const string EVALUATE_SH_MIXED = "EVALUATE_SH_MIXED";

        /// <summary> Keyword used for vertex Spherical Harmonic (SH) evaluation in URP Lit shaders.</summary>
        public const string EVALUATE_SH_VERTEX = "EVALUATE_SH_VERTEX";

        /// <summary> Keyword used for APV with SH L1 </summary>
        public const string ProbeVolumeL1 = "PROBE_VOLUMES_L1";

        /// <summary> Keyword used for APV with SH L2 </summary>
        public const string ProbeVolumeL2 = "PROBE_VOLUMES_L2";

        /// <summary> Keyword used for opting out of lightmap texture arrays, when using BatchRendererGroup. </summary>
        public const string USE_LEGACY_LIGHTMAPS = "USE_LEGACY_LIGHTMAPS";

        /// <summary> Keyword used for CopyDepth pass. </summary>
        public const string _OUTPUT_DEPTH = "_OUTPUT_DEPTH";

        /// <summary> Keyword used for enable alpha output. Used in post processing. </summary>
        public const string _ENABLE_ALPHA_OUTPUT = "_ENABLE_ALPHA_OUTPUT";
    }

    public sealed partial class UniversalRenderPipeline
    {
        // Holds light direction for directional lights or position for punctual lights.
        // When w is set to 1.0, it means it's a punctual light.
        static Vector4 k_DefaultLightPosition = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        static Vector4 k_DefaultLightColor = Color.black;

        // Default light attenuation is setup in a particular way that it causes
        // directional lights to return 1.0 for both distance and angle attenuation
        static Vector4 k_DefaultLightAttenuation = new Vector4(0.0f, 1.0f, 0.0f, 1.0f);
        static Vector4 k_DefaultLightSpotDirection = new Vector4(0.0f, 0.0f, 1.0f, 0.0f);
        static Vector4 k_DefaultLightsProbeChannel = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

        static List<Vector4> m_ShadowBiasData = new List<Vector4>();
        static List<int> m_ShadowResolutionData = new List<int>();

        /// <summary>
        /// Checks if a camera is a game camera.
        /// </summary>
        /// <param name="camera">Camera to check state from.</param>
        /// <returns>true if given camera is a game camera, false otherwise.</returns>
        public static bool IsGameCamera(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            return camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR;
        }

        /// <summary>
        /// Returns the current render pipeline asset for the current quality setting.
        /// If no render pipeline asset is assigned in QualitySettings, then returns the one assigned in GraphicsSettings.
        /// </summary>
        public static UniversalRenderPipelineAsset asset
        {
            get => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }

        Comparison<Camera> cameraComparison = (camera1, camera2) => { return (int)camera1.depth - (int)camera2.depth; };
#if UNITY_2021_1_OR_NEWER
        void SortCameras(List<Camera> cameras)
        {
            if (cameras.Count > 1)
                cameras.Sort(cameraComparison);
        }

#else
        void SortCameras(Camera[] cameras)
        {
            if (cameras.Length > 1)
                Array.Sort(cameras, cameraComparison);
        }

#endif

        /// <summary>
        /// Returns the index of the last base camera to draw ScreenSpace Overlay UI at the last base camera.
        /// </summary>
        private int GetLastBaseCameraIndex(List<Camera> cameras)
        {
            int lastBaseCameraIndex = 0;
            for (int i = 0; i < cameras.Count; i++)
            {
                cameras[i].TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraAdditionalData);
                if (baseCameraAdditionalData?.renderType == CameraRenderType.Base)
                    lastBaseCameraIndex = i;
            }
            return lastBaseCameraIndex;
        }

        internal static GraphicsFormat MakeRenderTextureGraphicsFormat(bool isHdrEnabled, HDRColorBufferPrecision requestHDRColorBufferPrecision, bool needsAlpha)
        {
            if (isHdrEnabled)
            {
                // TODO: we need a proper format scoring system. Score formats, sort, pick first or pick first supported (if not in score).
                // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
                // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
                if (!needsAlpha && requestHDRColorBufferPrecision != HDRColorBufferPrecision._64Bits && SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, GraphicsFormatUsage.Blend))
                    return GraphicsFormat.B10G11R11_UFloatPack32;
                if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.Blend))
                    return GraphicsFormat.R16G16B16A16_SFloat;
                return SystemInfo.GetGraphicsFormat(DefaultFormat.HDR); // This might actually be a LDR format on old devices.
            }

            return SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
        }

        // Returns a UNORM based render texture format
        // When supported by the device, this function will prefer formats with higher precision, but the same bit-depth
        // NOTE: This function does not guarantee that the returned format will contain an alpha channel.
        internal static GraphicsFormat MakeUnormRenderTextureGraphicsFormat()
        {
            // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
            // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
            if (SystemInfo.IsFormatSupported(GraphicsFormat.A2B10G10R10_UNormPack32, GraphicsFormatUsage.Blend))
                return GraphicsFormat.A2B10G10R10_UNormPack32;
            else
                return GraphicsFormat.R8G8B8A8_UNorm;
        }

        internal static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, UniversalCameraData cameraData,
            bool isHdrEnabled, HDRColorBufferPrecision requestHDRColorBufferPrecision, int msaaSamples, bool needsAlpha, bool requiresOpaqueTexture)
        {
            RenderTextureDescriptor desc;

            if (camera.targetTexture == null)
            {
                desc = new RenderTextureDescriptor(cameraData.scaledWidth, cameraData.scaledHeight);
                desc.graphicsFormat = MakeRenderTextureGraphicsFormat(isHdrEnabled, requestHDRColorBufferPrecision, needsAlpha);
                desc.depthStencilFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
                desc.msaaSamples = msaaSamples;
                desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            }
            else
            {
                // Note: External texture replaces internal (intermediate) color buffer here, ignoring the configured internal rendering color buffer format.
                // This is incorrect. We should use the internal rendering format throughout and blit the result to the external texture at the end (blit could be skipped if the formats match).
                // However, this would lead to breaking changes in the URP asset as we would need to move the internal rendering format to the renderer asset.
                // This way it could be selected separately for each target.
                // Current workflow/workaround is to simply pick a suitable format for the external texture.
                desc = camera.targetTexture.descriptor;
                desc.msaaSamples = msaaSamples;
                // Note: This does not scale the underlying target size.
                // Instead, it is the scaled viewport rect size which means the viewport offset into the target is always (0,0).
                desc.width = cameraData.scaledWidth;
                desc.height = cameraData.scaledHeight;

                if (camera.cameraType == CameraType.SceneView && !isHdrEnabled)
                {
                    desc.graphicsFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
                }
                // SystemInfo.SupportsRenderTextureFormat(camera.targetTexture.descriptor.colorFormat)
                // will assert on R8_SINT since it isn't a valid value of RenderTextureFormat.
                // If this is fixed then we can implement debug statement to the user explaining why some
                // RenderTextureFormats available resolves in a black render texture when no warning or error
                // is given.
            }

            desc.enableRandomWrite = false;
            desc.bindMS = false;
            desc.useDynamicScale = camera.allowDynamicResolution;

            // check that the requested MSAA samples count is supported by the current platform. If it's not supported,
            // replace the requested desc.msaaSamples value with the actual value the engine falls back to
            desc.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(desc);

            // if the target platform doesn't support storing multisampled RTs and we are doing any offscreen passes, using a Load load action on the subsequent passes
            // will result in loading Resolved data, which on some platforms is discarded, resulting in losing the results of the previous passes.
            // As a workaround we disable MSAA to make sure that the results of previous passes are stored. (fix for Case 1247423).
            if (!SystemInfo.supportsStoreAndResolveAction)
                desc.msaaSamples = 1;

            return desc;
        }

        private static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
        {
            LightDataGI lightData = new LightDataGI();
#if UNITY_EDITOR
            // Always extract lights in the Editor.
            for (int i = 0; i < requests.Length; i++)
            {
                Light light = requests[i];
                var additionalLightData = light.GetUniversalAdditionalLightData();

                LightmapperUtils.Extract(light, out Cookie cookie);

                switch (light.type)
                {
                    case LightType.Directional:
                        DirectionalLight directionalLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref directionalLight);

                        if (light.cookie != null)
                        {
                            // Size == 1 / Scale
                            cookie.sizes = additionalLightData.lightCookieSize;
                            // Offset, Map cookie UV offset to light position on along local axes.
                            if (additionalLightData.lightCookieOffset != Vector2.zero)
                            {
                                var r = light.transform.right * additionalLightData.lightCookieOffset.x;
                                var u = light.transform.up * additionalLightData.lightCookieOffset.y;
                                var offset = r + u;

                                directionalLight.position += offset;
                            }
                        }

                        lightData.Init(ref directionalLight, ref cookie);
                        break;
                    case LightType.Point:
                        PointLight pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        lightData.Init(ref pointLight, ref cookie);
                        break;
                    case LightType.Spot:
                        SpotLight spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                        spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                        lightData.Init(ref spotLight, ref cookie);
                        break;
                    case LightType.Rectangle:
                        RectangleLight rectangleLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectangleLight);
                        rectangleLight.mode = LightMode.Baked;
                        lightData.Init(ref rectangleLight);
                        break;
                    case LightType.Disc:
                        DiscLight discLight = new DiscLight();
                        LightmapperUtils.Extract(light, ref discLight);
                        discLight.mode = LightMode.Baked;
                        lightData.Init(ref discLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }

                lightData.falloff = FalloffType.InverseSquared;
                lightsOutput[i] = lightData;
            }
#else
            // If Enlighten realtime GI isn't active, we don't extract lights.
            if (SupportedRenderingFeatures.active.enlighten == false || ((int)SupportedRenderingFeatures.active.lightmapBakeTypes | (int)LightmapBakeType.Realtime) == 0)
            {
                for (int i = 0; i < requests.Length; i++)
                {
                    Light light = requests[i];
                    lightData.InitNoBake(light.GetInstanceID());
                    lightsOutput[i] = lightData;
                }
            }
            else
            {
                for (int i = 0; i < requests.Length; i++)
                {
                    Light light = requests[i];
                    switch (light.type)
                    {
                        case LightType.Directional:
                            DirectionalLight directionalLight = new DirectionalLight();
                            LightmapperUtils.Extract(light, ref directionalLight);
                            lightData.Init(ref directionalLight);
                            break;
                        case LightType.Point:
                            PointLight pointLight = new PointLight();
                            LightmapperUtils.Extract(light, ref pointLight);
                            lightData.Init(ref pointLight);
                            break;
                        case LightType.Spot:
                            SpotLight spotLight = new SpotLight();
                            LightmapperUtils.Extract(light, ref spotLight);
                            spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                            spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                            lightData.Init(ref spotLight);
                            break;
                        case LightType.Rectangle:
                            // Rect area light is baked only in URP.
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                        case LightType.Disc:
                            // Disc light is baked only.
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                        default:
                            lightData.InitNoBake(light.GetInstanceID());
                            break;
                    }
                    lightData.falloff = FalloffType.InverseSquared;
                    lightsOutput[i] = lightData;
                }
            }
#endif
        };

        // Called from DeferredLights.cs too
        /// <summary>
        /// Calculates the attenuation for a given light and also direction for spot lights.
        /// </summary>
        /// <param name="lightType">The type of light.</param>
        /// <param name="lightRange">The range of the light.</param>
        /// <param name="lightLocalToWorldMatrix">The local to world light matrix.</param>
        /// <param name="spotAngle">The spotlight angle.</param>
        /// <param name="innerSpotAngle">The spotlight inner angle.</param>
        /// <param name="lightAttenuation">The light attenuation.</param>
        /// <param name="lightSpotDir">The spot light direction.</param>
        public static void GetLightAttenuationAndSpotDirection(
            LightType lightType, float lightRange, Matrix4x4 lightLocalToWorldMatrix,
            float spotAngle, float? innerSpotAngle,
            out Vector4 lightAttenuation, out Vector4 lightSpotDir)
        {
            // Default is directional
            lightAttenuation = k_DefaultLightAttenuation;
            lightSpotDir = k_DefaultLightSpotDirection;

            if (lightType != LightType.Directional)
            {
                GetPunctualLightDistanceAttenuation(lightRange, ref lightAttenuation);

                if (lightType == LightType.Spot)
                {
                    GetSpotDirection(ref lightLocalToWorldMatrix, out lightSpotDir);
                    GetSpotAngleAttenuation(spotAngle, innerSpotAngle, ref lightAttenuation);
                }
            }
        }

        internal static void GetPunctualLightDistanceAttenuation(float lightRange, ref Vector4 lightAttenuation)
        {
            // Light attenuation in universal matches the unity vanilla one (HINT_NICE_QUALITY).
            // attenuation = 1.0 / distanceToLightSqr
            // The smoothing factor makes sure that the light intensity is zero at the light range limit.
            // (We used to offer two different smoothing factors.)

            // The current smoothing factor matches the one used in the Unity lightmapper.
            // smoothFactor = (1.0 - saturate((distanceSqr * 1.0 / lightRangeSqr)^2))^2
            float lightRangeSqr = lightRange * lightRange;
            float fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
            float fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
            float lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
            float oneOverLightRangeSqr = 1.0f / Mathf.Max(0.0001f, lightRangeSqr);

            // On all devices: Use the smoothing factor that matches the GI.
            lightAttenuation.x = oneOverLightRangeSqr;
            lightAttenuation.y = lightRangeSqrOverFadeRangeSqr;
        }

        internal static void GetSpotAngleAttenuation(
            float spotAngle, float? innerSpotAngle,
            ref Vector4 lightAttenuation)
        {
            // Spot Attenuation with a linear falloff can be defined as
            // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
            // This can be rewritten as
            // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
            // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
            // If we precompute the terms in a MAD instruction
            float cosOuterAngle = Mathf.Cos(Mathf.Deg2Rad * spotAngle * 0.5f);
            // We need to do a null check for particle lights
            // This should be changed in the future
            // Particle lights will use an inline function
            float cosInnerAngle;
            if (innerSpotAngle.HasValue)
                cosInnerAngle = Mathf.Cos(innerSpotAngle.Value * Mathf.Deg2Rad * 0.5f);
            else
                cosInnerAngle = Mathf.Cos((2.0f * Mathf.Atan(Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f)) * 0.5f);
            float smoothAngleRange = Mathf.Max(0.001f, cosInnerAngle - cosOuterAngle);
            float invAngleRange = 1.0f / smoothAngleRange;
            float add = -cosOuterAngle * invAngleRange;

            lightAttenuation.z = invAngleRange;
            lightAttenuation.w = add;
        }

        internal static void GetSpotDirection(ref Matrix4x4 lightLocalToWorldMatrix, out Vector4 lightSpotDir)
        {
            Vector4 dir = lightLocalToWorldMatrix.GetColumn(2);
            lightSpotDir = new Vector4(-dir.x, -dir.y, -dir.z, 0.0f);
        }

        /// <summary>
        /// Initializes common light constants.
        /// </summary>
        /// <param name="lights">List of lights to iterate.</param>
        /// <param name="lightIndex">The index of the light.</param>
        /// <param name="lightPos">The position of the light.</param>
        /// <param name="lightColor">The color of the light.</param>
        /// <param name="lightAttenuation">The attenuation of the light.</param>
        /// <param name="lightSpotDir">The direction of the light.</param>
        /// <param name="lightOcclusionProbeChannel">The occlusion probe channel for the light.</param>
        public static void InitializeLightConstants_Common(NativeArray<VisibleLight> lights, int lightIndex, out Vector4 lightPos, out Vector4 lightColor, out Vector4 lightAttenuation, out Vector4 lightSpotDir, out Vector4 lightOcclusionProbeChannel)
        {
            lightPos = k_DefaultLightPosition;
            lightColor = k_DefaultLightColor;
            lightOcclusionProbeChannel = k_DefaultLightsProbeChannel;
            lightAttenuation = k_DefaultLightAttenuation;  // Directional by default.
            lightSpotDir = k_DefaultLightSpotDirection;

            // When no lights are visible, main light will be set to -1.
            // In this case we initialize it to default values and return
            if (lightIndex < 0)
                return;

            // Avoid memcpys. Pass by ref and locals for multiple uses.
            ref VisibleLight lightData = ref lights.UnsafeElementAtMutable(lightIndex);
            var light = lightData.light;
            var lightLocalToWorld = lightData.localToWorldMatrix;
            var lightType = lightData.lightType;

            if (lightType == LightType.Directional)
            {
                Vector4 dir = -lightLocalToWorld.GetColumn(2);
                lightPos = new Vector4(dir.x, dir.y, dir.z, 0.0f);
            }
            else
            {
                Vector4 pos = lightLocalToWorld.GetColumn(3);
                lightPos = new Vector4(pos.x, pos.y, pos.z, 1.0f);

                GetPunctualLightDistanceAttenuation(lightData.range, ref lightAttenuation);

                if (lightType == LightType.Spot)
                {
                    GetSpotAngleAttenuation(lightData.spotAngle, light?.innerSpotAngle, ref lightAttenuation);
                    GetSpotDirection(ref lightLocalToWorld, out lightSpotDir);
                }
            }

            // VisibleLight.finalColor already returns color in active color space
            lightColor = lightData.finalColor;

            if (light != null && light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed &&
                0 <= light.bakingOutput.occlusionMaskChannel &&
                light.bakingOutput.occlusionMaskChannel < 4)
            {
                lightOcclusionProbeChannel[light.bakingOutput.occlusionMaskChannel] = 1.0f;
            }
        }
    }

    // URP Profile Id
    // - Scopes using this enum are automatically picked up by the performance testing framework.
    // - You can use [HideInDebugUI] attribute to hide a given id from the Detailed Stats section of Rendering Debugger.
    internal enum URPProfileId
    {
        // CPU
        UniversalRenderTotal,
        UpdateVolumeFramework,
        RenderCameraStack,

        // GPU
        AdditionalLightsShadow,
        ColorGradingLUT,
        CopyColor,
        CopyDepth,
        DrawDepthNormalPrepass,
        DepthPrepass,
        UpdateReflectionProbeAtlas,

        // DrawObjectsPass
        DrawOpaqueObjects,
        DrawTransparentObjects,
        DrawScreenSpaceUI,

        //Full Record Render Graph
        RecordRenderGraph,

        // RenderObjectsPass
        //RenderObjects,

        LightCookies,

        MainLightShadow,
        ResolveShadows,
        SSAO,

        // PostProcessPass
        StopNaNs,
        SMAA,
        GaussianDepthOfField,
        BokehDepthOfField,
        TemporalAA,
        MotionBlur,
        PaniniProjection,
        UberPostProcess,
        Bloom,
        LensFlareDataDrivenComputeOcclusion,
        LensFlareDataDriven,
        LensFlareScreenSpace,
        DrawMotionVectors,
        DrawFullscreen,

        // PostProcessPass RenderGraph
        [HideInDebugUI] RG_SetupPostFX,
        [HideInDebugUI] RG_StopNaNs,
        [HideInDebugUI] RG_SMAAMaterialSetup,
        [HideInDebugUI] RG_SMAAEdgeDetection,
        [HideInDebugUI] RG_SMAABlendWeight,
        [HideInDebugUI] RG_SMAANeighborhoodBlend,
        [HideInDebugUI] RG_SetupDoF,
        [HideInDebugUI] RG_DOFComputeCOC,
        [HideInDebugUI] RG_DOFDownscalePrefilter,
        [HideInDebugUI] RG_DOFBlurH,
        [HideInDebugUI] RG_DOFBlurV,
        [HideInDebugUI] RG_DOFBlurBokeh,
        [HideInDebugUI] RG_DOFPostFilter,
        [HideInDebugUI] RG_DOFComposite,
        [HideInDebugUI] RG_TAA,
        [HideInDebugUI] RG_TAACopyHistory,
        [HideInDebugUI] RG_MotionBlur,
        [HideInDebugUI] RG_BloomSetup,
        [HideInDebugUI] RG_BloomPrefilter,
        [HideInDebugUI] RG_BloomDownsample,
        [HideInDebugUI] RG_BloomUpsample,
        [HideInDebugUI] RG_UberPostSetupBloomPass,
        [HideInDebugUI] RG_UberPost,
        [HideInDebugUI] RG_FinalSetup,
        [HideInDebugUI] RG_FinalFSRScale,
        [HideInDebugUI] RG_FinalBlit,

        BlitFinalToBackBuffer,
        DrawSkybox
    }

    // Internal class to detect and cache runtime platform information.
    // TODO: refine the logic to provide platform abstraction. Eg, we should divide platforms based on capabilities and perf budget.
    // TODO: isXRMobile is a bad category. Alignment and refactor needed.
    // TODO: Compress all the query data into "isXRMobile" style booleans and enums.
    internal static class PlatformAutoDetect
    {
        /// <summary>
        /// Detect and cache runtime platform information. This function should only be called once when creating the URP.
        /// </summary>
        internal static void Initialize()
        {
            bool isRunningMobile = false;
            #if ENABLE_VR && ENABLE_VR_MODULE
                #if PLATFORM_WINRT || PLATFORM_ANDROID
                    isRunningMobile = IsRunningXRMobile();
                #endif
            #endif

            isXRMobile = isRunningMobile;
            isShaderAPIMobileDefined = GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
            isSwitch = Application.platform == RuntimePlatform.Switch;
        }

#if ENABLE_VR && ENABLE_VR_MODULE
    #if PLATFORM_WINRT || PLATFORM_ANDROID
        // XR mobile platforms are not treated as dedicated mobile platforms in Core. Handle them specially here. (Quest and HL).
        private static List<XR.XRDisplaySubsystem> displaySubsystemList = new List<XR.XRDisplaySubsystem>();
        private static bool IsRunningXRMobile()
        {
            var platform = Application.platform;
            if (platform == RuntimePlatform.WSAPlayerX86 || platform == RuntimePlatform.WSAPlayerARM || platform == RuntimePlatform.WSAPlayerX64 || platform == RuntimePlatform.Android)
            {
                XR.XRDisplaySubsystem display = null;
                SubsystemManager.GetSubsystems(displaySubsystemList);

                if (displaySubsystemList.Count > 0)
                    display = displaySubsystemList[0];

                if (display != null)
                    return true;
            }
            return false;
        }
    #endif
#endif

        /// <summary>
        /// If true, the runtime platform is an XR mobile platform.
        /// </summary>
        internal static bool isXRMobile { get; private set; } = false;

        /// <summary>
        /// If true, then SHADER_API_MOBILE has been defined in URP Shaders.
        /// </summary>
        internal static bool isShaderAPIMobileDefined { get; private set; } = false;

        /// <summary>
        /// If true, then the runtime platform is set to Switch.
        /// </summary>
        internal static bool isSwitch { get; private set; } = false;

        /// <summary>
        /// Gives the SH evaluation mode when set to automatically detect.
        /// </summary>
        /// <param name="mode">The current SH evaluation mode.</param>
        /// <returns>Returns the SH evaluation mode to use.</returns>
        internal static ShEvalMode ShAutoDetect(ShEvalMode mode)
        {
            if (mode == ShEvalMode.Auto)
            {
                if (isXRMobile || isShaderAPIMobileDefined || isSwitch)
                    return ShEvalMode.PerVertex;
                else
                    return ShEvalMode.PerPixel;
            }

            return mode;
        }

        internal static bool isRunningOnPowerVRGPU = SystemInfo.graphicsDeviceName.Contains("PowerVR");
    }
}
