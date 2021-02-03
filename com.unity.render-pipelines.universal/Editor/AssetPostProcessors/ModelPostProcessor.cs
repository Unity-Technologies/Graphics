using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class ModelPostprocessor : AssetPostprocessor
    {
        void OnPostprocessModel(GameObject go)
        {
            CoreEditorUtils.AddAdditionalData<Camera, UniversalAdditionalCameraData>(go);
            CoreEditorUtils.AddAdditionalData<Light, UniversalAdditionalLightData>(go);
        }
    }
}
