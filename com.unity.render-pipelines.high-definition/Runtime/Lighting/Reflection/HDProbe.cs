using System;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
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

        // Only used in editor, but this data needs to be probe instance specific
        // (Contains: UI section states)
        [SerializeField]
        uint m_EditorOnlyData;

        // Runtime Data
        RTHandle m_RealtimeTexture;
        RTHandle m_RealtimeDepthBuffer;
        RenderData m_RealtimeRenderData;
        bool m_WasRenderedSinceLastOnDemandRequest = true;
#if UNITY_EDITOR
        bool m_WasRenderedDuringAsyncCompilation = false;
#endif

        // Array of names that will be used in the Render Loop to name the probes in debug
        internal string[] probeName = new string[6];

        internal bool requiresRealtimeUpdate
        {
            get
            {
#if UNITY_EDITOR
                if (m_WasRenderedDuringAsyncCompilation && !ShaderUtil.anythingCompiling)
                    return true;
#endif
                if (mode != ProbeSettings.Mode.Realtime)
                    return false;
                switch (realtimeMode)
                {
                    case ProbeSettings.RealtimeMode.EveryFrame: return true;
                    case ProbeSettings.RealtimeMode.OnEnable: return !wasRenderedAfterOnEnable;
                    case ProbeSettings.RealtimeMode.OnDemand: return !m_WasRenderedSinceLastOnDemandRequest;
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
                bool hasEverRendered = lastRenderedFrame != int.MinValue;
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
                m_RealtimeTexture = RTHandles.Alloc(value);
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
                m_RealtimeDepthBuffer = RTHandles.Alloc(value);
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
        public ProbeSettings.Mode mode { get => m_ProbeSettings.mode; set => m_ProbeSettings.mode = value; }
        /// <summary>
        /// The realtime mode of the probe
        /// </summary>
        public ProbeSettings.RealtimeMode realtimeMode { get => m_ProbeSettings.realtimeMode; set => m_ProbeSettings.realtimeMode = value; }
        /// <summary>
        /// Resolution of the probe.
        /// </summary>
        public PlanarReflectionAtlasResolution resolution
        {
            get
            {
                var hdrp = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
                // We return whatever value is in resolution if there is no hdrp pipeline (nothing will work anyway)
                return hdrp != null ? m_ProbeSettings.resolutionScalable.Value(hdrp.asset.currentPlatformRenderPipelineSettings.planarReflectionResolution) : m_ProbeSettings.resolution;
            }
        }

        // Lighting
        /// <summary>Light layer to use by this probe.</summary>
        public LightLayerEnum lightLayers
        { get => m_ProbeSettings.lighting.lightLayer; set => m_ProbeSettings.lighting.lightLayer = value; }
        /// <summary>This function return a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1</summary>
        public uint lightLayersAsUInt => lightLayers < 0 ? (uint)LightLayerEnum.Everything : (uint)lightLayers;
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
        public bool useInfluenceVolumeAsProxyVolume => m_ProbeSettings.proxySettings.useInfluenceVolumeAsProxyVolume;
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
        internal int lastRenderedFrame { get; private set; } = int.MinValue;

        internal void SetIsRendered(int frame)
        {
#if UNITY_EDITOR
            m_WasRenderedDuringAsyncCompilation = ShaderUtil.anythingCompiling;
#endif
            m_WasRenderedSinceLastOnDemandRequest = true;
            wasRenderedAfterOnEnable = true;
            lastRenderedFrame = frame;
        }

        // API
        /// <summary>
        /// Prepare the probe for culling.
        /// You should call this method when you update the <see cref="influenceVolume"/> parameters during runtime.
        /// </summary>
        public virtual void PrepareCulling() { }

        /// <summary>
        /// Request to render this probe next update.
        ///
        /// Call this method with the mode <see cref="ProbeSettings.RealtimeMode.OnDemand"/> and the probe will
        /// be rendered the next time it will influence a camera rendering.
        /// </summary>
        public void RequestRenderNextUpdate() => m_WasRenderedSinceLastOnDemandRequest = false;

        // Forces the re-rendering for both OnDemand and OnEnable
        internal void ForceRenderingNextUpdate()
        {
            m_WasRenderedSinceLastOnDemandRequest = false;
            wasRenderedAfterOnEnable = false;
        }

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

        void OnEnable()
        {
            wasRenderedAfterOnEnable = false;
            PrepareCulling();
            HDProbeSystem.RegisterProbe(this);
            UpdateProbeName();

#if UNITY_EDITOR
            // Moving the garbage outside of the render loop:
            UnityEditor.EditorApplication.hierarchyChanged += UpdateProbeName;
#endif
        }
        void OnDisable()
        {
            HDProbeSystem.UnregisterProbe(this);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.hierarchyChanged -= UpdateProbeName;
#endif
        }

        void OnValidate()
        {
            HDProbeSystem.UnregisterProbe(this);

            if (isActiveAndEnabled)
            {
                PrepareCulling();
                HDProbeSystem.RegisterProbe(this);
            }
        }
    }
}
