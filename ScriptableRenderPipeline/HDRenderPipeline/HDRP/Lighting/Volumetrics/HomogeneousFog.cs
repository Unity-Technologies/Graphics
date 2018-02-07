namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [AddComponentMenu("RenderPipeline/High Definition/Homogenous Fog", -1)]
    public class HomogeneousFog : MonoBehaviour
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
        public static HomogeneousFog GetGlobalFogComponent()
        {
            HomogeneousFog globalFogComponent = null;

            HomogeneousFog[] fogComponents = FindObjectsOfType(typeof(HomogeneousFog)) as HomogeneousFog[];

            foreach (HomogeneousFog fogComponent in fogComponents)
            {
                if (fogComponent.enabled && !fogComponent.volumeParameters.IsLocalVolume())
                {
                    globalFogComponent = fogComponent;
                    break;
                }
            }

            return globalFogComponent;
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
