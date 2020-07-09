using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    using AntialiasingMode = HDAdditionalCameraData.AntialiasingMode;

    // This holds all the matrix data we need for rendering, including data from the previous frame
    // (which is the main reason why we need to keep them around for a minimum of one frame).
    // HDCameras are automatically created & updated from a source camera and will be destroyed if
    // not used during a frame.

    /// <summary>
    /// HDCamera class.
    /// This class holds all information for a given camera. Constants used for shading as well as buffers persistent from one frame to another etc.
    /// </summary>
    [DebuggerDisplay("({camera.name})")]
    public class HDCamera
    {
        #region Public API
        /// <summary>
        /// Structure containing all shader view related constants for this camera.
        /// </summary>
        public struct ViewConstants
        {
            /// <summary>View matrix.</summary>
            public Matrix4x4 viewMatrix;
            /// <summary>Inverse View matrix.</summary>
            public Matrix4x4 invViewMatrix;
            /// <summary>Projection matrix.</summary>
            public Matrix4x4 projMatrix;
            /// <summary>Inverse Projection matrix.</summary>
            public Matrix4x4 invProjMatrix;
            /// <summary>View Projection matrix.</summary>
            public Matrix4x4 viewProjMatrix;
            /// <summary>Inverse View Projection matrix.</summary>
            public Matrix4x4 invViewProjMatrix;
            /// <summary>Non-jittered View Projection matrix.</summary>
            public Matrix4x4 nonJitteredViewProjMatrix;
            /// <summary>Non-jittered View Projection matrix from previous frame.</summary>
            public Matrix4x4 prevViewProjMatrix;
            /// <summary>Non-jittered Inverse View Projection matrix from previous frame.</summary>
            public Matrix4x4 prevInvViewProjMatrix;
            /// <summary>Non-jittered View Projection matrix from previous frame without translation.</summary>
            public Matrix4x4 prevViewProjMatrixNoCameraTrans;

            /// <summary>Utility matrix (used by sky) to map screen position to WS view direction.</summary>
            public Matrix4x4 pixelCoordToViewDirWS;

            // We need this to track the previous VP matrix with camera translation excluded. Internal since it is used only in its "previous" form
            internal Matrix4x4 viewProjectionNoCameraTrans;

            /// <summary>World Space camera position.</summary>
            public Vector3 worldSpaceCameraPos;
            internal float pad0;
            /// <summary>Offset from the main view position for stereo view constants.</summary>
            public Vector3 worldSpaceCameraPosViewOffset;
            internal float pad1;
            /// <summary>World Space camera position from previous frame.</summary>
            public Vector3 prevWorldSpaceCameraPos;
            internal float pad2;
        };

        /// <summary>
        /// Screen resolution information.
        /// Width, height, inverse width, inverse height.
        /// </summary>
        public Vector4              screenSize;
        /// <summary>Camera frustum.</summary>
        public Frustum              frustum;
        /// <summary>Camera component.</summary>
        public Camera               camera;
        /// <summary>TAA jitter information.</summary>
        public Vector4              taaJitter;
        /// <summary>View constants.</summary>
        public ViewConstants        mainViewConstants;
        /// <summary>Color pyramid history buffer state.</summary>
        public bool                 colorPyramidHistoryIsValid = false;
        /// <summary>Volumetric history buffer state.</summary>
        public bool                 volumetricHistoryIsValid = false;

        /// <summary>Width actually used for rendering after dynamic resolution and XR is applied.</summary>
        public int                  actualWidth { get; private set; }
        /// <summary>Height actually used for rendering after dynamic resolution and XR is applied.</summary>
        public int                  actualHeight { get; private set; }
        /// <summary>Number of MSAA samples used for this frame.</summary>
        public MSAASamples          msaaSamples { get; private set; }
        /// <summary>Frame settings for this camera.</summary>
        public FrameSettings        frameSettings { get; private set; }
        /// <summary>RTHandle properties for the camera history buffers.</summary>
        public RTHandleProperties   historyRTHandleProperties { get { return m_HistoryRTSystem.rtHandleProperties; } }
        /// <summary>Volume stack used for this camera.</summary>
        public VolumeStack          volumeStack { get; private set; }
        /// <summary>Current time for this camera.</summary>
        public float                time; // Take the 'animateMaterials' setting into account.

        // Pass all the systems that may want to initialize per-camera data here.
        // That way you will never create an HDCamera and forget to initialize the data.
        /// <summary>
        /// Get the existing HDCamera for the provided camera or create a new if it does not exist yet.
        /// </summary>
        /// <param name="camera">Camera for which the HDCamera is needed.</param>
        /// <param name="xrMultipassId">XR multi-pass Id.</param>
        /// <returns></returns>
        public static HDCamera GetOrCreate(Camera camera, int xrMultipassId = 0)
        {
            HDCamera hdCamera;

            if (!s_Cameras.TryGetValue((camera, xrMultipassId), out hdCamera))
            {
                hdCamera = new HDCamera(camera);
                s_Cameras.Add((camera, xrMultipassId), hdCamera);
            }

            return hdCamera;
        }

        /// <summary>
        /// Reset the camera persistent informations.
        /// This needs to be used when doing camera cuts for example in order to avoid information from previous unrelated frames to be used.
        /// </summary>
        public void Reset()
        {
            isFirstFrame = true;
            cameraFrameCount = 0;
            resetPostProcessingHistory = true;
        }

        /// <summary>
        /// Allocates a history RTHandle with the unique identifier id.
        /// </summary>
        /// <param name="id">Unique id for this history buffer.</param>
        /// <param name="allocator">Allocator function for the history RTHandle.</param>
        /// <param name="bufferCount">Number of buffer that should be allocated.</param>
        /// <returns>A new RTHandle.</returns>
        public RTHandle AllocHistoryFrameRT(int id, Func<string, int, RTHandleSystem, RTHandle> allocator, int bufferCount)
        {
            m_HistoryRTSystem.AllocBuffer(id, (rts, i) => allocator(camera.name, i, rts), bufferCount);
            return m_HistoryRTSystem.GetFrameRT(id, 0);
        }

        /// <summary>
        /// Returns the id RTHandle from the previous frame.
        /// </summary>
        /// <param name="id">Id of the history RTHandle.</param>
        /// <returns>The RTHandle from previous frame.</returns>
        public RTHandle GetPreviousFrameRT(int id)
        {
            return m_HistoryRTSystem.GetFrameRT(id, 1);
        }

        /// <summary>
        /// Returns the id RTHandle of the current frame.
        /// </summary>
        /// <param name="id">Id of the history RTHandle.</param>
        /// <returns>The RTHandle of the current frame.</returns>
        public RTHandle GetCurrentFrameRT(int id)
        {
            return m_HistoryRTSystem.GetFrameRT(id, 0);
        }
        #endregion

        #region Internal API
        internal struct ShadowHistoryUsage
        {
            public int lightInstanceID;
            public uint frameCount;
            public GPULightType lightType;
        }

        internal Vector4[]              frustumPlaneEquations;
        internal int                    taaFrameIndex;
        internal float                  taaSharpenStrength;
        internal Vector4                zBufferParams;
        internal Vector4                unity_OrthoParams;
        internal Vector4                projectionParams;
        internal Vector4                screenParams;
        internal int                    volumeLayerMask;
        internal Transform              volumeAnchor;
        internal Rect                   finalViewport; // This will have the correct viewport position and the size will be full resolution (ie : not taking dynamic rez into account)
        internal int                    colorPyramidHistoryMipCount = 0;
        internal VBufferParameters[]    vBufferParams; // Double-buffered; needed even if reprojection is off
        // Currently the frame count is not increase every render, for ray tracing shadow filtering. We need to have a number that increases every render
        internal uint                   cameraFrameCount = 0;
        internal bool                   animateMaterials;
        internal float                  lastTime;
        internal Camera                 parentCamera = null; // Used for recursive rendering, e.g. a reflection in a scene view.

        // This property is ray tracing specific. It allows us to track for the RayTracingShadow history which light was using which slot.
        // This avoid ghosting and many other problems that may happen due to an unwanted history usge
        internal ShadowHistoryUsage[]   shadowHistoryUsage = null;

        internal SkyUpdateContext       m_LightingOverrideSky = new SkyUpdateContext();

        // VisualSky is the sky used for rendering in the main view.
        // LightingSky is the sky used for lighting the scene (ambient probe and sky reflection)
        // It's usually the visual sky unless a sky lighting override is setup.
        //      Ambient Probe: Only used if Ambient Mode is set to dynamic in the Visual Environment component. Updated according to the Update Mode parameter.
        //      (Otherwise it uses the one from the static lighting sky)
        //      Sky Reflection Probe : Always used and updated according to the Update Mode parameter.
        internal SkyUpdateContext       visualSky { get; private set; } = new SkyUpdateContext();
        internal SkyUpdateContext       lightingSky { get; private set; } = null;
        // We need to cache this here because it's need in SkyManager.SetupAmbientProbe
        // The issue is that this is called during culling which happens before Volume updates so we can't query it via volumes in there.
        internal SkyAmbientMode         skyAmbientMode { get; private set; }

        // XR multipass and instanced views are supported (see XRSystem)
        internal XRPass xr { get; private set; }

        // Non oblique projection matrix (RHS)
        // TODO: this code is never used and not compatible with XR
        internal Matrix4x4 nonObliqueProjMatrix
        {
            get
            {
                return m_AdditionalCameraData != null
                    ? m_AdditionalCameraData.GetNonObliqueProjection(camera)
                    : GeometryUtils.CalculateProjectionMatrix(camera);
            }
        }

        // Always true for cameras that just got added to the pool - needed for previous matrices to
        // avoid one-frame jumps/hiccups with temporal effects (motion blur, TAA...)
        internal bool isFirstFrame { get; private set; }

        internal bool isMainGameView { get { return camera.cameraType == CameraType.Game && camera.targetTexture == null; } }

        // Helper property to inform how many views are rendered simultaneously
        internal int viewCount { get => Math.Max(1, xr.viewCount); }

        internal bool clearDepth
        {
            get { return m_AdditionalCameraData != null ? m_AdditionalCameraData.clearDepth : camera.clearFlags != CameraClearFlags.Nothing; }
        }

        internal HDAdditionalCameraData.ClearColorMode clearColorMode
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

        HDAdditionalCameraData.ClearColorMode m_PreviousClearColorMode = HDAdditionalCameraData.ClearColorMode.None;


        internal Color backgroundColorHDR
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

        internal HDAdditionalCameraData.FlipYMode flipYMode
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
        internal AntialiasingMode antialiasing { get; private set; } = AntialiasingMode.None;

        internal HDAdditionalCameraData.SMAAQualityLevel SMAAQuality { get; private set; } = HDAdditionalCameraData.SMAAQualityLevel.Medium;

        internal bool resetPostProcessingHistory = true;

        internal bool dithering => m_AdditionalCameraData != null && m_AdditionalCameraData.dithering;

        internal bool stopNaNs => m_AdditionalCameraData != null && m_AdditionalCameraData.stopNaNs;

        internal HDPhysicalCamera physicalParameters { get; private set; }

        internal IEnumerable<AOVRequestData> aovRequests =>
            m_AdditionalCameraData != null && !m_AdditionalCameraData.Equals(null)
                ? m_AdditionalCameraData.aovRequests
                : Enumerable.Empty<AOVRequestData>();

        internal LayerMask probeLayerMask
            => m_AdditionalCameraData != null
            ? m_AdditionalCameraData.probeLayerMask
            : (LayerMask)~0;

        internal float probeRangeCompressionFactor
            => m_AdditionalCameraData != null
            ? m_AdditionalCameraData.probeCustomFixedExposure
            : 1.0f;

        internal bool ValidShadowHistory(HDAdditionalLightData lightData, int screenSpaceShadowIndex, GPULightType lightType)
        {
            return shadowHistoryUsage[screenSpaceShadowIndex].lightInstanceID == lightData.GetInstanceID()
                    && (shadowHistoryUsage[screenSpaceShadowIndex].frameCount == (cameraFrameCount - 1))
                    && (shadowHistoryUsage[screenSpaceShadowIndex].lightType == lightType);
        }

        internal void PropagateShadowHistory(HDAdditionalLightData lightData, int screenSpaceShadowIndex, GPULightType lightType)
        {
            shadowHistoryUsage[screenSpaceShadowIndex].lightInstanceID = lightData.GetInstanceID();
            shadowHistoryUsage[screenSpaceShadowIndex].frameCount = cameraFrameCount;
            shadowHistoryUsage[screenSpaceShadowIndex].lightType = lightType;
        }

        internal ProfilingSampler profilingSampler => m_AdditionalCameraData?.profilingSampler ?? ProfilingSampler.Get(HDProfileId.HDRenderPipelineRenderCamera);

        internal HDCamera(Camera cam)
        {
            camera = cam;

            frustum = new Frustum();
            frustum.planes = new Plane[6];
            frustum.corners = new Vector3[8];

            frustumPlaneEquations = new Vector4[6];

            volumeStack = VolumeManager.instance.CreateStack();

            Reset();
        }

        internal bool IsTAAEnabled()
        {
            return antialiasing == AntialiasingMode.TemporalAntialiasing;
        }

        internal bool IsSSREnabled()
        {
            var ssr = volumeStack.GetComponent<ScreenSpaceReflection>();
            return frameSettings.IsEnabled(FrameSettingsField.SSR) && ssr.enabled.value;
        }

        internal bool IsVolumetricReprojectionEnabled()
        {
            bool a = Fog.IsVolumetricFogEnabled(this);
            bool b = frameSettings.IsEnabled(FrameSettingsField.ReprojectionForVolumetrics);
            bool c = camera.cameraType == CameraType.Game;
            bool d = Application.isPlaying;

            return a && b && c && d;
        }

        // Pass all the systems that may want to update per-camera data here.
        // That way you will never update an HDCamera and forget to update the dependent system.
        // NOTE: This function must be called only once per rendering (not frame, as a single camera can be rendered multiple times with different parameters during the same frame)
        // Otherwise, previous frame view constants will be wrong.
        internal void Update(FrameSettings currentFrameSettings, HDRenderPipeline hdrp, MSAASamples newMSAASamples, XRPass xrPass)
        {
            // Inherit animation settings from the parent camera.
            Camera aniCam = (parentCamera != null) ? parentCamera : camera;

            // Different views/tabs may have different values of the "Animated Materials" setting.
            animateMaterials = CoreUtils.AreAnimatedMaterialsEnabled(aniCam);

            time = animateMaterials ? hdrp.GetTime() : 0;
            lastTime = animateMaterials ? hdrp.GetLastTime() : 0;

            // Make sure that the shadow history identification array is allocated and is at the right size
            if (shadowHistoryUsage == null || shadowHistoryUsage.Length != hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots)
            {
                shadowHistoryUsage = new ShadowHistoryUsage[hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots];
            }

            // store a shortcut on HDAdditionalCameraData (done here and not in the constructor as
            // we don't create HDCamera at every frame and user can change the HDAdditionalData later (Like when they create a new scene).
            camera.TryGetComponent<HDAdditionalCameraData>(out m_AdditionalCameraData);

            UpdateVolumeAndPhysicalParameters();

            xr = xrPass;
            frameSettings = currentFrameSettings;

            UpdateAntialiasing();

            // Handle memory allocation.
            {
                // Have to do this every frame in case the settings have changed.
                // The condition inside controls whether we perform init/deinit or not.
                hdrp.ReinitializeVolumetricBufferParams(this);

                bool isCurrentColorPyramidRequired = frameSettings.IsEnabled(FrameSettingsField.Refraction) || frameSettings.IsEnabled(FrameSettingsField.Distortion);
                bool isHistoryColorPyramidRequired = IsSSREnabled() || antialiasing == AntialiasingMode.TemporalAntialiasing;
                bool isVolumetricHistoryRequired = IsVolumetricReprojectionEnabled();

                int numColorPyramidBuffersRequired = 0;
                if (isCurrentColorPyramidRequired)
                    numColorPyramidBuffersRequired = 1;
                if (isHistoryColorPyramidRequired) // Superset of case above
                    numColorPyramidBuffersRequired = 2;

                int numVolumetricBuffersRequired = isVolumetricHistoryRequired ? 2 : 0; // History + feedback

                if ((m_NumColorPyramidBuffersAllocated != numColorPyramidBuffersRequired) ||
                    (m_NumVolumetricBuffersAllocated != numVolumetricBuffersRequired))
                {
                    // Reinit the system.
                    colorPyramidHistoryIsValid = false;
                    volumetricHistoryIsValid = false;
                    // Since we nuke all history we must inform the post process system too.
                    resetPostProcessingHistory = true;

                    // The history system only supports the "nuke all" option.
                    m_HistoryRTSystem.Dispose();
                    m_HistoryRTSystem = new BufferedRTHandleSystem();

                    if (numColorPyramidBuffersRequired != 0)
                    {
                        AllocHistoryFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain, HistoryBufferAllocatorFunction, numColorPyramidBuffersRequired);
                    }

                    if (numVolumetricBuffersRequired != 0)
                    {
                        hdrp.AllocateVolumetricHistoryBuffers(this, numVolumetricBuffersRequired);
                    }

                    // Mark as init.
                    m_NumColorPyramidBuffersAllocated = numColorPyramidBuffersRequired;
                    m_NumVolumetricBuffersAllocated   = numVolumetricBuffersRequired;
                }
            }

            // Update viewport
            {
                if (xr.enabled)
                {
                    finalViewport = xr.GetViewport();
                }
                else
                {
                    finalViewport = new Rect(camera.pixelRect.x, camera.pixelRect.y, camera.pixelWidth, camera.pixelHeight);
                }

                actualWidth = Math.Max((int)finalViewport.size.x, 1);
                actualHeight = Math.Max((int)finalViewport.size.y, 1);
            }

            Vector2Int nonScaledViewport = new Vector2Int(actualWidth, actualHeight);
            if (isMainGameView)
            {
                Vector2Int scaledSize = DynamicResolutionHandler.instance.GetScaledSize(new Vector2Int(actualWidth, actualHeight));
                actualWidth = scaledSize.x;
                actualHeight = scaledSize.y;
            }

            var screenWidth = actualWidth;
            var screenHeight = actualHeight;

            msaaSamples = newMSAASamples;

            screenSize = new Vector4(screenWidth, screenHeight, 1.0f / screenWidth, 1.0f / screenHeight);
            screenParams = new Vector4(screenSize.x, screenSize.y, 1 + screenSize.z, 1 + screenSize.w);

            const int kMaxSampleCount = 8;
            if (++taaFrameIndex >= kMaxSampleCount)
                taaFrameIndex = 0;

            UpdateAllViewConstants();
            isFirstFrame = false;
            cameraFrameCount++;

            hdrp.UpdateVolumetricBufferParams(this);
        }

        // Updating RTHandle needs to be done at the beginning of rendering (not during update of HDCamera which happens in batches)
        // The reason is that RTHandle will hold data necessary to setup RenderTargets and viewports properly.
        internal void BeginRender(CommandBuffer cmd)
        {
            RTHandles.SetReferenceSize(actualWidth, actualHeight, msaaSamples);
            m_HistoryRTSystem.SwapAndSetReferenceSize(actualWidth, actualHeight, msaaSamples);

            m_RecorderCaptureActions = CameraCaptureBridge.GetCaptureActions(camera);

            SetupCurrentMaterialQuality(cmd);
        }

        internal void UpdateAllViewConstants(bool jitterProjectionMatrix)
        {
            UpdateAllViewConstants(jitterProjectionMatrix, false);
        }

        /// <param name="aspect">
        /// The aspect ratio to use.
        /// if negative, then the aspect ratio of <paramref name="resolution"/> will be used.
        /// It is different from the aspect ratio of <paramref name="resolution"/> for anamorphic projections.
        /// </param>
        internal void GetPixelCoordToViewDirWS(Vector4 resolution, float aspect, ref Matrix4x4[] transforms)
        {
            if (xr.singlePassEnabled)
            {
                for (int viewIndex = 0; viewIndex < viewCount; ++viewIndex)
                {
                    transforms[viewIndex] = ComputePixelCoordToWorldSpaceViewDirectionMatrix(m_XRViewConstants[viewIndex], resolution, aspect);
                }
            }
            else
            {
                transforms[0] = ComputePixelCoordToWorldSpaceViewDirectionMatrix(mainViewConstants, resolution, aspect);
            }
        }

        internal static void ClearAll()
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
        internal static void CleanUnused()
        {

            foreach (var key in s_Cameras.Keys)
            {
                var camera = s_Cameras[key];

                // Unfortunately, the scene view camera is always isActiveAndEnabled==false so we can't rely on this. For this reason we never release it (which should be fine in the editor)
                if (camera.camera != null && camera.camera.cameraType == CameraType.SceneView)
                    continue;

                bool hasPersistentHistory = camera.m_AdditionalCameraData != null && camera.m_AdditionalCameraData.hasPersistentHistory;
                // We keep preview camera around as they are generally disabled/enabled every frame. They will be destroyed later when camera.camera is null
                if (camera.camera == null || (!camera.camera.isActiveAndEnabled && camera.camera.cameraType != CameraType.Preview && !hasPersistentHistory))
                    s_Cleanup.Add(key);
            }

            foreach (var cam in s_Cleanup)
            {
                s_Cameras[cam].Dispose();
                s_Cameras.Remove(cam);
            }

            s_Cleanup.Clear();
        }

        internal static void ResetAllHistoryRTHandleSystems(int width, int height)
        {
            foreach (var kvp in s_Cameras)
            {
                var hdCamera = kvp.Value;
                var currentHistorySize = hdCamera.m_HistoryRTSystem.rtHandleProperties.currentRenderTargetSize;
                // We only reset if the new size if smaller than current reference (otherwise we might increase the size of off screen camera with lower resolution than the new reference.
                if (width < currentHistorySize.x || height < currentHistorySize.y)
                {
                    hdCamera.m_HistoryRTSystem.ResetReferenceSize(width, height);
                }
            }
        }

        // Set up UnityPerView CBuffer.
        internal void SetupGlobalParams(CommandBuffer cmd, int frameCount)
        {
            bool taaEnabled = frameSettings.IsEnabled(FrameSettingsField.Postprocess)
                && antialiasing == AntialiasingMode.TemporalAntialiasing
                && camera.cameraType == CameraType.Game;

            cmd.SetGlobalMatrix(HDShaderIDs._ViewMatrix, mainViewConstants.viewMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewMatrix, mainViewConstants.invViewMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._ProjMatrix, mainViewConstants.projMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvProjMatrix, mainViewConstants.invProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._ViewProjMatrix, mainViewConstants.viewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._InvViewProjMatrix, mainViewConstants.invViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._NonJitteredViewProjMatrix, mainViewConstants.nonJitteredViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._PrevViewProjMatrix, mainViewConstants.prevViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._PrevInvViewProjMatrix, mainViewConstants.prevInvViewProjMatrix);
            cmd.SetGlobalMatrix(HDShaderIDs._CameraViewProjMatrix, mainViewConstants.viewProjMatrix);
            cmd.SetGlobalVector(HDShaderIDs._WorldSpaceCameraPos, mainViewConstants.worldSpaceCameraPos);
            cmd.SetGlobalVector(HDShaderIDs._PrevCamPosRWS, mainViewConstants.prevWorldSpaceCameraPos);
            cmd.SetGlobalVector(HDShaderIDs._ScreenSize, screenSize);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScale, RTHandles.rtHandleProperties.rtHandleScale);
            cmd.SetGlobalVector(HDShaderIDs._RTHandleScaleHistory, m_HistoryRTSystem.rtHandleProperties.rtHandleScale);
            cmd.SetGlobalVector(HDShaderIDs._ZBufferParams, zBufferParams);
            cmd.SetGlobalVector(HDShaderIDs._ProjectionParams, projectionParams);
            cmd.SetGlobalVector(HDShaderIDs.unity_OrthoParams, unity_OrthoParams);
            cmd.SetGlobalVector(HDShaderIDs._ScreenParams, screenParams);
            cmd.SetGlobalVector(HDShaderIDs._TaaFrameInfo, new Vector4(taaSharpenStrength, 0, taaFrameIndex, taaEnabled ? 1 : 0));
            cmd.SetGlobalVector(HDShaderIDs._TaaJitterStrength, taaJitter);
            cmd.SetGlobalInt(HDShaderIDs._FrameCount, frameCount);
            cmd.SetGlobalVectorArray(HDShaderIDs._FrustumPlanes, frustumPlaneEquations);

            float ct = time;
            float pt = lastTime;
            float dt = Time.deltaTime;
            float sdt = Time.smoothDeltaTime;

            cmd.SetGlobalVector(HDShaderIDs._Time, new Vector4(ct * 0.05f, ct, ct * 2.0f, ct * 3.0f));
            cmd.SetGlobalVector(HDShaderIDs._SinTime, new Vector4(Mathf.Sin(ct * 0.125f), Mathf.Sin(ct * 0.25f), Mathf.Sin(ct * 0.5f), Mathf.Sin(ct)));
            cmd.SetGlobalVector(HDShaderIDs._CosTime, new Vector4(Mathf.Cos(ct * 0.125f), Mathf.Cos(ct * 0.25f), Mathf.Cos(ct * 0.5f), Mathf.Cos(ct)));
            cmd.SetGlobalVector(HDShaderIDs.unity_DeltaTime, new Vector4(dt, 1.0f / dt, sdt, 1.0f / sdt));
            cmd.SetGlobalVector(HDShaderIDs._TimeParameters, new Vector4(ct, Mathf.Sin(ct), Mathf.Cos(ct), 0.0f));
            cmd.SetGlobalVector(HDShaderIDs._LastTimeParameters, new Vector4(pt, Mathf.Sin(pt), Mathf.Cos(pt), 0.0f));

            float exposureMultiplierForProbes = 1.0f / Mathf.Max(probeRangeCompressionFactor, 1e-6f);
            cmd.SetGlobalFloat(HDShaderIDs._ProbeExposureScale, exposureMultiplierForProbes);

            // XRTODO: qualify this code with xr.singlePassEnabled when compute shaders can use keywords
            if (true)
            {
                cmd.SetGlobalInt(HDShaderIDs._XRViewCount, viewCount);

                // Convert AoS to SoA for GPU constant buffer until we can use StructuredBuffer via command buffer
                // XRTODO: use the new API and remove this code
                for (int i = 0; i < viewCount; i++)
                {
                    m_XRViewMatrix[i] = m_XRViewConstants[i].viewMatrix;
                    m_XRInvViewMatrix[i] = m_XRViewConstants[i].invViewMatrix;
                    m_XRProjMatrix[i] = m_XRViewConstants[i].projMatrix;
                    m_XRInvProjMatrix[i] = m_XRViewConstants[i].invProjMatrix;
                    m_XRViewProjMatrix[i] = m_XRViewConstants[i].viewProjMatrix;
                    m_XRInvViewProjMatrix[i] = m_XRViewConstants[i].invViewProjMatrix;
                    m_XRNonJitteredViewProjMatrix[i] = m_XRViewConstants[i].nonJitteredViewProjMatrix;
                    m_XRPrevViewProjMatrix[i] = m_XRViewConstants[i].prevViewProjMatrix;
                    m_XRPrevInvViewProjMatrix[i] = m_XRViewConstants[i].prevInvViewProjMatrix;
                    m_XRPrevViewProjMatrixNoCameraTrans[i] = m_XRViewConstants[i].prevViewProjMatrixNoCameraTrans;
                    m_XRPixelCoordToViewDirWS[i] = m_XRViewConstants[i].pixelCoordToViewDirWS;
                    m_XRWorldSpaceCameraPos[i] = m_XRViewConstants[i].worldSpaceCameraPos;
                    m_XRWorldSpaceCameraPosViewOffset[i] = m_XRViewConstants[i].worldSpaceCameraPosViewOffset;
                    m_XRPrevWorldSpaceCameraPos[i] = m_XRViewConstants[i].prevWorldSpaceCameraPos;
                }

                cmd.SetGlobalMatrixArray(HDShaderIDs._XRViewMatrix, m_XRViewMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRInvViewMatrix, m_XRInvViewMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRProjMatrix, m_XRProjMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRInvProjMatrix, m_XRInvProjMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRViewProjMatrix, m_XRViewProjMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRInvViewProjMatrix, m_XRInvViewProjMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRNonJitteredViewProjMatrix, m_XRNonJitteredViewProjMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRPrevViewProjMatrix, m_XRPrevViewProjMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRPrevInvViewProjMatrix, m_XRPrevInvViewProjMatrix);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRPrevViewProjMatrixNoCameraTrans, m_XRPrevViewProjMatrixNoCameraTrans);
                cmd.SetGlobalMatrixArray(HDShaderIDs._XRPixelCoordToViewDirWS, m_XRPixelCoordToViewDirWS);
                cmd.SetGlobalVectorArray(HDShaderIDs._XRWorldSpaceCameraPos, m_XRWorldSpaceCameraPos);
                cmd.SetGlobalVectorArray(HDShaderIDs._XRWorldSpaceCameraPosViewOffset, m_XRWorldSpaceCameraPosViewOffset);
                cmd.SetGlobalVectorArray(HDShaderIDs._XRPrevWorldSpaceCameraPos, m_XRPrevWorldSpaceCameraPos);
            }

            cmd.SetGlobalInt(HDShaderIDs._TransparentCameraOnlyMotionVectors, (frameSettings.IsEnabled(FrameSettingsField.MotionVectors) && !frameSettings.IsEnabled(FrameSettingsField.TransparentsWriteMotionVector)) ? 1 : 0);
        }

        internal void AllocateAmbientOcclusionHistoryBuffer(float scaleFactor)
        {
            if (scaleFactor != m_AmbientOcclusionResolutionScale || GetCurrentFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion) == null)
            {
                ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);

                var aoAlloc = new AmbientOcclusionAllocator(scaleFactor);
                AllocHistoryFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion, aoAlloc.Allocator, 2);

                m_AmbientOcclusionResolutionScale = scaleFactor;
            }
        }

        internal void ReleaseHistoryFrameRT(int id)
        {
            m_HistoryRTSystem.ReleaseBuffer(id);
        }

        internal void ExecuteCaptureActions(RTHandle input, CommandBuffer cmd)
        {
            if (m_RecorderCaptureActions == null || !m_RecorderCaptureActions.MoveNext())
                return;

            // We need to blit to an intermediate texture because input resolution can be bigger than the camera resolution
            // Since recorder does not know about this, we need to send a texture of the right size.
            cmd.GetTemporaryRT(m_RecorderTempRT, actualWidth, actualHeight, 0, FilterMode.Point, input.rt.graphicsFormat);

            var blitMaterial = HDUtils.GetBlitMaterial(input.rt.dimension);

            var rtHandleScale = RTHandles.rtHandleProperties.rtHandleScale;
            Vector2 viewportScale = new Vector2(rtHandleScale.x, rtHandleScale.y);

            m_RecorderPropertyBlock.SetTexture(HDShaderIDs._BlitTexture, input);
            m_RecorderPropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, viewportScale);
            m_RecorderPropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
            cmd.SetRenderTarget(m_RecorderTempRT);
            cmd.DrawProcedural(Matrix4x4.identity, blitMaterial, 0, MeshTopology.Triangles, 3, 1, m_RecorderPropertyBlock);

            for (m_RecorderCaptureActions.Reset(); m_RecorderCaptureActions.MoveNext();)
                m_RecorderCaptureActions.Current(m_RecorderTempRT, cmd);
        }

        class ExecuteCaptureActionsPassData
        {
            public RenderGraphResource input;
            public RenderGraphMutableResource tempTexture;
            public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> recorderCaptureActions;
            public Vector2 viewportScale;
            public Material blitMaterial;
        }

        internal void ExecuteCaptureActions(RenderGraph renderGraph, RenderGraphResource input)
        {
            if (m_RecorderCaptureActions == null || !m_RecorderCaptureActions.MoveNext())
                return;

            using (var builder = renderGraph.AddRenderPass<ExecuteCaptureActionsPassData>("Execute Capture Actions", out var passData))
            {
                var inputDesc = renderGraph.GetTextureDesc(input);
                var rtHandleScale = renderGraph.rtHandleProperties.rtHandleScale;
                passData.viewportScale = new Vector2(rtHandleScale.x, rtHandleScale.y);
                passData.blitMaterial = HDUtils.GetBlitMaterial(inputDesc.dimension);
                passData.recorderCaptureActions = m_RecorderCaptureActions;
                passData.input = builder.ReadTexture(input);
                // We need to blit to an intermediate texture because input resolution can be bigger than the camera resolution
                // Since recorder does not know about this, we need to send a texture of the right size.
                passData.tempTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(actualWidth, actualHeight)
                { colorFormat = inputDesc.colorFormat, name = "TempCaptureActions" }));

                builder.SetRenderFunc(
                (ExecuteCaptureActionsPassData data, RenderGraphContext ctx) =>
                {
                    var tempRT = ctx.resources.GetTexture(data.tempTexture);
                    var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                    mpb.SetTexture(HDShaderIDs._BlitTexture, ctx.resources.GetTexture(data.input));
                    mpb.SetVector(HDShaderIDs._BlitScaleBias, data.viewportScale);
                    mpb.SetFloat(HDShaderIDs._BlitMipLevel, 0);
                    ctx.cmd.SetRenderTarget(tempRT);
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.blitMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);

                    for (data.recorderCaptureActions.Reset(); data.recorderCaptureActions.MoveNext();)
                        data.recorderCaptureActions.Current(tempRT, ctx.cmd);
                });
            }
        }

        internal void UpdateCurrentSky(SkyManager skyManager)
        {
#if UNITY_EDITOR
            if (HDUtils.IsRegularPreviewCamera(camera))
            {
                visualSky.skySettings = skyManager.GetDefaultPreviewSkyInstance();
                lightingSky = visualSky;
                skyAmbientMode = SkyAmbientMode.Dynamic;
            }
            else
#endif
            {
                skyAmbientMode = volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;

                visualSky.skySettings = SkyManager.GetSkySetting(volumeStack);

                // Now, see if we have a lighting override
                // Update needs to happen before testing if the component is active other internal data structure are not properly updated yet.
                VolumeManager.instance.Update(skyManager.lightingOverrideVolumeStack, volumeAnchor, skyManager.lightingOverrideLayerMask);
                if (VolumeManager.instance.IsComponentActiveInMask<VisualEnvironment>(skyManager.lightingOverrideLayerMask))
                {
                    SkySettings newSkyOverride = SkyManager.GetSkySetting(skyManager.lightingOverrideVolumeStack);
                    if (m_LightingOverrideSky.skySettings != null && newSkyOverride == null)
                    {
                        // When we switch from override to no override, we need to make sure that the visual sky will actually be properly re-rendered.
                        // Resetting the visual sky hash will ensure that.
                        visualSky.skyParametersHash = -1;
                    }

                    m_LightingOverrideSky.skySettings = newSkyOverride;
                    lightingSky = m_LightingOverrideSky;

                }
                else
                {
                    lightingSky = visualSky;
                }
            }
        }
        #endregion

        #region Private API
        // Workaround for the Allocator callback so it doesn't allocate memory because of the capture of scaleFactor.
        struct AmbientOcclusionAllocator
        {
            float scaleFactor;

            public AmbientOcclusionAllocator(float scaleFactor) => this.scaleFactor = scaleFactor;

            public RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R32_UInt, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: string.Format("AO Packed history_{0}", frameIndex));
            }
        }

        static Dictionary<(Camera, int), HDCamera> s_Cameras = new Dictionary<(Camera, int), HDCamera>();
        static List<(Camera, int)> s_Cleanup = new List<(Camera, int)>(); // Recycled to reduce GC pressure

        HDAdditionalCameraData  m_AdditionalCameraData = null; // Init in Update
        BufferedRTHandleSystem  m_HistoryRTSystem = new BufferedRTHandleSystem();
        int                     m_NumColorPyramidBuffersAllocated = 0;
        int                     m_NumVolumetricBuffersAllocated   = 0;
        float                   m_AmbientOcclusionResolutionScale = 0.0f; // Factor used to track if history should be reallocated for Ambient Occlusion

        ViewConstants[]         m_XRViewConstants;
        // XR View Constants arrays (required due to limitations of API for StructuredBuffer)
        Matrix4x4[]             m_XRViewMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRInvViewMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRProjMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRInvProjMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRViewProjMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRInvViewProjMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRNonJitteredViewProjMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRPrevViewProjMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRPrevInvViewProjMatrix = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRPrevViewProjMatrixNoCameraTrans = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Matrix4x4[]             m_XRPixelCoordToViewDirWS = new Matrix4x4[ShaderConfig.s_XrMaxViews];
        Vector4[]               m_XRWorldSpaceCameraPos = new Vector4[ShaderConfig.s_XrMaxViews];
        Vector4[]               m_XRWorldSpaceCameraPosViewOffset = new Vector4[ShaderConfig.s_XrMaxViews];
        Vector4[]               m_XRPrevWorldSpaceCameraPos = new Vector4[ShaderConfig.s_XrMaxViews];

        // Recorder specific
        IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> m_RecorderCaptureActions;
        int                     m_RecorderTempRT = Shader.PropertyToID("TempRecorder");
        MaterialPropertyBlock   m_RecorderPropertyBlock = new MaterialPropertyBlock();

        void SetupCurrentMaterialQuality(CommandBuffer cmd)
        {
            var asset = HDRenderPipeline.currentAsset;
            MaterialQuality availableQualityLevels = asset.availableMaterialQualityLevels;
            MaterialQuality currentMaterialQuality = frameSettings.materialQuality == (MaterialQuality)0 ? asset.defaultMaterialQualityLevel : frameSettings.materialQuality;

            availableQualityLevels.GetClosestQuality(currentMaterialQuality).SetGlobalShaderKeywords(cmd);
        }

        void UpdateAntialiasing()
        {
            AntialiasingMode previousAntialiasing = antialiasing;

            // Handle post-process AA
            //  - If post-processing is disabled all together, no AA
            //  - In scene view, only enable TAA if animated materials are enabled
            //  - Else just use the currently set AA mode on the camera
            {
                if (!frameSettings.IsEnabled(FrameSettingsField.Postprocess) || !CoreUtils.ArePostProcessesEnabled(camera))
                    antialiasing = AntialiasingMode.None;
#if UNITY_EDITOR
                else if (camera.cameraType == CameraType.SceneView)
                {
                    var mode = HDAdditionalSceneViewSettings.sceneViewAntialiasing;

                    if (mode == AntialiasingMode.TemporalAntialiasing && !animateMaterials)
                        antialiasing = AntialiasingMode.None;
                    else
                        antialiasing = mode;
                }
#endif
                else if (m_AdditionalCameraData != null)
                {
                    antialiasing = m_AdditionalCameraData.antialiasing;
                    SMAAQuality = m_AdditionalCameraData.SMAAQuality;
                    taaSharpenStrength = m_AdditionalCameraData.taaSharpenStrength;
                }
                else
                    antialiasing = AntialiasingMode.None;
            }

            if (antialiasing != AntialiasingMode.TemporalAntialiasing)
            {
                taaFrameIndex = 0;
                taaJitter = Vector4.zero;
            }

            // When changing antialiasing mode to TemporalAA we must reset the history, otherwise we get one frame of garbage
            if ( (previousAntialiasing != antialiasing && antialiasing == AntialiasingMode.TemporalAntialiasing)
                || (m_PreviousClearColorMode != clearColorMode))
            {
                resetPostProcessingHistory = true;
                m_PreviousClearColorMode = clearColorMode;
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
            if (m_XRViewConstants == null || m_XRViewConstants.Length != viewCount)
            {
                m_XRViewConstants = new ViewConstants[viewCount];
                resetPostProcessingHistory = true;
                isFirstFrame = true;
            }

            UpdateAllViewConstants(IsTAAEnabled(), true);
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

            // XR single-pass support
            if (xr.singlePassEnabled)
            {
                for (int viewIndex = 0; viewIndex < viewCount; ++viewIndex)
                {
                    GetXrViewParameters(viewIndex, out proj, out view, out cameraPosition);
                    UpdateViewConstants(ref m_XRViewConstants[viewIndex], proj, view, cameraPosition, jitterProjectionMatrix, updatePreviousFrameConstants);

                    // Compute offset between the main camera and the instanced views
                    m_XRViewConstants[viewIndex].worldSpaceCameraPosViewOffset = m_XRViewConstants[viewIndex].worldSpaceCameraPos - mainViewConstants.worldSpaceCameraPos;
                }
            }
            else
            {
                // Compute shaders always use the XR single-pass path due to the lack of multi-compile
                m_XRViewConstants[0] = mainViewConstants;
            }

            UpdateFrustum(mainViewConstants);

            m_RecorderCaptureActions = CameraCaptureBridge.GetCaptureActions(camera);
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
            Matrix4x4 noTransViewMatrix = gpuView;
            if (ShaderConfig.s_CameraRelativeRendering == 0)
            {
                noTransViewMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

            }
            var gpuVPNoTrans = gpuNonJitteredProj * noTransViewMatrix;

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
                    viewConstants.prevInvViewProjMatrix = viewConstants.prevViewProjMatrix.inverse;
                    viewConstants.prevViewProjMatrixNoCameraTrans = gpuVPNoTrans;
                }
                else
                {
                    viewConstants.prevWorldSpaceCameraPos = viewConstants.worldSpaceCameraPos;
                    viewConstants.prevViewProjMatrix = viewConstants.nonJitteredViewProjMatrix;
                    viewConstants.prevViewProjMatrixNoCameraTrans = viewConstants.viewProjectionNoCameraTrans;
                }
            }

            viewConstants.viewMatrix = gpuView;
            viewConstants.invViewMatrix = gpuView.inverse;
            viewConstants.projMatrix = gpuProj;
            viewConstants.invProjMatrix = gpuProj.inverse;
            viewConstants.viewProjMatrix = gpuProj * gpuView;
            viewConstants.invViewProjMatrix = viewConstants.viewProjMatrix.inverse;
            viewConstants.nonJitteredViewProjMatrix = gpuNonJitteredProj * gpuView;
            viewConstants.worldSpaceCameraPos = cameraPosition;
            viewConstants.worldSpaceCameraPosViewOffset = Vector3.zero;
            viewConstants.viewProjectionNoCameraTrans = gpuVPNoTrans;

            var gpuProjAspect = HDUtils.ProjectionMatrixAspect(gpuProj);
            viewConstants.pixelCoordToViewDirWS = ComputePixelCoordToWorldSpaceViewDirectionMatrix(viewConstants, screenSize, gpuProjAspect);

            if (updatePreviousFrameConstants)
            {
                Vector3 cameraDisplacement = viewConstants.worldSpaceCameraPos - viewConstants.prevWorldSpaceCameraPos;
                viewConstants.prevWorldSpaceCameraPos -= viewConstants.worldSpaceCameraPos; // Make it relative w.r.t. the curr cam pos
                viewConstants.prevViewProjMatrix *= Matrix4x4.Translate(cameraDisplacement); // Now prevViewProjMatrix correctly transforms this frame's camera-relative positionWS
                viewConstants.prevInvViewProjMatrix = viewConstants.prevViewProjMatrix.inverse;
            }
        }

        void UpdateFrustum(in ViewConstants viewConstants)
        {
            // Update frustum and projection parameters
            var projMatrix = mainViewConstants.projMatrix;
            var invProjMatrix = mainViewConstants.invProjMatrix;
            var viewProjMatrix = mainViewConstants.viewProjMatrix;

            if (xr.enabled)
            {
                var combinedProjMatrix = xr.cullingParams.stereoProjectionMatrix;
                var combinedViewMatrix = xr.cullingParams.stereoViewMatrix;

                if (ShaderConfig.s_CameraRelativeRendering != 0)
                {
                    var combinedOrigin = combinedViewMatrix.inverse.GetColumn(3) - (Vector4)(camera.transform.position);
                    combinedViewMatrix.SetColumn(3, combinedOrigin);
                }

                projMatrix = GL.GetGPUProjectionMatrix(combinedProjMatrix, true);
                invProjMatrix = projMatrix.inverse;
                viewProjMatrix = projMatrix * combinedViewMatrix;
            }

            float n = camera.nearClipPlane;
            float f = camera.farClipPlane;

            // Analyze the projection matrix.
            // p[2][3] = (reverseZ ? 1 : -1) * (depth_0_1 ? 1 : 2) * (f * n) / (f - n)
            float scale     = projMatrix[2, 3] / (f * n) * (f - n);
            bool  depth_0_1 = Mathf.Abs(scale) < 1.5f;
            bool  reverseZ  = scale > 0;
            bool  flipProj  = invProjMatrix.MultiplyPoint(new Vector3(0, 1, 0)).y < 0;

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

            Vector3 viewDir = -viewConstants.invViewMatrix.GetColumn(2);
            viewDir.Normalize();
            Frustum.Create(ref frustum, viewProjMatrix, viewConstants.invViewMatrix.GetColumn(3), viewDir, n, f);

            // Left, right, top, bottom, near, far.
            for (int i = 0; i < 6; i++)
            {
                frustumPlaneEquations[i] = new Vector4(frustum.planes[i].normal.x, frustum.planes[i].normal.y, frustum.planes[i].normal.z, frustum.planes[i].distance);
            }
        }

        void UpdateVolumeAndPhysicalParameters()
        {
            volumeAnchor = null;
            volumeLayerMask = -1;
            physicalParameters = null;

            if (m_AdditionalCameraData != null)
            {
                volumeLayerMask = m_AdditionalCameraData.volumeLayerMask;
                volumeAnchor = m_AdditionalCameraData.volumeAnchorOverride;
                physicalParameters = m_AdditionalCameraData.physicalParameters;
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
                        if (mainCamera.TryGetComponent<HDAdditionalCameraData>(out var mainCamAdditionalData))
                        {
                            volumeLayerMask = mainCamAdditionalData.volumeLayerMask;
                            volumeAnchor = mainCamAdditionalData.volumeAnchorOverride;
                            physicalParameters = mainCamAdditionalData.physicalParameters;
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
                            // Remove lighting override mask and layer 31 which is used by preview/lookdev
                            volumeLayerMask = (-1 & ~(hdPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask | (1 << 31)));

                        // No fallback for the physical camera as we can't assume anything in this regard
                        // Kept at null so the exposure will just use the default physical camera values
                    }
                }
            }

            // If no override is provided, use the camera transform.
            if (volumeAnchor == null)
                volumeAnchor = camera.transform;

            using (new ProfilingScope(null, ProfilingSampler.Get(HDProfileId.VolumeUpdate)))
            {
                VolumeManager.instance.Update(volumeStack, volumeAnchor, volumeLayerMask);
            }
        }

        Matrix4x4 GetJitteredProjectionMatrix(Matrix4x4 origProj)
        {
            // Do not add extra jitter in VR unless requested (micro-variations from head tracking are usually enough)
            if (xr.enabled && !HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.xrSettings.cameraJitter)
            {
                taaJitter = Vector4.zero;
                return origProj;
            }

            // The variance between 0 and the actual halton sequence values reveals noticeable
            // instability in Unity's shadow maps, so we avoid index 0.
            float jitterX = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 3) - 0.5f;
            taaJitter = new Vector4(jitterX, jitterY, jitterX / actualWidth, jitterY / actualHeight);

            Matrix4x4 proj;

            if (camera.orthographic)
            {
                float vertical = camera.orthographicSize;
                float horizontal = vertical * camera.aspect;

                var offset = taaJitter;
                offset.x *= horizontal / (0.5f * actualWidth);
                offset.y *= vertical / (0.5f * actualHeight);

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

                var planeJitter = new Vector2(jitterX * horizFov / actualWidth,
                    jitterY * vertFov / actualHeight);

                planes.left += planeJitter.x;
                planes.right += planeJitter.x;
                planes.top += planeJitter.y;
                planes.bottom += planeJitter.y;

                proj = Matrix4x4.Frustum(planes);
            }

            return proj;
        }

        /// <summary>
        /// Compute the matrix from screen space (pixel) to world space direction (RHS).
        ///
        /// You can use this matrix on the GPU to compute the direction to look in a cubemap for a specific
        /// screen pixel.
        /// </summary>
        /// <param name="viewConstants"></param>
        /// <param name="resolution">The target texture resolution.</param>
        /// <param name="aspect">
        /// The aspect ratio to use.
        ///
        /// if negative, then the aspect ratio of <paramref name="resolution"/> will be used.
        ///
        /// It is different from the aspect ratio of <paramref name="resolution"/> for anamorphic projections.
        /// </param>
        /// <returns></returns>
        Matrix4x4 ComputePixelCoordToWorldSpaceViewDirectionMatrix(ViewConstants viewConstants, Vector4 resolution, float aspect = -1)
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

            float verticalFoV = camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            if (!camera.usePhysicalProperties)
            {
                verticalFoV = Mathf.Atan(-1.0f / viewConstants.projMatrix[1, 1]) * 2;
            }
            Vector2 lensShift = camera.GetGateFittedLensShift();

            return HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(verticalFoV, lensShift, resolution, viewConstants.viewMatrix, false, aspect);
        }

        void Dispose()
        {
            VolumeManager.instance.DestroyStack(volumeStack);

            if (m_HistoryRTSystem != null)
            {
                m_HistoryRTSystem.Dispose();
                m_HistoryRTSystem = null;
            }

            if (lightingSky != null && lightingSky != visualSky)
                lightingSky.Cleanup();

            if (visualSky != null)
                visualSky.Cleanup();
        }

        // BufferedRTHandleSystem API expects an allocator function. We define it here.
        static RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            frameIndex &= 1;
            var hdPipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;

            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: (GraphicsFormat)hdPipeline.currentPlatformRenderPipelineSettings.colorBufferFormat,
                                        dimension: TextureXR.dimension, enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, useDynamicScale: true,
                                        name: string.Format("CameraColorBufferMipChain{0}", frameIndex));
        }

        void ReleaseHistoryBuffer()
        {
            m_HistoryRTSystem.ReleaseAll();
        }
        #endregion
    }
}
