#if ENABLE_UPSCALER_FRAMEWORK
using System;
using UnityEditor;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Selects how an upscaler's render resolution is determined. Only meaningful for upscalers that expose a quality
    /// mode (<see cref="IUpscaler.hasQualityMode"/>); upscalers without one always behave as <see cref="CustomScaling"/>.
    /// </summary>
    public enum UpscalerResolutionMode
    {
        /// <summary>
        /// The upscaler's quality preset determines a single fixed render resolution (for example a "Quality" preset,
        /// or a 1:1 native preset). The pipeline renders at that resolution; any pipeline-level scaling
        /// (render scale or dynamic resolution) is ignored for the camera.
        /// </summary>
        QualityMode,

        /// <summary>
        /// The pipeline drives the render resolution — via render scale, dynamic resolution (adaptive or forced),
        /// or a custom scale — and the upscaler only constrains it to the hard <see cref="UpscalerResolutionInfo.minResolution"/>/
        /// <see cref="UpscalerResolutionInfo.maxResolution"/> limits it supports (no clamp at all when it has no quality
        /// mode). This is the "dynamic / driven" mode, as opposed to the fixed <see cref="QualityMode"/>.
        /// </summary>
        CustomScaling,
    }

    /// <summary>
    /// How an upscaler constrains the pipeline's render resolution. This is the resolved result reported via
    /// <see cref="UpscalerResolutionInfo.constraint"/> — the upscaler's capabilities combined with the user-selected
    /// <see cref="UpscalerResolutionMode"/> — not a user-facing setting.
    /// </summary>
    public enum UpscalerResolutionConstraint
    {
        /// <summary>
        /// No constraint: the pipeline drives the render resolution unconstrained (e.g. STP, spatial filters, or FSR2 in
        /// custom scaling, which has no hard per-quality range).
        /// </summary>
        None,

        /// <summary>
        /// Pinned: the pipeline renders exactly at <see cref="UpscalerResolutionInfo.qualityModeResolution"/> and ignores
        /// whatever render-resolution scaling it would otherwise apply (render scale, dynamic resolution).
        /// </summary>
        Fixed,

        /// <summary>
        /// Bounded: the pipeline drives the render resolution (render scale / dynamic resolution / forced) but must clamp
        /// it to <see cref="UpscalerResolutionInfo.minResolution"/>..<see cref="UpscalerResolutionInfo.maxResolution"/>
        /// (the upscaler's hard limits).
        /// </summary>
        Range,
    }

    /// <summary>
    /// Resolution information from an upscaler for a given display resolution and quality mode.
    /// Used by pipelines to configure Dynamic Resolution Scaling bounds or display resolution info in UI.
    /// </summary>
    /// <remarks>
    /// The <see cref="constraint"/> tells the pipeline how to select the render resolution:
    /// <list type="bullet">
    /// <item><see cref="UpscalerResolutionConstraint.Fixed"/> — render exactly at <see cref="qualityModeResolution"/>;
    /// the pipeline does not drive the resolution. [<see cref="Fixed"/>]</item>
    /// <item><see cref="UpscalerResolutionConstraint.Range"/> — the pipeline drives the render resolution
    /// (render scale / dynamic resolution / forced) and must clamp it to <see cref="minResolution"/>..<see cref="maxResolution"/>
    /// (the upscaler's hard limits). [<see cref="Range"/>]</item>
    /// <item><see cref="UpscalerResolutionConstraint.None"/> — the pipeline drives the render resolution, unconstrained.</item>
    /// </list>
    /// </remarks>
    public struct UpscalerResolutionInfo
    {
        /// <summary>
        /// Recommended render resolution for the current quality mode.
        /// This is the "optimal" resolution the upscaler suggests for best quality/performance balance.
        /// Authoritative when <see cref="constraint"/> is <see cref="UpscalerResolutionConstraint.Fixed"/>.
        /// </summary>
        public Vector2Int qualityModeResolution;

        /// <summary>
        /// How the upscaler constrains the render resolution (pin / bounded range / no constraint). See
        /// <see cref="UpscalerResolutionConstraint"/> and the type remarks. DLSS and XeSS expose per-quality min/max
        /// ranges (<see cref="UpscalerResolutionConstraint.Range"/>); FSR2 does not (global DRS support only).
        /// </summary>
        public UpscalerResolutionConstraint constraint;

        /// <summary>
        /// Minimum resolution when <see cref="constraint"/> is <see cref="UpscalerResolutionConstraint.Range"/>.
        /// The upscaler guarantees acceptable quality down to this resolution.
        /// Equals <see cref="qualityModeResolution"/> otherwise.
        /// </summary>
        public Vector2Int minResolution;

        /// <summary>
        /// Maximum resolution when <see cref="constraint"/> is <see cref="UpscalerResolutionConstraint.Range"/>.
        /// This is typically the upper bound before the upscaler stops providing benefit.
        /// Equals <see cref="qualityModeResolution"/> otherwise.
        /// </summary>
        public Vector2Int maxResolution;

        /// <summary>
        /// Advisory lower bound on the render scale (render resolution ÷ display resolution): below this the upscaler
        /// still works but its quality degrades (e.g. FSR2 past its recommended ~3x maximum upscale ratio, i.e. ~0.33).
        /// It is a resolution-independent ratio, unlike the hard <see cref="minResolution"/>/<see cref="maxResolution"/>
        /// pixel bounds. This is <b>never clamped to</b> — consumers may surface an informational notice when the render
        /// scale falls below it, but must keep rendering at the chosen scale. Defaults to 0 meaning "no recommendation".
        /// </summary>
        public float recommendedMinScale;

        /// <summary>
        /// Creates resolution info for a fixed resolution (no DRS range).
        /// </summary>
        /// <param name="resolution">The fixed optimal resolution.</param>
        /// <returns>Resolution info with <see cref="constraint"/> = <see cref="UpscalerResolutionConstraint.Fixed"/>.</returns>
        public static UpscalerResolutionInfo Fixed(Vector2Int resolution)
        {
            return new UpscalerResolutionInfo
            {
                qualityModeResolution = resolution,
                constraint = UpscalerResolutionConstraint.Fixed,
                minResolution = resolution,
                maxResolution = resolution,
            };
        }

        /// <summary>
        /// Creates resolution info for a render-resolution range the pipeline drives within (render scale / dynamic
        /// resolution / forced), clamped to the upscaler's hard <paramref name="min"/>..<paramref name="max"/> limits.
        /// Use to expose a quality mode's [min,max] as a clamp (e.g. DLSS in Custom Scaling), or for any upscaler with a
        /// hard render-resolution limit. The <see cref="constraint"/> is <see cref="UpscalerResolutionConstraint.Range"/>
        /// (clamped, not pinned); when <paramref name="min"/> equals <paramref name="max"/> there is effectively no range,
        /// so the constraint is <see cref="UpscalerResolutionConstraint.None"/>.
        /// </summary>
        /// <param name="min">The minimum render resolution the upscaler supports.</param>
        /// <param name="max">The maximum render resolution (typically the display resolution).</param>
        /// <returns>Resolution info with a <see cref="UpscalerResolutionConstraint.Range"/> clamp (or <see cref="UpscalerResolutionConstraint.None"/> when min == max).</returns>
        public static UpscalerResolutionInfo Range(Vector2Int min, Vector2Int max)
        {
            return new UpscalerResolutionInfo
            {
                // Not used to select the resolution (the pipeline drives it); reported as the full-quality value for UI.
                qualityModeResolution = max,
                // A degenerate range (min == max) is no real range, so the upscaler expresses no constraint.
                constraint = (min != max) ? UpscalerResolutionConstraint.Range : UpscalerResolutionConstraint.None,
                minResolution = min,
                maxResolution = max,
            };
        }
    }


    /// <summary>
    /// Defines the essential contract for any upscaling technology.
    /// </summary>
    /// <remarks>
    /// An upscaler instance is a shared singleton: one instance per type serves every camera and XR view. Store no
    /// per-camera or per-frame state on it — per-camera state belongs in <see cref="IUpscalerContext"/>, and per-frame
    /// inputs (options, resolutions, matrices, ...) arrive via <see cref="UpscalingIO"/> each frame. In particular,
    /// read options from <c>io.options</c> (passed by the pipeline); the instance does not hold them.
    /// </remarks>
    public interface IUpscaler : IRenderGraphRecorder
    {
        #region PROPERTIES
        /// <summary>
        /// Gets the display name of the upscaler (e.g., "FSR2").
        /// </summary>
        string name { get; }

        /// <summary>
        /// Returns true if the upscaler uses temporal information from previous frames.
        /// </summary>
        bool isTemporal { get; }

        /// <summary>
        /// Returns true if the upscaler supports sharpening within the upscaling pass.
        /// </summary>
        bool supportsSharpening { get; }

        /// <summary>
        /// Returns true if the upscaler exposes a quality mode that can dictate the render resolution (e.g. the
        /// DLSS/FSR presets). When false, the upscaler has no opinion on resolution and the pipeline drives the render
        /// resolution (e.g. STP, spatial filters), which is equivalent to <see cref="UpscalerResolutionMode.CustomScaling"/>.
        /// </summary>
        bool hasQualityMode { get; }
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

        /// <summary>
        /// Gets resolution information for this upscaler given a display resolution and options.
        /// Used by pipelines to configure DRS bounds or display resolution info in UI.
        /// </summary>
        /// <remarks>
        /// DLSS and XeSS provide per-quality-mode min/max ranges via their SDK APIs.
        /// FSR2 provides only optimal resolution (no per-quality range).
        /// Spatial upscalers return the display resolution unchanged.
        ///
        /// The options parameter is provided by the framework (Upscaling.GetGlobalOptions) and enables future per-camera
        /// quality settings; pipelines pass the framework-resolved options (asset default today; per-camera later).
        ///
        /// Example usage:
        /// <code>
        /// var info = upscaler.GetResolutionInfo(displayResolution, upscaling.GetGlobalOptions(upscaler));
        /// if (info.constraint == UpscalerResolutionConstraint.Range)
        /// {
        ///     drsSettings.minPercentage = (float)info.minResolution.x / displayResolution.x * 100f;
        ///     drsSettings.maxPercentage = (float)info.maxResolution.x / displayResolution.x * 100f;
        /// }
        /// </code>
        /// </remarks>
        /// <param name="displayResolution">The target display/output resolution.</param>
        /// <param name="options">The upscaler options to use for resolution calculation.</param>
        /// <returns>Resolution info containing optimal and optional min/max resolutions.</returns>
        UpscalerResolutionInfo GetResolutionInfo(Vector2Int displayResolution, UpscalerOptions options);
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

        /// <inheritdoc cref="IUpscaler.hasQualityMode"/>
        public virtual bool hasQualityMode => false;

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

        /// <inheritdoc cref="IUpscaler.GetResolutionInfo(Vector2Int, UpscalerOptions)"/>
        /// <remarks>
        /// Default implementation reports <see cref="UpscalerResolutionConstraint.None"/>: the upscaler has no quality
        /// mode, so the pipeline drives the render resolution. Override in quality-mode upscalers to
        /// return <see cref="UpscalerResolutionInfo.Fixed"/>/<see cref="UpscalerResolutionInfo.Range"/>.
        /// </remarks>
        public virtual UpscalerResolutionInfo GetResolutionInfo(Vector2Int displayResolution, UpscalerOptions options)
        {
            // No quality mode: the pipeline drives the render resolution, unconstrained.
            return new UpscalerResolutionInfo
            {
                qualityModeResolution = displayResolution,
                minResolution = displayResolution,
                maxResolution = displayResolution,
                constraint = UpscalerResolutionConstraint.None,
            };
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
        /// Releases native resources associated with this context. Called automatically by the context manager when the
        /// context expires, needs recreation, or the upscaling system is disposed.
        /// </summary>
        /// <param name="cmd">The command buffer used to record cleanup commands into. The caller executes it.</param>
        /// <remarks>
        /// A command buffer is taken rather than this being a plain managed Dispose because some upscalers own native
        /// resources that must be released on the render thread. Implementations may record a render-thread signal here
        /// instead of GPU work, and the release may complete asynchronously.
        /// </remarks>
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
        /// The native context from the plugin. Subclasses create it lazily on first use (creation needs a CommandBuffer,
        /// so it can't happen in the constructor) and assign it here; the base class destroys it via <see cref="Cleanup"/>
        /// (calling <see cref="DestroyNativeContext"/>). The lazy-creation method is the subclass's own — e.g. DLSS's
        /// create-once <c>GetOrCreateNativeContext</c>, or FSR2's <c>EnsureNativeContext</c> which also recreates when its
        /// allocation size changes.
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
        /// Subclasses should extract and store context-creation settings (quality mode, presets)
        /// in their own fields for use in RecordRenderGraph.
        /// Per-frame settings (sharpness, etc.) should be read from io.options instead.
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
        private Vector2Int m_MaxPreUpscaleResolution;
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
        private DynamicResolutionType? m_DynamicResolution;
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
        /// The resolution of the source image rendered this frame (the actual render size; under dynamic resolution
        /// this varies frame to frame). Upscalers use this as the per-frame dispatch/subrect size.
        /// </summary>
        public Vector2Int preUpscaleResolution
        {
            get { return m_PreUpscaleResolution; }
            set { m_PreUpscaleResolution = value; }
        }

        /// <summary>
        /// The maximum render resolution the source image can reach (the allocation size), set by the pipeline. Stable
        /// across the dynamic-resolution range, so upscalers should allocate per-camera history/native contexts at this
        /// size and dispatch at <see cref="preUpscaleResolution"/> — this avoids per-frame reallocation when the render
        /// size varies. Equals <see cref="preUpscaleResolution"/> when dynamic resolution is inactive.
        /// (Note: DLSS is an exception — its feature must be created at the SDK's recommended optimal, not this max.)
        /// </summary>
        public Vector2Int maxPreUpscaleResolution
        {
            get { return m_MaxPreUpscaleResolution; }
            set { m_MaxPreUpscaleResolution = value; }
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
        /// The number of active views (e.g., 2 for stereo rendering). This is the authoritative per-view count: the
        /// per-view arrays below (positions/matrices) may be longer than this, so always iterate
        /// <c>[0, numActiveViews)</c> and never rely on <c>array.Length</c>.
        /// </summary>
        public int numActiveViews
        {
            get { return m_NumActiveViews; }
            set { m_NumActiveViews = value; }
        }

        /// <summary>
        /// Absolute world-space camera position per view, current frame (length: use <see cref="numActiveViews"/>).
        /// These are always absolute, even when <see cref="viewMatrices"/> are camera-relative (camera translation
        /// stripped from the matrix, e.g. HDRP). Reconstruct an absolute view matrix by combining the two; do not assume
        /// the view matrix carries the world translation.
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
        /// View matrices per view, current frame, in the pipeline's render space — these may be CAMERA-RELATIVE (camera
        /// translation removed, e.g. HDRP) for precision. Use <see cref="worldSpaceCameraPositions"/> for the absolute
        /// camera position; do not derive world position from the view-matrix translation. Length: use
        /// <see cref="numActiveViews"/>.
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
        /// The active Dynamic Resolution Scaling state for this frame, or <c>null</c> when DRS is inactive.
        /// <list type="bullet">
        /// <item><c>null</c> — no DRS; the render resolution is fixed this frame (fixed-ratio upscaling).</item>
        /// <item><see cref="DynamicResolutionType.Hardware"/> — render targets are allocated at display resolution
        /// and scaled via hardware memory aliasing; history/RTHandles should be sized at <see cref="postUpscaleResolution"/>.</item>
        /// <item><see cref="DynamicResolutionType.Software"/> — the render resolution may vary frame-to-frame with the
        /// target allocated at the render resolution.</item>
        /// </list>
        /// Convenience: <c>dynamicResolution.HasValue</c> means DRS is active; <c>dynamicResolution == DynamicResolutionType.Hardware</c>
        /// gates display-resolution history allocation. Upscalers using native plugin-managed history (DLSS, FSR2) can ignore the distinction.
        /// </summary>
        public DynamicResolutionType? dynamicResolution
        {
            get { return m_DynamicResolution; }
            set { m_DynamicResolution = value; }
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
        /// The sub-pixel jitter offset for this frame, in the same convention your
        /// <see cref="IUpscaler.CalculateJitter"/> returns. Pass it to your native upscaler's jitter-offset input
        /// unchanged. Values are typically in the range [-0.5, 0.5].
        /// </summary>
        /// <remarks>
        /// The pipeline derives this from your <see cref="IUpscaler.CalculateJitter"/> result and applies it to the
        /// projection before handing it back here. Read it in RecordRenderGraph; do not recompute jitter there.
        /// </remarks>
        public Vector2 subpixelJitter
        {
            get { return m_SubpixelJitter; }
            set { m_SubpixelJitter = value; }
        }
        private Vector2 m_SubpixelJitter;

        /// <summary>
        /// The current upscaler options for this frame/camera.
        /// Populated by the pipeline before calling RecordRenderGraph().
        /// Upscalers should read per-frame settings (sharpness, etc.) from this property.
        /// Context-creation settings (quality mode, presets) are stored in the context itself.
        /// </summary>
        /// <remarks>
        /// This separation enables dynamic settings changes without context recreation:
        /// - Quality mode changes: Invalidate context via IsValidForOptions(), recreate
        /// - Sharpness changes: Context stays valid, read new value from io.options
        /// </remarks>
        public UpscalerOptions options
        {
            get { return m_Options; }
            set { m_Options = value; }
        }
        private UpscalerOptions m_Options;
        #endregion


        /// <inheritdoc cref="ContextItem.Reset()"/>
        public override void Reset()
        {
            context = null;
            subpixelJitter = Vector2.zero;
            options = null;
            cameraColor = TextureHandle.nullHandle;
            cameraDepth = TextureHandle.nullHandle;
            motionVectorColor = TextureHandle.nullHandle;
            exposureTexture = TextureHandle.nullHandle;
            preUpscaleResolution = new();
            maxPreUpscaleResolution = new();
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
            dynamicResolution = null;
        }
    }
}
#endif
