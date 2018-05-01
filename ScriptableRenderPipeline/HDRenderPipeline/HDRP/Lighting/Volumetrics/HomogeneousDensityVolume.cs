namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Homogeneous Density Volume", 1100)]
    public class HomogeneousDensityVolume : MonoBehaviour
    {
        public DensityVolumeParameters parameters;

        public HomogeneousDensityVolume()
        {
            parameters.albedo       = new Color(0.5f, 0.5f, 0.5f);
            parameters.meanFreePath = 10.0f;
            parameters.asymmetry    = 0.0f;
        }

        private void Awake()
        {
        }

        private void OnEnable()
        {
            DensityVolumeManager.manager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            DensityVolumeManager.manager.DeRegisterVolume(this);
        }

        private void Update()
        {
        }

        private void OnValidate()
        {
            parameters.Constrain();
        }

        void OnDrawGizmos()
        {
            Gizmos.color  = parameters.albedo;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
