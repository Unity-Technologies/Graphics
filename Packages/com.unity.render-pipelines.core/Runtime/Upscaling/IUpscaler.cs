#if ENABLE_UPSCALER_FRAMEWORK
using System;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Defines the essential contract for any upscaling technology.
    /// </summary>
    public interface IUpscaler : IRenderGraphRecorder
    {
        #region PROPERTIES
        /// <summary>
        /// Gets the display name of the upscaler (e.g., "FSR2").
        /// </summary>
        string name { get; }

        /// <summary>
        /// Gets the options for this particular upscaler.
        /// </summary>
        UpscalerOptions options { get; }

        /// <summary>
        /// Returns true if the upscaler uses temporal information from previous frames.
        /// </summary>
        bool isTemporal { get; }

        /// <summary>
        /// Returns true if the upscaler supports sharpening within the upscaling pass.
        /// </summary>
        bool supportsSharpening { get; }

        /// <summary>
        /// Returns true if the upscaler supports XR rendering.
        /// </summary>
        bool supportsXR { get; }
        #endregion

        #region METHODS
        /// <summary>
        /// Calculates the pixel jitter for the current frame.
        /// </summary>
        /// <param name="frameIndex">The index of the current frame, used to cycle through jitter patterns.</param>
        /// <param name="upscaleRatio">The ratio of output resolution to input resolution (e.g., 2.0 for 1080p → 4K).</param>
        /// <param name="jitter">Outputs the calculated sub-pixel jitter vector.</param>
        /// <param name="allowScaling">Outputs whether the jitter vector permits scaling relative to resolution.</param>
        void CalculateJitter(int frameIndex, float upscaleRatio, out Vector2 jitter, out bool allowScaling);

        /// <summary>
        /// Determines the render resolution based on display resolution and optional internal upscaler state or options.
        /// </summary>
        /// <param name="preUpscaleResolution">The rendering resolution prior to upscaling. This is passed by reference and can be modified.</param>
        /// <param name="postUpscaleResolution">The target display or output resolution.</param>
        void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution);

        /// <summary>
        /// Creates a new context for this upscaler. Upscaler authors implement this to create per-camera state.
        /// Called by the context manager when a new camera needs upscaling or when an existing context is invalidated.
        /// </summary>
        /// <param name="options">The upscaler options to create the context with.</param>
        /// <param name="displayResolution">The target display resolution.</param>
        /// <returns>A new context instance, or null for spatial upscalers that don't need context.</returns>
        IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution);

        /// <summary>
        /// Calculates the recommended global mip bias for texture sampling during rendering.
        /// Temporal upscalers typically recommend a negative bias to sample finer mip levels,
        /// providing more detail for temporal accumulation.
        /// </summary>
        /// <remarks>
        /// The standard formula is <c>log2(renderWidth / displayWidth)</c>, which produces negative
        /// values when upscaling (e.g., -1.0 for 2x upscaling). Some upscalers recommend an additional
        /// offset (e.g., -1.0) for sharper results.
        /// When a temporal upscaler is active, the render pipeline should use this value directly,
        /// bypassing any TAA mip bias settings.
        /// The render pipelines must guarantee this method is only called when upscaling is active
        /// (preUpscaleResolution &lt; postUpscaleResolution). Implementations do not need to handle
        /// the no-upscaling case.
        /// </remarks>
        /// <param name="preUpscaleResolution">The render resolution before upscaling.</param>
        /// <param name="postUpscaleResolution">The target display resolution after upscaling.</param>
        /// <returns>The recommended mip bias value (typically negative when upscaling).</returns>
        float CalculateMipBias(Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution);
        #endregion
    }

    /// <summary>
    /// Base class for an upscaling technology implementation.
    /// </summary>
    public abstract class AbstractUpscaler : IUpscaler
    {
        /// <inheritdoc cref="IUpscaler.name"/>
        public abstract string name { get; }

        /// <inheritdoc cref="IUpscaler.isTemporal"/>
        public abstract bool isTemporal { get; }

        /// <inheritdoc cref="IUpscaler.supportsSharpening"/>
        public abstract bool supportsSharpening { get; }


        /// <inheritdoc cref="IUpscaler.options"/>
        public virtual UpscalerOptions options => null;

        /// <inheritdoc cref="IUpscaler.supportsXR"/>
        public virtual bool supportsXR => false;

        /// <inheritdoc cref="IUpscaler.NegotiatePreUpscaleResolution(ref Vector2Int, Vector2Int)"/>
        public virtual void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution) {}

        /// <inheritdoc cref="IUpscaler.CreateContext(UpscalerOptions, Vector2Int)"/>
        public virtual IUpscalerContext CreateContext(UpscalerOptions options, Vector2Int displayResolution) => null;

        /// <inheritdoc cref="IUpscaler.CalculateJitter(int, float, out Vector2, out bool)" />
        public virtual void CalculateJitter(int frameIndex, float upscaleRatio, out Vector2 jitter, out bool allowScaling)
        {
            jitter = -STP.Jit16(frameIndex);
            allowScaling = false;
        }

        /// <inheritdoc cref="IUpscaler.CalculateMipBias(Vector2Int, Vector2Int)"/>
        public virtual float CalculateMipBias(Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution)
        {
            // Standard formula: log2(renderRes / displayRes)
            // Returns negative values when upscaling (e.g., -1.0 for 2x upscaling)
            // Use minimum of both axes to ensure sufficient detail for non-uniform scaling
            // Note: Pipeline ensures this is only called when actually upscaling (preUpscale < postUpscale)
            float xBias = Mathf.Log((float)preUpscaleResolution.x / postUpscaleResolution.x, 2f);
            float yBias = Mathf.Log((float)preUpscaleResolution.y / postUpscaleResolution.y, 2f);
            return Mathf.Min(xBias, yBias);
        }

        /// <inheritdoc cref="IRenderGraphRecorder.RecordRenderGraph"/>
        public virtual void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) { }
    }

    /// <summary>
    /// Represents a per-camera context for temporal upscalers.
    /// Each camera (and XR view) gets its own context to maintain separate temporal history.
    /// Spatial upscalers that don't need history can return null from CreateContext().
    /// </summary>
    public interface IUpscalerContext
    {
        /// <summary>
        /// The display resolution this context was created for. Upscaler authors set this at creation time.
        /// The context manager reads it to detect resolution and options changes that require context recreation.
        /// </summary>
        Vector2Int createdForDisplayResolution { get; }

        /// <summary>
        /// The frame number when this context was last used.
        /// Set automatically by the context manager each frame to track when unused contexts should be cleaned up.
        /// </summary>
        int lastUsedFrame { get; set; }

        /// <summary>
        /// Checks if this context is still valid for the given options. Upscaler authors implement the validation logic.
        /// The context manager calls this each frame to determine if the context needs recreation.
        /// </summary>
        /// <param name="options">The current upscaler options to validate against.</param>
        /// <returns>True if the context is still valid, false if it needs to be recreated.</returns>
        bool IsValidForOptions(UpscalerOptions options);

        /// <summary>
        /// Releases native resources associated with this context. Upscaler authors implement this to release GPU resources
        /// when plugins or native code require command buffer access for cleanup.
        /// Called automatically by the context manager when the context expires or needs recreation.
        /// </summary>
        /// <param name="cmd">The command buffer to record cleanup commands into.</param>
        void Cleanup(CommandBuffer cmd);
    }

    /// <summary>
    /// Base class for plugin-based upscaler contexts (DLSS, FSR, XeSS, etc.).
    /// Handles common patterns: resolution tracking, type-safe validation, and cleanup.
    /// Plugin upscalers extend this class and implement the abstract methods.
    /// </summary>
    /// <typeparam name="TNativeContext">The native context type from the plugin (e.g., DLSSContext, FSR2Context).</typeparam>
    /// <typeparam name="TOptions">The options type for this upscaler (e.g., DLSSOptions, FSR2Options).</typeparam>
    public abstract class PluginUpscalerContext<TNativeContext, TOptions> : IUpscalerContext
        where TNativeContext : class
        where TOptions : UpscalerOptions
    {
        /// <summary>
        /// The native context from the plugin. Subclasses create this via GetOrCreateNativeContext().
        /// </summary>
        protected TNativeContext m_NativeContext;

        /// <inheritdoc/>
        public Vector2Int createdForDisplayResolution { get; }

        /// <inheritdoc/>
        public int lastUsedFrame { get; set; }

        /// <summary>
        /// Returns true if the native context has been created.
        /// Use this to skip building initialization settings when the context already exists.
        /// </summary>
        public bool hasNativeContext => m_NativeContext != null;

        /// <summary>
        /// Creates a new plugin upscaler context for the specified display resolution.
        /// </summary>
        /// <param name="displayResolution">The target display resolution.</param>
        protected PluginUpscalerContext(Vector2Int displayResolution)
        {
            createdForDisplayResolution = displayResolution;
        }

        /// <summary>
        /// Destroys the native context using the plugin API.
        /// Called by the base class Cleanup() method.
        /// </summary>
        /// <param name="cmd">The command buffer to record destruction commands into.</param>
        /// <param name="context">The native context to destroy.</param>
        protected abstract void DestroyNativeContext(CommandBuffer cmd, TNativeContext context);

        /// <summary>
        /// Validates whether the current options match the options this context was created with.
        /// Subclasses compare tracked option values against the provided options.
        /// </summary>
        /// <param name="options">The current options to validate against.</param>
        /// <returns>True if the context is still valid for these options.</returns>
        protected abstract bool ValidateOptions(TOptions options);

        /// <inheritdoc/>
        public bool IsValidForOptions(UpscalerOptions options)
        {
            return options is TOptions typed && ValidateOptions(typed);
        }

        /// <inheritdoc/>
        public void Cleanup(CommandBuffer cmd)
        {
            if (m_NativeContext != null)
            {
                DestroyNativeContext(cmd, m_NativeContext);
                m_NativeContext = null;
            }
        }
    }

    /// <summary>
    /// Defines the inputs and outputs required for an upscaling pass.
    /// </summary>
    public class UpscalingIO : ContextItem
    {
        #region DEFINITIONS
        /// <summary>
        /// Defines how motion vector values should be interpreted by the upscaler.
        /// Upscalers (e.g., DLSS, FSR) typically expect screen space values representing motion from the current frame to the previous frame.
        /// Since Render Pipelines may use different configurations (e.g., NDC), this enum specifies the domain to derive the correct scaling factor.
        /// </summary>
        public enum MotionVectorDomain
        {
            /// <summary>
            /// Normalized Device Coordinates: [-1, 1] for X and Y.
            /// </summary>
            NDC,

            /// <summary>
            /// Screen Space Coordinates: [-Width, Width] for X, [-Height, Height] for Y.
            /// </summary>
            ScreenSpace,
        }

        /// <summary>
        /// Defines the temporal direction of the motion vectors.
        /// </summary>
        public enum MotionVectorDirection
        {
            /// <summary>
            /// Motion points from the previous frame to the current frame.
            /// </summary>
            PreviousFrameToCurrentFrame,

            /// <summary>
            /// Motion points from the current frame to the previous frame.
            /// </summary>
            CurrentFrameToPreviousFrame
        }
        #endregion

        #region BACKING_FIELDS
        // Context
        private IUpscalerContext m_Context;

        // Texture I/O
        private TextureHandle m_CameraColor;
        private TextureHandle m_CameraDepth;
        private TextureHandle m_MotionVectorColor;
        private TextureHandle m_ExposureTexture;
        private Vector2Int m_PreUpscaleResolution;
        private Vector2Int m_PreviousPreUpscaleResolution;
        private Vector2Int m_PostUpscaleResolution;
        private bool m_EnableTexArray;
        private bool m_InvertedDepth;
        private bool m_FlippedY;
        private bool m_FlippedX;
        private bool m_HdrInput;
        private Vector2Int m_MotionVectorTextureSize;
        private MotionVectorDomain m_MotionVectorDomain;
        private MotionVectorDirection m_MotionVectorDirection;
        private bool m_JitteredMotionVectors;
        private Texture2D[] m_BlueNoiseTextureSet;

        // Camera
        private ulong m_CameraInstanceID;
        private float m_NearClipPlane;
        private float m_FarClipPlane;
        private float m_FieldOfViewDegrees;
        private int m_NumActiveViews;
        private Vector3[] m_WorldSpaceCameraPositions;
        private Vector3[] m_PreviousWorldSpaceCameraPositions;
        private Vector3[] m_PreviousPreviousWorldSpaceCameraPositions;
        private Matrix4x4[] m_ProjectionMatrices;
        private Matrix4x4[] m_PreviousProjectionMatrices;
        private Matrix4x4[] m_PreviousPreviousProjectionMatrices;
        private Matrix4x4[] m_ViewMatrices;
        private Matrix4x4[] m_PreviousViewMatrices;
        private Matrix4x4[] m_PreviousPreviousViewMatrices;
        private float m_PreExposureValue;
        private HDROutputUtils.HDRDisplayInformation m_HdrDisplayInformation;

        // Time
        private bool m_ResetHistory;
        private int m_FrameIndex;
        private float m_DeltaTime;
        private float m_PreviousDeltaTime;

        // Misc
        private bool m_EnableMotionScaling;
        private bool m_EnableHwDrs;
        #endregion

        #region TEXTURE_IO
        /// <summary>
        /// The input color texture to be upscaled.
        /// </summary>
        public TextureHandle cameraColor
        {
            get { return m_CameraColor; }
            set { m_CameraColor = value; }
        }

        /// <summary>
        /// The depth texture associated with the camera color.
        /// </summary>
        public TextureHandle cameraDepth
        {
            get { return m_CameraDepth; }
            set { m_CameraDepth = value; }
        }

        /// <summary>
        /// The texture containing per-pixel motion vectors.
        /// </summary>
        public TextureHandle motionVectorColor
        {
            get { return m_MotionVectorColor; }
            set { m_MotionVectorColor = value; }
        }

        /// <summary>
        /// The texture containing exposure data, typically 1x1.
        /// </summary>
        public TextureHandle exposureTexture
        {
            get { return m_ExposureTexture; }
            set { m_ExposureTexture = value; }
        }

        /// <summary>
        /// The resolution of the source image before upscaling.
        /// </summary>
        public Vector2Int preUpscaleResolution
        {
            get { return m_PreUpscaleResolution; }
            set { m_PreUpscaleResolution = value; }
        }

        /// <summary>
        /// The resolution of the source image from the previous frame.
        /// </summary>
        public Vector2Int previousPreUpscaleResolution
        {
            get { return m_PreviousPreUpscaleResolution; }
            set { m_PreviousPreUpscaleResolution = value; }
        }

        /// <summary>
        /// The target resolution after upscaling.
        /// </summary>
        public Vector2Int postUpscaleResolution
        {
            get { return m_PostUpscaleResolution; }
            set { m_PostUpscaleResolution = value; }
        }

        /// <summary>
        /// Indicates if texture arrays are enabled/supported for input textures.
        /// </summary>
        public bool enableTexArray
        {
            get { return m_EnableTexArray; }
            set { m_EnableTexArray = value; }
        }

        /// <summary>
        /// Indicates if the depth buffer is inverted (Near: 1.0, Far: 0.0).
        /// </summary>
        public bool invertedDepth
        {
            get { return m_InvertedDepth; }
            set { m_InvertedDepth = value; }
        }

        /// <summary>
        /// Indicates if the Y-axis is flipped (upside down).
        /// </summary>
        public bool flippedY
        {
            get { return m_FlippedY; }
            set { m_FlippedY = value; }
        }

        /// <summary>
        /// Indicates if the X-axis is flipped (right to left).
        /// </summary>
        public bool flippedX
        {
            get { return m_FlippedX; }
            set { m_FlippedX = value; }
        }

        /// <summary>
        /// Indicates if the input color texture contains HDR data.
        /// </summary>
        public bool hdrInput
        {
            get { return m_HdrInput; }
            set { m_HdrInput = value; }
        }

        /// <summary>
        /// The actual size of the motion vector texture, which may differ from the render resolution.
        /// </summary>
        public Vector2Int motionVectorTextureSize
        {
            get { return m_MotionVectorTextureSize; }
            set { m_MotionVectorTextureSize = value; }
        }

        /// <summary>
        /// Specifies the coordinate space used within the motion vector texture.
        /// </summary>
        public MotionVectorDomain motionVectorDomain
        {
            get { return m_MotionVectorDomain; }
            set { m_MotionVectorDomain = value; }
        }

        /// <summary>
        /// Specifies the temporal direction of the motion vectors.
        /// </summary>
        public MotionVectorDirection motionVectorDirection
        {
            get { return m_MotionVectorDirection; }
            set { m_MotionVectorDirection = value; }
        }

        /// <summary>
        /// Indicates if the motion vectors include the camera jitter offset.
        /// </summary>
        public bool jitteredMotionVectors
        {
            get { return m_JitteredMotionVectors; }
            set { m_JitteredMotionVectors = value; }
        }

        /// <summary>
        /// A set of blue noise textures used for dithering or other stochastic effects during upscaling.
        /// </summary>
        public Texture2D[] blueNoiseTextureSet
        {
            get { return m_BlueNoiseTextureSet; }
            set { m_BlueNoiseTextureSet = value; }
        }
        #endregion

        #region CAMERA
        /// <summary>
        /// The unique instance ID of the camera rendering this frame.
        /// </summary>
        public ulong cameraInstanceID
        {
            get { return m_CameraInstanceID; }
            set { m_CameraInstanceID = value; }
        }

        /// <summary>
        /// The distance to the near clipping plane.
        /// </summary>
        public float nearClipPlane
        {
            get { return m_NearClipPlane; }
            set { m_NearClipPlane = value; }
        }

        /// <summary>
        /// The distance to the far clipping plane.
        /// </summary>
        public float farClipPlane
        {
            get { return m_FarClipPlane; }
            set { m_FarClipPlane = value; }
        }

        /// <summary>
        /// The vertical field of view in degrees.
        /// </summary>
        public float fieldOfViewDegrees
        {
            get { return m_FieldOfViewDegrees; }
            set { m_FieldOfViewDegrees = value; }
        }

        /// <summary>
        /// The number of active views (e.g., 2 for stereo rendering).
        /// </summary>
        public int numActiveViews
        {
            get { return m_NumActiveViews; }
            set { m_NumActiveViews = value; }
        }

        /// <summary>
        /// The camera positions in world space for the current frame.
        /// </summary>
        public Vector3[] worldSpaceCameraPositions
        {
            get { return m_WorldSpaceCameraPositions; }
            set { m_WorldSpaceCameraPositions = value; }
        }

        /// <summary>
        /// The camera positions in world space for the previous frame.
        /// </summary>
        public Vector3[] previousWorldSpaceCameraPositions
        {
            get { return m_PreviousWorldSpaceCameraPositions; }
            set { m_PreviousWorldSpaceCameraPositions = value; }
        }

        /// <summary>
        /// The camera positions in world space for the frame before the previous one.
        /// </summary>
        public Vector3[] previousPreviousWorldSpaceCameraPositions
        {
            get { return m_PreviousPreviousWorldSpaceCameraPositions; }
            set { m_PreviousPreviousWorldSpaceCameraPositions = value; }
        }

        /// <summary>
        /// The projection matrices for the current frame.
        /// </summary>
        public Matrix4x4[] projectionMatrices
        {
            get { return m_ProjectionMatrices; }
            set { m_ProjectionMatrices = value; }
        }

        /// <summary>
        /// The projection matrices for the previous frame.
        /// </summary>
        public Matrix4x4[] previousProjectionMatrices
        {
            get { return m_PreviousProjectionMatrices; }
            set { m_PreviousProjectionMatrices = value; }
        }

        /// <summary>
        /// The projection matrices for the frame before the previous one.
        /// </summary>
        public Matrix4x4[] previousPreviousProjectionMatrices
        {
            get { return m_PreviousPreviousProjectionMatrices; }
            set { m_PreviousPreviousProjectionMatrices = value; }
        }

        /// <summary>
        /// The view matrices for the current frame.
        /// </summary>
        public Matrix4x4[] viewMatrices
        {
            get { return m_ViewMatrices; }
            set { m_ViewMatrices = value; }
        }

        /// <summary>
        /// The view matrices for the previous frame.
        /// </summary>
        public Matrix4x4[] previousViewMatrices
        {
            get { return m_PreviousViewMatrices; }
            set { m_PreviousViewMatrices = value; }
        }

        /// <summary>
        /// The view matrices for the frame before the previous one.
        /// </summary>
        public Matrix4x4[] previousPreviousViewMatrices
        {
            get { return m_PreviousPreviousViewMatrices; }
            set { m_PreviousPreviousViewMatrices = value; }
        }

        /// <summary>
        /// The pre-exposure value applied to the lighting accumulation buffer.
        /// Some implementations (e.g., HDRP) apply exposure before tonemapping.
        /// Upscalers (DLSS/FSR) need this value to reconstruct the current frame without ghosting artifacts,
        /// usually obtained via a CPU readback on the 1x1 exposure texture.
        /// </summary>
        public float preExposureValue
        {
            get { return m_PreExposureValue; }
            set { m_PreExposureValue = value; }
        }

        /// <summary>
        /// Information required to convert HDR color gamuts to SDR.
        /// This is used because some upscalers do not natively support specific HDR color gamuts.
        /// </summary>
        public HDROutputUtils.HDRDisplayInformation hdrDisplayInformation
        {
            get { return m_HdrDisplayInformation; }
            set { m_HdrDisplayInformation = value; }
        }
        #endregion

        #region TIME
        /// <summary>
        /// Indicates whether the upscaler history should be cleared (e.g., on camera cuts).
        /// </summary>
        public bool resetHistory
        {
            get { return m_ResetHistory; }
            set { m_ResetHistory = value; }
        }

        /// <summary>
        /// The current frame index.
        /// </summary>
        public int frameIndex
        {
            get { return m_FrameIndex; }
            set { m_FrameIndex = value; }
        }

        /// <summary>
        /// The time elapsed since the last frame.
        /// </summary>
        public float deltaTime
        {
            get { return m_DeltaTime; }
            set { m_DeltaTime = value; }
        }

        /// <summary>
        /// The time elapsed between the previous frame and the one before it.
        /// </summary>
        public float previousDeltaTime
        {
            get { return m_PreviousDeltaTime; }
            set { m_PreviousDeltaTime = value; }
        }
        #endregion

        #region MISC
        /// <summary>
        /// Indicates if motion vector scaling is enabled.
        /// </summary>
        public bool enableMotionScaling
        {
            get { return m_EnableMotionScaling; }
            set { m_EnableMotionScaling = value; }
        }

        /// <summary>
        /// Indicates if Hardware Dynamic Resolution Scaling (HW DRS) is enabled.
        /// </summary>
        public bool enableHwDrs
        {
            get { return m_EnableHwDrs; }
            set { m_EnableHwDrs = value; }
        }
        #endregion

        #region CONTEXT
        /// <summary>
        /// The per-camera context for the active upscaler.
        /// Populated by the pipeline before calling RecordRenderGraph().
        /// Temporal upscalers read this to access their history buffers.
        /// Spatial upscalers that don't need context will have this set to null.
        /// </summary>
        public IUpscalerContext context
        {
            get { return m_Context; }
            set { m_Context = value; }
        }

        /// <summary>
        /// The sub-pixel jitter offset applied to the projection matrix this frame.
        /// Populated by the pipeline after calling IUpscaler.CalculateJitter().
        /// Values are typically in the range [-0.5, 0.5].
        /// Temporal upscalers pass this to their native APIs to inform them of the jitter applied.
        /// </summary>
        public Vector2 subpixelJitter
        {
            get { return m_SubpixelJitter; }
            set { m_SubpixelJitter = value; }
        }
        private Vector2 m_SubpixelJitter;
        #endregion


        /// <inheritdoc cref="ContextItem.Reset()"/>
        public override void Reset()
        {
            context = null;
            subpixelJitter = Vector2.zero;
            cameraColor = TextureHandle.nullHandle;
            cameraDepth = TextureHandle.nullHandle;
            motionVectorColor = TextureHandle.nullHandle;
            exposureTexture = TextureHandle.nullHandle;
            preUpscaleResolution = new();
            previousPreUpscaleResolution = new();
            postUpscaleResolution = new();
            enableTexArray = false;
            invertedDepth = false;
            flippedX = false;
            flippedY = false;
            hdrInput = false;
            motionVectorTextureSize = new();
            motionVectorDomain = MotionVectorDomain.NDC;
            motionVectorDirection = MotionVectorDirection.PreviousFrameToCurrentFrame;
            jitteredMotionVectors = false;
            blueNoiseTextureSet = null;

            cameraInstanceID = ulong.MaxValue;
            nearClipPlane = 0f;
            farClipPlane = 0f;
            fieldOfViewDegrees = 0f;
            numActiveViews = 0;
            worldSpaceCameraPositions = Array.Empty<Vector3>();
            previousWorldSpaceCameraPositions = Array.Empty<Vector3>();
            previousPreviousWorldSpaceCameraPositions = Array.Empty<Vector3>();
            projectionMatrices = Array.Empty<Matrix4x4>();
            previousProjectionMatrices = Array.Empty<Matrix4x4>();
            previousPreviousProjectionMatrices = Array.Empty<Matrix4x4>();
            viewMatrices = Array.Empty<Matrix4x4>();
            previousViewMatrices = Array.Empty<Matrix4x4>();
            previousPreviousViewMatrices = Array.Empty<Matrix4x4>();
            preExposureValue = 1.0f;
            hdrDisplayInformation = new HDROutputUtils.HDRDisplayInformation();

            resetHistory = true;
            frameIndex = 0;
            deltaTime = 0f;
            previousDeltaTime = 0f;

            enableMotionScaling = false;
            enableHwDrs = false;
        }
    }
}
#endif
