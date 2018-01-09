namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class PlanarReflectionProbe : MonoBehaviour
    {
        [SerializeField]
        ProjectionVolumeComponent m_ProjectionVolumeReference;
        [SerializeField]
        InfluenceVolume m_InfluenceVolume;

        public ProjectionVolumeComponent projectionVolumeReference { get { return m_ProjectionVolumeReference; } }
        public InfluenceVolume influenceVolume { get { return m_InfluenceVolume; } }

        public BoundingSphere boundingSphere
        {
            get { return m_InfluenceVolume.GetBoundingSphereAt(transform); }
        }

        public Texture texture { get; private set; }
        public Bounds bounds { get { return m_InfluenceVolume.GetBoundsAt(transform); } }

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
