namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [AddComponentMenu("RenderPipeline/High Definition/Homogeneous Medium Volume", -1)]
    public class HomogeneousMediumVolume : MonoBehaviour
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
        public static HomogeneousMediumVolume GetGlobalHomogeneousMediumVolume()
        {
            HomogeneousMediumVolume globalVolume = null;

            HomogeneousMediumVolume[] volumes = FindObjectsOfType(typeof(HomogeneousMediumVolume)) as HomogeneousMediumVolume[];

            foreach (HomogeneousMediumVolume volume in volumes)
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
