using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>Rendering steps for time slicing realtime reflection probes.</summary>
    [Flags]
    public enum ProbeRenderSteps
    {
        /// <summary>No rendering steps needed.</summary>
        None = 0,
        /// <summary>Render reflection probe cube face 0.</summary>
        CubeFace0 = (1 << 0),
        /// <summary>Render reflection probe cube face 1.</summary>
        CubeFace1 = (1 << 1),
        /// <summary>Render reflection probe cube face 2.</summary>
        CubeFace2 = (1 << 2),
        /// <summary>Render reflection probe cube face 3.</summary>
        CubeFace3 = (1 << 3),
        /// <summary>Render reflection probe cube face 4.</summary>
        CubeFace4 = (1 << 4),
        /// <summary>Render reflection probe cube face 5.</summary>
        CubeFace5 = (1 << 5),
        /// <summary>Render planar reflection.</summary>
        Planar = (1 << 6),
        /// <summary>Increment the realtime render count, which also updates the cache for cubemap probes.</summary>
        IncrementRenderCount = (1 << 7),
        /// <summary>All steps required for a reflection probe.</summary>
        ReflectionProbeMask = CubeFace0 | CubeFace1 | CubeFace2 | CubeFace3 | CubeFace4 | CubeFace5 | IncrementRenderCount,
        /// <summary>Render planar reflection probe, always only one step.</summary>
        PlanarProbeMask = Planar | IncrementRenderCount,
    }

    /// <summary>Extension methods for ProbeRenderSteps.</summary>
    public static class ProbeRenderStepsExt
    {
        /// <summary>
        /// Test if any bits are set.
        /// </summary>
        /// <param name="steps">The probe rendering steps.</param>
        /// <returns>True if any bits are set, false otherwise.</returns>
        public static bool IsNone(this ProbeRenderSteps steps)
        {
            return steps == ProbeRenderSteps.None;
        }

        /// <summary>
        /// Test if the bit for the given cubemap face is set.
        /// </summary>
        /// <param name="steps">The probe rendering steps.</param>
        /// <param name="face">The cubemap face.</param>
        /// <returns>True if the cubemap face bit is set, false otherwise.</returns>
        public static bool HasCubeFace(this ProbeRenderSteps steps, CubemapFace face)
        {
            var flags = FromCubeFace(face);
            return flags == 0 || (steps & flags) == flags; // Don't use Enum.HasFlag because it generates GCAlloc.
        }

        /// <summary>
        /// Creates the render step for the given cubemap face.
        /// </summary>
        /// <param name="face">The cubemap face.</param>
        /// <returns>The render step for the cubemap face, or planar if the face is unknown.</returns>
        public static ProbeRenderSteps FromCubeFace(CubemapFace face)
        {
            switch (face)
            {
                case CubemapFace.PositiveX: return ProbeRenderSteps.CubeFace0;
                case CubemapFace.NegativeX: return ProbeRenderSteps.CubeFace1;
                case CubemapFace.PositiveY: return ProbeRenderSteps.CubeFace2;
                case CubemapFace.NegativeY: return ProbeRenderSteps.CubeFace3;
                case CubemapFace.PositiveZ: return ProbeRenderSteps.CubeFace4;
                case CubemapFace.NegativeZ: return ProbeRenderSteps.CubeFace5;
                default: return ProbeRenderSteps.Planar;
            }
        }

        /// <summary>
        /// Creates the render steps for the given probe type.
        /// </summary>
        /// <param name="probeType">The probe type.</param>
        /// <returns>The render steps for the given probe type.</returns>
        public static ProbeRenderSteps FromProbeType(ProbeSettings.ProbeType probeType)
        {
            switch (probeType)
            {
                case ProbeSettings.ProbeType.ReflectionProbe: return ProbeRenderSteps.ReflectionProbeMask;
                case ProbeSettings.ProbeType.PlanarProbe: return ProbeRenderSteps.PlanarProbeMask;
                default: return ProbeRenderSteps.None;
            }
        }

        /// <summary>
        /// Extract the lowest set bit.
        /// </summary>
        /// <param name="steps">The probe rendering steps.</param>
        /// <returns>The lowest set bit, or None.</returns>
        public static ProbeRenderSteps LowestSetBit(this ProbeRenderSteps steps)
        {
            int bits = (int)steps;
            return (ProbeRenderSteps)(bits & -bits);
        }
    }

    /// <summary>
    /// Base class for reflection like probes.
    /// </summary>
    [ExecuteAlways]
    public abstract partial class HDProbe : MonoBehaviour
    {
        /// <summary>
        /// Store the settings computed during a rendering
        /// </summary>
        [Serializable]
        public struct RenderData
        {
            [SerializeField, FormerlySerializedAs("worldToCameraRHS")]
            Matrix4x4 m_WorldToCameraRHS;
            [SerializeField, FormerlySerializedAs("projectionMatrix")]
            Matrix4x4 m_ProjectionMatrix;
            [SerializeField, FormerlySerializedAs("capturePosition")]
            Vector3 m_CapturePosition;
            [SerializeField]
            Quaternion m_CaptureRotation;
            [SerializeField]
            float m_FieldOfView;
            [SerializeField]
            float m_Aspect;


            /// <summary>World to camera matrix (Right Hand).</summary>
            public Matrix4x4 worldToCameraRHS => m_WorldToCameraRHS;
            /// <summary>Projection matrix.</summary>
            public Matrix4x4 projectionMatrix => m_ProjectionMatrix;
            /// <summary>The capture position.</summary>
            public Vector3 capturePosition => m_CapturePosition;
            /// <summary>The capture rotation.</summary>
            public Quaternion captureRotation => m_CaptureRotation;
            /// <summary>The field of view.</summary>
            public float fieldOfView => m_FieldOfView;
            /// <summary>The aspect ratio.</summary>
            public float aspect => m_Aspect;

            /// <summary>
            /// Instantiate a new RenderData from camera and position settings.
            /// </summary>
            /// <param name="camera">The camera settings used.</param>
            /// <param name="position">The position settings used.</param>
            public RenderData(CameraSettings camera, CameraPositionSettings position)
                : this(
                    position.GetUsedWorldToCameraMatrix(),
                    camera.frustum.GetUsedProjectionMatrix(),
                    position.position,
                    position.rotation,
                    camera.frustum.fieldOfView,
                    camera.frustum.aspect
                )
            {
            }

            /// <summary>
            /// Instantiate a new RenderData from specified inputs.
            /// </summary>
            /// <param name="worldToCameraRHS">The world to camera matrix (Right Hand)</param>
            /// <param name="projectionMatrix">The projection matrix.</param>
            /// <param name="capturePosition">The capture position.</param>
            /// <param name="captureRotation">The capture rotation.</param>
            /// <param name="fov">The field of view.</param>
            /// <param name="aspect">The aspect ratio.</param>
            public RenderData(
                Matrix4x4 worldToCameraRHS,
                Matrix4x4 projectionMatrix,
                Vector3 capturePosition,
                Quaternion captureRotation,
                float fov,
                float aspect
            )
            {
                m_WorldToCameraRHS = worldToCameraRHS;
                m_ProjectionMatrix = projectionMatrix;
                m_CapturePosition = capturePosition;
                m_CaptureRotation = captureRotation;
                m_FieldOfView = fov;
                m_Aspect = aspect;
            }
        }

        /// <summary>
        /// Backed values of the probe settings.
        /// Don't use directly this except for migration code.
        /// </summary>
        // Serialized Data
        [SerializeField]
        // This one is protected only to have access during migration of children classes.
        // In children classes, it must be used only during the migration.
        protected ProbeSettings m_ProbeSettings = ProbeSettings.NewDefault();
#pragma warning disable 649
        [SerializeField]
        ProbeSettingsOverride m_ProbeSettingsOverride;
        [SerializeField]
        ReflectionProxyVolumeComponent m_ProxyVolume;
#pragma warning restore 649

        [SerializeField]
        Texture m_BakedTexture;
        [SerializeField]
        Texture m_CustomTexture;
        [SerializeField]
        RenderData m_BakedRenderData;
        [SerializeField]
        RenderData m_CustomRenderData;

#if UNITY_EDITOR
        // Maintain the GUID of the custom and the baked texture so that we can switch back to it in editor mode, but still release
        // the resource if the probe is turned off or set back to baked/cutom or realtime mode.
        private string m_CustomTextureGUID;
        private string m_BakedTextureGUID;

        // Need to keep track of the previous selected mode in editor to handle the case if a user selects no custom texture.
        private ProbeSettings.Mode m_PreviousMode = ProbeSettings.Mode.Baked;
#endif

        // Runtime Data
        RTHandle m_RealtimeTexture;
        RTHandle m_RealtimeDepthBuffer;
        RenderData m_RealtimeRenderData;
        bool m_WasRenderedSinceLastOnDemandRequest = true;
        ProbeRenderSteps m_RemainingRenderSteps = ProbeRenderSteps.None;
        bool m_HasPendingRenderRequest = false;
        uint m_RealtimeRenderCount = 0;
        int m_LastStepFrameCount = -1;
#if UNITY_EDITOR
        bool m_WasRenderedDuringAsyncCompilation = false;
#endif

        [SerializeField] bool m_HasValidSHForNormalization;
        [SerializeField] SphericalHarmonicsL2 m_SHForNormalization;
        [SerializeField] Vector3 m_SHValidForCapturePosition;
        [SerializeField] Vector3 m_SHValidForSourcePosition;

        // Array of names that will be used in the Render Loop to name the probes in debug
        internal string[] probeName = new string[6];

        //This probe object is dumb, its the caller / pipelines responsibility
        //to calculate its exposure values, since this requires frame data.
        float m_ProbeExposureValue = 1.0f;

        ///<summary>Set and used by the pipeline, depending on the resolved configuration of a probe.</summary>
        public bool ExposureControlEnabled { set; get; }

        internal void SetProbeExposureValue(float exposure)
        {
            m_ProbeExposureValue = exposure;
        }

        internal float ProbeExposureValue()
        {
            return m_ProbeExposureValue;
        }

        private bool HasRemainingRenderSteps()
        {
            return !m_RemainingRenderSteps.IsNone() || m_HasPendingRenderRequest;
        }

        private void EnqueueAllRenderSteps()
        {
            ProbeRenderSteps allRenderSteps = ProbeRenderStepsExt.FromProbeType(type);
            if (m_RemainingRenderSteps != allRenderSteps)
                m_HasPendingRenderRequest = true;
        }

        /// <summary>
        /// Checks weather the current probe is set to off.
        /// </summary>
        /// <returns>Returns true if the the selected probe has its resolution set to off.</returns>
        public bool IsTurnedOff()
        {
            return (type == ProbeSettings.ProbeType.PlanarProbe && resolution == PlanarReflectionAtlasResolution.Resolution0) ||(type == ProbeSettings.ProbeType.ReflectionProbe && cubeResolution == CubeReflectionResolution.CubeReflectionResolution0);
        }

        internal ProbeRenderSteps NextRenderSteps()
        {
            if (m_RemainingRenderSteps.IsNone() && m_HasPendingRenderRequest)
            {
                m_RemainingRenderSteps = ProbeRenderStepsExt.FromProbeType(type);
                m_HasPendingRenderRequest = false;
            }
            
            if (type == ProbeSettings.ProbeType.ReflectionProbe)
            {
                // pick one bit or all remaining bits
                ProbeRenderSteps nextSteps = timeSlicing ? m_RemainingRenderSteps.LowestSetBit() : m_RemainingRenderSteps;

                // limit work to once per frame if necessary
                bool limitToOncePerFrame = (realtimeMode == ProbeSettings.RealtimeMode.EveryFrame || timeSlicing);
                if (!nextSteps.IsNone() && limitToOncePerFrame)
                {
                    int frameCount = Time.frameCount;
                    if (m_LastStepFrameCount == frameCount)
                        nextSteps = ProbeRenderSteps.None;
                    else
                        m_LastStepFrameCount = frameCount;
                }

                m_RemainingRenderSteps &= ~nextSteps;
                return nextSteps;
            }
            else
            {
                // always render the full planar reflection
                m_RemainingRenderSteps = ProbeRenderSteps.None;
                return ProbeRenderSteps.PlanarProbeMask;
            }
        }

        internal void IncrementRealtimeRenderCount()
        {
            m_RealtimeRenderCount += 1;

            texture.IncrementUpdateCount();
        }

        internal void RepeatRenderSteps(ProbeRenderSteps renderSteps)
        {
            m_RemainingRenderSteps |= renderSteps;
        }

        internal uint GetTextureHash()
        {
            uint textureHash = (mode == ProbeSettings.Mode.Realtime) ? m_RealtimeRenderCount : texture.updateCount;
            // For baked probes in the editor we need to factor in the actual hash of texture because we can't increment the render count of a texture that's baked on the disk.
#if UNITY_EDITOR
            textureHash += (uint)texture.imageContentsHash.GetHashCode();
#endif
            return textureHash;
        }

        internal bool requiresRealtimeUpdate
        {
            get
            {
#if UNITY_EDITOR
                if (m_WasRenderedDuringAsyncCompilation && !ShaderUtil.anythingCompiling)
                    return true;
#endif
                if (mode != ProbeSettings.Mode.Realtime || IsTurnedOff())
                    return false;
                switch (realtimeMode)
                {
                    case ProbeSettings.RealtimeMode.EveryFrame: return true;
                    case ProbeSettings.RealtimeMode.OnEnable: return !wasRenderedAfterOnEnable || HasRemainingRenderSteps();
                    case ProbeSettings.RealtimeMode.OnDemand: return !m_WasRenderedSinceLastOnDemandRequest || HasRemainingRenderSteps();
                    default: throw new ArgumentOutOfRangeException(nameof(realtimeMode));
                }
            }
        }

        internal bool HasValidRenderedData()
        {
            bool hasValidTexture = texture != null;
            if (mode != ProbeSettings.Mode.Realtime)
            {
                return hasValidTexture;
            }
            else
            {
                return hasEverRendered && hasValidTexture;
            }
        }

        // Public API
        // Texture asset
        /// <summary>
        /// The baked texture. Can be null if the probe was never baked.
        ///
        /// Most of the time, you do not need to set this value yourself. You can set this property in situations
        /// where you want to manually assign data that differs from what Unity generates.
        /// </summary>
        public Texture bakedTexture
        {
            get => m_BakedTexture;
            set => m_BakedTexture = value;
        }

        /// <summary>
        /// Texture used in custom mode.
        /// </summary>
        public Texture customTexture
        {
            get => m_CustomTexture;
            set => m_CustomTexture = value;
        }

        /// <summary>
        /// The allocated realtime texture. Can be null if the probe never rendered with the realtime mode.
        ///
        /// Most of the time, you do not need to set this value yourself. You can set this property in situations
        /// where you want to manually assign data that differs from what Unity generates.
        /// </summary>
        public RenderTexture realtimeTexture
        {
            get => m_RealtimeTexture != null ? m_RealtimeTexture : null;
            set
            {
                if (m_RealtimeTexture != null)
                    m_RealtimeTexture.Release();
                m_RealtimeTexture = RTHandles.Alloc(value, transferOwnership: true);
                m_RealtimeTexture.rt.name = $"ProbeRealTimeTexture_{name}";
            }
        }

        /// <summary>
        /// The allocated realtime depth texture. Can be null if the probe never rendered with the realtime mode.
        ///
        /// Most of the time, you do not need to set this value yourself. You can set this property in situations
        /// where you want to manually assign data that differs from what Unity generates.
        /// </summary>
        public RenderTexture realtimeDepthTexture
        {
            get => m_RealtimeDepthBuffer != null ? m_RealtimeDepthBuffer : null;
            set
            {
                if (m_RealtimeDepthBuffer != null)
                    m_RealtimeDepthBuffer.Release();
                m_RealtimeDepthBuffer = RTHandles.Alloc(value, transferOwnership: true);
                m_RealtimeDepthBuffer.rt.name = $"ProbeRealTimeDepthTexture_{name}";
            }
        }

        /// <summary>
        /// Returns an RThandle reference to the realtime texture where the color result of the probe is stored.
        /// </summary>
        public RTHandle realtimeTextureRTH
        {
            get => m_RealtimeTexture;
        }
        /// <summary>
        /// Returns an RThandle reference to the realtime texture where the depth result of the probe is stored.
        /// </summary>
        public RTHandle realtimeDepthTextureRTH
        {
            get => m_RealtimeDepthBuffer;
        }

        /// <summary>
        /// The texture used during lighting for this probe.
        /// </summary>
        public Texture texture => GetTexture(mode);
        /// <summary>
        /// Get the texture for a specific mode.
        /// </summary>
        /// <param name="targetMode">The mode to query.</param>
        /// <returns>The texture for this specified mode.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the<paramref name="targetMode"/> is invalid.</exception>
        public Texture GetTexture(ProbeSettings.Mode targetMode)
        {
            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return m_BakedTexture;
                case ProbeSettings.Mode.Custom: return m_CustomTexture;
                case ProbeSettings.Mode.Realtime: return m_RealtimeTexture;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Set the texture for a specific target mode.
        /// </summary>
        /// <param name="targetMode">The mode to update.</param>
        /// <param name="texture">The texture to set.</param>
        /// <returns>The texture that was set.</returns>
        /// <exception cref="ArgumentException">When the texture is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">When the mode is invalid</exception>
        public Texture SetTexture(ProbeSettings.Mode targetMode, Texture texture)
        {
            if (targetMode == ProbeSettings.Mode.Realtime && !(texture is RenderTexture))
                throw new ArgumentException("'texture' must be a RenderTexture for the Realtime mode.");

            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return m_BakedTexture = texture;
                case ProbeSettings.Mode.Custom: return m_CustomTexture = texture;
                case ProbeSettings.Mode.Realtime: return realtimeTexture = (RenderTexture)texture;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Set the depth texture for a specific target mode.
        /// </summary>
        /// <param name="targetMode">The mode to update.</param>
        /// <param name="texture">The texture to set.</param>
        /// <returns>The texture that was set.</returns>
        public Texture SetDepthTexture(ProbeSettings.Mode targetMode, Texture texture)
        {
            if (targetMode == ProbeSettings.Mode.Realtime && !(texture is RenderTexture))
                throw new ArgumentException("'texture' must be a RenderTexture for the Realtime mode.");

            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return m_BakedTexture = texture;
                case ProbeSettings.Mode.Custom: return m_CustomTexture = texture;
                case ProbeSettings.Mode.Realtime: return realtimeDepthTexture = (RenderTexture)texture;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// The render data of the last bake
        /// </summary>
        public RenderData bakedRenderData { get => m_BakedRenderData; set => m_BakedRenderData = value; }
        /// <summary>
        /// The render data of the custom mode
        /// </summary>
        public RenderData customRenderData { get => m_CustomRenderData; set => m_CustomRenderData = value; }
        /// <summary>
        /// The render data of the last realtime rendering
        /// </summary>
        public RenderData realtimeRenderData { get => m_RealtimeRenderData; set => m_RealtimeRenderData = value; }
        /// <summary>
        /// The currently used render data.
        /// </summary>
        public RenderData renderData => GetRenderData(mode);
        /// <summary>
        /// Get the render data of a specific mode.
        ///
        /// Note: The HDProbe stores only one RenderData per mode, even for view dependent probes with multiple viewers.
        /// In that case, make sure that you have set the RenderData relative to the expected viewer before rendering.
        /// Otherwise the data retrieved by this function will be wrong.
        /// </summary>
        /// <param name="targetMode">The mode to query</param>
        /// <returns>The requested render data</returns>
        /// <exception cref="ArgumentOutOfRangeException">When the mode is invalid</exception>
        public RenderData GetRenderData(ProbeSettings.Mode targetMode)
        {
            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return bakedRenderData;
                case ProbeSettings.Mode.Custom: return customRenderData;
                case ProbeSettings.Mode.Realtime: return realtimeRenderData;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Set the render data for a specific mode.
        ///
        /// Note: The HDProbe stores only one RenderData per mode, even for view dependent probes with multiple viewers.
        /// In that case, make sure that you have set the RenderData relative to the expected viewer before rendering.
        /// </summary>
        /// <param name="targetMode">The mode to update</param>
        /// <param name="renderData">The data to set</param>
        /// <exception cref="ArgumentOutOfRangeException">When the mode is invalid</exception>
        public void SetRenderData(ProbeSettings.Mode targetMode, RenderData renderData)
        {
            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: bakedRenderData = renderData; break;
                case ProbeSettings.Mode.Custom: customRenderData = renderData; break;
                case ProbeSettings.Mode.Realtime: realtimeRenderData = renderData; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        // Settings
        // General
        /// <summary>
        /// The probe type
        /// </summary>
        public ProbeSettings.ProbeType type { get => m_ProbeSettings.type; protected set => m_ProbeSettings.type = value; }

        /// <summary>The capture mode.</summary>
        public ProbeSettings.Mode mode
        {
            get => m_ProbeSettings.mode;
            set
            {
                m_ProbeSettings.mode = value;

#if UNITY_EDITOR
                // Validate in case we are in the editor and we have to release a custom texture reference.
                OnValidate();
#endif
            }
        }
        /// <summary>
        /// The realtime mode of the probe
        /// </summary>
        public ProbeSettings.RealtimeMode realtimeMode { get => m_ProbeSettings.realtimeMode; set => m_ProbeSettings.realtimeMode = value; }
        /// <summary>
        /// Whether the realtime probe uses time slicing
        /// </summary>
        public bool timeSlicing { get => m_ProbeSettings.timeSlicing; set => m_ProbeSettings.timeSlicing = value; }
        /// <summary>
        /// Resolution of the planar probe.
        /// </summary>
        public PlanarReflectionAtlasResolution resolution
        {
            get
            {
                var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                // We return whatever value is in resolution if there is no hdrp pipeline (nothing will work anyway)
                return hdrp != null ? m_ProbeSettings.resolutionScalable.Value(hdrp.asset.currentPlatformRenderPipelineSettings.planarReflectionResolution) : m_ProbeSettings.resolution;
            }
        }
        /// <summary>
        /// Resolution of the cube probe.
        /// </summary>
        public CubeReflectionResolution cubeResolution
        {
            get
            {
                var hdrp = RenderPipelineManager.currentPipeline as HDRenderPipeline;
                return hdrp != null ? m_ProbeSettings.cubeResolution.Value(hdrp.asset.currentPlatformRenderPipelineSettings.cubeReflectionResolution) : ProbeSettings.k_DefaultCubeResolution;
            }
        }

        // Lighting
        /// <summary>Light layer to use by this probe.</summary>
        public RenderingLayerMask lightLayers
        { get => m_ProbeSettings.lighting.lightLayer; set => m_ProbeSettings.lighting.lightLayer = value; }
        /// <summary>This function return a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1</summary>
        public uint lightLayersAsUInt => lightLayers < 0 ? (uint)RenderingLayerMask.Everything : (uint)lightLayers;
        /// <summary>Importance value for sorting the probes (higher values display over lower ones).</summary>
        public int importance
        { get => m_ProbeSettings.lighting.importance; set => m_ProbeSettings.lighting.importance = Mathf.Clamp(value, 0, 32767); }
        /// <summary>Multiplier factor of reflection (non PBR parameter).</summary>
        public float multiplier
        { get => m_ProbeSettings.lighting.multiplier; set => m_ProbeSettings.lighting.multiplier = value; }
        /// <summary>Weight for blending amongst probes (non PBR parameter).</summary>
        public float weight
        { get => m_ProbeSettings.lighting.weight; set => m_ProbeSettings.lighting.weight = value; }
        /// <summary>The distance at which reflections smoothly fade out before HDRP cut them completely.</summary>
        public float fadeDistance
        { get => m_ProbeSettings.lighting.fadeDistance; set => m_ProbeSettings.lighting.fadeDistance = value; }
        /// <summary>The result of the rendering of the probe will be divided by this factor. When the probe is read, this factor is undone as the probe data is read. This is to simply avoid issues with values clamping due to precision of the storing format.</summary>
        public float rangeCompressionFactor
        { get => m_ProbeSettings.lighting.rangeCompressionFactor; set => m_ProbeSettings.lighting.rangeCompressionFactor = value; }


        // Proxy
        /// <summary>ProxyVolume currently used by this probe.</summary>
        public ReflectionProxyVolumeComponent proxyVolume
        {
            get => m_ProxyVolume;
            set => m_ProxyVolume = value;
        }

        /// <summary>
        /// Use the influence volume as the proxy volume if this is true.
        /// </summary>
        public bool useInfluenceVolumeAsProxyVolume
        {
            get
            {
                return m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume;
            }

            internal set
            {
                m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume = value;
            }
        }

        /// <summary>Is the projection at infinite? Value could be changed by Proxy mode.</summary>
        public bool isProjectionInfinite
            => m_ProxyVolume != null && m_ProxyVolume.proxyVolume.shape == ProxyShape.Infinite
            || m_ProxyVolume == null && !m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume;

        // Influence
        /// <summary>InfluenceVolume of the probe.</summary>
        public InfluenceVolume influenceVolume
        {
            get => m_ProbeSettings.influence ?? (m_ProbeSettings.influence = new InfluenceVolume());
            private set => m_ProbeSettings.influence = value;
        }
        // Camera
        /// <summary>Frame settings in use with this probe.</summary>
        public ref FrameSettings frameSettings => ref m_ProbeSettings.cameraSettings.renderingPathCustomFrameSettings;
        /// <summary>
        /// Specify the settings overriden for the frame settins
        /// </summary>
        public ref FrameSettingsOverrideMask frameSettingsOverrideMask => ref m_ProbeSettings.cameraSettings.renderingPathCustomFrameSettingsOverrideMask;

        /// <summary>
        /// The extents of the proxy volume
        /// </summary>
        public Vector3 proxyExtents
            => proxyVolume != null ? proxyVolume.proxyVolume.extents : influenceExtents;

        /// <summary>
        /// The bounding sphere of the influence
        /// </summary>
        public BoundingSphere boundingSphere => influenceVolume.GetBoundingSphereAt(transform.position);
        /// <summary>
        /// The bounding box of the influence
        /// </summary>
        public Bounds bounds => influenceVolume.GetBoundsAt(transform.position);

        /// <summary>
        /// To read the settings of this probe, most of the time you should use the sanitized version of
        /// this property: <see cref="settings"/>.
        /// Use this property to read the settings of the probe only when it is important that you read the raw data.
        /// </summary>
        public ref ProbeSettings settingsRaw => ref m_ProbeSettings;

        /// <summary>
        /// Use this property to get the settings used for calculations.
        ///
        /// To edit the settings of the probe, use the unsanitized version of this property: <see cref="settingsRaw"/>.
        /// </summary>
        public ProbeSettings settings
        {
            get
            {
                var settings = m_ProbeSettings;
                // Special case here, we reference a component that is a wrapper
                // So we need to update with the actual value for the proxyVolume
                settings.proxy = m_ProxyVolume?.proxyVolume;
                settings.influence = settings.influence ?? new InfluenceVolume();
                return settings;
            }
        }

        internal Matrix4x4 influenceToWorld => Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        internal Vector3 influenceExtents => influenceVolume.extents;
        internal Matrix4x4 proxyToWorld
            => proxyVolume != null
            ? Matrix4x4.TRS(proxyVolume.transform.position, proxyVolume.transform.rotation, Vector3.one)
            : influenceToWorld;

        internal bool wasRenderedAfterOnEnable { get; private set; } = false;
        internal bool hasEverRendered { get { return m_RealtimeRenderCount != 0; } }

        internal void SetIsRendered()
        {
#if UNITY_EDITOR
            bool isCompiling = ShaderUtil.anythingCompiling;
            if (m_WasRenderedDuringAsyncCompilation && !isCompiling)
                EnqueueAllRenderSteps();
            m_WasRenderedDuringAsyncCompilation = isCompiling;
#endif
            switch (realtimeMode)
            {
                case ProbeSettings.RealtimeMode.EveryFrame:
                    EnqueueAllRenderSteps();
                    break;
                case ProbeSettings.RealtimeMode.OnEnable:
                    if (!wasRenderedAfterOnEnable)
                    {
                        EnqueueAllRenderSteps();
                        wasRenderedAfterOnEnable = true;
                    }
                    break;
                case ProbeSettings.RealtimeMode.OnDemand:
                    if (!m_WasRenderedSinceLastOnDemandRequest)
                    {
                        EnqueueAllRenderSteps();
                        m_WasRenderedSinceLastOnDemandRequest = true;
                    }
                    break;
            }
        }

        // API
        /// <summary>
        /// Prepare the probe for culling.
        /// You should call this method when you update the <see cref="influenceVolume"/> parameters during runtime.
        /// </summary>
        public virtual void PrepareCulling() { }

        /// <summary>
        /// Requests that Unity renders this Reflection Probe during the next update.
        /// </summary>
        /// <remarks>
        /// If the Reflection Probe uses <see cref="ProbeSettings.RealtimeMode.OnDemand"/> mode, Unity renders the probe the next time the probe influences a Camera rendering.
        ///
        /// If the Reflection Probe doesn't have an attached <see cref="HDAdditionalReflectionData"/> component, calling this function has no effect.
        ///
        /// Note: If any part of a Camera's frustum intersects a Reflection Probe's influence volume, the Reflection Probe influences the Camera.
        /// </remarks>
        public void RequestRenderNextUpdate() => m_WasRenderedSinceLastOnDemandRequest = false;


        internal void TryUpdateLuminanceSHL2ForNormalization()
        {
#if UNITY_EDITOR
            const float kValidSHThresh = 0.33f; // This threshold is used to make the code below functionally equivalent to the obsolete RetrieveProbeSH. 
            m_HasValidSHForNormalization = AdditionalGIBakeRequestsManager.instance.RetrieveProbe(GetEntityId(), out m_SHValidForCapturePosition, out m_SHForNormalization, out float validity);
            m_HasValidSHForNormalization = m_HasValidSHForNormalization && validity < kValidSHThresh;
            if (m_HasValidSHForNormalization)
                m_SHValidForSourcePosition = transform.position;
#endif
        }

#if UNITY_EDITOR
        private void ClearSHBaking()
        {
            // Lighting data was cleared - clear out any stale SH data.
            m_HasValidSHForNormalization = false;
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 0, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 1, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 2, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 3, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 4, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 5, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 6, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 7, Vector3.zero);
            SphericalHarmonicsL2Utils.SetCoefficient(ref m_SHForNormalization, 8, Vector3.zero);

            AdditionalGIBakeRequestsManager.instance.DequeueRequest(GetInstanceID());

            QueueSHBaking();
        }

#endif
        // Return luma of coefficients
        internal bool GetSHForNormalization(out Vector4 outL0L1, out Vector4 outL2_1, out float outL2_2)
        {

            var hdrp = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            var hasValidSHData = m_HasValidSHForNormalization && hdrp.asset.currentPlatformRenderPipelineSettings.supportProbeVolume;

            if (!hasValidSHData)
            {
                // No valid data, so we disable the feature.
                outL0L1 = outL2_1 = Vector4.zero; outL2_2 = 0f;
                return false;
            }

            if (m_SHForNormalization[0, 0] == float.MaxValue)
            {
                // Valid data, but probe is fully black. Setup coefficients so that light loop cancels out reflection probe contribution.
                outL0L1 = new Vector4(float.MaxValue, 0f, 0f, 0f);
                outL2_1 = Vector4.zero;
                outL2_2 = 0f;
                return true;
            }

            var L0 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 0);
            var L1_0 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 1);
            var L1_1 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 2);
            var L1_2 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 3);
            var L2_0 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 4);
            var L2_1 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 5);
            var L2_2 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 6);
            var L2_3 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 7);
            var L2_4 = SphericalHarmonicsL2Utils.GetCoefficient(m_SHForNormalization, 8);

            // If we are going to evaluate L2, we need to fixup the coefficients.
            if (hdrp.asset.currentPlatformRenderPipelineSettings.probeVolumeSHBands == ProbeVolumeSHBands.SphericalHarmonicsL2)
            {
                L0 -= L2_2;
                L2_2 *= 3.0f;
            }

            outL0L1.x = ColorUtils.Luminance(new Color(L0.x, L0.y, L0.z));
            outL0L1.y = ColorUtils.Luminance(new Color(L1_0.x, L1_0.y, L1_0.z));
            outL0L1.z = ColorUtils.Luminance(new Color(L1_1.x, L1_1.y, L1_1.z));
            outL0L1.w = ColorUtils.Luminance(new Color(L1_2.x, L1_2.y, L1_2.z));
            outL2_1.x = ColorUtils.Luminance(new Color(L2_0.x, L2_0.y, L2_0.z));
            outL2_1.y = ColorUtils.Luminance(new Color(L2_1.x, L2_1.y, L2_1.z));
            outL2_1.z = ColorUtils.Luminance(new Color(L2_2.x, L2_2.y, L2_2.z));
            outL2_1.w = ColorUtils.Luminance(new Color(L2_3.x, L2_3.y, L2_3.z));
            outL2_2 = ColorUtils.Luminance(new Color(L2_4.x, L2_4.y, L2_4.z));

            return true;
        }

#if UNITY_EDITOR
        private Vector3 ComputeCapturePositionWS()
        {
            var probePositionSettings = ProbeCapturePositionSettings.ComputeFrom(this, null);
            HDRenderUtilities.ComputeCameraSettingsFromProbeSettings(
                this.settings, probePositionSettings,
                out _, out var cameraPositionSettings, 0
            );
            return cameraPositionSettings.position;
        }

        private void QueueSHBaking()
        {
            if (Application.isPlaying)
                return;

            var asset = HDRenderPipeline.currentAsset;
            if (asset == null || !asset.currentPlatformRenderPipelineSettings.supportProbeVolume)
                return;

            Vector3 capturePositionWS = ComputeCapturePositionWS();
            // If already enqueued this will just change the position, otherwise it'll enqueue the request.
            AdditionalGIBakeRequestsManager.instance.UpdatePositionForRequest(GetInstanceID(), capturePositionWS);

            ValidateSHNormalizationSourcePosition(transform.position);
            ValidateSHNormalizationCapturePosition(capturePositionWS);
        }

        // Allow a probe to move this far before its baked normalization data gets invalidated. We could go two routes with this:
        // either we set the threshold really low so any change invalidates the data (currently), or we make it configurable so one
        // can have some leeway in moving them around.
        private const float kMaxAllowedNormalizedProbePositionDeltaSqr = 0.01f * 0.01f;

        // Returns true if capture position changed
        private bool ValidateSHNormalizationCapturePosition(Vector3 capturePositionWS)
        {
            var capturePositionChanged = Vector3.SqrMagnitude(capturePositionWS - m_SHValidForCapturePosition) > kMaxAllowedNormalizedProbePositionDeltaSqr;

            // If capture position has changed, the captured normalization data is no longer valid, so we discard it.
            if (m_HasValidSHForNormalization & capturePositionChanged)
            {
                m_HasValidSHForNormalization = false;
            }

            return capturePositionChanged;
        }

        // Returns true if source position changed
        private bool ValidateSHNormalizationSourcePosition(Vector3 position)
        {
            var sourcePositionChanged = Vector3.SqrMagnitude(position - m_SHValidForSourcePosition) > kMaxAllowedNormalizedProbePositionDeltaSqr;

            // If probe position has changed, the captured normalization data is no longer valid, so we discard it.
            if (m_HasValidSHForNormalization & sourcePositionChanged)
            {
                m_HasValidSHForNormalization = false;
            }

            return sourcePositionChanged;
        }
#endif

        void UpdateProbeName()
        {
            if (settings.type == ProbeSettings.ProbeType.ReflectionProbe)
            {
                for (int i = 0; i < 6; i++)
                    probeName[i] = $"Reflection Probe RenderCamera ({name}: {(CubemapFace)i})";
            }
            else
            {
                probeName[0] = $"Planar Probe RenderCamera ({name})";
            }
        }

        void DequeueSHRequest()
        {
#if UNITY_EDITOR
            AdditionalGIBakeRequestsManager.instance.DequeueRequest(GetInstanceID());
#endif
        }

        void SetOrReleaseCustomTextureReference()
        {
#if UNITY_EDITOR
            if (m_PreviousMode != mode)
            {
                if (m_PreviousMode == ProbeSettings.Mode.Custom)
                {
                    // Try to fetch the asset GUID before we release the reference to it.
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_CustomTexture, out m_CustomTextureGUID, out long unused);

                    // Release the asset reference.
                    m_CustomTexture = null;
                }
                else if (mode == ProbeSettings.Mode.Custom)
                {
                    if (!string.IsNullOrEmpty(m_CustomTextureGUID))
                    {
                        // Try to reset the asset reference.
                        var customTexturePath = AssetDatabase.GUIDToAssetPath(m_CustomTextureGUID);
                        m_CustomTexture = AssetDatabase.LoadAssetAtPath<Texture>(customTexturePath);
                    }
                }
            }

            m_PreviousMode = mode;
#endif
        }

        void SetOrReleaseBakedTextureReference()
        {
#if UNITY_EDITOR
            if (type == ProbeSettings.ProbeType.ReflectionProbe)
            {
                if (cubeResolution == CubeReflectionResolution.CubeReflectionResolution0)
                {
                    if (m_BakedTexture != null)
                    {
                        // Try to fetch the asset GUID before we release the reference to it.
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_BakedTexture, out m_BakedTextureGUID,
                            out long unused);

                        // Release the asset reference.
                        m_BakedTexture = null;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(m_BakedTextureGUID))
                    {
                        // Try to reset the asset reference.
                        var bakedTexturePath = AssetDatabase.GUIDToAssetPath(m_BakedTextureGUID);
                        m_BakedTexture = AssetDatabase.LoadAssetAtPath<Texture>(bakedTexturePath);
                    }
                }
            }
#endif
        }

        void OnEnable()
        {
            wasRenderedAfterOnEnable = false;
            PrepareCulling();
            HDProbeSystem.RegisterProbe(this);
            UpdateProbeName();

#if UNITY_EDITOR
            // Ensure that the custom texture is set.
            SetOrReleaseCustomTextureReference();
            SetOrReleaseBakedTextureReference();

            // Moving the garbage outside of the render loop:
            UnityEditor.EditorApplication.hierarchyChanged += UpdateProbeName;
            UnityEditor.Lightmapping.lightingDataCleared -= ClearSHBaking;
            UnityEditor.Lightmapping.lightingDataCleared += ClearSHBaking;
            QueueSHBaking();
#endif
        }

        void OnDisable()
        {
            HDProbeSystem.UnregisterProbe(this);
#if UNITY_EDITOR
            UnityEditor.Lightmapping.lightingDataCleared -= ClearSHBaking;
            DequeueSHRequest();
            UnityEditor.EditorApplication.hierarchyChanged -= UpdateProbeName;
            UnityEditor.Lightmapping.lightingDataCleared -= ClearSHBaking;
#endif
        }

#if UNITY_EDITOR
        void Update()
        {
            // Update is conveniently called when moving gameobjects in the editor so we can use that to track probe position changes.
            if (!Application.isPlaying)
            {
                // If position changed, calculate and upload a new capture position.
                if (ValidateSHNormalizationSourcePosition(transform.position))
                {
                    QueueSHBaking();
                }
            }
        }

        void OnValidate()
        {
            HDProbeSystem.UnregisterProbe(this);

            SetOrReleaseCustomTextureReference();
            SetOrReleaseBakedTextureReference();

            if (isActiveAndEnabled)
            {
                PrepareCulling();
                HDProbeSystem.RegisterProbe(this);

                UnityEditor.Lightmapping.lightingDataCleared -= ClearSHBaking;
                DequeueSHRequest();
                QueueSHBaking();
            }
        }

        void OnDestroy()
        {
            m_RealtimeTexture?.Release();
            m_RealtimeDepthBuffer?.Release();
        }
#endif
    }
}
