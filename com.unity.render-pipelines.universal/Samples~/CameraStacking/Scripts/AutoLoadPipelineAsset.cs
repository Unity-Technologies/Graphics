using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CameraStacking
{
    [ExecuteAlways]
    public class AutoLoadPipelineAsset : MonoBehaviour
    {
        public UniversalRenderPipelineAsset pipelineAsset;

        private void OnEnable()
        {
            UpdatePipeline();
        }

        void UpdatePipeline()
        {
            if (pipelineAsset)
            {
                GraphicsSettings.renderPipelineAsset = pipelineAsset;
            }
        }
    }
}