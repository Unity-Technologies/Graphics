using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDCameraEditor
    {
        void OnSceneGUI()
        {
            var c = (Camera)target;

            if (!CameraEditorUtils.IsViewPortRectValidToRender(c.rect))
                return;
            
            SceneViewOverlay.Window(
                EditorGUIUtility.TrTextContent("Camera Preview"),
                OnOverlayGUI,
                -100,
                target,
                SceneViewOverlay.WindowDisplayOption.OneWindowPerTarget);
            
            UnityEditor.CameraEditorUtils.HandleFrustum(c, c.GetInstanceID());
        }

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            CameraEditorUtils.DrawCameraSceneViewOverlay(target, sceneView, InitializePreviewCamera);
        }

        Camera InitializePreviewCamera(Camera c, Vector2 previewSize)
        {
            m_PreviewCamera.CopyFrom(c);
            EditorUtility.CopySerialized(c, m_PreviewCamera);
            var cameraData = c.GetComponent<HDAdditionalCameraData>();
            EditorUtility.CopySerialized(cameraData, m_PreviewAdditionalCameraData);
            // We need to explicitly reset the camera type here
            // It is probably a CameraType.Game, because we copied the source camera's properties.
            m_PreviewCamera.cameraType = CameraType.Preview;

            var previewTexture = GetPreviewTextureWithSize((int)previewSize.x, (int)previewSize.y);
            m_PreviewCamera.targetTexture = previewTexture;
            m_PreviewCamera.pixelRect = new Rect(0, 0, previewSize.x, previewSize.y);

            return m_PreviewCamera;
        }
    }
}
