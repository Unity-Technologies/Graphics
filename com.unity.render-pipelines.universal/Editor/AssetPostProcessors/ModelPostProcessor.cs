using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.HighDefinition
{
    class ModelPostprocessor : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject go)
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
            {
                CoreEditorUtils.AddAdditionalData<Camera, UniversalAdditionalCameraData>(go);
                CoreEditorUtils.AddAdditionalData<Light, UniversalAdditionalLightData>(go);
            }
        }
    }
}
