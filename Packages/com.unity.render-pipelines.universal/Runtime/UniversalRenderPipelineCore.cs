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
        FSR
    }

    /// <summary>
    /// Struct that flattens several rendering settings used to render a camera stack.
    /// URP builds the <c>RenderingData</c> settings from several places, including the pipeline asset, camera and light settings.
    /// The settings also might vary on different platforms and depending on if Adaptive Performance is used.
    /// </summary>
    public struct RenderingData
    {
        internal CommandBuffer commandBuffer;

        /// <summary>
        /// Returns culling results that exposes handles to visible objects, lights and probes.
        /// You can use this to draw objects with <c>ScriptableRenderContext.DrawRenderers</c>
        /// <see cref="CullingResults"/>
        /// <seealso cref="ScriptableRenderContext"/>
        /// </summary>
        public CullingResults cullResults;

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
        public bool supportsDynamicBatching;

        /// <summary>
        /// Holds per-object data that are requested when drawing
        /// <see cref="PerObjectData"/>
        /// </summary>
        public PerObjectData perObjectData;

        /// <summary>
        /// True if post-processing effect is enabled while rendering the camera stack.
        /// </summary>
        public bool postProcessingEnabled;
    }

    /// <summary>
    /// Struct that holds settings related to lights.
    /// </summary>
    public struct LightData
    {
        /// <summary>
        /// Holds the main light index from the <c>VisibleLight</c> list returned by culling. If there's no main light in the scene, <c>mainLightIndex</c> is set to -1.
        /// The main light is the directional light assigned as Sun source in light settings or the brightest directional light.
        /// <seealso cref="CullingResults"/>
        /// </summary>
        public int mainLightIndex;

        /// <summary>
        /// The number of additional lights visible by the camera.
        /// </summary>
        public int additionalLightsCount;

        /// <summary>
        /// Maximum amount of lights that can be shaded per-object. This value only affects forward rendering.
        /// </summary>
        public int maxPerObjectAdditionalLightsCount;

        /// <summary>
        /// List of visible lights returned by culling.
        /// </summary>
        public NativeArray<VisibleLight> visibleLights;

        /// <summary>
        /// True if additional lights should be shaded in vertex shader, otherwise additional lights will be shaded per pixel.
        /// </summary>
        public bool shadeAdditionalLightsPerVertex;

        /// <summary>
        /// True if mixed lighting is supported.
        /// </summary>
        public bool supportsMixedLighting;

        /// <summary>
        /// True if box projection is enabled for reflection probes.
        /// </summary>
        public bool reflectionProbeBoxProjection;

        /// <summary>
        /// True if blending is enabled for reflection probes.
        /// </summary>
        public bool reflectionProbeBlending;

        /// <summary>
        /// True if light layers are enabled.
        /// </summary>
        public bool supportsLightLayers;

        /// <summary>
        /// True if additional lights enabled.
        /// </summary>
        public bool supportsAdditionalLights;
    }


    /// <summary>
    /// Struct that holds settings related to camera.
    /// </summary>
    public struct CameraData
    {
        // Internal camera data as we are not yet sure how to expose View in stereo context.
        // We might change this API soon.
        Matrix4x4 m_ViewMatrix;
        Matrix4x4 m_ProjectionMatrix;
        Matrix4x4 m_JitterMatrix;

        internal void SetViewAndProjectionMatrix(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            m_ViewMatrix = viewMatrix;
            m_ProjectionMatrix = projectionMatrix;
            m_JitterMatrix = Matrix4x4.identity;
        }

        internal void SetViewProjectionAndJitterMatrix(Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix, Matrix4x4 jitterMatrix)
        {
            m_ViewMatrix = viewMatrix;
            m_ProjectionMatrix = projectionMatrix;
            m_JitterMatrix = jitterMatrix;
        }

        // Helper function to populate builtin stereo matricies as well as URP stereo matricies
        internal void PushBuiltinShaderConstantsXR(CommandBuffer cmd, bool renderIntoTexture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
            {
                cmd.SetViewProjectionMatrices(GetViewMatrix(), GetProjectionMatrix());
                if (xr.singlePassEnabled)
                {
                    for (int viewId = 0; viewId < xr.viewCount; viewId++)
                    {
                        XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(GetViewMatrix(viewId), GetProjectionMatrix(viewId), renderIntoTexture, viewId);
                    }
                    XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);
                }
                else
                {
                    // Update multipass worldSpace camera pos
                    Vector3 worldSpaceCameraPos = Matrix4x4.Inverse(GetViewMatrix(0)).GetColumn(3);
                    cmd.SetGlobalVector(ShaderPropertyId.worldSpaceCameraPos, worldSpaceCameraPos);
                }
            }
#endif
        }

        /// <summary>
        /// Returns the camera view matrix.
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <returns> The camera view matrix. </returns>
        public Matrix4x4 GetViewMatrix(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
                return xr.GetViewMatrix(viewIndex);
#endif
            return m_ViewMatrix;
        }

        /// <summary>
        /// Returns the camera projection matrix. Might be jittered for temporal features.
        /// </summary>
        /// <param name="viewIndex"> View index in case of stereo rendering. By default <c>viewIndex</c> is set to 0. </param>
        /// <returns> The camera projection matrix. </returns>
        public Matrix4x4 GetProjectionMatrix(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
                return m_JitterMatrix * xr.GetProjMatrix(viewIndex);
#endif
            return m_JitterMatrix * m_ProjectionMatrix;
        }

        internal Matrix4x4 GetProjectionMatrixNoJitter(int viewIndex = 0)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
                return xr.GetProjMatrix(viewIndex);
#endif
            return m_ProjectionMatrix;
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
            // GetGPUProjectionMatrix takes a projection matrix and returns a GfxAPI adjusted version, does not set or get any state.
            return m_JitterMatrix * GL.GetGPUProjectionMatrix(GetProjectionMatrixNoJitter(viewIndex), IsCameraProjectionMatrixFlipped());
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
            // GetGPUProjectionMatrix takes a projection matrix and returns a GfxAPI adjusted version, does not set or get any state.
            return GL.GetGPUProjectionMatrix(GetProjectionMatrixNoJitter(viewIndex), IsCameraProjectionMatrixFlipped());
        }

        internal Matrix4x4 GetGPUProjectionMatrix(bool renderIntoTexture, int viewIndex = 0)
        {
            return m_JitterMatrix * GL.GetGPUProjectionMatrix(GetProjectionMatrix(viewIndex), renderIntoTexture);
        }

        /// <summary>
        /// The camera component.
        /// </summary>
        public Camera camera;

        /// <summary>
        /// The camera render type used for camera stacking.
        /// <see cref="CameraRenderType"/>
        /// </summary>
        public CameraRenderType renderType;

        /// <summary>
        /// Controls the final target texture for a camera. If null camera will resolve rendering to screen.
        /// </summary>
        public RenderTexture targetTexture;

        /// <summary>
        /// Render texture settings used to create intermediate camera textures for rendering.
        /// </summary>
        public RenderTextureDescriptor cameraTargetDescriptor;
        internal Rect pixelRect;
        internal bool useScreenCoordOverride;
        internal Vector4 screenSizeOverride;
        internal Vector4 screenCoordScaleBias;
        internal int pixelWidth;
        internal int pixelHeight;
        internal float aspectRatio;

        /// <summary>
        /// Render scale to apply when creating camera textures.
        /// </summary>
        public float renderScale;
        internal ImageScalingMode imageScalingMode;
        internal ImageUpscalingFilter upscalingFilter;
        internal bool fsrOverrideSharpness;
        internal float fsrSharpness;
        internal HDRColorBufferPrecision hdrColorBufferPrecision;

        /// <summary>
        /// True if this camera should clear depth buffer. This setting only applies to cameras of type <c>CameraRenderType.Overlay</c>
        /// <seealso cref="CameraRenderType"/>
        /// </summary>
        public bool clearDepth;

        /// <summary>
        /// The camera type.
        /// <seealso cref="UnityEngine.CameraType"/>
        /// </summary>
        public CameraType cameraType;

        /// <summary>
        /// True if this camera is drawing to a viewport that maps to the entire screen.
        /// </summary>
        public bool isDefaultViewport;

        /// <summary>
        /// True if this camera should render to high dynamic range color targets.
        /// </summary>
        public bool isHdrEnabled;

        /// <summary>
        /// True if this camera allow color conversion and encoding for high dynamic range displays.
        /// </summary>
        public bool allowHDROutput;

        /// <summary>
        /// True if this camera requires to write _CameraDepthTexture.
        /// </summary>
        public bool requiresDepthTexture;

        /// <summary>
        /// True if this camera requires to copy camera color texture to _CameraOpaqueTexture.
        /// </summary>
        public bool requiresOpaqueTexture;

        /// <summary>
        /// Returns true if post processing passes require depth texture.
        /// </summary>
        public bool postProcessingRequiresDepthTexture;

        /// <summary>
        /// Returns true if XR rendering is enabled.
        /// </summary>
        public bool xrRendering;

        internal bool requireSrgbConversion
        {
            get
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                // For some XR platforms we need to encode in SRGB but can't use a _SRGB format texture, only required for 8bit per channel 32 bit formats.
                if (xr.enabled)
                    return !xr.renderTargetDesc.sRGB && (xr.renderTargetDesc.graphicsFormat == GraphicsFormat.R8G8B8A8_UNorm || xr.renderTargetDesc.graphicsFormat == GraphicsFormat.B8G8R8A8_UNorm) && (QualitySettings.activeColorSpace == ColorSpace.Linear);
#endif

                return targetTexture == null && Display.main.requiresSrgbBlitToBackbuffer;
            }
        }

        /// <summary>
        /// True if the camera rendering is for the scene window in the editor.
        /// </summary>
        public bool isSceneViewCamera => cameraType == CameraType.SceneView;

        /// <summary>
        /// True if the camera rendering is for the preview window in the editor.
        /// </summary>
        public bool isPreviewCamera => cameraType == CameraType.Preview;

        internal bool isRenderPassSupportedCamera => (cameraType == CameraType.Game || cameraType == CameraType.Reflection);

        internal bool resolveToScreen => targetTexture == null && resolveFinalTarget && (cameraType == CameraType.Game || camera.cameraType == CameraType.VR);

        /// <summary>
        /// True if the Camera should output to an HDR display.
        /// </summary>
        public bool isHDROutputActive
        {
            get
            {
                bool hdrDisplayOutputActive = UniversalRenderPipeline.HDROutputForMainDisplayIsActive();
#if ENABLE_VR && ENABLE_XR_MODULE
                // If we are rendering to xr then we need to look at the XR Display rather than the main non-xr display.
                if (xr.enabled)
                    hdrDisplayOutputActive = xr.isHDRDisplayOutputActive;
#endif
                return hdrDisplayOutputActive && allowHDROutput && resolveToScreen;
            }
        }

        /// <summary>
        /// True if the last camera in the stack outputs to an HDR screen
        /// </summary>
        internal bool stackLastCameraOutputToHDR;

        /// <summary>
        /// HDR Display information about the current display this camera is rendering to.
        /// </summary>
        public HDROutputUtils.HDRDisplayInformation hdrDisplayInformation
        {
            get
            {
                HDROutputUtils.HDRDisplayInformation displayInformation;
#if ENABLE_VR && ENABLE_XR_MODULE
                // If we are rendering to xr then we need to look at the XR Display rather than the main non-xr display.
                if (xr.enabled)
                {
                    displayInformation = xr.hdrDisplayOutputInformation;
                }
                else
#endif
                {
                    HDROutputSettings displaySettings = HDROutputSettings.main;
                    displayInformation = new HDROutputUtils.HDRDisplayInformation(displaySettings.maxFullFrameToneMapLuminance,
                        displaySettings.maxToneMapLuminance,
                        displaySettings.minToneMapLuminance,
                        displaySettings.paperWhiteNits);
                }

                return displayInformation;
            }
        }

        /// <summary>
        /// HDR Display Color Gamut
        /// </summary>
        public ColorGamut hdrDisplayColorGamut
        {
            get
            {
#if ENABLE_VR && ENABLE_XR_MODULE
                // If we are rendering to xr then we need to look at the XR Display rather than the main non-xr display.
                if (xr.enabled)
                {
                    return xr.hdrDisplayOutputColorGamut;
                }
                else
#endif
                {
                    HDROutputSettings displaySettings = HDROutputSettings.main;
                    return displaySettings.displayColorGamut;
                }
            }
        }

        /// <summary>
        /// True if the Camera should render overlay UI.
        /// </summary>
        public bool rendersOverlayUI => SupportedRenderingFeatures.active.rendersUIOverlay && resolveToScreen;

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
            if (!SystemInfo.graphicsUVStartsAtTop)
                return false;

            if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)
                return true;

            var handleID = new RenderTargetIdentifier(handle.nameID, 0, CubemapFace.Unknown, 0);
            bool isBackbuffer = handleID == BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
                isBackbuffer |= handleID == new RenderTargetIdentifier(xr.renderTarget, 0, CubemapFace.Unknown, 0);
#endif
            return !isBackbuffer;
        }

        /// <summary>
        /// True if the camera device projection matrix is flipped. This happens when the pipeline is rendering
        /// to a render texture in non OpenGL platforms. If you are doing a custom Blit pass to copy camera textures
        /// (_CameraColorTexture, _CameraDepthAttachment) you need to check this flag to know if you should flip the
        /// matrix when rendering with for cmd.Draw* and reading from camera textures.
        /// </summary>
        /// <returns> True if the camera device projection matrix is flipped. </returns>
        public bool IsCameraProjectionMatrixFlipped()
        {
            if (!SystemInfo.graphicsUVStartsAtTop)
                return false;

            // Users only have access to CameraData on URP rendering scope. The current renderer should never be null.
            var renderer = ScriptableRenderer.current;
            Debug.Assert(renderer != null, "IsCameraProjectionMatrixFlipped is being called outside camera rendering scope.");

            if (renderer != null)
            {
                var handle = renderer.cameraColorTargetHandle;

                bool flipped;
#pragma warning disable 0618 // Obsolete usage: Backwards compatibility for renderer using cameraColorTarget instead of cameraColorTargetHandle
                if (handle == null)
                {
                    if (cameraType == CameraType.SceneView)
                    {
                        flipped = true;
                    }
                    else
                    {
                        var handleID = new RenderTargetIdentifier(renderer.cameraColorTarget, 0, CubemapFace.Unknown, 0);
                        bool isBackbuffer = handleID == BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
                        if (xr.enabled)
                            isBackbuffer |= handleID == new RenderTargetIdentifier(xr.renderTarget, 0, CubemapFace.Unknown, 0);
#endif
                        flipped = !isBackbuffer;
                    }
                }
                else
#pragma warning restore 0618
                    flipped = IsHandleYFlipped(handle);

                return flipped || targetTexture != null;
            }

            return true;
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
            if (!SystemInfo.graphicsUVStartsAtTop)
                return false;

            return targetTexture != null || IsHandleYFlipped(color ?? depth);
        }

        internal bool IsTemporalAAEnabled()
        {
            UniversalAdditionalCameraData additionalCameraData;
            camera.TryGetComponent(out additionalCameraData);

            return (antialiasing == AntialiasingMode.TemporalAntiAliasing)                                                            // Enabled
                   && (taaPersistentData != null)                                                                                     // Initialized
                   && (cameraTargetDescriptor.msaaSamples == 1)                                                                       // No MSAA
                   && !(additionalCameraData?.renderType == CameraRenderType.Overlay || additionalCameraData?.cameraStack.Count > 0)  // No Camera stack
                   && !camera.allowDynamicResolution                                                                                  // No Dynamic Resolution
                   && postProcessEnabled;                                                                                             // No Postprocessing
        }

        /// <summary>
        /// The sorting criteria used when drawing opaque objects by the internal URP render passes.
        /// When a GPU supports hidden surface removal, URP will rely on that information to avoid sorting opaque objects front to back and
        /// benefit for more optimal static batching.
        /// </summary>
        /// <seealso cref="SortingCriteria"/>
        public SortingCriteria defaultOpaqueSortFlags;

        /// <summary>
        /// XRPass holds the render target information and a list of XRView.
        /// XRView contains the parameters required to render (projection and view matrices, viewport, etc)
        /// </summary>
        public XRPass xr { get; internal set; }

        internal XRPassUniversal xrUniversal => xr as XRPassUniversal;

        /// <summary>
        /// Is XR enabled or not.
        /// This is obsolete, please use xr.enabled instead.
        /// </summary>
        [Obsolete("Please use xr.enabled instead.", true)]
        public bool isStereoEnabled;

        /// <summary>
        /// Maximum shadow distance visible to the camera. When set to zero shadows will be disable for that camera.
        /// </summary>
        public float maxShadowDistance;

        /// <summary>
        /// True if post-processing is enabled for this camera.
        /// </summary>
        public bool postProcessEnabled;

        /// <summary>
        /// True if post-processing is enabled for any camera in this camera's stack.
        /// </summary>
        internal bool stackAnyPostProcessingEnabled;

        /// <summary>
        /// Provides set actions to the renderer to be triggered at the end of the render loop for camera capture.
        /// </summary>
        public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> captureActions;

        /// <summary>
        /// The camera volume layer mask.
        /// </summary>
        public LayerMask volumeLayerMask;

        /// <summary>
        /// The camera volume trigger.
        /// </summary>
        public Transform volumeTrigger;

        /// <summary>
        /// If set to true, the integrated post-processing stack will replace any NaNs generated by render passes prior to post-processing with black/zero.
        /// Enabling this option will cause a noticeable performance impact. It should be used while in development mode to identify NaN issues.
        /// </summary>
        public bool isStopNaNEnabled;

        /// <summary>
        /// If set to true a final post-processing pass will be applied to apply dithering.
        /// This can be combined with post-processing antialiasing.
        /// <seealso cref="antialiasing"/>
        /// </summary>
        public bool isDitheringEnabled;

        /// <summary>
        /// Controls the anti-alising mode used by the integrated post-processing stack.
        /// When any other value other than <c>AntialiasingMode.None</c> is chosen, a final post-processing pass will be applied to apply anti-aliasing.
        /// This pass can be combined with dithering.
        /// <see cref="AntialiasingMode"/>
        /// <seealso cref="isDitheringEnabled"/>
        /// </summary>
        public AntialiasingMode antialiasing;

        /// <summary>
        /// Controls the anti-alising quality of the anti-aliasing mode.
        /// <see cref="antialiasingQuality"/>
        /// <seealso cref="AntialiasingMode"/>
        /// </summary>
        public AntialiasingQuality antialiasingQuality;

        /// <summary>
        /// Returns the current renderer used by this camera.
        /// <see cref="ScriptableRenderer"/>
        /// </summary>
        public ScriptableRenderer renderer;

        /// <summary>
        /// True if this camera is resolving rendering to the final camera render target.
        /// When rendering a stack of cameras only the last camera in the stack will resolve to camera target.
        /// </summary>
        public bool resolveFinalTarget;

        /// <summary>
        /// Camera position in world space.
        /// </summary>
        public Vector3 worldSpaceCameraPos;

        /// <summary>
        /// Final background color in the active color space.
        /// </summary>
        public Color backgroundColor;

        /// <summary>
        /// Persistent TAA data, primarily for the accumulation texture.
        /// </summary>
        internal TaaPersistentData taaPersistentData;

        // TAA settings.
        internal TemporalAA.Settings taaSettings;

        // Post-process history reset has been triggered for this camera.
        internal bool resetHistory
        {
            get => taaSettings.resetHistoryFrames != 0;
        }


        /// <summary>
        /// Camera at the top of the overlay camera stack
        /// </summary>
        public Camera baseCamera;
    }

    /// <summary>
    /// Container struct for various data used for shadows in URP.
    /// </summary>
    public struct ShadowData
    {
        /// <summary>
        /// True if main light shadows are enabled.
        /// </summary>
        public bool supportsMainLightShadows;

        /// <summary>
        /// True if additional lights shadows are enabled in the URP Asset
        /// </summary>
        internal bool mainLightShadowsEnabled;

        /// <summary>
        /// True if screen space shadows are required.
        /// Obsolete, this feature was replaced by new 'ScreenSpaceShadows' renderer feature
        /// </summary>
        [Obsolete("Obsolete, this feature was replaced by new 'ScreenSpaceShadows' renderer feature")]
        public bool requiresScreenSpaceShadowResolve;

        /// <summary>
        /// The width of the main light shadow map.
        /// </summary>
        public int mainLightShadowmapWidth;

        /// <summary>
        /// The height of the main light shadow map.
        /// </summary>
        public int mainLightShadowmapHeight;

        /// <summary>
        /// The number of shadow cascades.
        /// </summary>
        public int mainLightShadowCascadesCount;

        /// <summary>
        /// The split between cascades.
        /// </summary>
        public Vector3 mainLightShadowCascadesSplit;

        /// <summary>
        /// Main light last cascade shadow fade border.
        /// Value represents the width of shadow fade that ranges from 0 to 1.
        /// Where value 0 is used for no shadow fade.
        /// </summary>
        public float mainLightShadowCascadeBorder;

        /// <summary>
        /// True if additional lights shadows are enabled.
        /// </summary>
        public bool supportsAdditionalLightShadows;

        /// <summary>
        /// True if additional lights shadows are enabled in the URP Asset
        /// </summary>
        internal bool additionalLightShadowsEnabled;

        /// <summary>
        /// The width of the additional light shadow map.
        /// </summary>
        public int additionalLightsShadowmapWidth;

        /// <summary>
        /// The height of the additional light shadow map.
        /// </summary>
        public int additionalLightsShadowmapHeight;

        /// <summary>
        /// True if soft shadows are enabled.
        /// </summary>
        public bool supportsSoftShadows;

        /// <summary>
        /// The number of bits used.
        /// </summary>
        public int shadowmapDepthBufferBits;

        /// <summary>
        /// A list of shadow bias.
        /// </summary>
        public List<Vector4> bias;

        /// <summary>
        /// A list of resolution for the shadow maps.
        /// </summary>
        public List<int> resolution;

        internal bool isKeywordAdditionalLightShadowsEnabled;
        internal bool isKeywordSoftShadowsEnabled;
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

        public static readonly int blitTexture = Shader.PropertyToID("_BlitTexture");
        public static readonly int blitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        public static readonly int sourceTex = Shader.PropertyToID("_SourceTex");
        public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");
        public static readonly int scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");

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
        /// <summary>
        /// The <c>ColorGradingMode</c> to use.
        /// </summary>
        /// <seealso cref="ColorGradingMode"/>
        public ColorGradingMode gradingMode;

        /// <summary>
        /// The size of the Look Up Table (LUT)
        /// </summary>
        public int lutSize;

        /// <summary>
        /// True if fast approximation functions are used when converting between the sRGB and Linear color spaces, false otherwise.
        /// </summary>
        public bool useFastSRGBLinearConversion;

        /// <summary>
        /// Returns true if Data Driven Lens Flare are supported by this asset, false otherwise.
        /// </summary>
        public bool supportDataDrivenLensFlare;
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

        /// <summary> Keyword used for RGBM format for Bloom. </summary>
        public const string UseRGBM = "_USE_RGBM";

        /// <summary> Keyword used for Distortion. </summary>
        public const string Distortion = "_DISTORTION";

        /// <summary> Keyword used for Chromatic Aberration. </summary>
        public const string ChromaticAberration = "_CHROMATIC_ABERRATION";

        /// <summary> Keyword used for HDR Color Grading. </summary>
        public const string HDRGrading = "_HDR_GRADING";

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

        /// <summary> Keyword used for Fast Approximate Anti-aliasing (FXAA) with Gamma 2.0 encoding. </summary>
        public const string FxaaAndGamma20 = "_FXAA_AND_GAMMA_20";

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

        /// <summary> Keyword used for editor visualization. </summary>
        public const string EDITOR_VISUALIZATION = "EDITOR_VISUALIZATION";

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

        /// <summary> Keyword used for foveated rendering. </summary>
        public const string FoveatedRenderingNonUniformRaster = "_FOVEATED_RENDERING_NON_UNIFORM_RASTER";

        /// <summary> Keyword used for mixed Spherical Harmonic (SH) evaluation in URP Lit shaders.</summary>
        public const string EVALUATE_SH_MIXED = "EVALUATE_SH_MIXED";

        /// <summary> Keyword used for vertex Spherical Harmonic (SH) evaluation in URP Lit shaders.</summary>
        public const string EVALUATE_SH_VERTEX = "EVALUATE_SH_VERTEX";

        /// <summary> Keyword used for Drawing procedurally.</summary>
        public const string UseDrawProcedural = "_USE_DRAW_PROCEDURAL";
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
        /// Checks if a camera is rendering in stereo mode.
        /// </summary>
        /// <param name="camera">Camera to check state from.</param>
        /// <returns>Returns true if the given camera is rendering in stereo mode, false otherwise.</returns>
        [Obsolete("Please use CameraData.xr.enabled instead.", true)]
        public static bool IsStereoEnabled(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException("camera");

            return IsGameCamera(camera) && (camera.stereoTargetEye == StereoTargetEyeMask.Both);
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

        internal static GraphicsFormat MakeRenderTextureGraphicsFormat(bool isHdrEnabled, HDRColorBufferPrecision requestHDRColorBufferPrecision, bool needsAlpha)
        {
            if (isHdrEnabled)
            {
                // TODO: we need a proper format scoring system. Score formats, sort, pick first or pick first supported (if not in score).
                if (!needsAlpha && requestHDRColorBufferPrecision != HDRColorBufferPrecision._64Bits && RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
                    return GraphicsFormat.B10G11R11_UFloatPack32;
                if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Linear | FormatUsage.Render))
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
            if (RenderingUtils.SupportsGraphicsFormat(GraphicsFormat.A2B10G10R10_UNormPack32, FormatUsage.Linear | FormatUsage.Render))
                return GraphicsFormat.A2B10G10R10_UNormPack32;
            else
                return GraphicsFormat.R8G8B8A8_UNorm;
        }

        static RenderTextureDescriptor CreateRenderTextureDescriptor(Camera camera, float renderScale,
            bool isHdrEnabled, HDRColorBufferPrecision requestHDRColorBufferPrecision, int msaaSamples, bool needsAlpha, bool requiresOpaqueTexture)
        {
            int scaledWidth = (int)((float)camera.pixelWidth * renderScale);
            int scaledHeight = (int)((float)camera.pixelHeight * renderScale);

            RenderTextureDescriptor desc;

            if (camera.targetTexture == null)
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
                desc.width = scaledWidth;
                desc.height = scaledHeight;
                desc.graphicsFormat = MakeRenderTextureGraphicsFormat(isHdrEnabled, requestHDRColorBufferPrecision, needsAlpha);
                desc.depthBufferBits = 32;
                desc.msaaSamples = msaaSamples;
                desc.sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            }
            else
            {
                desc = camera.targetTexture.descriptor;
                desc.msaaSamples = msaaSamples;
                desc.width = scaledWidth;
                desc.height = scaledHeight;

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

            // Make sure dimension is non zero
            desc.width = Mathf.Max(1, desc.width);
            desc.height = Mathf.Max(1, desc.height);

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
                    case LightType.Area:
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
                        case LightType.Area:
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
        DepthNormalPrepass,
        DepthPrepass,
        UpdateReflectionProbeAtlas,

        // DrawObjectsPass
        DrawOpaqueObjects,
        DrawTransparentObjects,
        DrawScreenSpaceUI,

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
        MotionVectors,
        DrawFullscreen,

        FinalBlit
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
                SubsystemManager.GetInstances(displaySubsystemList);

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
    }
}
