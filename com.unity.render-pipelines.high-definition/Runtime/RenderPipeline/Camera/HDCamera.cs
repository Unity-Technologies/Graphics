using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    using AntialiasingMode = HDAdditionalCameraData.AntialiasingMode;

    // This holds all the matrix data we need for rendering, including data from the previous frame
    // (which is the main reason why we need to keep them around for a minimum of one frame).
    // HDCameras are automatically created & updated from a source camera and will be destroyed if
    // not used during a frame.
    public class HDCamera
    {
        [GenerateHLSL(PackingRules.Exact, false)]
        public struct ViewConstants
        {
            public Matrix4x4 viewMatrix;
            public Matrix4x4 invViewMatrix;
            public Matrix4x4 projMatrix;
            public Matrix4x4 invProjMatrix;
            public Matrix4x4 viewProjMatrix;
            public Matrix4x4 invViewProjMatrix;
            public Matrix4x4 nonJitteredViewProjMatrix;

            // View-projection matrix from the previous frame (non-jittered)
            public Matrix4x4 prevViewProjMatrix;
            public Matrix4x4 prevViewProjMatrixNoCameraTrans;

            // Utility matrix (used by sky) to map screen position to WS view direction
            public Matrix4x4 pixelCoordToViewDirWS;

            public Vector3 worldSpaceCameraPos;
            public float pad0;
            public Vector3 worldSpaceCameraPosViewOffset;
            public float pad1;
            public Vector3 prevWorldSpaceCameraPos;
            public float pad2;
        };

        public ViewConstants mainViewConstants;

        public Vector4   screenSize;
        public Frustum   frustum;
        public Vector4[] frustumPlaneEquations;
        public Camera    camera;
        public Vector4   taaJitter;
        public int       taaFrameIndex;
        public Vector2   taaFrameRotation;
        public Vector4   zBufferParams;
        public Vector4   unity_OrthoParams;
        public Vector4   projectionParams;
        public Vector4   screenParams;
        public int       volumeLayerMask;
        public Transform volumeAnchor;
        // This will have the correct viewport position and the size will be full resolution (ie : not taking dynamic rez into account)
        public Rect      finalViewport;
<<<<<<< HEAD
=======

        public RTHandleProperties historyRTHandleProperties { get { return m_HistoryRTSystem.rtHandleProperties; } }
>>>>>>> master

        public bool colorPyramidHistoryIsValid = false;
        public bool volumetricHistoryIsValid   = false; // Contains garbage otherwise
        public int  colorPyramidHistoryMipCount = 0;
<<<<<<< HEAD
        public VolumetricLightingSystem.VBufferParameters[] vBufferParams; // Double-buffered

        public Matrix4x4[]  viewMatrixStereo;
        public Matrix4x4[]  projMatrixStereo;
        // XRTODO: remove once SinglePassInstanced is working
        public Vector4      textureWidthScaling; // (2.0, 0.5) for SinglePassDoubleWide (stereo) and (1.0, 1.0) otherwise

        Matrix4x4[] viewProjStereo;
        Matrix4x4[] invViewStereo;
        Matrix4x4[] invProjStereo;
        Matrix4x4[] invViewProjStereo;
        Vector4[] worldSpaceCameraPosStereo;
        Vector4[] worldSpaceCameraPosStereoEyeOffset;
        Vector4[] prevWorldSpaceCameraPosStereo;

=======
        public VBufferParameters[] vBufferParams; // Double-buffered

        public bool sceneLightingWasDisabledForCamera = false;

        // XR multipass and instanced views are supported (see XRSystem)
        XRPass m_XRPass;
        public XRPass xr { get { return m_XRPass; } }
        public ViewConstants[] xrViewConstants;
        ComputeBuffer xrViewConstantsGpu;

>>>>>>> master
        // Recorder specific
        IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> m_RecorderCaptureActions;
        int m_RecorderTempRT = Shader.PropertyToID("TempRecorder");
        MaterialPropertyBlock m_RecorderPropertyBlock = new MaterialPropertyBlock();

        // Non oblique projection matrix (RHS)
        // TODO: this code is never used and not compatible with XR
        public Matrix4x4 nonObliqueProjMatrix
        {
            get
            {
                return m_AdditionalCameraData != null
                    ? m_AdditionalCameraData.GetNonObliqueProjection(camera)
                    : GeometryUtils.CalculateProjectionMatrix(camera);
            }
        }

        // This is the viewport size actually used for this camera (as it can be altered by VR for example)
        int m_ActualWidth;
        int m_ActualHeight;

<<<<<<< HEAD
        // This is the scale of the camera viewport compared to the reference size of our Render Targets (RTHandle.maxSize)
        Vector2 m_ViewportScaleCurrentFrame;
        Vector2 m_ViewportScalePreviousFrame;
        Vector2 m_ViewportScaleCurrentFrameHistory;
        Vector2 m_ViewportScalePreviousFrameHistory;
=======
>>>>>>> master
        // Current mssa sample
        MSAASamples m_msaaSamples;
        FrameSettings m_frameSettings;

        public int actualWidth { get { return m_ActualWidth; } }
        public int actualHeight { get { return m_ActualHeight; } }
<<<<<<< HEAD
        public Vector2 viewportScale { get { return m_ViewportScaleCurrentFrame; } }
        public Vector2Int viewportSizePrevFrame { get { return m_ViewportSizePrevFrame; } }
        public Vector4 doubleBufferedViewportScale {
            get
            {
                if (HDDynamicResolutionHandler.instance.HardwareDynamicResIsEnabled())
                {
                    return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                }

                return new Vector4(m_ViewportScaleCurrentFrame.x, m_ViewportScaleCurrentFrame.y, m_ViewportScalePreviousFrame.x, m_ViewportScalePreviousFrame.y);
            }
        }
        public Vector4 doubleBufferedViewportScaleHistory
        {
            get
            {
                if (HDDynamicResolutionHandler.instance.HardwareDynamicResIsEnabled())
                {
                    return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
                }

                return new Vector4(m_ViewportScaleCurrentFrameHistory.x, m_ViewportScaleCurrentFrameHistory.y, m_ViewportScalePreviousFrameHistory.x, m_ViewportScalePreviousFrameHistory.y);
            }
        }
=======

>>>>>>> master
        public MSAASamples msaaSamples { get { return m_msaaSamples; } }

        public FrameSettings frameSettings { get { return m_frameSettings; } }

<<<<<<< HEAD
        public Matrix4x4 viewProjMatrix
        {
            get { return projMatrix * viewMatrix; }
        }

        public Matrix4x4 nonJitteredViewProjMatrix
        {
            get { return nonJitteredProjMatrix * viewMatrix; }
        }

        public Matrix4x4 GetViewProjMatrixStereo(uint eyeIndex)
        {
            return (projMatrixStereo[eyeIndex] * viewMatrixStereo[eyeIndex]);
        }

        public Matrix4x4[] prevViewProjMatrixStereo = new Matrix4x4[2];

=======
>>>>>>> master
        // Always true for cameras that just got added to the pool - needed for previous matrices to
        // avoid one-frame jumps/hiccups with temporal effects (motion blur, TAA...)
        public bool isFirstFrame { get; private set; }

        // Ref: An Efficient Depth Linearization Method for Oblique View Frustums, Eq. 6.
        // TODO: pass this as "_ZBufferParams" if the projection matrix is oblique.
        public Vector4 invProjParam
        {
            get
            {
                var p = mainViewConstants.projMatrix;
                return new Vector4(
                    p.m20 / (p.m00 * p.m23),
                    p.m21 / (p.m11 * p.m23),
                    -1f / p.m23,
                    (-p.m22 + p.m20 * p.m02 / p.m00 + p.m21 * p.m12 / p.m11) / p.m23
                    );
            }
        }

        public bool isMainGameView { get { return camera.cameraType == CameraType.Game && camera.targetTexture == null; } }
<<<<<<< HEAD

        // View-projection matrix from the previous frame (non-jittered).
        public Matrix4x4 prevViewProjMatrix;
        public Matrix4x4 prevViewProjMatrixNoCameraTrans;

        // Helper property to inform how many views are rendered simultaneously
        public int viewCount
        {
            get
            {
                if (camera.stereoEnabled && XRGraphics.stereoRenderingMode != XRGraphics.StereoRenderingMode.MultiPass)
                    return 2;

                return 1;
            }
        }

        public int computePassCount
        {
            get
            {
                // XRTODO: double-wide cleanup
                if (camera.stereoEnabled && XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePass)
                    return 1;

                return viewCount;
            }
        }
=======
>>>>>>> master

        // Helper property to inform how many views are rendered simultaneously
        public int viewCount { get => Math.Max(1, xr.viewCount); }

        public bool clearDepth
        {
            get { return m_AdditionalCameraData != null ? m_AdditionalCameraData.clearDepth : camera.clearFlags != CameraClearFlags.Nothing; }
        }

        public HDAdditionalCameraData.ClearColorMode clearColorMode
        {
            get
            {
                if (m_AdditionalCameraData != null)
                {
                    return m_AdditionalCameraData.clearColorMode;
                }

                if (camera.clearFlags == CameraClearFlags.Skybox)
                    return HDAdditionalCameraData.ClearColorMode.Sky;
                else if (camera.clearFlags == CameraClearFlags.SolidColor)
                    return HDAdditionalCameraData.ClearColorMode.Color;
                else // None
                    return HDAdditionalCameraData.ClearColorMode.None;
            }
        }

        public Color backgroundColorHDR
        {
            get
            {
                if (m_AdditionalCameraData != null)
                {
                    return m_AdditionalCameraData.backgroundColorHDR;
                }

                // The scene view has no additional data so this will correctly pick the editor preference backround color here.
                return camera.backgroundColor.linear;
            }
        }

        public HDAdditionalCameraData.FlipYMode flipYMode
        {
            get
            {
                if (m_AdditionalCameraData != null)
                    return m_AdditionalCameraData.flipYMode;
                return HDAdditionalCameraData.FlipYMode.Automatic;
            }
        }

        // This value will always be correct for the current camera, no need to check for
        // game view / scene view / preview in the editor, it's handled automatically
        public AntialiasingMode antialiasing { get; private set; } = AntialiasingMode.None;
<<<<<<< HEAD

        public HDAdditionalCameraData.SMAAQualityLevel SMAAQuality { get; private set; } = HDAdditionalCameraData.SMAAQualityLevel.Medium;


        public bool dithering => m_AdditionalCameraData != null && m_AdditionalCameraData.dithering;

        public bool stopNaNs => m_AdditionalCameraData != null && m_AdditionalCameraData.stopNaNs;

        public HDPhysicalCamera physicalParameters => m_AdditionalCameraData?.physicalParameters;

        public bool invertFaceCulling
            => m_AdditionalCameraData != null && m_AdditionalCameraData.invertFaceCulling;

        public LayerMask probeLayerMask
            => m_AdditionalCameraData != null
            ? m_AdditionalCameraData.probeLayerMask
            : (LayerMask)~0;

        static Dictionary<Camera, HDCamera> s_Cameras = new Dictionary<Camera, HDCamera>();
        static List<Camera> s_Cleanup = new List<Camera>(); // Recycled to reduce GC pressure
=======
>>>>>>> master

        public HDAdditionalCameraData.SMAAQualityLevel SMAAQuality { get; private set; } = HDAdditionalCameraData.SMAAQualityLevel.Medium;


        public bool dithering => m_AdditionalCameraData != null && m_AdditionalCameraData.dithering;

        public bool stopNaNs => m_AdditionalCameraData != null && m_AdditionalCameraData.stopNaNs;

        public HDPhysicalCamera physicalParameters => m_AdditionalCameraData?.physicalParameters;

        public IEnumerable<AOVRequestData> aovRequests =>
            m_AdditionalCameraData != null && !m_AdditionalCameraData.Equals(null)
                ? m_AdditionalCameraData.aovRequests
                : Enumerable.Empty<AOVRequestData>();

        public bool invertFaceCulling
            => m_AdditionalCameraData != null && m_AdditionalCameraData.invertFaceCulling;

        public LayerMask probeLayerMask
            => m_AdditionalCameraData != null
            ? m_AdditionalCameraData.probeLayerMask
            : (LayerMask)~0;

        static Dictionary<(Camera, int), HDCamera> s_Cameras = new Dictionary<(Camera, int), HDCamera>();
        static List<(Camera, int)> s_Cleanup = new List<(Camera, int)>(); // Recycled to reduce GC pressure

        HDAdditionalCameraData m_AdditionalCameraData = null; // Init in Update

        BufferedRTHandleSystem m_HistoryRTSystem = new BufferedRTHandleSystem();

        int m_NumColorPyramidBuffersAllocated = 0;
        int m_NumVolumetricBuffersAllocated   = 0;

        public HDCamera(Camera cam)
        {
            camera = cam;

            frustum = new Frustum();
            frustum.planes = new Plane[6];
            frustum.corners = new Vector3[8];

            frustumPlaneEquations = new Vector4[6];

<<<<<<< HEAD
            viewMatrixStereo = new Matrix4x4[2];
            projMatrixStereo = new Matrix4x4[2];

            viewProjStereo = new Matrix4x4[2];
            invViewStereo = new Matrix4x4[2];
            invProjStereo = new Matrix4x4[2];
            invViewProjStereo = new Matrix4x4[2];

            worldSpaceCameraPosStereo = new Vector4[2];
            worldSpaceCameraPosStereoEyeOffset = new Vector4[2];
            prevWorldSpaceCameraPosStereo = new Vector4[2];

            m_AdditionalCameraData = null; // Init in Update

=======
>>>>>>> master
            Reset();
        }

        public bool IsTAAEnabled()
        {
            return antialiasing == AntialiasingMode.TemporalAntialiasing;
        }

        // Pass all the systems that may want to update per-camera data here.
        // That way you will never update an HDCamera and forget to update the dependent system.
<<<<<<< HEAD
        public void Update(FrameSettings currentFrameSettings, VolumetricLightingSystem vlSys, MSAASamples msaaSamples)
=======
        // NOTE: This function must be called only once per rendering (not frame, as a single camera can be rendered multiple times with different parameters during the same frame)
        // Otherwise, previous frame view constants will be wrong.
        public void Update(FrameSettings currentFrameSettings, HDRenderPipeline hdrp, MSAASamples msaaSamples, XRPass xrPass)
>>>>>>> master
        {
            // store a shortcut on HDAdditionalCameraData (done here and not in the constructor as
            // we don't create HDCamera at every frame and user can change the HDAdditionalData later (Like when they create a new scene).
            m_AdditionalCameraData = camera.GetComponent<HDAdditionalCameraData>();

            m_XRPass = xrPass;
            m_frameSettings = currentFrameSettings;

            UpdateAntialiasing();

            // Handle memory allocation.
            {
                bool isColorPyramidHistoryRequired = m_frameSettings.IsEnabled(FrameSettingsField.SSR); // TODO: TAA as well
<<<<<<< HEAD
                bool isVolumetricHistoryRequired   = m_frameSettings.IsEnabled(FrameSettingsField.Volumetrics) && m_frameSettings.IsEnabled(FrameSettingsField.ReprojectionForVolumetrics);
=======
                bool isVolumetricHistoryRequired = m_frameSettings.IsEnabled(FrameSettingsField.Volumetrics) && m_frameSettings.IsEnabled(FrameSettingsField.ReprojectionForVolumetrics);
>>>>>>> master

                int numColorPyramidBuffersRequired = isColorPyramidHistoryRequired ? 2 : 1; // TODO: 1 -> 0
                int numVolumetricBuffersRequired = isVolumetricHistoryRequired ? 2 : 0; // History + feedback

                if ((m_NumColorPyramidBuffersAllocated != numColorPyramidBuffersRequired) ||
                    (m_NumVolumetricBuffersAllocated != numVolumetricBuffersRequired))
                {
                    // Reinit the system.
                    colorPyramidHistoryIsValid = false;
                    hdrp.DeinitializeVolumetricLightingPerCameraData(this);

                    // The history system only supports the "nuke all" option.
                    m_HistoryRTSystem.Dispose();
                    m_HistoryRTSystem = new BufferedRTHandleSystem();

                    if (numColorPyramidBuffersRequired != 0)
                    {
                        AllocHistoryFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain, HistoryBufferAllocatorFunction, numColorPyramidBuffersRequired);
                        colorPyramidHistoryIsValid = false;
                    }

                    hdrp.InitializeVolumetricLightingPerCameraData(this, numVolumetricBuffersRequired);

                    // Mark as init.
                    m_NumColorPyramidBuffersAllocated = numColorPyramidBuffersRequired;
                    m_NumVolumetricBuffersAllocated = numVolumetricBuffersRequired;
                }
            }

<<<<<<< HEAD
            UpdateViewConstants(IsTAAEnabled());
=======
            // Update viewport
            {
                finalViewport = new Rect(camera.pixelRect.x, camera.pixelRect.y, camera.pixelWidth, camera.pixelHeight);

                if (xr.enabled)
                {
                    // XRTODO: update viewport code once XR SDK is working
                    if (xr.xrSdkEnabled)
                    {
                        finalViewport.x = 0;
                        finalViewport.y = 0;
                        finalViewport.width = xr.renderTargetDesc.width;
                        finalViewport.height = xr.renderTargetDesc.height;
                    }
                    else
                    {
                        // XRTODO: support instanced views with different viewport
                        finalViewport = xr.GetViewport();
                    }
                }

                m_ActualWidth = Math.Max((int)finalViewport.size.x, 1);
                m_ActualHeight = Math.Max((int)finalViewport.size.y, 1);
            }
>>>>>>> master

            Vector2Int nonScaledViewport = new Vector2Int(m_ActualWidth, m_ActualHeight);
            if (isMainGameView)
            {
                Vector2Int scaledSize = HDDynamicResolutionHandler.instance.GetRTHandleScale(new Vector2Int(m_ActualWidth, m_ActualHeight));
                m_ActualWidth = scaledSize.x;
                m_ActualHeight = scaledSize.y;
            }

            Vector2Int nonScaledSize = new Vector2Int(m_ActualWidth, m_ActualHeight);
            if (isMainGameView)
            {
                Vector2Int scaledSize = HDDynamicResolutionHandler.instance.GetRTHandleScale(new Vector2Int(camera.pixelWidth, camera.pixelHeight));
                nonScaledSize = HDDynamicResolutionHandler.instance.cachedOriginalSize;
                m_ActualWidth = scaledSize.x;
                m_ActualHeight = scaledSize.y;
            }

            var screenWidth = m_ActualWidth;
            var screenHeight = m_ActualHeight;
<<<<<<< HEAD

            // XRTODO: double-wide cleanup
            textureWidthScaling = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
            if (camera.stereoEnabled && XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePass)
            {
                Debug.Assert(HDDynamicResolutionHandler.instance.SoftwareDynamicResIsEnabled() == false);

                var xrDesc = XRGraphics.eyeTextureDesc;
                nonScaledSize.x = screenWidth  = m_ActualWidth  = xrDesc.width;
                nonScaledSize.y = screenHeight = m_ActualHeight = xrDesc.height;

                textureWidthScaling = new Vector4(2.0f, 0.5f, 0.0f, 0.0f);
            }

            m_LastFrameActive = Time.frameCount;

            // TODO: cache this, or make the history system spill the beans...
            Vector2Int prevColorPyramidBufferSize = Vector2Int.zero;

            if (numColorPyramidBuffersAllocated > 0)
            {
                var rt = GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain).rt;

                prevColorPyramidBufferSize.x = rt.width;
                prevColorPyramidBufferSize.y = rt.height;
            }

            // TODO: cache this, or make the history system spill the beans...
            Vector3Int prevVolumetricBufferSize = Vector3Int.zero;

            if (numVolumetricBuffersAllocated != 0)
            {
                var rt = GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting).rt;

                prevVolumetricBufferSize.x = rt.width;
                prevVolumetricBufferSize.y = rt.height;
                prevVolumetricBufferSize.z = rt.volumeDepth;
            }

            m_msaaSamples = msaaSamples;
            // Here we use the non scaled resolution for the RTHandleSystem ref size because we assume that at some point we will need full resolution anyway.
            // This is also useful because we have some RT after final up-rez that will need the full size.
            RTHandles.SetReferenceSize(nonScaledSize.x, nonScaledSize.y, m_msaaSamples);
            m_HistoryRTSystem.SetReferenceSize(nonScaledSize.x, nonScaledSize.y, m_msaaSamples);
            m_HistoryRTSystem.Swap();

            Vector3Int currColorPyramidBufferSize = Vector3Int.zero;

            if (numColorPyramidBuffersAllocated != 0)
            {
                var rt = GetCurrentFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain).rt;

                currColorPyramidBufferSize.x = rt.width;
                currColorPyramidBufferSize.y = rt.height;

                if ((currColorPyramidBufferSize.x != prevColorPyramidBufferSize.x) ||
                    (currColorPyramidBufferSize.y != prevColorPyramidBufferSize.y))
                {
                    // A reallocation has happened, so the new texture likely contains garbage.
                    colorPyramidHistoryIsValid = false;
                }
            }

            Vector3Int currVolumetricBufferSize = Vector3Int.zero;

            if (numVolumetricBuffersAllocated != 0)
            {
                var rt = GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricLighting).rt;

                currVolumetricBufferSize.x = rt.width;
                currVolumetricBufferSize.y = rt.height;
                currVolumetricBufferSize.z = rt.volumeDepth;

                if ((currVolumetricBufferSize.x != prevVolumetricBufferSize.x) ||
                    (currVolumetricBufferSize.y != prevVolumetricBufferSize.y) ||
                    (currVolumetricBufferSize.z != prevVolumetricBufferSize.z))
                {
                    // A reallocation has happened, so the new texture likely contains garbage.
                    volumetricHistoryIsValid = false;
                }
            }


            Vector2 rcpTextureSize = Vector2.one / new Vector2(RTHandles.maxWidth, RTHandles.maxHeight);
            Vector2 rcpTextureSizeHistory = Vector2.one / new Vector2(m_HistoryRTSystem.maxWidth, m_HistoryRTSystem.maxHeight);

            m_ViewportScalePreviousFrame = m_ViewportSizePrevFrame * rcpTextureSize;
            m_ViewportScalePreviousFrameHistory = m_ViewportSizePrevFrame * rcpTextureSizeHistory;
            m_ViewportScaleCurrentFrame  = new Vector2Int(m_ActualWidth, m_ActualHeight) * rcpTextureSize;
            m_ViewportScaleCurrentFrameHistory = m_ViewportSizePrevFrame * rcpTextureSizeHistory;

            screenSize = new Vector4(screenWidth, screenHeight, 1.0f / screenWidth, 1.0f / screenHeight);
            screenParams = new Vector4(screenSize.x, screenSize.y, 1 + screenSize.z, 1 + screenSize.w);

            finalViewport = new Rect(camera.pixelRect.x, camera.pixelRect.y, nonScaledSize.x, nonScaledSize.y);

            if (vlSys != null)
            {
                vlSys.UpdatePerCameraData(this);
            }

            UpdateVolumeParameters();

            m_RecorderCaptureActions = CameraCaptureBridge.GetCaptureActions(camera);
        }

        void UpdateAntialiasing()
        {
            // Handle post-process AA
            //  - If post-processing is disabled all together, no AA
            //  - In scene view, only enable TAA if animated materials are enabled
            //  - Else just use the currently set AA mode on the camera
            {
                if (!m_frameSettings.IsEnabled(FrameSettingsField.Postprocess) || !CoreUtils.ArePostProcessesEnabled(camera))
                    antialiasing = AntialiasingMode.None;
#if UNITY_EDITOR
                else if (camera.cameraType == CameraType.SceneView)
                {
                    var mode = HDRenderPipelinePreferences.sceneViewAntialiasing;

                    if (mode == AntialiasingMode.TemporalAntialiasing && !CoreUtils.AreAnimatedMaterialsEnabled(camera))
                        antialiasing = AntialiasingMode.None;
                    else
                        antialiasing = mode;
                }
#endif
                else if (m_AdditionalCameraData != null)
                {
                    antialiasing = m_AdditionalCameraData.antialiasing;
                    if(antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    {
                        SMAAQuality = m_AdditionalCameraData.SMAAQuality;
                    }
                }
                else
                    antialiasing = AntialiasingMode.None;
            }

            if (antialiasing != AntialiasingMode.TemporalAntialiasing)
            {
                taaFrameIndex = 0;
                taaJitter = Vector4.zero;
            }

            // TODO: is this used?
            {
                float t = taaFrameIndex * (0.5f * Mathf.PI);
                taaFrameRotation = new Vector2(Mathf.Sin(t), Mathf.Cos(t));
            }
        }

        internal void UpdateViewConstants(bool jitterProjectionMatrix)
        {
             // If TAA is enabled projMatrix will hold a jittered projection matrix. The original,
            // non-jittered projection matrix can be accessed via nonJitteredProjMatrix.
            var nonJitteredCameraProj = camera.projectionMatrix;
            var cameraProj = jitterProjectionMatrix
                ? GetJitteredProjectionMatrix(nonJitteredCameraProj)
                : nonJitteredCameraProj;

            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(cameraProj, true); // Had to change this from 'false'
            var gpuView = camera.worldToCameraMatrix;
            var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(nonJitteredCameraProj, true);
=======

            m_msaaSamples = msaaSamples;

            screenSize = new Vector4(screenWidth, screenHeight, 1.0f / screenWidth, 1.0f / screenHeight);
            screenParams = new Vector4(screenSize.x, screenSize.y, 1 + screenSize.z, 1 + screenSize.w);

            UpdateAllViewConstants();
            isFirstFrame = false;

            hdrp.UpdateVolumetricLightingPerCameraData(this);

            UpdateVolumeParameters();

            // Here we use the non scaled resolution for the RTHandleSystem ref size because we assume that at some point we will need full resolution anyway.
            // This is necessary because we assume that after post processes, we have the full size render target for debug rendering
            // The only point of calling this here is to grow the render targets. The call in BeginRender will setup the current RTHandle viewport size.
            RTHandles.SetReferenceSize(nonScaledViewport.x, nonScaledViewport.y, m_msaaSamples);
        }

        // Updating RTHandle needs to be done at the beginning of rendering (not during update of HDCamera which happens in batches)
        // The reason is that RTHandle will hold data necessary to setup RenderTargets and viewports properly.
        public void BeginRender()
        {
            RTHandles.SetReferenceSize(m_ActualWidth, m_ActualHeight, m_msaaSamples);
            m_HistoryRTSystem.SwapAndSetReferenceSize(m_ActualWidth, m_ActualHeight, m_msaaSamples);

            m_RecorderCaptureActions = CameraCaptureBridge.GetCaptureActions(camera);
        }
>>>>>>> master

        void UpdateAntialiasing()
        {
            // Handle post-process AA
            //  - If post-processing is disabled all together, no AA
            //  - In scene view, only enable TAA if animated materials are enabled
            //  - Else just use the currently set AA mode on the camera
            {
<<<<<<< HEAD
                for (uint eyeIndex = 0; eyeIndex < 2; eyeIndex++)
=======
                if (!m_frameSettings.IsEnabled(FrameSettingsField.Postprocess) || !CoreUtils.ArePostProcessesEnabled(camera))
                    antialiasing = AntialiasingMode.None;
#if UNITY_EDITOR
                else if (camera.cameraType == CameraType.SceneView)
>>>>>>> master
                {
                    var mode = HDRenderPipelinePreferences.sceneViewAntialiasing;

                    if (mode == AntialiasingMode.TemporalAntialiasing && !CoreUtils.AreAnimatedMaterialsEnabled(camera))
                        antialiasing = AntialiasingMode.None;
                    else
                        antialiasing = mode;
                }
#endif
                else if (m_AdditionalCameraData != null)
                {
                    antialiasing = m_AdditionalCameraData.antialiasing;
                    if(antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    {
                        SMAAQuality = m_AdditionalCameraData.SMAAQuality;
                    }
                }
                else
                    antialiasing = AntialiasingMode.None;
            }

<<<<<<< HEAD
                    // A camera could be rendered multiple times per frame, only updates the previous view proj & pos if needed
                    if (m_LastFrameActive != Time.frameCount)
                    {
                        if (isFirstFrame)
                        {
                            prevWorldSpaceCameraPosStereo[eyeIndex] = gpuCurrViewStereo.inverse.GetColumn(3);
                            prevViewProjMatrixStereo[eyeIndex] = gpuCurrVPStereo;
                        }
                        else
                        {
                            prevWorldSpaceCameraPosStereo[eyeIndex] = worldSpaceCameraPosStereo[eyeIndex];
                            prevViewProjMatrixStereo[eyeIndex] = GetViewProjMatrixStereo(eyeIndex); // Grabbing this before ConfigureStereoMatrices updates view/proj
                        }

                        isFirstFrame = false;
                    }
                }

                // XRTODO: fix this
                isFirstFrame = true; // So that mono vars can still update when stereo active
=======
            if (antialiasing != AntialiasingMode.TemporalAntialiasing)
            {
                taaFrameIndex = 0;
                taaJitter = Vector4.zero;
            }

            // TODO: is this used?
            {
                float t = taaFrameIndex * (0.5f * Mathf.PI);
                taaFrameRotation = new Vector2(Mathf.Sin(t), Mathf.Cos(t));
            }
        }

        void GetXrViewParameters(int xrViewIndex, out Matrix4x4 proj, out Matrix4x4 view, out Vector3 cameraPosition)
        {
            proj = xr.GetProjMatrix(xrViewIndex);
            view = xr.GetViewMatrix(xrViewIndex);
            cameraPosition = view.inverse.GetColumn(3);
        }

        void UpdateAllViewConstants()
        {
            // Allocate or resize view constants buffers
            if (xrViewConstants == null || xrViewConstants.Length != viewCount)
            {
                CoreUtils.SafeRelease(xrViewConstantsGpu);

                xrViewConstants = new ViewConstants[viewCount];
                xrViewConstantsGpu = new ComputeBuffer(viewCount, System.Runtime.InteropServices.Marshal.SizeOf(typeof(ViewConstants)));
            }

            UpdateAllViewConstants(IsTAAEnabled(), true);
        }

        public void UpdateAllViewConstants(bool jitterProjectionMatrix)
        {
            UpdateAllViewConstants(jitterProjectionMatrix, false);
        }

        void UpdateAllViewConstants(bool jitterProjectionMatrix, bool updatePreviousFrameConstants)
        {
            var proj = camera.projectionMatrix;
            var view = camera.worldToCameraMatrix;
            var cameraPosition = camera.transform.position;

            // XR multipass support
            if (xr.enabled && viewCount == 1)
                GetXrViewParameters(0, out proj, out view, out cameraPosition);

            UpdateViewConstants(ref mainViewConstants, proj, view, cameraPosition, jitterProjectionMatrix, updatePreviousFrameConstants);

            // XR instancing support
            if (xr.instancingEnabled)
            {
                for (int viewIndex = 0; viewIndex < viewCount; ++viewIndex)
                {
                    GetXrViewParameters(viewIndex, out proj, out view, out cameraPosition);
                    UpdateViewConstants(ref xrViewConstants[viewIndex], proj, view, cameraPosition, jitterProjectionMatrix, updatePreviousFrameConstants);

                    // Compute offset between the main camera and the instanced views
                    xrViewConstants[viewIndex].worldSpaceCameraPosViewOffset = xrViewConstants[viewIndex].worldSpaceCameraPos - mainViewConstants.worldSpaceCameraPos;
                }
>>>>>>> master
            }
            else
            {
                // Compute shaders always use the XR instancing path due to the lack of multi-compile
                xrViewConstants[0] = mainViewConstants;
            }

            xrViewConstantsGpu.SetData(xrViewConstants);
        }

        void UpdateViewConstants(ref ViewConstants viewConstants, Matrix4x4 projMatrix, Matrix4x4 viewMatrix, Vector3 cameraPosition, bool jitterProjectionMatrix, bool updatePreviousFrameConstants)
        {
             // If TAA is enabled projMatrix will hold a jittered projection matrix. The original,
            // non-jittered projection matrix can be accessed via nonJitteredProjMatrix.
            var nonJitteredCameraProj = projMatrix;
            var cameraProj = jitterProjectionMatrix
                ? GetJitteredProjectionMatrix(nonJitteredCameraProj)
                : nonJitteredCameraProj;

            // The actual projection matrix used in shaders is actually massaged a bit to work across all platforms
            // (different Z value ranges etc.)
            var gpuProj = GL.GetGPUProjectionMatrix(cameraProj, true); // Had to change this from 'false'
            var gpuView = viewMatrix;
            var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(nonJitteredCameraProj, true);

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                // Zero out the translation component.
                gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
            }

            var gpuVP = gpuNonJitteredProj * gpuView;

            // A camera can be rendered multiple times in a single frame with different resolution/fov that would change the projection matrix
            // In this case we need to update previous rendering information.
            // We need to make sure that this code is not called more than once for one camera rendering (not frame! multiple renderings can happen in one frame) otherwise we'd overwrite previous rendering info
            // Note: if your first rendered view during the frame is not the Game view, everything breaks.
            if (updatePreviousFrameConstants)
            {
                if (isFirstFrame)
                {
                    viewConstants.prevWorldSpaceCameraPos = cameraPosition;
                    viewConstants.prevViewProjMatrix = gpuVP;
                }
                else
                {
<<<<<<< HEAD
                    prevWorldSpaceCameraPos = worldSpaceCameraPos;
                    prevViewProjMatrix = nonJitteredViewProjMatrix;
                    prevViewProjMatrixNoCameraTrans = prevViewProjMatrix;
=======
                    viewConstants.prevWorldSpaceCameraPos = viewConstants.worldSpaceCameraPos;
                    viewConstants.prevViewProjMatrix = viewConstants.nonJitteredViewProjMatrix;
                    viewConstants.prevViewProjMatrixNoCameraTrans = viewConstants.prevViewProjMatrix;
>>>>>>> master
                }
            }

<<<<<<< HEAD
            // In stereo, this corresponds to the center eye position
            worldSpaceCameraPos = camera.transform.position;

            viewMatrix = gpuView;
            projMatrix = gpuProj;
            nonJitteredProjMatrix = gpuNonJitteredProj;

            ConfigureStereoMatrices();

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                prevWorldSpaceCameraPos = worldSpaceCameraPos - prevWorldSpaceCameraPos;
                // This fixes issue with cameraDisplacement stacking in prevViewProjMatrix when same camera renders multiple times each logical frame
                // causing glitchy motion blur when editor paused.
                if (m_LastFrameActive != Time.frameCount)
                {
                    Matrix4x4 cameraDisplacement = Matrix4x4.Translate(prevWorldSpaceCameraPos);
                    prevViewProjMatrix *= cameraDisplacement; // Now prevViewProjMatrix correctly transforms this frame's camera-relative positionWS
                }
            }
            else
            {
                Matrix4x4 noTransViewMatrix = camera.worldToCameraMatrix;
                noTransViewMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));
                prevViewProjMatrixNoCameraTrans = nonJitteredProjMatrix * noTransViewMatrix;
=======
            viewConstants.viewMatrix = gpuView;
            viewConstants.invViewMatrix = gpuView.inverse;
            viewConstants.projMatrix = gpuProj;
            viewConstants.invProjMatrix = gpuProj.inverse;
            viewConstants.viewProjMatrix = gpuProj * gpuView;
            viewConstants.invViewProjMatrix = viewConstants.viewProjMatrix.inverse;
            viewConstants.nonJitteredViewProjMatrix = gpuNonJitteredProj * gpuView;
            viewConstants.worldSpaceCameraPos = cameraPosition;
            viewConstants.worldSpaceCameraPosViewOffset = Vector3.zero;
            viewConstants.pixelCoordToViewDirWS = ComputePixelCoordToWorldSpaceViewDirectionMatrix(viewConstants, screenSize);

            if (updatePreviousFrameConstants)
            {
                Vector3 cameraDisplacement = viewConstants.worldSpaceCameraPos - viewConstants.prevWorldSpaceCameraPos;
                viewConstants.prevWorldSpaceCameraPos -= viewConstants.worldSpaceCameraPos; // Make it relative w.r.t. the curr cam pos
                viewConstants.prevViewProjMatrix *= Matrix4x4.Translate(cameraDisplacement); // Now prevViewProjMatrix correctly transforms this frame's camera-relative positionWS
            }
            else
            {
                Matrix4x4 noTransViewMatrix = viewMatrix;
                noTransViewMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));
                viewConstants.prevViewProjMatrixNoCameraTrans = gpuNonJitteredProj * noTransViewMatrix;
>>>>>>> master
            }

            // XRTODO: figure out if the following variables must be in ViewConstants
            float n = camera.nearClipPlane;
            float f = camera.farClipPlane;

            // Analyze the projection matrix.
            // p[2][3] = (reverseZ ? 1 : -1) * (depth_0_1 ? 1 : 2) * (f * n) / (f - n)
            float scale     = viewConstants.projMatrix[2, 3] / (f * n) * (f - n);
            bool  depth_0_1 = Mathf.Abs(scale) < 1.5f;
            bool  reverseZ  = scale > 0;
            bool  flipProj  = viewConstants.invProjMatrix.MultiplyPoint(new Vector3(0, 1, 0)).y < 0;

            // http://www.humus.name/temp/Linearize%20depth.txt
            if (reverseZ)
            {
                zBufferParams = new Vector4(-1 + f / n, 1, -1 / f + 1 / n, 1 / f);
            }
            else
            {
                zBufferParams = new Vector4(1 - f / n, f / n, 1 / f - 1 / n, 1 / n);
            }

            projectionParams = new Vector4(flipProj ? -1 : 1, n, f, 1.0f / f);

            float orthoHeight = camera.orthographic ? 2 * camera.orthographicSize : 0;
            float orthoWidth  = orthoHeight * camera.aspect;
            unity_OrthoParams = new Vector4(orthoWidth, orthoHeight, 0, camera.orthographic ? 1 : 0);

            Frustum.Create(frustum, viewConstants.viewProjMatrix, depth_0_1, reverseZ);

            // Left, right, top, bottom, near, far.
            for (int i = 0; i < 6; i++)
            {
                frustumPlaneEquations[i] = new Vector4(frustum.planes[i].normal.x, frustum.planes[i].normal.y, frustum.planes[i].normal.z, frustum.planes[i].distance);
            }
<<<<<<< HEAD
=======

            m_RecorderCaptureActions = CameraCaptureBridge.GetCaptureActions(camera);
>>>>>>> master
        }

        void UpdateVolumeParameters()
        {
            volumeAnchor = null;
            volumeLayerMask = -1;
            if (m_AdditionalCameraData != null)
            {
                volumeLayerMask = m_AdditionalCameraData.volumeLayerMask;
                volumeAnchor = m_AdditionalCameraData.volumeAnchorOverride;
            }
            else
            {
                // Temporary hack:
                // For scene view, by default, we use the "main" camera volume layer mask if it exists
                // Otherwise we just remove the lighting override layers in the current sky to avoid conflicts
                // This is arbitrary and should be editable in the scene view somehow.
                if (camera.cameraType == CameraType.SceneView)
                {
                    var mainCamera = Camera.main;
                    bool needFallback = true;
                    if (mainCamera != null)
                    {
                        var mainCamAdditionalData = mainCamera.GetComponent<HDAdditionalCameraData>();
                        if (mainCamAdditionalData != null)
                        {
                            volumeLayerMask = mainCamAdditionalData.volumeLayerMask;
                            volumeAnchor = mainCamAdditionalData.volumeAnchorOverride;
                            needFallback = false;
                        }
                    }

                    if (needFallback)
                    {
                        HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                        // If the override layer is "Everything", we fall-back to "Everything" for the current layer mask to avoid issues by having no current layer
                        // In practice we should never have "Everything" as an override mask as it does not make sense (a warning is issued in the UI)
                        if (hdPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask == -1)
                            volumeLayerMask = -1;
                        else
                            volumeLayerMask = (-1 & ~hdPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask);
                    }
                }
            }

            // If no override is provided, use the camera transform.
            if (volumeAnchor == null)
                volumeAnchor = camera.transform;
        }

        public void GetPixelCoordToViewDirWS(Vector4 resolution, ref Matrix4x4[] transforms)
        {
<<<<<<< HEAD
            if (!camera.stereoEnabled)
                return;

            // What constants in UnityPerPass need updating for stereo considerations?
            // _ViewProjMatrix - It is used directly for generating tesselation factors. This should be the same
            //                   across both eyes for consistency, and to keep shadow-generation eye-independent
            // _InvProjParam -   Intention was for generating linear depths, but not currently used.  Will need to be stereo-ized if
            //                   actually needed.
            // _FrustumPlanes -  Also used for generating tesselation factors.  Should be fine to use the combined stereo VP
            //                   to calculate frustum planes.

            // TODO: Would it be worth calculating my own combined view/proj matrix in Update?
            // In engine, we modify the view and proj matrices accordingly in order to generate the single cull
            // * Get the center eye view matrix, and pull it back to cover both eyes
            // * Generated an expanded projection matrix (one method - max bound of left/right proj matrices)
            //   and move near/far planes to match near/far locations of proj matrices located at eyes.
            // I think using the cull matrices is valid, as long as I only use them for tess factors in shader.
            // Using them for other calculations (like light list generation) could be problematic.

            var stereoCombinedViewMatrix = cullingParams.stereoViewMatrix;
            viewMatrix = stereoCombinedViewMatrix;
            var stereoCombinedProjMatrix = cullingParams.stereoProjectionMatrix;
            projMatrix = GL.GetGPUProjectionMatrix(stereoCombinedProjMatrix, true);

            Frustum.Create(frustum, viewProjMatrix, true, true);

            // Left, right, top, bottom, near, far.
            for (int i = 0; i < 6; i++)
=======
            if (xr.instancingEnabled)
            {
                for (int viewIndex = 0; viewIndex < viewCount; ++viewIndex)
                {
                    transforms[viewIndex] = ComputePixelCoordToWorldSpaceViewDirectionMatrix(xrViewConstants[viewIndex], resolution);
                }
            }
            else
>>>>>>> master
            {
                transforms[0] = ComputePixelCoordToWorldSpaceViewDirectionMatrix(mainViewConstants, resolution);
            }
        }

<<<<<<< HEAD
        Matrix4x4 GetJitteredProjectionMatrix(Matrix4x4 origProj)
        {
            // The variance between 0 and the actual halton sequence values reveals noticeable
            // instability in Unity's shadow maps, so we avoid index 0.
            float jitterX = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 3) - 0.5f;
            taaJitter = new Vector4(jitterX, jitterY, jitterX / camera.pixelWidth, jitterY / camera.pixelHeight);

            const int kMaxSampleCount = 8;
            if (++taaFrameIndex >= kMaxSampleCount)
                taaFrameIndex = 0;

            Matrix4x4 proj;

            if (camera.orthographic)
            {
                float vertical = camera.orthographicSize;
                float horizontal = vertical * camera.aspect;

                var offset = taaJitter;
                offset.x *= horizontal / (0.5f * camera.pixelWidth);
                offset.y *= vertical / (0.5f * camera.pixelHeight);

                float left = offset.x - horizontal;
                float right = offset.x + horizontal;
                float top = offset.y + vertical;
                float bottom = offset.y - vertical;

                proj = Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);
            }
            else
            {
                var planes = origProj.decomposeProjection;

                float vertFov = Math.Abs(planes.top) + Math.Abs(planes.bottom);
                float horizFov = Math.Abs(planes.left) + Math.Abs(planes.right);

                var planeJitter = new Vector2(jitterX * horizFov / camera.pixelWidth,
                    jitterY * vertFov / camera.pixelHeight);

                planes.left += planeJitter.x;
                planes.right += planeJitter.x;
                planes.top += planeJitter.y;
                planes.bottom += planeJitter.y;

                proj = Matrix4x4.Frustum(planes);
            }

            return proj;
        }

        void ConfigureStereoMatrices()
=======
        // Stopgap method used to extract stereo combined matrix state.
        public void UpdateStereoDependentState(ref ScriptableCullingParameters cullingParams)
>>>>>>> master
        {
            // XRTODO: remove this after culling management is finished
            if (xr.instancingEnabled)
            {
<<<<<<< HEAD
                for (uint eyeIndex = 0; eyeIndex < 2; eyeIndex++)
                {
                    viewMatrixStereo[eyeIndex] = camera.GetStereoViewMatrix((Camera.StereoscopicEye)eyeIndex);

                    if (ShaderConfig.s_CameraRelativeRendering != 0)
                    {
                        // Use the inverse view matrix to compute eye position
                        worldSpaceCameraPosStereo[eyeIndex] = viewMatrixStereo[eyeIndex].inverse.GetColumn(3);
                        prevWorldSpaceCameraPosStereo[eyeIndex] = worldSpaceCameraPosStereo[eyeIndex] - prevWorldSpaceCameraPosStereo[eyeIndex];

                        // Compute eye to center offset needed for proper shadows in stereo
                        for (int i = 0; i < 3; ++i)
                            worldSpaceCameraPosStereoEyeOffset[eyeIndex][i] = worldSpaceCameraPosStereo[eyeIndex][i] - camera.transform.position[i];

                        // Set translation to 0
                        viewMatrixStereo[eyeIndex].SetColumn(3, new Vector4(0, 0, 0, 1));
                    }

                    invViewStereo[eyeIndex] = viewMatrixStereo[eyeIndex].inverse;
=======
                var view = cullingParams.stereoViewMatrix;
                var proj = cullingParams.stereoProjectionMatrix;

                UpdateViewConstants(ref mainViewConstants, proj, view, cullingParams.origin, IsTAAEnabled(), false);
            }
        }
>>>>>>> master

        // XRTODO: this function should not rely on camera.pixelWidth and camera.pixelHeight
        Matrix4x4 GetJitteredProjectionMatrix(Matrix4x4 origProj)
        {
            // The variance between 0 and the actual halton sequence values reveals noticeable
            // instability in Unity's shadow maps, so we avoid index 0.
            float jitterX = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 3) - 0.5f;
            taaJitter = new Vector4(jitterX, jitterY, jitterX / camera.pixelWidth, jitterY / camera.pixelHeight);

<<<<<<< HEAD
                    viewProjStereo[eyeIndex] = GetViewProjMatrixStereo(eyeIndex);
                    invViewProjStereo[eyeIndex] = viewProjStereo[eyeIndex].inverse;
                }
=======
            const int kMaxSampleCount = 8;
            if (++taaFrameIndex >= kMaxSampleCount)
                taaFrameIndex = 0;

            Matrix4x4 proj;

            if (camera.orthographic)
            {
                float vertical = camera.orthographicSize;
                float horizontal = vertical * camera.aspect;

                var offset = taaJitter;
                offset.x *= horizontal / (0.5f * camera.pixelWidth);
                offset.y *= vertical / (0.5f * camera.pixelHeight);

                float left = offset.x - horizontal;
                float right = offset.x + horizontal;
                float top = offset.y + vertical;
                float bottom = offset.y - vertical;

                proj = Matrix4x4.Ortho(left, right, bottom, top, camera.nearClipPlane, camera.farClipPlane);
>>>>>>> master
            }
            else
            {
                var planes = origProj.decomposeProjection;

                float vertFov = Math.Abs(planes.top) + Math.Abs(planes.bottom);
                float horizFov = Math.Abs(planes.left) + Math.Abs(planes.right);

                var planeJitter = new Vector2(jitterX * horizFov / camera.pixelWidth,
                    jitterY * vertFov / camera.pixelHeight);

                planes.left += planeJitter.x;
                planes.right += planeJitter.x;
                planes.top += planeJitter.y;
                planes.bottom += planeJitter.y;

<<<<<<< HEAD
                worldSpaceCameraPosStereo[0] = worldSpaceCameraPos;
                prevWorldSpaceCameraPosStereo[0] = prevWorldSpaceCameraPos;
            }

            // TODO: Fetch the single cull matrix stuff
=======
                proj = Matrix4x4.Frustum(planes);
            }

            return proj;
        }

        public Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(ViewConstants viewConstants, Vector4 resolution)
        {
            // In XR mode, use a more generic matrix to account for asymmetry in the projection
            if (xr.enabled)
            {
                var transform = Matrix4x4.Scale(new Vector3(-1.0f, -1.0f, -1.0f)) * viewConstants.invViewProjMatrix;
                transform = transform * Matrix4x4.Scale(new Vector3(1.0f, -1.0f, 1.0f));
                transform = transform * Matrix4x4.Translate(new Vector3(-1.0f, -1.0f, 0.0f));
                transform = transform * Matrix4x4.Scale(new Vector3(2.0f * resolution.z, 2.0f * resolution.w, 1.0f));

                return transform.transpose;
            }

#if UNITY_2019_1_OR_NEWER
            float verticalFoV = camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            Vector2 lensShift = camera.GetGateFittedLensShift();
#else
            float verticalFoV = camera.fieldOfView * Mathf.Deg2Rad;
            Vector2 lensShift = Vector2.zero;
#endif

            return HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(verticalFoV, lensShift, resolution, viewConstants.viewMatrix, false);
>>>>>>> master
        }

        // Warning: different views can use the same camera!
        public long GetViewID()
        {
            long viewID = camera.GetInstanceID();
            // Make it positive.
            viewID += (-(long)int.MinValue) + 1;
            return viewID;
        }

        public void Reset()
        {
            isFirstFrame = true;
        }

        public void Dispose()
        {
            if (xrViewConstantsGpu != null)
            {
                xrViewConstantsGpu.Dispose();
                xrViewConstantsGpu = null;
            }

            if (m_HistoryRTSystem != null)
            {
                m_HistoryRTSystem.Dispose();
                m_HistoryRTSystem = null;
            }
        }

        // BufferedRTHandleSystem API expects an allocator function. We define it here.
        static RTHandleSystem.RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            frameIndex &= 1;
            var hdPipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;

<<<<<<< HEAD
            return rtHandleSystem.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: (GraphicsFormat)hdPipeline.currentPlatformRenderPipelineSettings.colorBufferFormat,
                                        enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, xrInstancing: true,
=======
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: (GraphicsFormat)hdPipeline.currentPlatformRenderPipelineSettings.colorBufferFormat,
                                        dimension: TextureXR.dimension, enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, useDynamicScale: true,
>>>>>>> master
                                        name: string.Format("CameraColorBufferMipChain{0}", frameIndex));
        }

        // Pass all the systems that may want to initialize per-camera data here.
        // That way you will never create an HDCamera and forget to initialize the data.
        public static HDCamera GetOrCreate(Camera camera, XRPass xrPass)
        {
            HDCamera hdCamera;

            if (!s_Cameras.TryGetValue((camera, xrPass.multipassId), out hdCamera))
        {
                hdCamera = new HDCamera(camera);
                s_Cameras.Add((camera, xrPass.multipassId), hdCamera);
            }

            return hdCamera;
        }

        public static void ClearAll()
        {
            foreach (var cam in s_Cameras)
            {
                cam.Value.ReleaseHistoryBuffer();
                cam.Value.Dispose();
            }

            s_Cameras.Clear();
            s_Cleanup.Clear();
        }

        // Look for any camera that hasn't been used in the last frame and remove them from the pool.
        public static void CleanUnused()
        {
            foreach (var kvp in s_Cameras)
            {
                // Unfortunately, the scene view camera is always isActiveAndEnabled==false so we can't rely on this. For this reason we never release it (which should be fine in the editor)
                if (kvp.Value.camera != null && kvp.Value.camera.cameraType == CameraType.SceneView)
                    continue;

                if (kvp.Value.camera == null || !kvp.Value.camera.isActiveAndEnabled)
                    s_Cleanup.Add(kvp.Key);
            }

            foreach (var cam in s_Cleanup)
            {
                s_Cameras[cam].Dispose();
                s_Cameras.Remove(cam);
            }

            s_Cleanup.Clear();
        }

        // Set up UnityPerView CBuffer.
<<<<<<< HEAD
        public void SetupGlobalParams(CommandBuffer cmd, float time, float lastTime, uint frameCount)
=======
        public void SetupGlobalParams(CommandBuffer cmd, float time, float lastTime, int frameCount)
>>>>>>> master
        {
            bool taaEnabled = m_frameSettings.IsEnabled(FrameSettingsField.Postprocess)
                && antialiasing == AntialiasingMode.TemporalAntialiasing
                && camera.cameraType == CameraType.Game;

<<<<<<< HEAD
            cmd.SetGlobalMatrix(HDShaderIDs._ViewMatrix,                viewMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewMatrix,             viewMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._ProjMatrix,                projMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvProjMatrix,             projMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._ViewProjMatrix,            viewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewProjMatrix,         viewProjMatrix.inverse);
            cmd.SetGlobalMatrix(HDShaderIDs._NonJitteredViewProjMatrix, nonJitteredViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._PrevViewProjMatrix,        prevViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._CameraViewProjMatrix,      viewProjMatrix);
            cmd.SetGlobalVector(HDShaderIDs._WorldSpaceCameraPos,       worldSpaceCameraPos);
            cmd.SetGlobalVector(HDShaderIDs._PrevCamPosRWS,             prevWorldSpaceCameraPos);
            cmd.SetGlobalVector(HDShaderIDs._ScreenSize,                screenSize);
            cmd.SetGlobalVector(HDShaderIDs._ScreenToTargetScale,       doubleBufferedViewportScale);
            cmd.SetGlobalVector(HDShaderIDs._ScreenToTargetScaleHistory, doubleBufferedViewportScaleHistory);
=======
            cmd.SetGlobalMatrix(HDShaderIDs._ViewMatrix,                mainViewConstants.viewMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewMatrix,             mainViewConstants.invViewMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._ProjMatrix,                mainViewConstants.projMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvProjMatrix,             mainViewConstants.invProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._ViewProjMatrix,            mainViewConstants.viewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewProjMatrix,         mainViewConstants.invViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._NonJitteredViewProjMatrix, mainViewConstants.nonJitteredViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._PrevViewProjMatrix,        mainViewConstants.prevViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._CameraViewProjMatrix,      mainViewConstants.viewProjMatrix);
            cmd.SetGlobalVector(HDShaderIDs._WorldSpaceCameraPos,       mainViewConstants.worldSpaceCameraPos);
            cmd.SetGlobalVector(HDShaderIDs._PrevCamPosRWS,             mainViewConstants.prevWorldSpaceCameraPos);
            cmd.SetGlobalVector(HDShaderIDs._ScreenSize,                screenSize);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScale,             RTHandles.rtHandleProperties.rtHandleScale);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScaleHistory,      m_HistoryRTSystem.rtHandleProperties.rtHandleScale);
>>>>>>> master
            cmd.SetGlobalVector(HDShaderIDs._ZBufferParams,             zBufferParams);
            cmd.SetGlobalVector(HDShaderIDs._ProjectionParams,          projectionParams);
            cmd.SetGlobalVector(HDShaderIDs.unity_OrthoParams,          unity_OrthoParams);
            cmd.SetGlobalVector(HDShaderIDs._ScreenParams,              screenParams);
            cmd.SetGlobalVector(HDShaderIDs._TaaFrameInfo,              new Vector4(taaFrameRotation.x, taaFrameRotation.y, taaFrameIndex, taaEnabled ? 1 : 0));
            cmd.SetGlobalVector(HDShaderIDs._TaaJitterStrength,         taaJitter);
            cmd.SetGlobalVectorArray(HDShaderIDs._FrustumPlanes,        frustumPlaneEquations);

            // Time is also a part of the UnityPerView CBuffer.
            // Different views can have different values of the "Animated Materials" setting.
            bool animateMaterials = CoreUtils.AreAnimatedMaterialsEnabled(camera);

            float  ct = animateMaterials ? time     : 0;
            float  pt = animateMaterials ? lastTime : 0;
            float  dt = Time.deltaTime;
            float sdt = Time.smoothDeltaTime;

            cmd.SetGlobalVector(HDShaderIDs._Time,           new Vector4(ct * 0.05f, ct, ct * 2.0f, ct * 3.0f));
            cmd.SetGlobalVector(HDShaderIDs._LastTime,       new Vector4(pt * 0.05f, pt, pt * 2.0f, pt * 3.0f));
            cmd.SetGlobalVector(HDShaderIDs.unity_DeltaTime, new Vector4(dt, 1.0f / dt, sdt, 1.0f / sdt));
            cmd.SetGlobalVector(HDShaderIDs._SinTime,        new Vector4(Mathf.Sin(ct * 0.125f), Mathf.Sin(ct * 0.25f), Mathf.Sin(ct * 0.5f), Mathf.Sin(ct)));
            cmd.SetGlobalVector(HDShaderIDs._CosTime,        new Vector4(Mathf.Cos(ct * 0.125f), Mathf.Cos(ct * 0.25f), Mathf.Cos(ct * 0.5f), Mathf.Cos(ct)));
            cmd.SetGlobalInt(HDShaderIDs._FrameCount,        frameCount);

<<<<<<< HEAD

            // TODO VR: Current solution for compute shaders grabs matrices from
            // stereo matrices even when not rendering stereo in order to reduce shader variants.
            // After native fix for compute shader keywords is completed, qualify this with stereoEnabled.
            SetupGlobalStereoParams(cmd);
        }

        public void SetupGlobalStereoParams(CommandBuffer cmd)
        {

            // corresponds to UnityPerPassStereo
            // TODO: Migrate the other stereo matrices to HDRP-managed UnityPerPassStereo?
            cmd.SetGlobalMatrixArray(HDShaderIDs._ViewMatrixStereo, viewMatrixStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._ProjMatrixStereo, projMatrixStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._ViewProjMatrixStereo, viewProjStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._InvViewMatrixStereo, invViewStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._InvProjMatrixStereo, invProjStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._InvViewProjMatrixStereo, invViewProjStereo);
            cmd.SetGlobalMatrixArray(HDShaderIDs._PrevViewProjMatrixStereo, prevViewProjMatrixStereo);
            cmd.SetGlobalVectorArray(HDShaderIDs._WorldSpaceCameraPosStereo, worldSpaceCameraPosStereo);
            cmd.SetGlobalVectorArray(HDShaderIDs._WorldSpaceCameraPosStereoEyeOffset, worldSpaceCameraPosStereoEyeOffset);
            cmd.SetGlobalVectorArray(HDShaderIDs._PrevCamPosRWSStereo, prevWorldSpaceCameraPosStereo);
            cmd.SetGlobalVector(HDShaderIDs._TextureWidthScaling, textureWidthScaling);
=======
            // TODO: qualify this code with xrInstancingEnabled when compute shaders can use keywords
            cmd.SetGlobalInt(HDShaderIDs._XRViewCount, viewCount);
            cmd.SetGlobalBuffer(HDShaderIDs._XRViewConstants, xrViewConstantsGpu);
>>>>>>> master
        }

        public RTHandleSystem.RTHandle GetPreviousFrameRT(int id)
        {
            return m_HistoryRTSystem.GetFrameRT(id, 1);
        }

        public RTHandleSystem.RTHandle GetCurrentFrameRT(int id)
        {
            return m_HistoryRTSystem.GetFrameRT(id, 0);
        }

        // Allocate buffers frames and return current frame
        public RTHandleSystem.RTHandle AllocHistoryFrameRT(int id, Func<string, int, RTHandleSystem, RTHandleSystem.RTHandle> allocator, int bufferCount)
        {
            m_HistoryRTSystem.AllocBuffer(id, (rts, i) => allocator(camera.name, i, rts), bufferCount);
            return m_HistoryRTSystem.GetFrameRT(id, 0);
        }

        void ReleaseHistoryBuffer()
        {
            m_HistoryRTSystem.ReleaseAll();
        }

        public void ExecuteCaptureActions(RTHandleSystem.RTHandle input, CommandBuffer cmd)
        {
            if (m_RecorderCaptureActions == null || !m_RecorderCaptureActions.MoveNext())
                return;

            // We need to blit to an intermediate texture because input resolution can be bigger than the camera resolution
            // Since recorder does not know about this, we need to send a texture of the right size.
            cmd.GetTemporaryRT(m_RecorderTempRT, actualWidth, actualHeight, 0, FilterMode.Point, input.rt.graphicsFormat);

            var blitMaterial = HDUtils.GetBlitMaterial(TextureDimension.Tex2D);

<<<<<<< HEAD
=======
            var rtHandleScale = RTHandles.rtHandleProperties.rtHandleScale;
            Vector2 viewportScale = new Vector2(rtHandleScale.x, rtHandleScale.y);

>>>>>>> master
            m_RecorderPropertyBlock.SetTexture(HDShaderIDs._BlitTexture, input);
            m_RecorderPropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, viewportScale);
            m_RecorderPropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
            cmd.SetRenderTarget(m_RecorderTempRT);
            cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 0, MeshTopology.Triangles, 3, 1, m_RecorderPropertyBlock);

            for (m_RecorderCaptureActions.Reset(); m_RecorderCaptureActions.MoveNext();)
                m_RecorderCaptureActions.Current(m_RecorderTempRT, cmd);
        }
    }
}
