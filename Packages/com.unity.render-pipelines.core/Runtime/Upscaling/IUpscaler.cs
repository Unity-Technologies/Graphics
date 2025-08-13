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
        /// <summary>
        /// Gets the display name of the upscaler (e.g., "FSR2").
        /// </summary>
        string GetName();

        /// <summary>
        /// Gets the options for this particular upscaler.
        /// </summary>
        UpscalerOptions GetOptions();

        /// <summary>
        /// Returns true if the upscaler uses temporal information from previous frames.
        /// </summary>
        bool IsTemporalUpscaler();

        /// <summary>
        /// Returns true if the upscaler supports XR rendering.
        /// </summary>
        bool IsSupportedXR();


        /// <summary>
        /// Calculates the pixel jitter for the current frame.
        /// </summary>
        void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling);

        /// <summary>
        /// Determines the render resolution based on display resolution and optional internal state or options.
        /// </summary>
        void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution);
    }

    public abstract class AbstractUpscaler : IUpscaler
    {
        public abstract string GetName();
        public abstract bool IsTemporalUpscaler();

        public virtual UpscalerOptions GetOptions() { return null; }
        public virtual bool IsSupportedXR() { return false; }
        public virtual void NegotiatePreUpscaleResolution(ref Vector2Int preUpscaleResolution, Vector2Int postUpscaleResolution) {}
        public virtual void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling)
        {
            jitter = -STP.Jit16(frameIndex);
            allowScaling = false;
        }

        public virtual void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) { }
    }

    public class UpscalingIO : ContextItem
    {
        // Upscalers like DLSS & FSR expect screen space values representing motion
        // from _current frame to previous frame_. SRPs can have different configurations
        // for motion vectors than the requirements, so we need to specify to the upscaler
        // inputs on how to treat the values in the MV textures. They usually include a scaling
        // factor which can be derived from the enums below.
        public enum MotionVectorDomain
        {
            NDC,         // [-1, 1] for X & Y
            ScreenSpace, // [-W, W] for X, [-H, H] for Y
        };
        public enum MotionVectorDirection
        {
            PreviousFrameToCurrentFrame,
            CurrentFrameToPreviousFrame
        };

        // -------------------------------------------------------
        // TEXTURE I/O
        // -------------------------------------------------------
        public TextureHandle cameraColor;
        public TextureHandle cameraDepth;
        public TextureHandle motionVectorColor;
        public TextureHandle exposureTexture;
        public Vector2Int preUpscaleResolution;
        public Vector2Int previousPreUpscaleResolution;
        public Vector2Int postUpscaleResolution;
        public bool enableTexArray;
        public bool invertedDepth; // near plane: 1.0f, far plane: 0.0f
        public bool flippedY; // upside down
        public bool flippedX; // right to left
        public bool hdrInput;
        public Vector2Int motionVectorTextureSize;
        public MotionVectorDomain motionVectorDomain;
        public MotionVectorDirection motionVectorDirection;
        public bool jitteredMotionVectors;
        public Texture2D[] blueNoiseTextureSet;

        // -------------------------------------------------------
        // CAMERA
        // -------------------------------------------------------
        public int cameraInstanceID;
        public float nearClipPlane;
        public float farClipPlane;
        public float fieldOfViewDegrees;
        public int numActiveViews;
        public int eyeIndex;
        public Vector3[] worldSpaceCameraPositions;
        public Vector3[] previousWorldSpaceCameraPositions;
        public Vector3[] previousPreviousWorldSpaceCameraPositions;
        public Matrix4x4[] projectionMatrices;
        public Matrix4x4[] previousProjectionMatrices;
        public Matrix4x4[] previousPreviousProjectionMatrices;
        public Matrix4x4[] viewMatrices;
        public Matrix4x4[] previousViewMatrices;
        public Matrix4x4[] previousPreviousViewMatrices;

        // Some implementations (HDRP) has the exposure value pre-applied
        // to the lighting accumulation buffer as opposed to doing exposure
        // in tonemapper. Some upscalers (DLSS/FSR) need to know about this
        // exposure value used in the (previous) color input so that it can properly
        // construct the current frame without ghosting artifacts while ignoring
        // the exposure texture input listed above. This value is usually obtained
        // by a CPU readback on the 1x1 exposure texture.
        public float preExposureValue;

        // Some upscalers can't support HDR color gamuts.
        // Therefore, use HDRDisplayInformation to temporarily convert to SDR.
        public HDROutputUtils.HDRDisplayInformation hdrDisplayInformation;

        // -------------------------------------------------------
        // TIME
        // -------------------------------------------------------
        public bool resetHistory;
        public int frameIndex;
        public float deltaTime;
        public float previousDeltaTime;

        // -------------------------------------------------------
        // MISC
        // -------------------------------------------------------
        public bool enableMotionScaling;
        public bool enableHwDrs;

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

            cameraInstanceID = -1;
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
