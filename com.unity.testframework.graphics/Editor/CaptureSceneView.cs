using System;
using System.Collections;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Capture the current scene view into a Texture2D for use in ImageAssert tests.
    /// </summary>
    public class CaptureSceneView
    {
        private static Texture2D CapturedTexture;

        /// <summary>
        /// Returns the result of the Scene View capture
        /// </summary>
        ///
        public static Texture2D Result { get => CapturedTexture; }

        /// <summary>
        /// Captures a scene view from the perspective of the MainCamera in the hierarchy.
        /// </summary>
        /// <param name="sceneView"> An existing scene view that will be used instead of creating a new one. </param>
        /// <param name="sceneViewWidth"> The width (in pixels) for the captured scene view image. Defaults to 512. </param>
        /// <param name="sceneViewHeight"> The height (in pixels) for the captured scene view image. Defaults to 512. </param>
        /// <param name="delayBeforeCapture"> The delay between setting up the camera and capturing. Defaults to 100. </param>
        /// <returns> an IEnumerator to yield return to UnityTests </returns>
        public static IEnumerator CaptureFromMainCamera(SceneView sceneView = null, int sceneViewWidth = 512, int sceneViewHeight = 512, int delayBeforeCapture = 100)
        {
            yield return Capture(Camera.main, sceneView, sceneViewWidth, sceneViewHeight, delayBeforeCapture);
        }

        /// <summary>
        /// Captures a scene view from the perspective of the chosen viewpoint transform.
        /// </summary>
        /// <param name="imageComparisonViewpoint"> The viewpoint camera to be used for capturing the image. </param>
        /// <param name="sceneView"> An existing scene view that will be used instead of creating a new one. </param>
        /// <param name="sceneViewWidth"> The size (in pixels) for the captured scene view image. Defaults to 512. </param>
        /// <param name="sceneViewHeight"> The height (in pixels) for the captured scene view image. Defaults to 512. </param>
        /// <param name="delayBeforeCapture"> The delay between setting up the camera and capturing. Defaults to 100. </param>
        /// <returns> an IEnumerator to yield return to UnityTests </returns>
        public static IEnumerator Capture(Camera imageComparisonViewpoint, SceneView sceneView = null, int sceneViewWidth = 512, int sceneViewHeight = 512, int delayBeforeCapture = 100)
        {
            // Create the Scene View or use the user-provided one
            if (sceneView == null)
            {
                sceneView = EditorWindow.CreateWindow<SceneView>();
            }

            sceneView.position = new Rect(0, 0, imageComparisonViewpoint.pixelWidth * 0.5f, imageComparisonViewpoint.pixelHeight * 0.5f);
            sceneView.minSize = new Vector2(sceneViewWidth, sceneViewHeight);
            sceneView.maxSize = new Vector2(sceneViewWidth, sceneViewHeight);
            yield return null;

            // Move the scene view camera to the scene's MainCamera
            sceneView.AlignViewToObject(imageComparisonViewpoint.transform);

            // Wait for the view to change
            while (!sceneView.camera.transform.position.Equals(imageComparisonViewpoint.transform.position) ||
                   !sceneView.camera.transform.rotation.Equals(imageComparisonViewpoint.transform.rotation))
                yield return null;

            // Wait for all shaders to finish compiling
            bool asyncAllowedPriorState = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            while (ShaderUtil.anythingCompiling)
                yield return null;

            for (int i = 0; i < delayBeforeCapture; i++) yield return null; // Just waiting for shaders not enough for SRPs

            // Capture the screen
            sceneView.Focus();
            Color[] svCol = InternalEditorUtility.ReadScreenPixel(sceneView.position.position, sceneViewWidth, sceneViewHeight);

            // Write the screen capture to a texture
            GameObject.DestroyImmediate(CapturedTexture);
            CapturedTexture = new Texture2D(sceneViewWidth, sceneViewHeight, TextureFormat.RGB24, false);
            CapturedTexture.SetPixels(svCol);
            CapturedTexture.Apply();

            ShaderUtil.allowAsyncCompilation = asyncAllowedPriorState;
            sceneView.Close();
        }
    }
}
