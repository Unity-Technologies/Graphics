using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class PlanarReflectionProbe : HDProbe, ISerializationCallbackReceiver
    {
        const int currentVersion = 2;

        [SerializeField, FormerlySerializedAs("version")]
        int m_Version;

        public enum CapturePositionMode
        {
            Static,
            MirrorCamera,
        }

        [SerializeField]
        Vector3 m_CaptureLocalPosition;
        [SerializeField]
        Texture m_CustomTexture;
        [SerializeField]
        Texture m_BakedTexture;
        [SerializeField]
        FrameSettings m_FrameSettings = null;
        [SerializeField]
        float m_CaptureNearPlane = 1;
        [SerializeField]
        float m_CaptureFarPlane = 1000;
        [SerializeField]
        CapturePositionMode m_CapturePositionMode = CapturePositionMode.Static;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalPosition;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalNormal = Vector3.up;
        [SerializeField]
        bool m_OverrideFieldOfView = false;
        [SerializeField]
        [Range(0, 180)]
        float m_FieldOfViewOverride = 90;

        RenderTexture m_RealtimeTexture;

        public bool overrideFieldOfView { get { return m_OverrideFieldOfView; } }
        public float fieldOfViewOverride { get { return m_FieldOfViewOverride; } }

        public BoundingSphere boundingSphere { get { return influenceVolume.GetBoundingSphereAt(transform); } }

        public Texture texture
        {
            get
            {
                switch (mode)
                {
                    default:
                    case ReflectionProbeMode.Baked:
                        return bakedTexture;
                    case ReflectionProbeMode.Custom:
                        return customTexture;
                    case ReflectionProbeMode.Realtime:
                        return realtimeTexture;
                }
            }
        }
        public Bounds bounds { get { return influenceVolume.GetBoundsAt(transform); } }
        public Vector3 captureLocalPosition { get { return m_CaptureLocalPosition; } set { m_CaptureLocalPosition = value; } }
        public Matrix4x4 influenceToWorld
        {
            get
            {
                var tr = transform;
                var influencePosition = influenceVolume.GetWorldPosition(tr);
                return Matrix4x4.TRS(
                    influencePosition,
                    tr.rotation,
                    Vector3.one
                    );
            }
        }
        public Texture customTexture { get { return m_CustomTexture; } set { m_CustomTexture = value; } }
        public Texture bakedTexture { get { return m_BakedTexture; } set { m_BakedTexture = value; }}
        public RenderTexture realtimeTexture { get { return m_RealtimeTexture; } internal set { m_RealtimeTexture = value; } }
        public FrameSettings frameSettings { get { return m_FrameSettings; } }
        public float captureNearPlane { get { return m_CaptureNearPlane; } }
        public float captureFarPlane { get { return m_CaptureFarPlane; } }
        public CapturePositionMode capturePositionMode { get { return m_CapturePositionMode; } }
        public Vector3 captureMirrorPlaneLocalPosition
        {
            get { return m_CaptureMirrorPlaneLocalPosition; }
            set { m_CaptureMirrorPlaneLocalPosition = value; }
        }
        public Vector3 captureMirrorPlanePosition { get { return transform.TransformPoint(m_CaptureMirrorPlaneLocalPosition); } }
        public Vector3 captureMirrorPlaneLocalNormal
        {
            get { return m_CaptureMirrorPlaneLocalNormal; }
            set { m_CaptureMirrorPlaneLocalNormal = value; }
        }
        public Vector3 captureMirrorPlaneNormal { get { return transform.TransformDirection(m_CaptureMirrorPlaneLocalNormal); } }

        #region Proxy Properties
        public Matrix4x4 proxyToWorld
        {
            get
            {
                return proxyVolume != null
                    ? Matrix4x4.TRS(proxyVolume.transform.position, proxyVolume.transform.rotation, Vector3.one)
                    : influenceToWorld;
            }
        }
        public ProxyShape proxyShape
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.shape
                    : (ProxyShape)influenceVolume.shape;
            }
        }
        public Vector3 proxyExtents
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.extents
                    : influenceVolume.boxSize;
            }
        }
        public bool infiniteProjection
        {
            get
            {
                return proxyVolume != null
                    && proxyVolume.proxyVolume.shape == ProxyShape.Infinite;
            }
        }

        public bool useMirrorPlane
        {
            get
            {
                return mode == ReflectionProbeMode.Realtime
                    && refreshMode == ReflectionProbeRefreshMode.EveryFrame
                    && capturePositionMode == CapturePositionMode.MirrorCamera;
            }
        }

        #endregion

        public void RequestRealtimeRender()
        {
            if (isActiveAndEnabled)
                ReflectionSystem.RequestRealtimeRender(this);
        }

        void OnEnable()
        {
            ReflectionSystem.RegisterProbe(this);
        }

        void OnDisable()
        {
            ReflectionSystem.UnregisterProbe(this);
        }

        void OnValidate()
        {
            ReflectionSystem.UnregisterProbe(this);

            if (isActiveAndEnabled)
                ReflectionSystem.RegisterProbe(this);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (m_Version != currentVersion)
            {
                // Add here data migration code
                if(m_Version < 2)
                {
                    influenceVolume.MigrateOffsetSphere();
                }
                m_Version = currentVersion;
            }
        }
    }
}
