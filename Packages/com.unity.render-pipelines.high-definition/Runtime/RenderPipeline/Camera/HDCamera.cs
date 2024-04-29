using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using Unity.Collections;

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
            /// <summary>Previous view matrix from previous frame.</summary>
            public Matrix4x4 prevViewMatrix;
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

        /// <summary>Camera name.</summary>
        public string name { get; private set; } // Needs to be cached because camera.name generates GCAllocs
        /// <summary>
        /// Screen resolution information.
        /// Width, height, inverse width, inverse height.
        /// </summary>
        public Vector4 screenSize;
        /// <summary>
        /// Screen resolution information for post processes passes.
        /// Width, height, inverse width, inverse height.
        /// </summary>
        public Vector4 postProcessScreenSize { get { return m_PostProcessScreenSize; } }
        /// <summary>Camera frustum.</summary>
        public Frustum frustum;
        /// <summary>Camera component.</summary>
        public Camera camera;
        /// <summary>TAA jitter information.</summary>
        public Vector4 taaJitter;
        /// <summary>View constants.</summary>
        public ViewConstants mainViewConstants;
        /// <summary>Color pyramid history buffer state.</summary>
        public bool colorPyramidHistoryIsValid = false;
        /// <summary>Volumetric history buffer state.</summary>
        public bool volumetricHistoryIsValid = false;

        internal int volumetricValidFrames = 0;
        internal int colorPyramidHistoryValidFrames = 0;

        internal float intermediateDownscaling = 0.5f;
        internal bool volumetricCloudsFullscaleHistory = false;

        /// <summary>Width actually used for rendering after dynamic resolution and XR is applied.</summary>
        public int actualWidth { get; private set; }
        /// <summary>Height actually used for rendering after dynamic resolution and XR is applied.</summary>
        public int actualHeight { get; private set; }
        /// <summary>Number of MSAA samples used for this frame.</summary>
        public MSAASamples msaaSamples { get; private set; }
        /// <summary>Returns true if MSAA is enabled for this camera (equivalent to msaaSamples != MSAASamples.None).</summary>
        public bool msaaEnabled { get { return msaaSamples != MSAASamples.None; } }
        /// <summary>Frame settings for this camera.</summary>
        public FrameSettings frameSettings { get; private set; }
        /// <summary>RTHandle properties for the camera history buffers.</summary>
        public RTHandleProperties historyRTHandleProperties { get { return m_HistoryRTSystem.rtHandleProperties; } }
        /// <summary>Volume stack used for this camera.</summary>
        public VolumeStack volumeStack { get; private set; }
        /// <summary>Current time for this camera.</summary>
        public float time; // Take the 'animateMaterials' setting into account.

        internal bool dofHistoryIsValid = false;  // used to invalidate DoF accumulation history when switching DoF modes

        // State needed to handle TAAU.
        internal bool previousFrameWasTAAUpsampled = false;

        /// <summary>Ray tracing acceleration structure that is used in case the user specified the build mode as manual for the RTAS.</summary>
        public RayTracingAccelerationStructure rayTracingAccelerationStructure = null;
        /// <summary>Flag that tracks if one of the objects that is included into the RTAS had its transform changed.</summary>
        public bool transformsDirty = false;
        /// <summary>Flag that tracks if one of the objects that is included into the RTAS had its material changed.</summary>
        public bool materialsDirty = false;

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
            volumetricHistoryIsValid = false;
            volumetricValidFrames = 0;
            colorPyramidHistoryIsValid = false;
            colorPyramidHistoryValidFrames = 0;
            dofHistoryIsValid = false;

            // Camera was potentially Reset() so we need to reset timers on the renderers.
            if (visualSky != null)
                visualSky.Reset();
            if (lightingSky != null && visualSky != lightingSky)
                lightingSky.Reset();
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
            public Matrix4x4 transform;
        }

        /// <summary>
        /// Enum that lists the various history slots that require tracking of their validity
        /// </summary>
        internal enum HistoryEffectSlot
        {
            GlobalIllumination0,
            GlobalIllumination1,
            RayTracedReflections,
            VolumetricClouds,
            RayTracedAmbientOcclusion,
            Count
        }

        internal enum HistoryEffectFlags
        {
            FullResolution = 1 << 0,
            RayTraced = 1 << 1,
            ExposureControl = 1 << 2,
            CustomBit0 = 1 << 3,
            CustomBit1 = 1 << 4,
            CustomBit2 = 1 << 5,
            CustomBit3 = 1 << 6,
            CustomBit4 = 1 << 7,
        }

        /// <summary>
        // Generic structure that captures various history validity states.
        /// </summary>
        internal struct HistoryEffectValidity
        {
            // The last internal camera frame count at which this effect was set
            public int frameCount;
            // A combination of masks that define the validity state of the history
            public int flagMask;
        }

        /// <summary>
        // Struct that lists the data required to perform the volumetric clouds animation
        /// </summary>
        internal struct VolumetricCloudsAnimationData
        {
            public float lastTime;
            public Vector2 cloudOffset;
            public float verticalShapeOffset;
            public float verticalErosionOffset;
        }

#if ENABLE_SENSOR_SDK
        internal RayTracingShader pathTracingShaderOverride = null;
        internal Action<UnityEngine.Rendering.CommandBuffer> prepareDispatchRays = null;
#endif

        internal Vector4[] frustumPlaneEquations;
        internal int taaFrameIndex;
        internal float taaSharpenStrength;
        internal float taaHistorySharpening;
        internal float taaAntiFlicker;
        internal float taaMotionVectorRejection;
        internal float taaBaseBlendFactor;
        internal float taaJitterScale;
        internal bool taaAntiRinging;

        internal Vector4 zBufferParams;
        internal Vector4 unity_OrthoParams;
        internal Vector4 projectionParams;
        internal Vector4 screenParams;
        internal int volumeLayerMask;
        internal Transform volumeAnchor;
        internal Rect finalViewport = new Rect(Vector2.zero, -1.0f * Vector2.one); // This will have the correct viewport position and the size will be full resolution (ie : not taking dynamic rez into account)
        internal Rect prevFinalViewport;
        internal int colorPyramidHistoryMipCount = 0;
        internal VBufferParameters[] vBufferParams;            // Double-buffered; needed even if reprojection is off
        internal RTHandle[] volumetricHistoryBuffers; // Double-buffered; only used for reprojection
        // Currently the frame count is not increase every render, for ray tracing shadow filtering. We need to have a number that increases every render
        internal uint cameraFrameCount = 0;
        internal bool animateMaterials;
        internal float lastTime;

        private Camera m_parentCamera = null; // Used for recursive rendering, e.g. a reflection in a scene view.
        internal Camera parentCamera { get { return m_parentCamera; } }

        private Vector2 m_LowResHWDRSFactor = new Vector2(0.0f, 0.0f);

        internal Vector2 lowResDrsFactor => DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled() ? m_LowResHWDRSFactor : new Vector2(RTHandles.rtHandleProperties.rtHandleScale.x, RTHandles.rtHandleProperties.rtHandleScale.y);
        internal float lowResScale = 0.5f;
        internal float historyLowResScale = 0.5f;
        internal bool isLowResScaleHalf { get { return lowResScale == 0.5f; } }
		
        internal float lowResScaleForScreenSpaceLighting = 0.5f;
        internal float historyLowResScaleForScreenSpaceLighting = 0.5f;	

        internal Rect lowResViewport
        {
            get
            {
                return new Rect(
                    0.0f, 0.0f,
                    (float)Mathf.RoundToInt(((float)actualWidth) * lowResScale),
                    (float)Mathf.RoundToInt(((float)actualHeight) * lowResScale));
            }
        }

        static private Vector2 CalculateLowResHWDrsFactor(Vector2Int scaledSize, DynamicResolutionHandler resolutionHandler, float lowResFactor)
        {
            // This function fixes some float precision issues against the runtime + drs system + low res transparency (on hardware DRS only).
            //
            // In hardware DRS the actual size underneath can be different because the runtime does a computation the following way:
            // finalLowRes = ceil(round(fullRes * lowResMultiplier) * drsPerc)
            //
            // meanwhile the SRP does it this way:
            // finalLowRes = round(ceil(fullRes * drsPerc) * lowResMultiplier)
            //
            // Unfortunately changing this would cause quite a bit of unknowns all over since its a change required on RTHandle scaling.
            // Its safer to fix it case by case for now, and this problem has only been seen on xb1 HW drs on low res transparent.
            // In this case we compute the error between both factors, and plumb it as a new DRS scaler. This ultimately means that low res transparency has its own
            // drs scale, which is used in TransparentUpsampling passes.
            // 
            Vector2Int originalLowResHWViewport = new Vector2Int(Mathf.RoundToInt((float)RTHandles.maxWidth * lowResFactor), Mathf.RoundToInt((float)RTHandles.maxHeight * lowResFactor));
            Vector2Int lowResHWViewport = resolutionHandler.GetScaledSize(originalLowResHWViewport);
            Vector2 lowResViewport = new Vector2(Mathf.RoundToInt((float)scaledSize.x * lowResFactor), Mathf.RoundToInt((float)scaledSize.y * lowResFactor));
            return lowResViewport / (Vector2)lowResHWViewport;
        }

        //Setting a parent camera also tries to use the parent's camera exposure textures.
        //One example is planar reflection probe volume being pre exposed.
        internal void SetParentCamera(HDCamera parentHdCam, bool useGpuFetchedExposure, float fetchedGpuExposure)
        {
            if (parentHdCam == null)
            {
                m_ExposureTextures.clear();
                m_ExposureTextures.useCurrentCamera = true;
                m_parentCamera = null;
                return;
            }

            m_parentCamera = parentHdCam.camera;

            if (!m_ExposureControlFS)
            {
                m_ExposureTextures.clear();
                m_ExposureTextures.useCurrentCamera = true;
                return;
            }

            m_ExposureTextures.clear();
            m_ExposureTextures.useCurrentCamera = false;
            m_ExposureTextures.parent = parentHdCam.currentExposureTextures.current;
            if (useGpuFetchedExposure)
            {
                m_ExposureTextures.useFetchedExposure = true;
                m_ExposureTextures.fetchedGpuExposure = fetchedGpuExposure;
            }
        }

        private Vector4 m_PostProcessScreenSize = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        private Vector4 m_PostProcessRTScales = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector4 m_PostProcessRTScalesHistory = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        private Vector2Int m_PostProcessRTHistoryMaxReference = new Vector2Int(1, 1);

        internal Vector2 postProcessRTScales { get { return new Vector2(m_PostProcessRTScales.x, m_PostProcessRTScales.y); } }
        internal Vector4 postProcessRTScalesHistory { get { return m_PostProcessRTScalesHistory; } }
        internal Vector2Int postProcessRTHistoryMaxReference { get { return m_PostProcessRTHistoryMaxReference; } }

        // This property is ray tracing specific. It allows us to track for the RayTracingShadow history which light was using which slot.
        // This avoid ghosting and many other problems that may happen due to an unwanted history usage
        internal ShadowHistoryUsage[] shadowHistoryUsage = null;
        // This property allows us to track for the various history accumulation based effects, the last registered validity frame ubdex of each effect as well as the resolution at which it was built.
        internal HistoryEffectValidity[] historyEffectUsage = null;

        // Boolean that allows us to track if the current camera maps to a real time reflection probe.
        internal bool realtimeReflectionProbe = false;

        internal SkyUpdateContext m_LightingOverrideSky = new SkyUpdateContext();

        /// <summary>Mark the HDCamera as persistant so it won't be destroyed if the camera is disabled</summary>
        internal bool isPersistent = false;

        internal HDUtils.PackedMipChainInfo m_DepthBufferMipChainInfo = new HDUtils.PackedMipChainInfo();

        internal ref HDUtils.PackedMipChainInfo depthBufferMipChainInfo => ref m_DepthBufferMipChainInfo;

        internal Vector2Int depthMipChainSize => m_DepthBufferMipChainInfo.textureSize;

        // VisualSky is the sky used for rendering in the main view.
        // LightingSky is the sky used for lighting the scene (ambient probe and sky reflection)
        // It's usually the visual sky unless a sky lighting override is setup.
        //      Ambient Probe: Only used if Ambient Mode is set to dynamic in the Visual Environment component. Updated according to the Update Mode parameter.
        //      (Otherwise it uses the one from the static lighting sky)
        //      Sky Reflection Probe : Always used and updated according to the Update Mode parameter.
        internal SkyUpdateContext visualSky { get; private set; } = new SkyUpdateContext();
        internal SkyUpdateContext lightingSky { get; private set; } = null;
        // We need to cache this here because it's need in SkyManager.SetupAmbientProbe
        // The issue is that this is called during culling which happens before Volume updates so we can't query it via volumes in there.
        internal SkyAmbientMode skyAmbientMode { get; private set; }

        // XR multipass and instanced views are supported (see XRSystem)
        internal XRPass xr { get; private set; }

        internal float globalMipBias { set; get; } = 0.0f;

        internal float deltaTime => time - lastTime;

        // Useful for the deterministic testing of motion vectors.
        // This is currently override only in com.unity.testing.hdrp/TestRunner/OverrideTime.cs
        internal float animateMaterialsTime { get; set; } = -1;
        internal float animateMaterialsTimeLast { get; set; } = -1;

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

        internal bool canDoDynamicResolution { get { return camera.cameraType == CameraType.Game; } }


        // Helper property to inform how many views are rendered simultaneously
        internal int viewCount { get => Math.Max(1, xr.viewCount); }

        internal bool clearDepth
        {
            get { return m_AdditionalCameraData != null ? m_AdditionalCameraData.clearDepth : camera.clearFlags != CameraClearFlags.Nothing; }
        }

        internal bool CameraIsSceneFiltering()
        {
            return CoreUtils.IsSceneFilteringEnabled() && camera.cameraType == CameraType.SceneView;
        }

        internal HDAdditionalCameraData.ClearColorMode clearColorMode
        {
            get
            {
                if (CameraIsSceneFiltering())
                    return HDAdditionalCameraData.ClearColorMode.Color;

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

        internal GameObject exposureTarget
        {
            get
            {
                if (m_AdditionalCameraData != null)
                {
                    return m_AdditionalCameraData.exposureTarget;
                }

                return null;
            }
        }

        private float m_GpuExposureValue = 1.0f;
        private float m_GpuDeExposureValue = 1.0f;

        private struct ExposureGpuReadbackRequest
        {
            public bool isDeExposure;
            public AsyncGPUReadbackRequest request;
        }

        // This member and function allow us to fetch the exposure value that was used to render the realtime HDProbe
        // without forcing a sync between the c# and the GPU code.
        private Queue<ExposureGpuReadbackRequest> m_ExposureAsyncRequest = new Queue<ExposureGpuReadbackRequest>();

        internal void RequestGpuExposureValue(RTHandle exposureTexture)
        {
            RequestGpuTexelValue(exposureTexture, false);
        }

        internal void RequestGpuDeExposureValue(RTHandle exposureTexture)
        {
            RequestGpuTexelValue(exposureTexture, true);
        }

        private void RequestGpuTexelValue(RTHandle exposureTexture, bool isDeExposure)
        {
            var readbackRequest = new ExposureGpuReadbackRequest();
            readbackRequest.request = AsyncGPUReadback.Request(exposureTexture.rt, 0, 0, 1, 0, 1, 0, 1);
            readbackRequest.isDeExposure = isDeExposure;
            m_ExposureAsyncRequest.Enqueue(readbackRequest);
        }

        private void PumpReadbackQueue()
        {
            while (m_ExposureAsyncRequest.Count != 0)
            {
                ExposureGpuReadbackRequest requestState = m_ExposureAsyncRequest.Peek();
                ref AsyncGPUReadbackRequest request = ref requestState.request;
#if UNITY_EDITOR
                //HACK: when we are in the unity editor, requests get updated very very infrequently
                // by the runtime. This can cause the m_ExposureAsyncRequest to become super bloated:
                // sometimes up to 800 requests get accumulated.
                // This hack forces an update of the request when in editor mode, now the m_ExposureAsyncRequest averages
                // 3 elements. Not necesary when running in player mode, since the requests get updated properly (due to swap chain complexities)
                request.Update();
#endif
                if (!request.done && !request.hasError)
                    break;

                // If this has an error, just skip it
                if (!request.hasError)
                {
                    // Grab the native array from this readback
                    NativeArray<float> exposureValue = request.GetData<float>();
                    if (requestState.isDeExposure)
                        m_GpuDeExposureValue = exposureValue[0];
                    else
                        m_GpuExposureValue = exposureValue[0];
                }
                m_ExposureAsyncRequest.Dequeue();
            }
        }

        // This function processes the asynchronous read-back requests for the exposure and updates the last known exposure value.
        internal float GpuExposureValue()
        {
            PumpReadbackQueue();
            return m_GpuExposureValue;
        }

        // This function processes the asynchronous read-back requests for the exposure and updates the last known exposure value.
        internal float GpuDeExposureValue()
        {
            PumpReadbackQueue();
            return m_GpuDeExposureValue;
        }

        internal struct ExposureTextures
        {
            public bool useCurrentCamera;
            public RTHandle parent;
            public RTHandle current;
            public RTHandle previous;

            public bool useFetchedExposure;
            public float fetchedGpuExposure;

            public void clear()
            {
                parent = null;
                current = null;
                previous = null;
                useFetchedExposure = false;
                fetchedGpuExposure = 1.0f;
            }
        }

        private bool m_ExposureControlFS = false;
        internal bool exposureControlFS { get { return m_ExposureControlFS; } }
        private ExposureTextures m_ExposureTextures = new ExposureTextures() { useCurrentCamera = true, current = null, previous = null };
        internal ExposureTextures currentExposureTextures { get { return m_ExposureTextures; } }

        internal void SetupExposureTextures()
        {
            if (!m_ExposureControlFS)
            {
                m_ExposureTextures.current = null;
                m_ExposureTextures.previous = null;
                return;
            }

            var currentTexture = GetCurrentFrameRT((int)HDCameraFrameHistoryType.Exposure);
            if (currentTexture == null)
            {
                RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
                {
                    // r: multiplier, g: EV100
                    var rt = rtHandleSystem.Alloc(1, 1, colorFormat: HDRenderPipeline.k_ExposureFormat,
                        enableRandomWrite: true, name: $"{id} Exposure Texture {frameIndex}"
                    );
                    HDRenderPipeline.SetExposureTextureToEmpty(rt);
                    return rt;
                }

                currentTexture = AllocHistoryFrameRT((int)HDCameraFrameHistoryType.Exposure, Allocator, 2);
            }

            // One frame delay + history RTs being flipped at the beginning of the frame means we
            // have to grab the exposure marked as "previous"
            m_ExposureTextures.current = GetPreviousFrameRT((int)HDCameraFrameHistoryType.Exposure);
            m_ExposureTextures.previous = currentTexture;
        }

        // This value will always be correct for the current camera, no need to check for
        // game view / scene view / preview in the editor, it's handled automatically
        internal AntialiasingMode antialiasing { get; private set; } = AntialiasingMode.None;

        internal HDAdditionalCameraData.SMAAQualityLevel SMAAQuality { get; private set; } = HDAdditionalCameraData.SMAAQualityLevel.Medium;
        internal HDAdditionalCameraData.TAAQualityLevel TAAQuality { get; private set; } = HDAdditionalCameraData.TAAQualityLevel.Medium;

        internal bool resetPostProcessingHistory = true;
        internal bool didResetPostProcessingHistoryInLastFrame = false;

        internal bool dithering => m_AdditionalCameraData != null && m_AdditionalCameraData.dithering;

        internal bool stopNaNs => m_AdditionalCameraData != null && m_AdditionalCameraData.stopNaNs;

        internal bool allowDynamicResolution => m_AdditionalCameraData != null && m_AdditionalCameraData.allowDynamicResolution;

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
            shadowHistoryUsage[screenSpaceShadowIndex].transform = lightData.transform.localToWorldMatrix;
        }

        internal bool EffectHistoryValidity(HistoryEffectSlot slot, int flagMask)
        {
            flagMask |= exposureControlFS ? (int)HistoryEffectFlags.ExposureControl : 0;
            return (historyEffectUsage[(int)slot].frameCount == (cameraFrameCount - 1))
                && (historyEffectUsage[(int)slot].flagMask == flagMask);
        }

        internal void PropagateEffectHistoryValidity(HistoryEffectSlot slot, int flagMask)
        {
            flagMask |= exposureControlFS ? (int)HistoryEffectFlags.ExposureControl : 0;
            historyEffectUsage[(int)slot].frameCount = (int)cameraFrameCount;
            historyEffectUsage[(int)slot].flagMask = flagMask;
        }

        internal uint GetCameraFrameCount()
        {
            return cameraFrameCount;
        }

        internal struct DynamicResolutionRequest
        {
            public bool enabled;
            public bool cameraRequested;
            public bool hardwareEnabled;
            public DynamicResUpscaleFilter filter;
        }

        internal DynamicResolutionRequest DynResRequest { set; get; }

        internal void RequestDynamicResolution(bool cameraRequestedDynamicRes, DynamicResolutionHandler dynResHandler)
        {
            //cache the state of the drs handler in the camera, it will be used by post processes later.
            DynResRequest = new DynamicResolutionRequest()
            {
                enabled = dynResHandler.DynamicResolutionEnabled(),
                cameraRequested = cameraRequestedDynamicRes,
                hardwareEnabled = dynResHandler.HardwareDynamicResIsEnabled(),
                filter = dynResHandler.filter
            };
        }

        internal ProfilingSampler profilingSampler => m_AdditionalCameraData?.profilingSampler ?? ProfilingSampler.Get(HDProfileId.HDRenderPipelineRenderCamera);


#if ENABLE_VIRTUALTEXTURES
        VTBufferManager virtualTextureFeedback = new VTBufferManager();
#endif


        internal HDCamera(Camera cam)
        {
            camera = cam;

            name = cam.name;

            frustum = new Frustum();
            frustum.planes = new Plane[6];
            frustum.corners = new Vector3[8];

            frustumPlaneEquations = new Vector4[6];

            volumeStack = VolumeManager.instance.CreateStack();

            m_DepthBufferMipChainInfo.Allocate();

            Reset();
        }

        internal bool IsDLSSEnabled()
        {
            return m_AdditionalCameraData == null ? false : m_AdditionalCameraData.cameraCanRenderDLSS;
        }

        internal bool IsTAAUEnabled()
        {
            return DynamicResolutionHandler.instance.DynamicResolutionEnabled() && DynamicResolutionHandler.instance.filter == DynamicResUpscaleFilter.TAAU && !IsDLSSEnabled();
        }

        internal bool IsPathTracingEnabled()
        {
            var pathTracing = volumeStack.GetComponent<PathTracing>();
            return pathTracing ? pathTracing.enable.value : false;
        }

        internal DynamicResolutionHandler.UpsamplerScheduleType UpsampleSyncPoint()
        {
            if (IsDLSSEnabled())
            {
                return HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.DLSSInjectionPoint;
            }
            else if (IsTAAUEnabled())
            {
                return DynamicResolutionHandler.UpsamplerScheduleType.BeforePost;
            }
            else
            {
                return DynamicResolutionHandler.UpsamplerScheduleType.AfterPost;
            }
        }

        internal bool allowDeepLearningSuperSampling => m_AdditionalCameraData == null ? false : m_AdditionalCameraData.allowDeepLearningSuperSampling;
        internal bool deepLearningSuperSamplingUseCustomQualitySettings => m_AdditionalCameraData == null ? false : m_AdditionalCameraData.deepLearningSuperSamplingUseCustomQualitySettings;
        internal uint deepLearningSuperSamplingQuality => m_AdditionalCameraData == null ? 0 : m_AdditionalCameraData.deepLearningSuperSamplingQuality;
        internal bool deepLearningSuperSamplingUseCustomAttributes => m_AdditionalCameraData == null ? false : m_AdditionalCameraData.deepLearningSuperSamplingUseCustomAttributes;
        internal bool deepLearningSuperSamplingUseOptimalSettings => m_AdditionalCameraData == null ? false : m_AdditionalCameraData.deepLearningSuperSamplingUseOptimalSettings;
        internal float deepLearningSuperSamplingSharpening => m_AdditionalCameraData == null ? 0.0f : m_AdditionalCameraData.deepLearningSuperSamplingSharpening;
        internal bool fsrOverrideSharpness => m_AdditionalCameraData == null ? false : m_AdditionalCameraData.fsrOverrideSharpness;
        internal float fsrSharpness => m_AdditionalCameraData == null ? FSRUtils.kDefaultSharpnessLinear : m_AdditionalCameraData.fsrSharpness;

        internal bool RequiresCameraJitter()
        {
            return (antialiasing == AntialiasingMode.TemporalAntialiasing || IsDLSSEnabled() || IsTAAUEnabled()) && !IsPathTracingEnabled();
        }

        internal bool IsSSREnabled(bool transparent = false)
        {
            var ssr = volumeStack.GetComponent<ScreenSpaceReflection>();
            if (!transparent)
                return frameSettings.IsEnabled(FrameSettingsField.SSR) && ssr.enabled.value && frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects);
            else
                return frameSettings.IsEnabled(FrameSettingsField.TransparentSSR) && ssr.enabledTransparent.value;
        }

        internal bool IsSSGIEnabled()
        {
            var ssgi = volumeStack.GetComponent<GlobalIllumination>();
            return frameSettings.IsEnabled(FrameSettingsField.SSGI) && ssgi.enable.value;
        }

        internal bool IsVolumetricReprojectionEnabled()
        {
            bool a = Fog.IsVolumetricFogEnabled(this);
            // We only enable volumetric re projection if we are processing the game view or a scene view with animated materials on
            bool b = camera.cameraType == CameraType.Game || (camera.cameraType == CameraType.SceneView && CoreUtils.AreAnimatedMaterialsEnabled(camera));
            bool c = frameSettings.IsEnabled(FrameSettingsField.ReprojectionForVolumetrics);

            return a && b && c;
        }

        // Pass all the systems that may want to update per-camera data here.
        // That way you will never update an HDCamera and forget to update the dependent system.
        // NOTE: This function must be called only once per rendering (not frame, as a single camera can be rendered multiple times with different parameters during the same frame)
        // Otherwise, previous frame view constants will be wrong.
        internal void Update(FrameSettings currentFrameSettings, HDRenderPipeline hdrp, XRPass xrPass, bool allocateHistoryBuffers = true)
        {
            // Inherit animation settings from the parent camera.
            Camera aniCam = (parentCamera != null) ? parentCamera : camera;

            // Different views/tabs may have different values of the "Animated Materials" setting.
            animateMaterials = CoreUtils.AreAnimatedMaterialsEnabled(aniCam);
            if (animateMaterials)
            {
                float newTime, deltaTime;
#if UNITY_EDITOR
                newTime = Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
                deltaTime = Application.isPlaying ? Time.deltaTime : 0.033f;
#else
                newTime = Time.time;
                deltaTime = Time.deltaTime;
#endif
                time = newTime;
                lastTime = newTime - deltaTime;
            }
            else
            {
                time = 0;
                lastTime = 0;
            }

            // Make sure that the shadow history identification array is allocated and is at the right size
            if (shadowHistoryUsage == null || shadowHistoryUsage.Length != hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots)
            {
                shadowHistoryUsage = new ShadowHistoryUsage[hdrp.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxScreenSpaceShadowSlots];
            }

            // Make sure that the shadow history identification array is allocated and is at the right size
            if (historyEffectUsage == null || historyEffectUsage.Length != (int)HistoryEffectSlot.Count)
            {
                historyEffectUsage = new HistoryEffectValidity[(int)HistoryEffectSlot.Count];
                for (int i = 0; i < (int)HistoryEffectSlot.Count; ++i)
                {
                    // We invalidate all the frame indices for the first usage
                    historyEffectUsage[i].frameCount = -1;
                }
            }

            // store a shortcut on HDAdditionalCameraData (done here and not in the constructor as
            // we don't create HDCamera at every frame and user can change the HDAdditionalData later (Like when they create a new scene).
            camera.TryGetComponent<HDAdditionalCameraData>(out m_AdditionalCameraData);

            globalMipBias = m_AdditionalCameraData == null ? 0.0f : m_AdditionalCameraData.materialMipBias;

            UpdateVolumeAndPhysicalParameters();

            xr = xrPass;
            frameSettings = currentFrameSettings;

            m_ExposureControlFS = frameSettings.IsEnabled(FrameSettingsField.ExposureControl);

            UpdateAntialiasing();

            // ORDER is important: we read the upsamplerSchedule when we decide if we need to refresh the history buffers, so be careful when moving this
            DynamicResolutionHandler.instance.upsamplerSchedule = UpsampleSyncPoint();

            // Handle memory allocation.
            if (allocateHistoryBuffers)
            {
                // Have to do this every frame in case the settings have changed.
                // The condition inside controls whether we perform init/deinit or not.
                HDRenderPipeline.ReinitializeVolumetricBufferParams(this);

                bool isCurrentColorPyramidRequired = frameSettings.IsEnabled(FrameSettingsField.Refraction) || frameSettings.IsEnabled(FrameSettingsField.Distortion) || frameSettings.IsEnabled(FrameSettingsField.Water);
                bool isHistoryColorPyramidRequired = IsSSREnabled(transparent: false) || IsSSREnabled(transparent: true) || IsSSGIEnabled();
                bool isVolumetricHistoryRequired = IsVolumetricReprojectionEnabled();

                // If we have a mismatch with color buffer format we need to reallocate the pyramid
                var hdPipeline = (HDRenderPipeline)(RenderPipelineManager.currentPipeline);
                bool forceReallocHistorySystem = false;
                int colorBufferID = (int)HDCameraFrameHistoryType.ColorBufferMipChain;
                int numColorPyramidBuffersAllocated = m_HistoryRTSystem.GetNumFramesAllocated(colorBufferID);
                if (numColorPyramidBuffersAllocated > 0)
                {
                    var currPyramid = GetCurrentFrameRT(colorBufferID);
                    if (currPyramid != null && currPyramid.rt.graphicsFormat != hdPipeline.GetColorBufferFormat())
                    {
                        forceReallocHistorySystem = true;
                    }
                }

                int numColorPyramidBuffersRequired = 0;
                if (isCurrentColorPyramidRequired)
                    numColorPyramidBuffersRequired = 1;
                if (isHistoryColorPyramidRequired) // Superset of case above
                    numColorPyramidBuffersRequired = 2;

                // Check if we have any AOV requests that require history buffer allocations (the actual allocation happens later in this function)
                foreach (var aovRequest in aovRequests)
                {
                    var aovHistory = GetHistoryRTHandleSystem(aovRequest);
                    if (aovHistory.GetNumFramesAllocated(colorBufferID) != numColorPyramidBuffersRequired)
                    {
                        forceReallocHistorySystem = true;
                        break;
                    }
                }

                // If we change the upscale schedule, refresh the history buffers. We need to do this, because if postprocess is after upscale, the size of some buffers needs to change.
                if (m_PrevUpsamplerSchedule != DynamicResolutionHandler.instance.upsamplerSchedule || previousFrameWasTAAUpsampled != IsTAAUEnabled())
                {
                    forceReallocHistorySystem = true;
                    m_PrevUpsamplerSchedule = DynamicResolutionHandler.instance.upsamplerSchedule;
                }

                // If view count changes, release all history buffers, they will get reallocated when needed
                if (viewCount != m_HistoryViewCount)
                {
                    forceReallocHistorySystem = true;
                    m_HistoryViewCount = viewCount;
                }

                // Handle the color buffers
                if (numColorPyramidBuffersAllocated != numColorPyramidBuffersRequired || forceReallocHistorySystem)
                {
                    // Reinit the system.
                    colorPyramidHistoryIsValid = false;

                    // Since we nuke all history we must inform the post process system too.
                    resetPostProcessingHistory = true;

                    if (forceReallocHistorySystem)
                    {
                        m_HistoryRTSystem.Dispose();
                        m_HistoryRTSystem = new BufferedRTHandleSystem();
                    }
                    else
                    {
                       // We only need to release all the ColorBufferMipChain buffers (and they will potentially be allocated just under if needed).
                        m_HistoryRTSystem.ReleaseBuffer((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                    }

                    m_ExposureTextures.clear();

                    if (numColorPyramidBuffersRequired != 0 || forceReallocHistorySystem)
                    {
                        // Make sure we don't try to allocate a history target with zero buffers
                        bool needColorPyramid = numColorPyramidBuffersRequired > 0;

                        if (needColorPyramid)
                            AllocHistoryFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain, HistoryBufferAllocatorFunction, numColorPyramidBuffersRequired);

                        // Handle the AOV history buffers
                        var cameraHistory = GetHistoryRTHandleSystem();
                        foreach (var aovRequest in aovRequests)
                        {
                            var aovHistory = GetHistoryRTHandleSystem(aovRequest);
                            BindHistoryRTHandleSystem(aovHistory);
                            if (needColorPyramid)
                                AllocHistoryFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain, HistoryBufferAllocatorFunction, numColorPyramidBuffersRequired);
                        }
                        BindHistoryRTHandleSystem(cameraHistory);
                    }
                }

                // Handle the volumetric fog buffers
                int numVolumetricBuffersRequired = isVolumetricHistoryRequired ? 2 : 0; // History + feedback
                if (m_NumVolumetricBuffersAllocated != numVolumetricBuffersRequired)
                {
                    HDRenderPipeline.DestroyVolumetricHistoryBuffers(this);
                    if (numVolumetricBuffersRequired != 0)
                        HDRenderPipeline.CreateVolumetricHistoryBuffers(this, numVolumetricBuffersRequired);
                    // Mark as init.
                    m_NumVolumetricBuffersAllocated = numVolumetricBuffersRequired;
                }
            }

            // Update viewport
            {
                prevFinalViewport = finalViewport;

                if (xr.enabled)
                {
                    finalViewport = xr.GetViewport();
                }
                else
                {
                    finalViewport = GetPixelRect();
                }

                actualWidth = Math.Max((int)finalViewport.size.x, 1);
                actualHeight = Math.Max((int)finalViewport.size.y, 1);
            }

            DynamicResolutionHandler.instance.finalViewport = new Vector2Int((int)finalViewport.width, (int)finalViewport.height);

            Vector2Int nonScaledViewport = new Vector2Int(actualWidth, actualHeight);

            m_DepthBufferMipChainInfo.ComputePackedMipChainInfo(nonScaledViewport);

            historyLowResScale = resetPostProcessingHistory ? 0.5f : lowResScale;
            historyLowResScaleForScreenSpaceLighting = resetPostProcessingHistory ? 0.5f : lowResScaleForScreenSpaceLighting;
            lowResScale = 0.5f;
			lowResScaleForScreenSpaceLighting = 0.5f;
            m_LowResHWDRSFactor = Vector2.one;
            if (canDoDynamicResolution)
            {
                Vector2Int originalSize = new Vector2Int(actualWidth, actualHeight);
                Vector2Int scaledSize = DynamicResolutionHandler.instance.GetScaledSize(originalSize);
                actualWidth = scaledSize.x;
                actualHeight = scaledSize.y;
                globalMipBias += DynamicResolutionHandler.instance.CalculateMipBias(scaledSize, nonScaledViewport, UpsampleSyncPoint() <= DynamicResolutionHandler.UpsamplerScheduleType.AfterDepthOfField);

                //setting up constants for low resolution rendering (i.e. transparent low res)
                lowResScale = DynamicResolutionHandler.instance.GetLowResMultiplier(lowResScale);

                lowResScaleForScreenSpaceLighting =  DynamicResolutionHandler.instance.GetLowResMultiplier(lowResScaleForScreenSpaceLighting, hdrp.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.lowResSSGIMinimumThreshold);
                m_LowResHWDRSFactor = CalculateLowResHWDrsFactor(scaledSize, DynamicResolutionHandler.instance, lowResScale);
            }

            var screenWidth = actualWidth;
            var screenHeight = actualHeight;

            msaaSamples = frameSettings.GetResolvedMSAAMode(hdrp.asset);

            screenSize = new Vector4(screenWidth, screenHeight, 1.0f / screenWidth, 1.0f / screenHeight);
            SetPostProcessScreenSize(screenWidth, screenHeight);
            screenParams = new Vector4(screenSize.x, screenSize.y, 1 + screenSize.z, 1 + screenSize.w);

            const int kMaxSampleCount = 8;
            if (++taaFrameIndex >= kMaxSampleCount)
                taaFrameIndex = 0;

            UpdateAllViewConstants();
            isFirstFrame = false;
            cameraFrameCount++;

            HDRenderPipeline.UpdateVolumetricBufferParams(this);
            HDRenderPipeline.ResizeVolumetricHistoryBuffers(this);
        }

        /// <summary>Set the RTHandle scale to the actual camera size (can be scaled)</summary>
        internal void SetReferenceSize()
        {
            RTHandles.SetReferenceSize(actualWidth, actualHeight);
            m_HistoryRTSystem.SwapAndSetReferenceSize(actualWidth, actualHeight);
            SetPostProcessScreenSize(actualWidth, actualHeight);

            foreach (var aovHistory in m_AOVHistoryRTSystem)
            {
                var historySystem = aovHistory.Value;
                historySystem.SwapAndSetReferenceSize(actualWidth, actualHeight);
            }
        }

        internal void SetPostProcessScreenSize(int width, int height)
        {
            m_PostProcessScreenSize = new Vector4((float)width, (float)height, 1.0f / (float)width, 1.0f / (float)height);
            Vector2 scales = RTHandles.CalculateRatioAgainstMaxSize(width, height);
            m_PostProcessRTScales = new Vector4(scales.x, scales.y, m_PostProcessRTScales.x, m_PostProcessRTScales.y);
        }

        internal void SetPostProcessHistorySizeAndReference(int width, int height, int referenceWidth, int referenceHeight)
        {
            m_PostProcessRTHistoryMaxReference = new Vector2Int(Math.Max(referenceWidth, m_PostProcessRTHistoryMaxReference.x), Math.Max(referenceHeight, m_PostProcessRTHistoryMaxReference.y));
            m_PostProcessRTScalesHistory = new Vector4((float)width / (float)m_PostProcessRTHistoryMaxReference.x, (float)height / (float)m_PostProcessRTHistoryMaxReference.y, m_PostProcessRTScalesHistory.x, m_PostProcessRTScalesHistory.y);
        }

        // Updating RTHandle needs to be done at the beginning of rendering (not during update of HDCamera which happens in batches)
        // The reason is that RTHandle will hold data necessary to setup RenderTargets and viewports properly.
        internal void BeginRender(CommandBuffer cmd)
        {
            SetReferenceSize();

            m_RecorderCaptureActions = CameraCaptureBridge.GetCaptureActions(camera);

            SetupCurrentMaterialQuality(cmd);

            SetupExposureTextures();

#if ENABLE_VIRTUALTEXTURES
            virtualTextureFeedback.BeginRender(this);
#endif
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
                if (camera.camera == null || (!camera.camera.isActiveAndEnabled && camera.camera.cameraType != CameraType.Preview && !hasPersistentHistory && !camera.isPersistent))
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

                    foreach (var aovHistory in hdCamera.m_AOVHistoryRTSystem)
                    {
                        var historySystem = aovHistory.Value;
                        historySystem.ResetReferenceSize(width, height);
                    }
                }
            }
        }

        unsafe internal void UpdateScalesAndScreenSizesCB(ref ShaderVariablesGlobal cb)
        {
            cb._ScreenSize = screenSize;
            cb._PostProcessScreenSize = postProcessScreenSize;
            cb._RTHandleScale = RTHandles.rtHandleProperties.rtHandleScale;
            cb._RTHandleScaleHistory = m_HistoryRTSystem.rtHandleProperties.rtHandleScale;
            cb._RTHandlePostProcessScale = m_PostProcessRTScales;
            cb._RTHandlePostProcessScaleHistory = m_PostProcessRTScalesHistory;
            cb._DynamicResolutionFullscreenScale = new Vector4(actualWidth / finalViewport.width, actualHeight / finalViewport.height, 0, 0);
        }

        unsafe internal void UpdateShaderVariablesGlobalCB(ref ShaderVariablesGlobal cb)
            => UpdateShaderVariablesGlobalCB(ref cb, (int)cameraFrameCount);

        unsafe internal void UpdateShaderVariablesGlobalCB(ref ShaderVariablesGlobal cb, int frameCount)
        {
            bool taaEnabled = frameSettings.IsEnabled(FrameSettingsField.Postprocess)
                && antialiasing == AntialiasingMode.TemporalAntialiasing
                && camera.cameraType == CameraType.Game;

            var additionalCameraDataIsNull = m_AdditionalCameraData == null;

            cb._ViewMatrix = mainViewConstants.viewMatrix;
            cb._CameraViewMatrix = mainViewConstants.viewMatrix;
            cb._InvViewMatrix = mainViewConstants.invViewMatrix;
            cb._ProjMatrix = mainViewConstants.projMatrix;
            cb._InvProjMatrix = mainViewConstants.invProjMatrix;
            cb._ViewProjMatrix = mainViewConstants.viewProjMatrix;
            cb._CameraViewProjMatrix = mainViewConstants.viewProjMatrix;
            cb._InvViewProjMatrix = mainViewConstants.invViewProjMatrix;
            cb._NonJitteredViewProjMatrix = mainViewConstants.nonJitteredViewProjMatrix;
            cb._PrevViewProjMatrix = mainViewConstants.prevViewProjMatrix;
            cb._PrevInvViewProjMatrix = mainViewConstants.prevInvViewProjMatrix;
            cb._WorldSpaceCameraPos_Internal = mainViewConstants.worldSpaceCameraPos;
            cb._PrevCamPosRWS_Internal = mainViewConstants.prevWorldSpaceCameraPos;
            UpdateScalesAndScreenSizesCB(ref cb);
            cb._ZBufferParams = zBufferParams;
            cb._ProjectionParams = projectionParams;
            cb.unity_OrthoParams = unity_OrthoParams;
            cb._ScreenParams = screenParams;
            for (int i = 0; i < 6; ++i)
                for (int j = 0; j < 4; ++j)
                    cb._FrustumPlanes[i * 4 + j] = frustumPlaneEquations[i][j];
            cb._TaaFrameInfo = new Vector4(taaSharpenStrength, 0, taaFrameIndex, taaEnabled ? 1 : 0);
            cb._TaaJitterStrength = taaJitter;
            cb._ColorPyramidLodCount = colorPyramidHistoryMipCount;
            cb._GlobalMipBias = globalMipBias;
            cb._GlobalMipBiasPow2 = (float)Math.Pow(2.0f, globalMipBias);

            float ct = time;
            float pt = lastTime;
#if UNITY_EDITOR
            // Apply editor mode time override if any.
            if (animateMaterials)
            {
                ct = animateMaterialsTime < 0 ? ct : animateMaterialsTime;
                pt = animateMaterialsTimeLast < 0 ? pt : animateMaterialsTimeLast;
            }

            float dt = time - lastTime;
            float sdt = dt;
#else
            float dt = Time.deltaTime;
            float sdt = Time.smoothDeltaTime;
#endif

            cb._Time = new Vector4(ct * 0.05f, ct, ct * 2.0f, ct * 3.0f);
            cb._SinTime = new Vector4(Mathf.Sin(ct * 0.125f), Mathf.Sin(ct * 0.25f), Mathf.Sin(ct * 0.5f), Mathf.Sin(ct));
            cb._CosTime = new Vector4(Mathf.Cos(ct * 0.125f), Mathf.Cos(ct * 0.25f), Mathf.Cos(ct * 0.5f), Mathf.Cos(ct));
            cb.unity_DeltaTime = new Vector4(dt, 1.0f / dt, sdt, 1.0f / sdt);
            cb._TimeParameters = new Vector4(ct, Mathf.Sin(ct), Mathf.Cos(ct), 0.0f);
            cb._LastTimeParameters = new Vector4(pt, Mathf.Sin(pt), Mathf.Cos(pt), 0.0f);
            cb._FrameCount = frameCount;
            cb._XRViewCount = (uint)viewCount;

            float exposureMultiplierForProbes = 1.0f / Mathf.Max(probeRangeCompressionFactor, 1e-6f);
            cb._ProbeExposureScale = exposureMultiplierForProbes;

            cb._DeExposureMultiplier = additionalCameraDataIsNull ? 1.0f : m_AdditionalCameraData.deExposureMultiplier;

            // IMPORTANT NOTE: This checks if we have Movec and not Transparent Motion Vectors because in that case we need to write camera motion vectors
            // for transparent objects, otherwise the transparent objects will look completely broken upon motion if Transparent Motion Vectors is off.
            // If TransparentsWriteMotionVector the camera motion vectors are baked into the per object motion vectors.
            cb._TransparentCameraOnlyMotionVectors = (frameSettings.IsEnabled(FrameSettingsField.MotionVectors) &&
                !frameSettings.IsEnabled(FrameSettingsField.TransparentsWriteMotionVector)) ? 1 : 0;

            cb._ScreenSizeOverride = additionalCameraDataIsNull ? cb._ScreenSize : m_AdditionalCameraData.screenSizeOverride;

            // Default to identity scale-bias.
            cb._ScreenCoordScaleBias = additionalCameraDataIsNull ? new Vector4(1, 1, 0, 0) : m_AdditionalCameraData.screenCoordScaleBias;
        }

        unsafe internal void PushBuiltinShaderConstantsXR(CommandBuffer cmd)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled)
            {
                cmd.SetViewProjectionMatrices(xr.GetViewMatrix(), xr.GetProjMatrix());
                if (xr.singlePassEnabled)
                {
                    for (int viewId = 0; viewId < viewCount; viewId++)
                    {
                        XRBuiltinShaderConstants.UpdateBuiltinShaderConstants(xr.GetViewMatrix(viewId), xr.GetProjMatrix(viewId), true, viewId);
                    }
                    XRBuiltinShaderConstants.SetBuiltinShaderConstants(cmd);
                }
            }
#endif
        }

        unsafe internal void UpdateShaderVariablesXRCB(ref ShaderVariablesXR cb)
        {
            for (int i = 0; i < viewCount; i++)
            {
                for (int j = 0; j < 16; ++j)
                {
                    cb._XRViewMatrix[i * 16 + j] = m_XRViewConstants[i].viewMatrix[j];
                    cb._XRInvViewMatrix[i * 16 + j] = m_XRViewConstants[i].invViewMatrix[j];
                    cb._XRProjMatrix[i * 16 + j] = m_XRViewConstants[i].projMatrix[j];
                    cb._XRInvProjMatrix[i * 16 + j] = m_XRViewConstants[i].invProjMatrix[j];
                    cb._XRViewProjMatrix[i * 16 + j] = m_XRViewConstants[i].viewProjMatrix[j];
                    cb._XRInvViewProjMatrix[i * 16 + j] = m_XRViewConstants[i].invViewProjMatrix[j];
                    cb._XRNonJitteredViewProjMatrix[i * 16 + j] = m_XRViewConstants[i].nonJitteredViewProjMatrix[j];
                    cb._XRPrevViewProjMatrix[i * 16 + j] = m_XRViewConstants[i].prevViewProjMatrix[j];
                    cb._XRPrevInvViewProjMatrix[i * 16 + j] = m_XRViewConstants[i].prevInvViewProjMatrix[j];
                    cb._XRViewProjMatrixNoCameraTrans[i * 16 + j] = m_XRViewConstants[i].viewProjectionNoCameraTrans[j];
                    cb._XRPrevViewProjMatrixNoCameraTrans[i * 16 + j] = m_XRViewConstants[i].prevViewProjMatrixNoCameraTrans[j];
                    cb._XRPixelCoordToViewDirWS[i * 16 + j] = m_XRViewConstants[i].pixelCoordToViewDirWS[j];
                }
                for (int j = 0; j < 3; ++j) // Inputs are vec3 but we align CB on float4
                {
                    cb._XRWorldSpaceCameraPos[i * 4 + j] = m_XRViewConstants[i].worldSpaceCameraPos[j];
                    cb._XRWorldSpaceCameraPosViewOffset[i * 4 + j] = m_XRViewConstants[i].worldSpaceCameraPosViewOffset[j];
                    cb._XRPrevWorldSpaceCameraPos[i * 4 + j] = m_XRViewConstants[i].prevWorldSpaceCameraPos[j];
                }
            }
        }

        internal bool AllocateAmbientOcclusionHistoryBuffer(float scaleFactor)
        {
            if (scaleFactor != m_AmbientOcclusionResolutionScale || GetCurrentFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion) == null)
            {
                ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion);

                var aoAlloc = new CustomHistoryAllocator(new Vector2(scaleFactor, scaleFactor), GraphicsFormat.R8G8B8A8_UNorm, "AO Packed history");
                AllocHistoryFrameRT((int)HDCameraFrameHistoryType.AmbientOcclusion, aoAlloc.Allocator, 2);

                m_AmbientOcclusionResolutionScale = scaleFactor;
                return true;
            }

            return false;
        }

        internal void AllocateScreenSpaceAccumulationHistoryBuffer(float scaleFactor)
        {
            if (scaleFactor != m_ScreenSpaceAccumulationResolutionScale || GetCurrentFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation) == null)
            {
                ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation);

                var ssrAlloc = new CustomHistoryAllocator(new Vector2(scaleFactor, scaleFactor), GraphicsFormat.R16G16B16A16_SFloat, "SSR_Accum Packed history");
                AllocHistoryFrameRT((int)HDCameraFrameHistoryType.ScreenSpaceReflectionAccumulation, ssrAlloc.Allocator, 2);

                m_ScreenSpaceAccumulationResolutionScale = scaleFactor;
            }
        }

        internal void ReleaseHistoryFrameRT(int id)
        {
            m_HistoryRTSystem.ReleaseBuffer(id);
        }

        class ExecuteCaptureActionsPassData
        {
            public TextureHandle input;
            public TextureHandle tempTexture;
            public IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> recorderCaptureActions;
            public Vector2 viewportScale;
            public Material blitMaterial;
            public Rect viewportSize;
        }

        internal void ExecuteCaptureActions(RenderGraph renderGraph, TextureHandle input)
        {
            if (m_RecorderCaptureActions == null || !m_RecorderCaptureActions.MoveNext())
                return;

            using (var builder = renderGraph.AddRenderPass<ExecuteCaptureActionsPassData>("Execute Capture Actions", out var passData))
            {
                var inputDesc = renderGraph.GetTextureDesc(input);
                var targetSize = RTHandles.rtHandleProperties.currentRenderTargetSize;
                passData.viewportScale = new Vector2(finalViewport.width / targetSize.x, finalViewport.height / targetSize.y);


                passData.blitMaterial = HDUtils.GetBlitMaterial(inputDesc.dimension);
                passData.recorderCaptureActions = m_RecorderCaptureActions;
                passData.input = builder.ReadTexture(input);
                passData.viewportSize = finalViewport;
                // We need to blit to an intermediate texture because input resolution can be bigger than the camera resolution
                // Since recorder does not know about this, we need to send a texture of the right size.
                passData.tempTexture = builder.CreateTransientTexture(new TextureDesc((int)finalViewport.width, (int)finalViewport.height)
                { colorFormat = inputDesc.colorFormat, name = "TempCaptureActions" });

                builder.SetRenderFunc(
                    (ExecuteCaptureActionsPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpb.SetTexture(HDShaderIDs._BlitTexture, data.input);
                        mpb.SetVector(HDShaderIDs._BlitScaleBias, data.viewportScale);
                        mpb.SetFloat(HDShaderIDs._BlitMipLevel, 0);
                        ctx.cmd.SetRenderTarget(data.tempTexture);
                        ctx.cmd.SetViewport(data.viewportSize);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.blitMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);

                        for (data.recorderCaptureActions.Reset(); data.recorderCaptureActions.MoveNext();)
                            data.recorderCaptureActions.Current(data.tempTexture, ctx.cmd);
                    });
            }
        }

        internal void UpdateCurrentSky(SkyManager skyManager)
        {
#if UNITY_EDITOR
            if (HDUtils.IsRegularPreviewCamera(camera))
            {
                visualSky.skySettings = skyManager.GetDefaultPreviewSkyInstance();
                visualSky.cloudSettings = null;
                visualSky.volumetricClouds = null;
                lightingSky = visualSky;
                skyAmbientMode = SkyAmbientMode.Static;
            }
            else
#endif
            {
                skyAmbientMode = volumeStack.GetComponent<VisualEnvironment>().skyAmbientMode.value;

                visualSky.skySettings = SkyManager.GetSkySetting(volumeStack);
                visualSky.cloudSettings = SkyManager.GetCloudSetting(volumeStack);
                visualSky.volumetricClouds = SkyManager.GetVolumetricClouds(volumeStack);

                lightingSky = visualSky;

                if (skyManager.lightingOverrideLayerMask != 0)
                {
                    // Now, see if we have a lighting override
                    // Update needs to happen before testing if the component is active other internal data structure are not properly updated yet.
                    VolumeManager.instance.Update(skyManager.lightingOverrideVolumeStack, volumeAnchor, skyManager.lightingOverrideLayerMask);

                    if (VolumeManager.instance.IsComponentActiveInMask<VisualEnvironment>(skyManager.lightingOverrideLayerMask))
                    {
                        SkySettings newSkyOverride = SkyManager.GetSkySetting(skyManager.lightingOverrideVolumeStack);
                        CloudSettings newCloudOverride = SkyManager.GetCloudSetting(skyManager.lightingOverrideVolumeStack);
                        VolumetricClouds newVolumetricCloudsOverride = SkyManager.GetVolumetricClouds(skyManager.lightingOverrideVolumeStack);

                        if ((m_LightingOverrideSky.skySettings != null && newSkyOverride == null) ||
                            (m_LightingOverrideSky.cloudSettings != null && newCloudOverride == null) ||
                            (m_LightingOverrideSky.volumetricClouds != null && newVolumetricCloudsOverride == null))
                        {
                            // When we switch from override to no override, we need to make sure that the visual sky will actually be properly re-rendered.
                            // Resetting the visual sky hash will ensure that.
                            visualSky.skyParametersHash = -1;
                        }

                        m_LightingOverrideSky.skySettings = newSkyOverride;
                        m_LightingOverrideSky.cloudSettings = newCloudOverride;
                        m_LightingOverrideSky.volumetricClouds = newVolumetricCloudsOverride;
                        lightingSky = m_LightingOverrideSky;
                    }
                }
            }
        }

        internal void OverridePixelRect(Rect newPixelRect) => m_OverridePixelRect = newPixelRect;
        internal void ResetPixelRect() => m_OverridePixelRect = null;

        // Workaround for the Allocator callback so it doesn't allocate memory because of the capture of scaleFactor.
        internal struct CustomHistoryAllocator
        {
            Vector2 scaleFactor;
            GraphicsFormat format;
            string name;

            public CustomHistoryAllocator(Vector2 scaleFactor, GraphicsFormat format, string name)
            {
                this.scaleFactor = scaleFactor;
                this.format = format;
                this.name = name;
            }

            public RTHandle Allocator(string id, int frameIndex, RTHandleSystem rtHandleSystem)
            {
                return rtHandleSystem.Alloc(Vector2.one * scaleFactor, TextureXR.slices, filterMode: FilterMode.Point, colorFormat: format, dimension: TextureXR.dimension, useDynamicScale: true, enableRandomWrite: true, name: string.Format("{0}_{1}_{2}", id, name, frameIndex));
            }
        }
        #endregion


        #region Private API


        static Dictionary<(Camera, int), HDCamera> s_Cameras = new Dictionary<(Camera, int), HDCamera>();
        static List<(Camera, int)> s_Cleanup = new List<(Camera, int)>(); // Recycled to reduce GC pressure

        HDAdditionalCameraData m_AdditionalCameraData = null; // Init in Update
        BufferedRTHandleSystem m_HistoryRTSystem = new BufferedRTHandleSystem();
        int m_HistoryViewCount = 0; // Used to track view count change if XR is enabled/disabled
        int m_NumVolumetricBuffersAllocated = 0;
        float m_AmbientOcclusionResolutionScale = 0.0f; // Factor used to track if history should be reallocated for Ambient Occlusion
        float m_ScreenSpaceAccumulationResolutionScale = 0.0f; // Use another scale if AO & SSR don't have the same resolution

        Dictionary<AOVRequestData, BufferedRTHandleSystem> m_AOVHistoryRTSystem = new Dictionary<AOVRequestData, BufferedRTHandleSystem>(new AOVRequestDataComparer());


        /// <summary>
        /// Store current algorithm which help to know if we trigger to reset history SSR Buffers.
        /// </summary>
        public ScreenSpaceReflectionAlgorithm
            currentSSRAlgorithm = ScreenSpaceReflectionAlgorithm.Approximation;

        internal ViewConstants[] m_XRViewConstants;

        // Recorder specific
        IEnumerator<Action<RenderTargetIdentifier, CommandBuffer>> m_RecorderCaptureActions;
        int m_RecorderTempRT = Shader.PropertyToID("TempRecorder");
        MaterialPropertyBlock m_RecorderPropertyBlock = new MaterialPropertyBlock();
        Rect? m_OverridePixelRect = null;

        internal bool hasCaptureActions => m_RecorderCaptureActions != null;

        // Keep track of the previous DLSS state
        private DynamicResolutionHandler.UpsamplerScheduleType m_PrevUpsamplerSchedule = DynamicResolutionHandler.UpsamplerScheduleType.AfterPost;

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
                    TAAQuality = m_AdditionalCameraData.TAAQuality;
                    taaSharpenStrength = m_AdditionalCameraData.taaSharpenStrength;
                    taaHistorySharpening = m_AdditionalCameraData.taaHistorySharpening;
                    taaAntiFlicker = m_AdditionalCameraData.taaAntiFlicker;
                    taaAntiRinging = m_AdditionalCameraData.taaAntiHistoryRinging;
                    taaJitterScale = m_AdditionalCameraData.taaJitterScale;
                    taaMotionVectorRejection = m_AdditionalCameraData.taaMotionVectorRejection;
                    taaBaseBlendFactor = m_AdditionalCameraData.taaBaseBlendFactor;
                }
                else
                    antialiasing = AntialiasingMode.None;
            }

            if (!RequiresCameraJitter())
            {
                taaFrameIndex = 0;
                taaJitter = Vector4.zero;
            }

            // If we have TAAU enabled, we need to force TAA to make it work.
            if (IsTAAUEnabled())
                antialiasing = AntialiasingMode.TemporalAntialiasing;

            // When changing antialiasing mode to TemporalAA we must reset the history, otherwise we get one frame of garbage
            if ((previousAntialiasing != antialiasing && antialiasing == AntialiasingMode.TemporalAntialiasing)
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

            UpdateAllViewConstants(RequiresCameraJitter(), true);
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
                // In case we are not camera relative, gpuView contains the camera translation component at this stage, so we need to remove it.
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
                    viewConstants.prevViewMatrix = gpuView;
                    viewConstants.prevViewProjMatrix = gpuVP;
                    viewConstants.prevInvViewProjMatrix = viewConstants.prevViewProjMatrix.inverse;
                    viewConstants.prevViewProjMatrixNoCameraTrans = gpuVPNoTrans;
                }
                else
                {
                    viewConstants.prevWorldSpaceCameraPos = viewConstants.worldSpaceCameraPos;
                    viewConstants.prevViewMatrix = viewConstants.viewMatrix;
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
            float scale = projMatrix[2, 3] / (f * n) * (f - n);
            bool depth_0_1 = Mathf.Abs(scale) < 1.5f;
            bool reverseZ = scale > 0;
            bool flipProj = invProjMatrix.MultiplyPoint(new Vector3(0, 1, 0)).y < 0;

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
            float orthoWidth = orthoHeight * camera.aspect;
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

        internal static int GetSceneViewLayerMaskFallback()
        {
            HDRenderPipeline hdPipeline = RenderPipelineManager.currentPipeline as HDRenderPipeline;
            // If the override layer is "Everything", we fall-back to "Everything" for the current layer mask to avoid issues by having no current layer
            // In practice we should never have "Everything" as an override mask as it does not make sense (a warning is issued in the UI)
            if (hdPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask == -1)
                return -1;

            // Remove lighting override mask and layer 31 which is used by preview/lookdev
            return (-1 & ~(hdPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyLightingOverrideLayerMask | (1 << 31)));

        }

        void UpdateVolumeAndPhysicalParameters()
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
                        if (mainCamera.TryGetComponent<HDAdditionalCameraData>(out var mainCamAdditionalData))
                        {
                            volumeLayerMask = mainCamAdditionalData.volumeLayerMask;
                            volumeAnchor = mainCamAdditionalData.volumeAnchorOverride;
                            needFallback = false;
                        }
                    }

                    if (needFallback)
                    {
                        volumeLayerMask = GetSceneViewLayerMaskFallback();
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

            // Update info about current target mid gray
            TargetMidGray requestedMidGray = volumeStack.GetComponent<Exposure>().targetMidGray.value;
            switch (requestedMidGray)
            {
                case TargetMidGray.Grey125:
                    ColorUtils.s_LightMeterCalibrationConstant = 12.5f;
                    break;
                case TargetMidGray.Grey14:
                    ColorUtils.s_LightMeterCalibrationConstant = 14.0f;
                    break;
                case TargetMidGray.Grey18:
                    ColorUtils.s_LightMeterCalibrationConstant = 18.0f;
                    break;
                default:
                    ColorUtils.s_LightMeterCalibrationConstant = 12.5f;
                    break;
            }
        }

        internal Matrix4x4 GetJitteredProjectionMatrix(Matrix4x4 origProj)
        {
            // Do not add extra jitter in VR unless requested (micro-variations from head tracking are usually enough)
            if (xr.enabled && !HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.xrSettings.cameraJitter)
            {
                taaJitter = Vector4.zero;
                return origProj;
            }
#if UNITY_2021_2_OR_NEWER
            if (UnityEngine.FrameDebugger.enabled)
            {
                taaJitter = Vector4.zero;
                return origProj;
            }
#endif

            // The variance between 0 and the actual halton sequence values reveals noticeable
            // instability in Unity's shadow maps, so we avoid index 0.
            float jitterX = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 2) - 0.5f;
            float jitterY = HaltonSequence.Get((taaFrameIndex & 1023) + 1, 3) - 0.5f;

            if (!(IsDLSSEnabled() || IsTAAUEnabled() || camera.cameraType == CameraType.SceneView))
            {
                jitterX *= taaJitterScale;
                jitterY *= taaJitterScale;
            }

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

                // Reconstruct the far plane for the jittered matrix.
                // For extremely high far clip planes, the decomposed projection zFar evaluates to infinity.
                if (float.IsInfinity(planes.zFar))
                    planes.zFar = frustum.planes[5].distance;

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
            // In XR mode, or if explicitely required, use a more generic matrix to account for asymmetry in the projection
            var useGenericMatrix = xr.enabled || frameSettings.IsEnabled(FrameSettingsField.AsymmetricProjection);

            // Asymmetry is also possible from a user-provided projection, so we must check for it too.
            // Note however, that in case of physical camera, the lens shift term is the only source of
            // asymmetry, and this is accounted for in the optimized path below. Additionally, Unity C++ will
            // automatically disable physical camera when the projection is overridden by user.
            useGenericMatrix |= HDUtils.IsProjectionMatrixAsymmetric(viewConstants.projMatrix) && !camera.usePhysicalProperties;

            if (useGenericMatrix)
            {
                var viewSpaceRasterTransform = new Matrix4x4(
                    new Vector4(2.0f * resolution.z, 0.0f, 0.0f, -1.0f),
                    new Vector4(0.0f, -2.0f * resolution.w, 0.0f, 1.0f),
                    new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                    new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                var transformT = viewConstants.invViewProjMatrix.transpose * Matrix4x4.Scale(new Vector3(-1.0f, -1.0f, -1.0f));
                return viewSpaceRasterTransform * transformT;
            }

            float verticalFoV = camera.GetGateFittedFieldOfView() * Mathf.Deg2Rad;
            if (!camera.usePhysicalProperties)
            {
                verticalFoV = Mathf.Atan(-1.0f / viewConstants.projMatrix[1, 1]) * 2;
            }
            Vector2 lensShift = camera.GetGateFittedLensShift();

            return HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(verticalFoV, lensShift, resolution, viewConstants.viewMatrix, false, aspect, camera.orthographic);
        }

        void Dispose()
        {
            HDRenderPipeline.DestroyVolumetricHistoryBuffers(this);

            VolumeManager.instance.DestroyStack(volumeStack);

            if (m_HistoryRTSystem != null)
            {
                m_HistoryRTSystem.Dispose();
                m_HistoryRTSystem = null;
            }

            foreach (var aovHistory in m_AOVHistoryRTSystem)
            {
                var historySystem = aovHistory.Value;
                historySystem.Dispose();
            }
            m_AOVHistoryRTSystem.Clear();

            if (lightingSky != null && lightingSky != visualSky)
                lightingSky.Cleanup();

            if (visualSky != null)
                visualSky.Cleanup();

#if ENABLE_VIRTUALTEXTURES
            virtualTextureFeedback?.Cleanup();
#endif
        }

        // BufferedRTHandleSystem API expects an allocator function. We define it here.
        static RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            frameIndex &= 1;
            var hdPipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;

            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: hdPipeline.GetColorBufferFormat(),
                dimension: TextureXR.dimension, enableRandomWrite: true, useMipMap: true, autoGenerateMips: false, useDynamicScale: true,
                name: string.Format("{0}_CameraColorBufferMipChain{1}", viewName, frameIndex));
        }

        void ReleaseHistoryBuffer()
        {
            m_HistoryRTSystem.ReleaseAll();

            foreach (var aovHistory in m_AOVHistoryRTSystem)
            {
                var historySystem = aovHistory.Value;
                historySystem.ReleaseAll();
            }
        }

        Rect GetPixelRect()
        {
            if (m_OverridePixelRect != null)
                return m_OverridePixelRect.Value;
            else
                return new Rect(camera.pixelRect.x, camera.pixelRect.y, camera.pixelWidth, camera.pixelHeight);
        }

        internal BufferedRTHandleSystem GetHistoryRTHandleSystem()
        {
            return m_HistoryRTSystem;
        }

        internal void BindHistoryRTHandleSystem(BufferedRTHandleSystem historyRTSystem)
        {
            m_HistoryRTSystem = historyRTSystem;
        }

        internal BufferedRTHandleSystem GetHistoryRTHandleSystem(AOVRequestData aovRequest)
        {
            if (m_AOVHistoryRTSystem.TryGetValue(aovRequest, out var aovHistory))
            {
                return aovHistory;
            }
            else
            {
                var newHistory = new BufferedRTHandleSystem();
                m_AOVHistoryRTSystem.Add(aovRequest, newHistory);
                return newHistory;
            }
        }

#if ENABLE_VIRTUALTEXTURES
        internal void ResolveVirtualTextureFeedback(RenderGraph renderGraph, TextureHandle vtFeedbackBuffer)
        {
            virtualTextureFeedback.Resolve(renderGraph, this, vtFeedbackBuffer);
        }

#endif
        #endregion
    }
}
