using UnityEngine;
using UnityEngine.Rendering;

namespace WaterSystem
{
    /// <summary>
    /// Camera script to align the water mesh with the camera in a quantized manner
    /// </summary>
    [ExecuteInEditMode]
    public class MainCameraAlign : MonoBehaviour
    {

        public float quantizeValue = 6.25f;
        public float forwards = 10f;
        public float yOffset = -0.25f;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += UpdatePosition;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= UpdatePosition;
        }

        private void UpdatePosition(ScriptableRenderContext src, Camera cam)
        {
            if (cam.cameraType == CameraType.Preview) return;
            
            var newPos = cam.transform.TransformPoint(Vector3.forward * forwards);
            newPos.y = yOffset;
            newPos.x = QuantizeValue(newPos.x);
            newPos.z = QuantizeValue(newPos.z);
            transform.position = newPos;
        }

        private float QuantizeValue(float value)
        {
            return quantizeValue * (int) (value / quantizeValue);
        }
    }
}
