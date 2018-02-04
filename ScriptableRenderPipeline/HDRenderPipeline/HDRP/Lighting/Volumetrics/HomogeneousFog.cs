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
            if (volumeParameters != null && !volumeParameters.IsVolumeUnbounded())
            {
                Gizmos.DrawWireCube(volumeParameters.bounds.center, volumeParameters.bounds.size);
            }
        }

        // Returns NULL if a global fog component does not exist, or is not enabled.
        public static HomogeneousFog GetGlobalFogComponent()
        {
            HomogeneousFog globalFogComponent = null;

            HomogeneousFog[] fogComponents = FindObjectsOfType(typeof(HomogeneousFog)) as HomogeneousFog[];

            foreach (HomogeneousFog fogComponent in fogComponents)
            {
                if (fogComponent.enabled && fogComponent.volumeParameters.IsVolumeUnbounded())
                {
                    globalFogComponent = fogComponent;
                    break;
                }
            }

            return globalFogComponent;
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
