using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class ModelPostprocessor : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject go)
        {
            CoreEditorUtils.AddAdditionalData<Camera, HDAdditionalCameraData>(go, HDAdditionalCameraData.InitDefaultHDAdditionalCameraData);
            CoreEditorUtils.AddAdditionalData<Light, HDAdditionalLightData>(go, HDAdditionalLightData.InitDefaultHDAdditionalLightData);
            CoreEditorUtils.AddAdditionalData<ReflectionProbe, HDAdditionalReflectionData>(go);
        }
    }
}
