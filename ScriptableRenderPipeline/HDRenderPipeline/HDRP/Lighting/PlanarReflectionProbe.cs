using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class PlanarReflectionProbe : MonoBehaviour
    {
        public enum CapturePositionMode
        {
            Static,
            MirrorCamera
        }

        [SerializeField]
        ProxyVolumeComponent m_ProxyVolumeReference;
        [SerializeField]
        InfluenceVolume m_InfluenceVolume;
        [SerializeField]
        Vector3 m_CaptureLocalPosition;
        [SerializeField]
        [Range(0, 1)]
        float m_Dimmer = 1;
        [SerializeField]
        ReflectionProbeMode m_Mode = ReflectionProbeMode.Baked;
        [SerializeField]
        ReflectionProbeRefreshMode m_RefreshMode = ReflectionProbeRefreshMode.OnAwake;
        [SerializeField]
        Texture m_CustomTexture;
        [SerializeField]
        Texture m_BakedTexture;
        [SerializeField]
        RenderTexture m_RealtimeTexture;
        [SerializeField]
        FrameSettings m_FrameSettings;
        [SerializeField]
        float m_CaptureNearPlane = 1;
        [SerializeField]
        float m_CaptureFarPlane = 1000;
        [SerializeField]
        CapturePositionMode m_CapturePositionMode = CapturePositionMode.Static;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalPosition;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalNormal = Vector3.forward;

        public ProxyVolumeComponent proxyVolumeReference { get { return m_ProxyVolumeReference; } }
        public InfluenceVolume influenceVolume { get { return m_InfluenceVolume; } }
        public BoundingSphere boundingSphere { get { return m_InfluenceVolume.GetBoundingSphereAt(transform); } }

        public Texture texture
        {
            get
            {
                switch (m_Mode)
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
        public Bounds bounds { get { return m_InfluenceVolume.GetBoundsAt(transform); } }
        public Vector3 captureLocalPosition { get { return m_CaptureLocalPosition; } set { m_CaptureLocalPosition = value; } }
        public float dimmer { get { return m_Dimmer; } }
        public ReflectionProbeMode mode { get { return m_Mode; } }
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
        public ReflectionProbeRefreshMode refreshMode { get { return m_RefreshMode; } }
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
        public Vector4 captureMirrorPlane { get { return CameraUtils.Plane(captureMirrorPlanePosition, captureMirrorPlaneNormal); } }

        #region Proxy Properties
        public Matrix4x4 proxyToWorld
        {
            get
            {
                return m_ProxyVolumeReference != null
                    ? m_ProxyVolumeReference.transform.localToWorldMatrix
                    : influenceToWorld;
            }
        }
        public ShapeType proxyShape
        {
            get
            {
                return m_ProxyVolumeReference != null
                    ? m_ProxyVolumeReference.proxyVolume.shapeType
                    : influenceVolume.shapeType;
            }
        }
        public Vector3 proxyExtents
        {
            get
            {
                return m_ProxyVolumeReference != null
                    ? m_ProxyVolumeReference.proxyVolume.boxSize
                    : influenceVolume.boxBaseSize;
            }
        }
        public bool infiniteProjection { get { return m_ProxyVolumeReference != null && m_ProxyVolumeReference.proxyVolume.infiniteProjection; } }
        #endregion

        public void RequestRealtimeRender()
        {
            if (enabled)
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
            if (enabled)
            {
                ReflectionSystem.UnregisterProbe(this);
                ReflectionSystem.RegisterProbe(this);
            }
        }

        public Matrix4x4 GetCaptureViewProj(Camera viewerCamera)
        {
            return GetCaptureProjection(viewerCamera) * GetCaptureToWorld(viewerCamera);
        }
        public Matrix4x4 GetCaptureProjection(Camera viewerCamera)
        {
            var fov = ReflectionSystem.GetCaptureCameraFOVFor(this, viewerCamera);
            var proj = Matrix4x4.Perspective(fov, 1, captureNearPlane, captureFarPlane);
            return proj;
        }

        public Matrix4x4 GetCaptureToWorld(Camera viewerCamera)
        {
            if (refreshMode == ReflectionProbeRefreshMode.EveryFrame
                && capturePositionMode == CapturePositionMode.MirrorCamera)
            {
                var planeCenter = influenceToWorld.MultiplyPoint(m_CaptureMirrorPlaneLocalPosition);
                var planeNormal = influenceToWorld.MultiplyVector(m_CaptureMirrorPlaneLocalNormal.normalized);
                var sourcePosition = viewerCamera.transform.position;
                var r = sourcePosition - planeCenter;
                var capturePosition = r - 2 * Vector3.Dot(planeNormal, r) * planeNormal + planeCenter;

                var tr = transform;
                var influencePosition = influenceVolume.GetWorldPosition(tr);
                return Matrix4x4.TRS(
                    capturePosition,
                    Quaternion.LookRotation(influencePosition - capturePosition, tr.up),
                    Vector3.one
                );
            }
            else
            {
                var tr = transform;
                var capturePosition = tr.TransformPoint(m_CaptureLocalPosition);
                var influencePosition = influenceVolume.GetWorldPosition(tr);
                return Matrix4x4.TRS(
                    capturePosition,
                    Quaternion.LookRotation(influencePosition - capturePosition, tr.up),
                    Vector3.one
                );
            }
        }

        public Matrix4x4 GetInfluenceToWorld()
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
}
