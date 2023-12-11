#if UNITY_EDITOR
using System.Collections.Generic;
using System.Collections;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    class SceneViewDrawMode
    {
        static HashSet<SceneView> sceneViewHaveValidateFunction = new HashSet<SceneView>();

        static private bool RejectDrawMode(SceneView.CameraMode cameraMode)
        {
            return cameraMode.drawMode != DrawCameraMode.ShadowCascades &&
                cameraMode.drawMode != DrawCameraMode.RenderPaths &&
                cameraMode.drawMode != DrawCameraMode.AlphaChannel &&
                cameraMode.drawMode != DrawCameraMode.Overdraw &&
                cameraMode.drawMode != DrawCameraMode.Mipmaps &&
                cameraMode.drawMode != DrawCameraMode.DeferredDiffuse &&
                cameraMode.drawMode != DrawCameraMode.DeferredSpecular &&
                cameraMode.drawMode != DrawCameraMode.DeferredSmoothness &&
                cameraMode.drawMode != DrawCameraMode.DeferredNormal &&
                cameraMode.drawMode != DrawCameraMode.ValidateAlbedo &&
                cameraMode.drawMode != DrawCameraMode.ValidateMetalSpecular &&
                cameraMode.drawMode != DrawCameraMode.SpriteMask &&
                cameraMode.drawMode != DrawCameraMode.TextureStreaming;
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

        static public void SetupDrawMode()
        {
            EditorApplication.update -= UpdateSceneViewStates;
            EditorApplication.update += UpdateSceneViewStates;
        }

        static public void ResetDrawMode()
        {
            EditorApplication.update -= UpdateSceneViewStates;

            foreach (var sceneView in sceneViewHaveValidateFunction)
                sceneView.onValidateCameraMode -= RejectDrawMode;
            sceneViewHaveValidateFunction.Clear();
        }
    }
}
#endif
