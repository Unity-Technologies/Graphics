using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class that holds settings related to camera.
    /// </summary>
    public class UniversalCameraData : ContextItem
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

#if ENABLE_VR && ENABLE_XR_MODULE
        private bool m_CachedRenderIntoTextureXR;
        private bool m_InitBuiltinXRConstants;
#endif
        // Helper function to populate builtin stereo matricies as well as URP stereo matricies
        internal void PushBuiltinShaderConstantsXR(RasterCommandBuffer cmd, bool renderIntoTexture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            // Multipass always needs update to prevent wrong view projection matrix set by other passes
            bool needsUpdate = !m_InitBuiltinXRConstants || m_CachedRenderIntoTextureXR != renderIntoTexture || !xr.singlePassEnabled;
            if (needsUpdate && xr.enabled )
            {
                var projection0 = GetProjectionMatrix();
                var view0 = GetViewMatrix();
                cmd.SetViewProjectionMatrices(view0, projection0);

                if (xr.singlePassEnabled)
                {
                    var projection1 = GetProjectionMatrix(1);
                    var view1 = GetViewMatrix(1);
                    XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(view0, projection0, renderIntoTexture, 0);
                    XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(view1, projection1, renderIntoTexture, 1);
                    XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);
                }
                else
                {
                    // Update multipass worldSpace camera pos
                    Vector3 worldSpaceCameraPos = Matrix4x4.Inverse(GetViewMatrix(0)).GetColumn(3);
                    cmd.SetGlobalVector(ShaderPropertyId.worldSpaceCameraPos, worldSpaceCameraPos);

                    //Multipass uses the same value as a normal render, and doesn't use the value set for stereo,
                    //which is why you need to set a value like unity_MatrixInvV.
                    //The values below should be the same as set in the SetCameraMatrices function in ScriptableRenderer.cs.
                    Matrix4x4 gpuProjectionMatrix = GetGPUProjectionMatrix(renderIntoTexture); // TODO: invProjection might NOT match the actual projection (invP*P==I) as the target flip logic has diverging paths.
                    Matrix4x4 inverseViewMatrix = Matrix4x4.Inverse(view0);
                    Matrix4x4 inverseProjectionMatrix = Matrix4x4.Inverse(gpuProjectionMatrix);
                    Matrix4x4 inverseViewProjection = inverseViewMatrix * inverseProjectionMatrix;

                    // There's an inconsistency in handedness between unity_matrixV and unity_WorldToCamera
                    // Unity changes the handedness of unity_WorldToCamera (see Camera::CalculateMatrixShaderProps)
                    // we will also change it here to avoid breaking existing shaders. (case 1257518)
                    Matrix4x4 worldToCameraMatrix = Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)) * view0;
                    Matrix4x4 cameraToWorldMatrix = worldToCameraMatrix.inverse;
                    cmd.SetGlobalMatrix(ShaderPropertyId.worldToCameraMatrix, worldToCameraMatrix);
                    cmd.SetGlobalMatrix(ShaderPropertyId.cameraToWorldMatrix, cameraToWorldMatrix);

                    cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewMatrix, inverseViewMatrix);
                    cmd.SetGlobalMatrix(ShaderPropertyId.inverseProjectionMatrix, inverseProjectionMatrix);
                    cmd.SetGlobalMatrix(ShaderPropertyId.inverseViewAndProjectionMatrix, inverseViewProjection);
                }
                m_CachedRenderIntoTextureXR = renderIntoTexture;
                m_InitBuiltinXRConstants = true;
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
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            // GetGPUProjectionMatrix takes a projection matrix and returns a GfxAPI adjusted version, does not set or get any state.
            return m_JitterMatrix * GL.GetGPUProjectionMatrix(GetProjectionMatrixNoJitter(viewIndex), IsCameraProjectionMatrixFlipped());
            #pragma warning restore CS0618
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
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            // GetGPUProjectionMatrix takes a projection matrix and returns a GfxAPI adjusted version, does not set or get any state.
            return GL.GetGPUProjectionMatrix(GetProjectionMatrixNoJitter(viewIndex), IsCameraProjectionMatrixFlipped());
            #pragma warning restore CS0618
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
        /// Returns the scaled width of the Camera
        /// By obtaining the pixelWidth of the camera and taking into account the render scale
        /// The min dimension is 1.
        /// </summary>
        public int scaledWidth => Mathf.Max(1, (int)(camera.pixelWidth * renderScale));

        /// <summary>
        /// Returns the scaled height of the Camera
        /// By obtaining the pixelHeight of the camera and taking into account the render scale
        /// The min dimension is 1.
        /// </summary>
        public int scaledHeight => Mathf.Max(1, (int)(camera.pixelHeight * renderScale));


        // NOTE: This is internal instead of private to allow ref return in the old CameraData compatibility property.
        // We can make this private when it is removed.
        //
        // A (non-owning) reference of full writable camera history for internal and injected render passes.
        // Only passes/code executing inside the pipeline should have access.
        // Use the "historyManager" property below to access.
        internal UniversalCameraHistory m_HistoryManager;

        /// <summary>
        /// The camera history texture manager. Used to access camera history from a ScriptableRenderPass.
        /// </summary>
        /// <seealso cref="ScriptableRenderPass"/>
        public UniversalCameraHistory historyManager { get => m_HistoryManager; set => m_HistoryManager = value; }

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
        /// Render scale to apply when creating camera textures. Scaled extents are rounded down to integers.
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
        /// True if this camera can write the alpha channel. Post-processing uses this. Requires the color target to have an alpha channel.
        /// </summary>
        public bool isAlphaOutputEnabled;

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

        // True if GPU occlusion culling should be used when rendering this camera.
        internal bool useGPUOcclusionCulling;

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
        /// True if the camera rendering is for regular in-game.
        /// </summary>
        public bool isGameCamera => cameraType == CameraType.Game;

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
                return true;

            if (cameraType == CameraType.SceneView || cameraType == CameraType.Preview)
                return true;

            var handleID = new RenderTargetIdentifier(handle.nameID, 0, CubemapFace.Unknown, 0);
            bool isBackbuffer = handleID == BuiltinRenderTextureType.CameraTarget || handleID == BuiltinRenderTextureType.Depth;
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

            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618
            if (renderer != null)
                return IsHandleYFlipped(renderer.cameraColorTargetHandle) || targetTexture != null;
            #pragma warning restore CS0618

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
                return true;

            return targetTexture != null || IsHandleYFlipped(color ?? depth);
        }

        /// <summary>
        /// Returns true if temporal anti-aliasing has been requested
        /// Use IsTemporalAAEnabled() to ensure that TAA is active at runtime
        /// </summary>
        /// <returns>True if TAA is requested</returns>
        internal bool IsTemporalAARequested()
        {
            return antialiasing == AntialiasingMode.TemporalAntiAliasing;
        }

        /// <summary>
        /// Returns true if the pipeline and the given camera are configured to render with temporal anti-aliasing post processing enabled
        ///
        /// Once selected, TAA necessitates some pre-requisites from the pipeline to run, mostly from the camera itself.
        /// </summary>
        /// <returns>True if TAA is enabled</returns>
        internal bool IsTemporalAAEnabled()
        {
            UniversalAdditionalCameraData additionalCameraData;
            camera.TryGetComponent(out additionalCameraData);

            return IsTemporalAARequested()                                                                                            // Requested
                   && postProcessEnabled                                                                                              // Postprocessing Enabled
                   && (taaHistory != null)                                                                                            // Initialized
                   && (cameraTargetDescriptor.msaaSamples == 1)                                                                       // No MSAA
                   && !(additionalCameraData?.renderType == CameraRenderType.Overlay || additionalCameraData?.cameraStack.Count > 0)  // No Camera stack
                   && !camera.allowDynamicResolution                                                                                  // No Dynamic Resolution
                   && renderer.SupportsMotionVectors();                                                                               // Motion Vectors implemented
        }

        /// <summary>
        /// Returns true if the STP upscaler has been requested
        /// Use IsSTPEnabled() to ensure that STP upscaler is active at runtime, it necessitates TAA pre-processing
        /// </summary>
        /// <returns>True if STP is requested</returns>
        internal bool IsSTPRequested()
        {
            return (imageScalingMode == ImageScalingMode.Upscaling) && (upscalingFilter == ImageUpscalingFilter.STP);
        }

        /// <summary>
        /// Returns true if the pipeline and the given camera are configured to render with the STP upscaler
        ///
        /// When STP runs, it relies on much of the existing TAA infrastructure provided by URP's native TAA. Due to this, URP forces the anti-aliasing mode to
        /// TAA when STP is requested to ensure that most TAA logic remains active. A side effect of this behavior is that STP inherits all of the same configuration
        /// restrictions as TAA and effectively cannot run if IsTemporalAAEnabled() returns false. The post processing pass logic that executes STP handles this
        /// situation and STP should behave identically to TAA in cases where TAA support requirements aren't met at runtime.
        /// </summary>
        /// <returns>True if STP is enabled</returns>
        internal bool IsSTPEnabled()
        {
            return IsSTPRequested() && IsTemporalAAEnabled();
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
        /// Controls the anti-aliasing mode used by the integrated post-processing stack.
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
        internal TaaHistory taaHistory;

        /// <summary>
        /// The STP history data. It contains both persistent state and textures.
        /// </summary>
        internal StpHistory stpHistory;

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

        ///<inheritdoc/>
        public override void Reset()
        {
            m_ViewMatrix = default;
            m_ProjectionMatrix = default;
            m_JitterMatrix = default;
#if ENABLE_VR && ENABLE_XR_MODULE
            m_CachedRenderIntoTextureXR = false;
            m_InitBuiltinXRConstants = false;
#endif
            camera = null;
            renderType = CameraRenderType.Base;
            targetTexture = null;
            cameraTargetDescriptor = default;
            pixelRect = default;
            useScreenCoordOverride = false;
            screenSizeOverride = default;
            screenCoordScaleBias = default;
            pixelWidth = 0;
            pixelHeight = 0;
            aspectRatio = 0.0f;
            renderScale = 1.0f;
            imageScalingMode = ImageScalingMode.None;
            upscalingFilter = ImageUpscalingFilter.Point;
            fsrOverrideSharpness = false;
            fsrSharpness = 0.0f;
            hdrColorBufferPrecision = HDRColorBufferPrecision._32Bits;
            clearDepth = false;
            cameraType = CameraType.Game;
            isDefaultViewport = false;
            isHdrEnabled = false;
            allowHDROutput = false;
            isAlphaOutputEnabled = false;
            requiresDepthTexture = false;
            requiresOpaqueTexture = false;
            postProcessingRequiresDepthTexture = false;
            xrRendering = false;
            useGPUOcclusionCulling = false;
            defaultOpaqueSortFlags = SortingCriteria.None;
            xr = default;
            maxShadowDistance = 0.0f;
            postProcessEnabled = false;
            captureActions = default;
            volumeLayerMask = 0;
            volumeTrigger = default;
            isStopNaNEnabled = false;
            isDitheringEnabled = false;
            antialiasing = AntialiasingMode.None;
            antialiasingQuality = AntialiasingQuality.Low;
            renderer = null;
            resolveFinalTarget = false;
            worldSpaceCameraPos = default;
            backgroundColor = Color.black;
            taaHistory = null;
            stpHistory = null;
            taaSettings = default;
            baseCamera = null;
            stackAnyPostProcessingEnabled = false;
            stackLastCameraOutputToHDR = false;
        }
    }
}
