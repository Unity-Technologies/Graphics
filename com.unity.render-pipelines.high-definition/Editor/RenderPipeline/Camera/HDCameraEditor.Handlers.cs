using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering.HighDefinition
{
    partial class HDCameraEditor
    {
        void OnSceneGUI()
        {
            var c = (Camera)target;

            if (!UnityEditor.Rendering.CameraEditorUtils.IsViewPortRectValidToRender(c.rect))
                return;

            SceneViewOverlay_Window(EditorGUIUtility.TrTextContent("Camera Preview"), OnOverlayGUI, -100, target);

            UnityEditor.CameraEditorUtils.HandleFrustum(c, c.GetInstanceID());
        }

        void OnOverlayGUI(Object target, SceneView sceneView)
        {
            UnityEditor.Rendering.CameraEditorUtils.DrawCameraSceneViewOverlay(target, sceneView, InitializePreviewCamera);
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

        static Type k_SceneViewOverlay_WindowFunction = Type.GetType("UnityEditor.SceneViewOverlay+WindowFunction,UnityEditor");
        static Type k_SceneViewOverlay_WindowDisplayOption = Type.GetType("UnityEditor.SceneViewOverlay+WindowDisplayOption,UnityEditor");
        static MethodInfo k_SceneViewOverlay_Window = Type.GetType("UnityEditor.SceneViewOverlay,UnityEditor")
            .GetMethod(
                "Window",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                CallingConventions.Any,
                new[] { typeof(GUIContent), k_SceneViewOverlay_WindowFunction, typeof(int), typeof(Object), k_SceneViewOverlay_WindowDisplayOption, typeof(EditorWindow) },
                null);
        static void SceneViewOverlay_Window(GUIContent title, Action<Object, SceneView> sceneViewFunc, int order, Object target)
        {
            k_SceneViewOverlay_Window.Invoke(null, new[]
            {
                title, DelegateUtility.Cast(sceneViewFunc, k_SceneViewOverlay_WindowFunction),
                order,
                target,
                Enum.ToObject(k_SceneViewOverlay_WindowDisplayOption, 1),
                null
            });
        }
    }
}
