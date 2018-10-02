using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

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

        public bool overrideFieldOfView { get { return m_OverrideFieldOfView; } }
        public float fieldOfViewOverride { get { return m_FieldOfViewOverride; } }

        public BoundingSphere boundingSphere { get { return influenceVolume.GetBoundingSphereAt(transform); } }
        public Bounds bounds { get { return influenceVolume.GetBoundsAt(transform); } }
        public Vector3 captureLocalPosition { get { return m_CaptureLocalPosition; } set { m_CaptureLocalPosition = value; } }

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
        internal override Vector3 capturePosition
        {
            get
            {
                return transform.TransformPoint(captureLocalPosition);
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

        //for strange reason, current ReflectionSystem needs a proxyExtents two time bigger for planar. To be fixed when refactoring the ReflectionSystem
        public override Vector3 proxyExtents
        {
            get
            {
                return proxyVolume != null
                    ? proxyVolume.proxyVolume.extents
                    : influenceVolume.boxSize; 
            }
        }

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
            Assert.IsNotNull(influenceVolume, "influenceVolume must have an instance at this point. See HDProbe.Awake()");
            if (m_Version != currentVersion)
            {
                // Add here data migration code
                if(m_Version < 2)
                {
                    influenceVolume.MigrateOffsetSphere();
                }
                m_Version = currentVersion;
            }

            influenceVolume.boxBlendNormalDistanceNegative = Vector3.zero;
            influenceVolume.boxBlendNormalDistancePositive = Vector3.zero;
        }
    }
}
