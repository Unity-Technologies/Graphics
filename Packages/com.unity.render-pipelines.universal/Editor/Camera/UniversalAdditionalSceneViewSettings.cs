using UnityEditor.Experimental;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal
{
    [InitializeOnLoad]
    static class UniversalAdditionalSceneViewSettings
    {
        static UniversalAdditionalSceneViewSettings()
        {
            SceneView.onCameraCreated += EnsureAdditionalData;
        }
        
        static void EnsureAdditionalData(SceneView sceneView)
        {
            if (!sceneView.camera.TryGetComponent(out UniversalAdditionalCameraData hdAdditionalCameraData))
                hdAdditionalCameraData = sceneView.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        }
    }
}