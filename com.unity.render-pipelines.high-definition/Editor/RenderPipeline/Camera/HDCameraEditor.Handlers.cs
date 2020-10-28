using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDCameraEditor
    {
        void OnSceneGUI()
        {
            s_Editor = null;
            var c = (Camera)target;

            if (!UnityEditor.Rendering.CameraEditorUtils.IsViewPortRectValidToRender(c.rect))
                return;

            s_Editor = this;

            UnityEditor.CameraEditorUtils.HandleFrustum(c, c.GetInstanceID());
        }

        void OnOverlayGUI(SceneView sceneView)
        {
            UnityEditor.Rendering.CameraEditorUtils.DrawCameraSceneViewOverlay((Camera)target, sceneView, InitializePreviewCamera);
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
            m_PreviewCamera.gameObject.SetActive(false);

            var previewTexture = GetPreviewTextureWithSize((int)previewSize.x, (int)previewSize.y);
            m_PreviewCamera.targetTexture = previewTexture;
            m_PreviewCamera.pixelRect = new Rect(0, 0, previewSize.x, previewSize.y);

            return m_PreviewCamera;
        }

        static HDCameraEditor s_Editor;

        [Overlay(typeof(SceneView),"Scene View/HDCamera","unity-sceneview-hdcamera","HD Camera")]
        class SceneViewCameraOverlay : SceneView.TransientSceneViewOverlay
        {
            public override bool ShouldDisplay()
            {
                return s_Editor != null;
            }
            public override void OnGUI()
            {
                s_Editor.OnOverlayGUI(containerWindow as SceneView);
            }
        }
    }
}
