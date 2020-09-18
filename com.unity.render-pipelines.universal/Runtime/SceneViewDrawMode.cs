#if UNITY_EDITOR
using System.Collections.Generic;
using System.Collections;

namespace UnityEditor.Rendering.Universal
{
    internal static class SceneViewDrawMode
    {
        static HashSet<SceneView> sceneViewHaveValidateFunction = new HashSet<SceneView>();

        static bool RejectDrawMode(SceneView.CameraMode cameraMode)
        {
            if (cameraMode.drawMode == DrawCameraMode.ShadowCascades ||
                cameraMode.drawMode == DrawCameraMode.RenderPaths ||
                cameraMode.drawMode == DrawCameraMode.AlphaChannel ||
                cameraMode.drawMode == DrawCameraMode.Overdraw ||
                cameraMode.drawMode == DrawCameraMode.Mipmaps ||
                cameraMode.drawMode == DrawCameraMode.SpriteMask ||
                cameraMode.drawMode == DrawCameraMode.DeferredDiffuse ||
                cameraMode.drawMode == DrawCameraMode.DeferredSpecular ||
                cameraMode.drawMode == DrawCameraMode.DeferredSmoothness ||
                cameraMode.drawMode == DrawCameraMode.DeferredNormal ||
                cameraMode.drawMode == DrawCameraMode.ValidateAlbedo ||
                cameraMode.drawMode == DrawCameraMode.ValidateMetalSpecular
            )
                return false;

            return true;
        }

        static void UpdateSceneViewStates()
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                if (sceneViewHaveValidateFunction.Contains(sceneView))
                    continue;
                

                sceneView.onValidateCameraMode += RejectDrawMode;
                sceneViewHaveValidateFunction.Add(sceneView);
            }
        }

        public static void SetupDrawMode()
        {
            EditorApplication.update -= UpdateSceneViewStates;
            EditorApplication.update += UpdateSceneViewStates;
        }

        public static void ResetDrawMode()
        {
            EditorApplication.update -= UpdateSceneViewStates;
            
            foreach (var sceneView in sceneViewHaveValidateFunction)
                sceneView.onValidateCameraMode -= RejectDrawMode;
            sceneViewHaveValidateFunction.Clear();
        }
    }
}
#endif
