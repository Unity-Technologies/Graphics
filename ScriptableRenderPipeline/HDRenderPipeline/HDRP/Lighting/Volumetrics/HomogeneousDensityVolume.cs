namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Homogeneous Density Volume", 1100)]
    public class HomogeneousDensityVolume : MonoBehaviour
    {
        public VolumeParameters volumeParameters = new VolumeParameters();

        private void Awake()
        {
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void Update()
        {
        }

        private void OnValidate()
        {
            volumeParameters.Constrain();
        }

        void OnDrawGizmos()
        {
            if (volumeParameters.IsLocalVolume())
            {
                Gizmos.color  = volumeParameters.albedo;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
        }

        // Returns NULL if a global fog component does not exist, or is not enabled.
        public static HomogeneousDensityVolume GetGlobalHomogeneousDensityVolume()
        {
            HomogeneousDensityVolume globalVolume = null;

            HomogeneousDensityVolume[] volumes = FindObjectsOfType(typeof(HomogeneousDensityVolume)) as HomogeneousDensityVolume[];

            foreach (HomogeneousDensityVolume volume in volumes)
            {
                if (volume.enabled && !volume.volumeParameters.IsLocalVolume())
                {
                    globalVolume = volume;
                    break;
                }
            }

            return globalVolume;
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
