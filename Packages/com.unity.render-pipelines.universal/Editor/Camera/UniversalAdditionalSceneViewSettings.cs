using UnityEditor.Experimental;
using UnityEngine;
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
            if (!sceneView.camera.TryGetComponent(out UniversalAdditionalCameraData additionalCameraData))
            {
                additionalCameraData = sceneView.camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                additionalCameraData.hideFlags = HideFlags.HideAndDontSave;
            }
        }
    }
}
