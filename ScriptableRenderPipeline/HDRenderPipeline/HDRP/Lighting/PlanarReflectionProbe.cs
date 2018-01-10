using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class PlanarReflectionProbe : MonoBehaviour
    {
        [SerializeField]
        ProxyVolumeComponent m_ProxyVolumeReference;
        [SerializeField]
        InfluenceVolume m_InfluenceVolume;
        [SerializeField]
        Vector3 m_CaptureOffset;
        [SerializeField]
        [Range(0, 1)]
        float m_Dimmer = 1;
        [SerializeField]
        ReflectionProbeMode m_Mode = ReflectionProbeMode.Baked;

        public ProxyVolumeComponent proxyVolumeReference { get { return m_ProxyVolumeReference; } }
        public InfluenceVolume influenceVolume { get { return m_InfluenceVolume; } }
        public BoundingSphere boundingSphere { get { return m_InfluenceVolume.GetBoundingSphereAt(transform); } }
        public Texture texture { get; private set; }
        public Bounds bounds { get { return m_InfluenceVolume.GetBoundsAt(transform); } }
        public Matrix4x4 influenceToWorld { get { return transform.localToWorldMatrix; } }
        public Vector3 captureOffset { get { return m_CaptureOffset; } }
        public float dimmer { get { return m_Dimmer; } }
        public ReflectionProbeMode mode { get { return m_Mode; } }

        #region Proxy Properties
        public Matrix4x4 proxyToWorld
        {
            get
            {
                return m_ProxyVolumeReference != null 
                    ? m_ProxyVolumeReference.transform.localToWorldMatrix 
                    : transform.localToWorldMatrix;
            }
        }
        public ShapeType proxyShape
        {
            get
            {
                return m_ProxyVolumeReference != null
                    ? m_ProxyVolumeReference.projectionVolume.shapeType
                    : influenceVolume.shapeType;
            }
        }
        public Vector3 proxyExtents
        {
            get
            {
                return m_ProxyVolumeReference != null
                    ? m_ProxyVolumeReference.projectionVolume.boxSize
                    : influenceVolume.boxBaseSize;
            }
        }
        public bool infiniteProjection { get { return m_ProxyVolumeReference != null && m_ProxyVolumeReference.projectionVolume.infiniteProjection; } }
        #endregion

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
            ReflectionSystem.SetProbeBoundsDirty(this);
        }
    }
}
