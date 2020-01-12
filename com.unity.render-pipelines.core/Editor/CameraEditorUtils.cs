using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Utility functions for cameras in the editor.
    /// </summary>
    public static class CameraEditorUtils
    {
        /// <summary>Delegate that must give an initialized preview camera</summary>
        /// <param name="sourceCamera">The initial Camera we want a preview from</param>
        /// <param name="previewSize">The size of the preview</param>
        /// <returns>The Preview camera, initialized</returns>
        public delegate Camera GetPreviewCamera(Camera sourceCamera, Vector2 previewSize);

        const float k_PreviewNormalizedSize = 0.2f;

        internal static Material s_GUITextureBlit2SRGBMaterial;

        /// <summary>
        /// The material used to display a texture into SRGB
        /// </summary>
        public static Material GUITextureBlit2SRGBMaterial
        {
            get
            {
                if (!s_GUITextureBlit2SRGBMaterial)
                {
                    Shader shader = EditorGUIUtility.LoadRequired("SceneView/GUITextureBlit2SRGB.shader") as Shader;
                    s_GUITextureBlit2SRGBMaterial = new Material(shader);
                    s_GUITextureBlit2SRGBMaterial.hideFlags = HideFlags.HideAndDontSave;
                }
                s_GUITextureBlit2SRGBMaterial.SetFloat("_ManualTex2SRGB", QualitySettings.activeColorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
                return s_GUITextureBlit2SRGBMaterial;
            }
        }
        /// <summary>
        /// Draw the overlay of a Camera
        /// </summary>
        /// <param name="target">The Camera that we want a preview</param>
        /// <param name="sceneView">The scene view where to draw it</param>
        /// <param name="previewCameraGetter">The way to get the preview camera corresponding to the target</param>

        public static void DrawCameraSceneViewOverlay(Object target, SceneView sceneView, GetPreviewCamera previewCameraGetter)
        {
            if (target == null) return;

            // cache some deep values
            var c = (Camera)target;

            var previewSize = Handles.GetMainGameViewSize();
            if (previewSize.x < 0f)
            {
                // Fallback to Scene View of not a valid game view size
                previewSize.x = sceneView.position.width;
                previewSize.y = sceneView.position.height;
            }
            // Apply normalizedviewport rect of camera
            var normalizedViewPortRect = c.rect;
            previewSize.x *= Mathf.Max(normalizedViewPortRect.width, 0f);
            previewSize.y *= Mathf.Max(normalizedViewPortRect.height, 0f);

            // Prevent using invalid previewSize
            if (previewSize.x <= 0f || previewSize.y <= 0f)
                return;

            var aspect = previewSize.x / previewSize.y;

            // Scale down (fit to scene view)
            previewSize.y = k_PreviewNormalizedSize * sceneView.position.height;
            previewSize.x = previewSize.y * aspect;
            if (previewSize.y > sceneView.position.height * 0.5f)
            {
                previewSize.y = sceneView.position.height * 0.5f;
                previewSize.x = previewSize.y * aspect;
            }
            if (previewSize.x > sceneView.position.width * 0.5f)
            {
                previewSize.x = sceneView.position.width * 0.5f;
                previewSize.y = previewSize.x / aspect;
            }

            // Get and reserve rect
            Rect cameraRect = GUILayoutUtility.GetRect(previewSize.x, previewSize.y);

            if (Event.current.type == EventType.Repaint)
            {
                var previewCamera = previewCameraGetter(c, previewSize);
                if (previewCamera.targetTexture == null)
                {
                    Debug.LogError("The preview camera must render in a render target");
                    return;
                }

                bool drawGizmo = sceneView.drawGizmos;
                sceneView.drawGizmos = false;
                previewCamera.Render();
                sceneView.drawGizmos = drawGizmo;
                Graphics.DrawTexture(cameraRect, previewCamera.targetTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, GUITextureBlit2SRGBMaterial);
                // We set target texture to null after this call otherwise if both sceneview and gameview are visible and we have a preview camera wwe
                // get this error: "Releasing render texture that is set as Camera.targetTexture!"
                previewCamera.targetTexture = null;
            }
        }
        /// <summary>
        /// Check if the view port rect have a positive size
        /// </summary>
        /// <param name="normalizedViewPortRect">The rect to check</param>
        /// <returns>True: the rect have positive size</returns>
        public static bool IsViewPortRectValidToRender(Rect normalizedViewPortRect)
        {
            if (normalizedViewPortRect.width <= 0f || normalizedViewPortRect.height <= 0f)
                return false;
            if (normalizedViewPortRect.x >= 1f || normalizedViewPortRect.xMax <= 0f)
                return false;
            if (normalizedViewPortRect.y >= 1f || normalizedViewPortRect.yMax <= 0f)
                return false;
            return true;
        }
    }
}
