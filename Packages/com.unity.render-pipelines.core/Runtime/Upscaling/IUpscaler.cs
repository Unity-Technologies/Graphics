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
        /// <param name="jitter">Outputs the calculated sub-pixel jitter vector.</param>
        /// <param name="allowScaling">Outputs whether the jitter vector permits scaling relative to resolution.</param>
        void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling);

        /// <summary>
        /// Determines the render resolution based on display resolution and optional internal upscaler state or options.
        /// </summary>
        /// <param name="preUpscaleResolution">The rendering resolution prior to upscaling. This is passed by reference and can be modified.</param>
        /// <param name="postUpscaleResolution">The target display or output resolution.</param>
        void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution);
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

        /// <inheritdoc cref="IUpscaler.CalculateJitter(int, out Vector2, out bool)" />
        public virtual void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling)
        {
            jitter = -STP.Jit16(frameIndex);
            allowScaling = false;
        }

        /// <inheritdoc cref="IRenderGraphRecorder.RecordRenderGraph"/>
        public virtual void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) { }
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
        private int m_EyeIndex;
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
        /// The index of the current eye being rendered (for XR).
        /// </summary>
        public int eyeIndex
        {
            get { return m_EyeIndex; }
            set { m_EyeIndex = value; }
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


        /// <inheritdoc cref="ContextItem.Reset()"/>
        public override void Reset()
        {
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
            eyeIndex = 0;
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
