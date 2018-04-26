namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    [AddComponentMenu("Rendering/Volumetric Lighting Controller", 1101)]
    public class VolumetricLightingController : MonoBehaviour
    {
        public VolumetricLightingSystem.ControllerParameters parameters;

        public VolumetricLightingController()
        {
            parameters.vBufferNearPlane                 = 0.5f;
            parameters.vBufferFarPlane                  = 64.0f;
            parameters.depthSliceDistributionUniformity = 0.75f;
        }

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
            var camera = GetComponent<Camera>();

            if (camera != null)
            {
                // We must not allow the V-Buffer to extend past the camera's frustum.
                float n = camera.nearClipPlane;
                float f = camera.farClipPlane;

                parameters.vBufferFarPlane  = Mathf.Clamp(parameters.vBufferFarPlane,  n, f);
                parameters.vBufferNearPlane = Mathf.Clamp(parameters.vBufferNearPlane, n, parameters.vBufferFarPlane);
                parameters.depthSliceDistributionUniformity = Mathf.Clamp01(parameters.depthSliceDistributionUniformity);
            }
            else
            {
                Debug.Log("Volumetric Lighting Controller must be attached to a camera!");
            }
        }
    }
} // UnityEngine.Experimental.Rendering.HDPipeline
