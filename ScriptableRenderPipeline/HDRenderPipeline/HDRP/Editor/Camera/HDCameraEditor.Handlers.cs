using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.Rendering
{
    using _ = CoreEditorUtils;

    partial class HDCameraEditor
    {
        static readonly Color k_ColorThemeCameraGizmo = new Color(233f / 255f, 233f / 255f, 233f / 255f, 128f / 255f);
        const float k_PreviewNormalizedSize = 0.2f;

        internal static Material s_GUITextureBlit2SRGBMaterial;
        internal static Material GUITextureBlit2SRGBMaterial
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

        void OnSceneGUI()
        {
            var c = (Camera)target;

            if (!IsViewPortRectValidToRender(c.rect))
                return;

            SceneViewOverlay_Window(_.GetContent("Camera Preview"), OnOverlayGUI, -100, target);

            Handle_Frustrum(c);
        }

        static void Handle_Frustrum(Camera c)
        {
            Color orgHandlesColor = Handles.color;
            Color slidersColor = k_ColorThemeCameraGizmo;
            slidersColor.a *= 2f;
            Handles.color = slidersColor;

            // get the corners of the far clip plane in world space
            var far = new Vector3[4];
            float frustumAspect;
            if (!GetFrustum(c, null, far, out frustumAspect))
                return;
            var leftBottomFar = far[0];
            var leftTopFar = far[1];
            var rightTopFar = far[2];
            var rightBottomFar = far[3];

            // manage our own gui changed state, so we can use it for individual slider changes
            var guiChanged = GUI.changed;

            // FOV handles
            var farMid = Vector3.Lerp(leftBottomFar, rightTopFar, 0.5f);

            // Top and bottom handles
            float halfHeight = -1.0f;
            var changedPosition = MidPointPositionSlider(leftTopFar, rightTopFar, c.transform.up);
            if (!GUI.changed)
                changedPosition = MidPointPositionSlider(leftBottomFar, rightBottomFar, -c.transform.up);
            if (GUI.changed)
                halfHeight = (changedPosition - farMid).magnitude;

            // Left and right handles
            GUI.changed = false;
            changedPosition = MidPointPositionSlider(rightBottomFar, rightTopFar, c.transform.right);
            if (!GUI.changed)
                changedPosition = MidPointPositionSlider(leftBottomFar, leftTopFar, -c.transform.right);
            if (GUI.changed)
                halfHeight = (changedPosition - farMid).magnitude / frustumAspect;

            // Update camera settings if changed
            if (halfHeight >= 0.0f)
            {
                Undo.RecordObject(c, "Adjust Camera");
                if (c.orthographic)
                    c.orthographicSize = halfHeight;
                else
                {
                    Vector3 pos = farMid + c.transform.up * halfHeight;
                    c.fieldOfView = Vector3.Angle(c.transform.forward, (pos - c.transform.position)) * 2f;
                }
                guiChanged = true;
            }

            GUI.changed = guiChanged;
            Handles.color = orgHandlesColor;
        }

        public void OnOverlayGUI(Object target, SceneView sceneView)
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
                // setup camera and render
                m_PreviewCamera.CopyFrom(c);

                var previewTexture = GetPreviewTextureWithSize((int)cameraRect.width, (int)cameraRect.height);
                m_PreviewCamera.targetTexture = previewTexture;
                m_PreviewCamera.pixelRect = new Rect(0, 0, cameraRect.width, cameraRect.height);

                GL.sRGBWrite = QualitySettings.activeColorSpace == ColorSpace.Linear;
                SynchronizePreviewCameraWithCamera(c);
                m_PreviewCamera.Render();
                GL.sRGBWrite = false;
                Graphics.DrawTexture(cameraRect, previewTexture, new Rect(0, 0, 1, 1), 0, 0, 0, 0, GUI.color, GUITextureBlit2SRGBMaterial);
            }
        }

        static float GetGameViewAspectRatio()
        {
            Vector2 gameViewSize = Handles.GetMainGameViewSize();
            if (gameViewSize.x < 0f)
            {
                // Fallback to Scene View of not a valid game view size
                gameViewSize.x = Screen.width;
                gameViewSize.y = Screen.height;
            }

            return gameViewSize.x / gameViewSize.y;
        }

        static float GetFrustumAspectRatio(Camera camera)
        {
            var normalizedViewPortRect = camera.rect;
            if (normalizedViewPortRect.width <= 0f || normalizedViewPortRect.height <= 0f)
                return -1f;

            var viewportAspect = normalizedViewPortRect.width / normalizedViewPortRect.height;
            return GetGameViewAspectRatio() * viewportAspect;
        }

        // Returns near- and far-corners in this order: leftBottom, leftTop, rightTop, rightBottom
        // Assumes input arrays are of length 4 (if allocated)
        static bool GetFrustum(Camera camera, Vector3[] near, Vector3[] far, out float frustumAspect)
        {
            frustumAspect = GetFrustumAspectRatio(camera);
            if (frustumAspect < 0)
                return false;

            if (far != null)
            {
                far[0] = new Vector3(0, 0, camera.farClipPlane); // leftBottomFar
                far[1] = new Vector3(0, 1, camera.farClipPlane); // leftTopFar
                far[2] = new Vector3(1, 1, camera.farClipPlane); // rightTopFar
                far[3] = new Vector3(1, 0, camera.farClipPlane); // rightBottomFar
                for (int i = 0; i < 4; ++i)
                    far[i] = camera.ViewportToWorldPoint(far[i]);
            }

            if (near != null)
            {
                near[0] = new Vector3(0, 0, camera.nearClipPlane); // leftBottomNear
                near[1] = new Vector3(0, 1, camera.nearClipPlane); // leftTopNear
                near[2] = new Vector3(1, 1, camera.nearClipPlane); // rightTopNear
                near[3] = new Vector3(1, 0, camera.nearClipPlane); // rightBottomNear
                for (int i = 0; i < 4; ++i)
                    near[i] = camera.ViewportToWorldPoint(near[i]);
            }
            return true;
        }

        static Vector3 MidPointPositionSlider(Vector3 position1, Vector3 position2, Vector3 direction)
        {
            Vector3 midPoint = Vector3.Lerp(position1, position2, 0.5f);
            return Handles.Slider(midPoint, direction, HandleUtility.GetHandleSize(midPoint) * 0.03f, Handles.DotHandleCap, 0f);
        }

        static bool IsViewPortRectValidToRender(Rect normalizedViewPortRect)
        {
            if (normalizedViewPortRect.width <= 0f || normalizedViewPortRect.height <= 0f)
                return false;
            if (normalizedViewPortRect.x >= 1f || normalizedViewPortRect.xMax <= 0f)
                return false;
            if (normalizedViewPortRect.y >= 1f || normalizedViewPortRect.yMax <= 0f)
                return false;
            return true;
        }

        static Type k_SceneViewOverlay_WindowFunction = Type.GetType("UnityEditor.SceneViewOverlay+WindowFunction,UnityEditor");
        static Type k_SceneViewOverlay_WindowDisplayOption = Type.GetType("UnityEditor.SceneViewOverlay+WindowDisplayOption,UnityEditor");
        static MethodInfo k_SceneViewOverlay_Window = Type.GetType("UnityEditor.SceneViewOverlay,UnityEditor")
            .GetMethod(
            "Window",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            CallingConventions.Any,
            new[] { typeof(GUIContent), k_SceneViewOverlay_WindowFunction, typeof(int), typeof(Object), k_SceneViewOverlay_WindowDisplayOption },
            null);
        static void SceneViewOverlay_Window(GUIContent title, Action<Object, SceneView> sceneViewFunc, int order, Object target)
        {
            k_SceneViewOverlay_Window.Invoke(null, new[]
            {
                title, DelegateUtility.Cast(sceneViewFunc, k_SceneViewOverlay_WindowFunction),
                order,
                target,
                Enum.ToObject(k_SceneViewOverlay_WindowDisplayOption, 1)
            });
        }
    }
}
