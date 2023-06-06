using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class CamerasOverlayCallbacks : Editor
    {
        [InitializeOnLoadMethod]
        static void InitializeCameraPreview()
        {
            UnityEditor.CameraEditorUtils.virtualCameraPreviewInstantiator = () =>
            {
                var camera = EditorUtility.CreateGameObjectWithHideFlags("HDRP Preview Camera", HideFlags.HideAndDontSave, typeof(Camera)).GetComponent<Camera>();

                camera.enabled = false;
                camera.cameraType = CameraType.Preview;

                var additionalData = camera.gameObject.AddComponent<HDAdditionalCameraData>();
                additionalData.isEditorCameraPreview = true;

                return camera;
            };
        }
    }

}
