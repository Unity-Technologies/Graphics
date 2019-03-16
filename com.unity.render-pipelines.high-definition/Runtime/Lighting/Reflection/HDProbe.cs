using System;
using UnityEngine.Serialization;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteAlways]
    public abstract partial class HDProbe : MonoBehaviour
    {
        [Serializable]
        public struct RenderData
        {
            [SerializeField, FormerlySerializedAs("worldToCameraRHS")]
            Matrix4x4 m_WorldToCameraRHS;
            [SerializeField, FormerlySerializedAs("projectionMatrix")]
            Matrix4x4 m_ProjectionMatrix;
            [SerializeField, FormerlySerializedAs("capturePosition")]
            Vector3 m_CapturePosition;
            Quaternion m_CaptureRotation;

            public Matrix4x4 worldToCameraRHS => m_WorldToCameraRHS;
            public Matrix4x4 projectionMatrix => m_ProjectionMatrix;
            public Vector3 capturePosition => m_CapturePosition;
            public Quaternion captureRotation => m_CaptureRotation;

            public RenderData(CameraSettings camera, CameraPositionSettings position)
            {
                m_WorldToCameraRHS = position.GetUsedWorldToCameraMatrix();
                m_ProjectionMatrix = camera.frustum.GetUsedProjectionMatrix();
                m_CapturePosition = position.position;
                m_CaptureRotation = position.rotation;
            }

            public RenderData(
                Matrix4x4 worldToCameraRHS,
                Matrix4x4 projectionMatrix,
                Vector3 capturePosition,
                Quaternion captureRotation
            )
            {
                m_WorldToCameraRHS = worldToCameraRHS;
                m_ProjectionMatrix = projectionMatrix;
                m_CapturePosition = capturePosition;
                m_CaptureRotation = captureRotation;
            }
        }

        // Serialized Data
        [SerializeField]
        // This one is protected only to have access during migration of children classes.
        // In children classes, it must be used only during the migration.
        protected ProbeSettings m_ProbeSettings = ProbeSettings.@default;
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

        // Runtime Data
        RenderTexture m_RealtimeTexture;
        RenderData m_RealtimeRenderData;
        bool m_WasRenderedSinceLastOnDemandRequest;

        internal bool requiresRealtimeUpdate
        {
            get
            {
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

        // Public API
        // Texture asset
        public Texture bakedTexture => m_BakedTexture;
        public Texture customTexture => m_CustomTexture;
        public RenderTexture realtimeTexture => m_RealtimeTexture;
        public Texture texture => GetTexture(mode);
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
        public Texture SetTexture(ProbeSettings.Mode targetMode, Texture texture)
        {
            if (targetMode == ProbeSettings.Mode.Realtime && !(texture is RenderTexture))
                throw new ArgumentException("'texture' must be a RenderTexture for the Realtime mode.");

            switch (targetMode)
            {
                case ProbeSettings.Mode.Baked: return m_BakedTexture = texture;
                case ProbeSettings.Mode.Custom: return m_CustomTexture = texture;
                case ProbeSettings.Mode.Realtime: return m_RealtimeTexture = (RenderTexture)texture;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public RenderData bakedRenderData { get => m_BakedRenderData; internal set => m_BakedRenderData = value; }
        public RenderData customRenderData { get => m_CustomRenderData; internal set => m_CustomRenderData = value; }
        public RenderData realtimeRenderData { get => m_RealtimeRenderData; internal set => m_RealtimeRenderData = value; }
        public RenderData renderData => GetRenderData(mode);
        public RenderData GetRenderData(ProbeSettings.Mode targetMode)
        {
            switch (mode)
            {
                case ProbeSettings.Mode.Baked: return bakedRenderData;
                case ProbeSettings.Mode.Custom: return customRenderData;
                case ProbeSettings.Mode.Realtime: return realtimeRenderData;
                default: throw new ArgumentOutOfRangeException();
            }
        }
        public void SetRenderData(ProbeSettings.Mode targetMode, RenderData renderData)
        {
            switch (mode)
            {
                case ProbeSettings.Mode.Baked: bakedRenderData = renderData; break;
                case ProbeSettings.Mode.Custom: customRenderData = renderData; break;
                case ProbeSettings.Mode.Realtime: realtimeRenderData = renderData; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        // Settings
        // General
        public ProbeSettings.ProbeType type { get => m_ProbeSettings.type; protected set => m_ProbeSettings.type = value; }
        /// <summary>The capture mode.</summary>
        public ProbeSettings.Mode mode { get => m_ProbeSettings.mode; set => m_ProbeSettings.mode = value; }
        public ProbeSettings.RealtimeMode realtimeMode { get => m_ProbeSettings.realtimeMode; set => m_ProbeSettings.realtimeMode = value; }

        // Lighting
        /// <summary>Light layer to use by this probe.</summary>
        public LightLayerEnum lightLayers
        { get => m_ProbeSettings.lighting.lightLayer; set => m_ProbeSettings.lighting.lightLayer = value; }
        // This function return a mask of light layers as uint and handle the case of Everything as being 0xFF and not -1
        public uint lightLayersAsUInt => lightLayers < 0 ? (uint)LightLayerEnum.Everything : (uint)lightLayers;
        /// <summary>Multiplier factor of reflection (non PBR parameter).</summary>
        public float multiplier
        { get => m_ProbeSettings.lighting.multiplier; set => m_ProbeSettings.lighting.multiplier = value; }
        /// <summary>Weight for blending amongst probes (non PBR parameter).</summary>
        public float weight
        { get => m_ProbeSettings.lighting.weight; set => m_ProbeSettings.lighting.weight = value; }

        // Proxy
        /// <summary>ProxyVolume currently used by this probe.</summary>
        public ReflectionProxyVolumeComponent proxyVolume => m_ProxyVolume;
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
        internal Matrix4x4 influenceToWorld => Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

        // Camera
        /// <summary>Frame settings in use with this probe.</summary>
        public ref FrameSettings frameSettings => ref m_ProbeSettings.camera.renderingPathCustomFrameSettings;
        public FrameSettingsOverrideMask frameSettingsOverrideMask => m_ProbeSettings.camera.renderingPathCustomFrameSettingsOverrideMask;
        internal Vector3 influenceExtents => influenceVolume.extents;
        internal Matrix4x4 proxyToWorld
            => proxyVolume != null
            ? Matrix4x4.TRS(proxyVolume.transform.position, proxyVolume.transform.rotation, Vector3.one)
            : influenceToWorld;
        public Vector3 proxyExtents
            => proxyVolume != null ? proxyVolume.proxyVolume.extents : influenceExtents;

        public BoundingSphere boundingSphere => influenceVolume.GetBoundingSphereAt(transform.position);
        public Bounds bounds => influenceVolume.GetBoundsAt(transform.position);

        internal ProbeSettings settings
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

        internal bool wasRenderedAfterOnEnable { get; private set; } = false;
        internal int lastRenderedFrame { get; private set; } = int.MinValue;

        internal void SetIsRendered(int frame)
        {
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

        void OnEnable()
        {
            wasRenderedAfterOnEnable = false;
            PrepareCulling();
            HDProbeSystem.RegisterProbe(this);
        }
        void OnDisable() => HDProbeSystem.UnregisterProbe(this);

        void OnValidate()
        {
            HDProbeSystem.UnregisterProbe(this);
            PrepareCulling();

            if (isActiveAndEnabled)
                HDProbeSystem.RegisterProbe(this);
        }
    }
}
