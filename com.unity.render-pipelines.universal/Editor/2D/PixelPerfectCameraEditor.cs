using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(PixelPerfectCamera))]
    class PixelPerfectCameraEditor : Editor
    {
        private class Style
        {
            public GUIContent x = new GUIContent("X");
            public GUIContent y = new GUIContent("Y");
            public GUIContent assetsPPU = new GUIContent("Assets Pixels Per Unit", "The amount of pixels that make up one unit of the Scene. Set this value to match the PPU value of Sprites in the Scene.");
            public GUIContent refRes = new GUIContent("Reference Resolution", "The original resolution the Assets are designed for.");
            public GUIContent gridSnapping = new GUIContent("Grid Snapping", "Sets the snapping behavior for the camera and sprites.");
            public GUIContent cropFrame = new GUIContent("Crop Frame", "Crops the viewport to match the Reference Resolution, along the checked axis. Black bars will be added to fit the screen aspect ratio.");
            public GUIContent filterMode = new GUIContent("Filter Mode", "Use selected Filter Mode when using Stretch Fill to upscale from Reference Resolution.");
            public GUIContent stretchFill = new GUIContent("Stretch Fill", "If enabled, expands the viewport to fit the screen resolution while maintaining the viewport aspect ratio.");
            public GUIContent currentPixelRatio = new GUIContent("Current Pixel Ratio", "Ratio of the rendered Sprites compared to their original size.");
            public GUIContent runInEditMode = new GUIContent("Run In Edit Mode", "Enable this to preview Camera setting changes in Edit Mode. This will cause constant changes to the Scene while active.");
            public const string cameraStackingWarning = "Pixel Perfect Camera won't function properly if stacked with another camera.";
            public const string nonRenderer2DError = "Pixel Perfect Camera requires a camera using a 2D Renderer.";

            public GUIStyle centeredLabel;

            public Style()
            {
                centeredLabel = new GUIStyle(EditorStyles.label);
                centeredLabel.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static Style m_Style;

        private const float k_SingleLetterLabelWidth = 15.0f;
        private const float k_DottedLineSpacing = 2.5f;

        private SerializedProperty m_AssetsPPU;
        private SerializedProperty m_RefResX;
        private SerializedProperty m_RefResY;
        private SerializedProperty m_CropFrame;
        private SerializedProperty m_FilterMode;
        private SerializedProperty m_GridSnapping;

        private Vector2 m_GameViewSize = Vector2.zero;
        private GUIContent m_CurrentPixelRatioValue;
        bool m_CameraStacking;

        private void LazyInit()
        {
            if (m_Style == null)
                m_Style = new Style();

            if (m_CurrentPixelRatioValue == null)
                m_CurrentPixelRatioValue = new GUIContent();
        }

        bool UsingRenderer2D()
        {
            PixelPerfectCamera obj = target as PixelPerfectCamera;
            UniversalAdditionalCameraData cameraData = null;
            obj?.TryGetComponent(out cameraData);

            if (cameraData != null)
            {
                Renderer2D renderer2D = cameraData.scriptableRenderer as Renderer2D;
                if (renderer2D != null)
                    return true;
            }

            return false;
        }

        void CheckForCameraStacking()
        {
            m_CameraStacking = false;

            PixelPerfectCamera obj = target as PixelPerfectCamera;
            UniversalAdditionalCameraData cameraData = null;
            obj?.TryGetComponent(out cameraData);

            if (cameraData == null)
                return;

            if (cameraData.renderType == CameraRenderType.Base)
            {
                var cameraStack = cameraData.cameraStack;
                m_CameraStacking = cameraStack != null ? cameraStack.Count > 0 : false;
            }
            else if (cameraData.renderType == CameraRenderType.Overlay)
                m_CameraStacking = true;
        }

        public void OnEnable()
        {
            m_AssetsPPU = serializedObject.FindProperty("m_AssetsPPU");
            m_RefResX = serializedObject.FindProperty("m_RefResolutionX");
            m_RefResY = serializedObject.FindProperty("m_RefResolutionY");
            m_CropFrame = serializedObject.FindProperty("m_CropFrame");
            m_FilterMode = serializedObject.FindProperty("m_FilterMode");
            m_GridSnapping = serializedObject.FindProperty("m_GridSnapping");
        }

        public override bool RequiresConstantRepaint()
        {
            PixelPerfectCamera obj = target as PixelPerfectCamera;
            if (obj == null || !obj.enabled)
                return false;

            // If game view size changes, we need to force a repaint of the inspector as the pixel ratio value may change accordingly.
            Vector2 gameViewSize = Handles.GetMainGameViewSize();
            if (gameViewSize != m_GameViewSize)
            {
                m_GameViewSize = gameViewSize;
                return true;
            }
            else
                return false;
        }

        public override void OnInspectorGUI()
        {
            LazyInit();

            if (!UsingRenderer2D())
            {
                EditorGUILayout.HelpBox(Style.nonRenderer2DError, MessageType.Error);
                return;
            }

            float originalLabelWidth = EditorGUIUtility.labelWidth;

            serializedObject.Update();

            if (Event.current.type == EventType.Layout)
                CheckForCameraStacking();

            if (m_CameraStacking)
                EditorGUILayout.HelpBox(Style.cameraStackingWarning, MessageType.Warning);

            EditorGUILayout.PropertyField(m_AssetsPPU, m_Style.assetsPPU);
            if (m_AssetsPPU.intValue <= 0)
                m_AssetsPPU.intValue = 1;

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(m_Style.refRes);

                EditorGUIUtility.labelWidth = k_SingleLetterLabelWidth * (EditorGUI.indentLevel + 1);

                EditorGUILayout.PropertyField(m_RefResX, m_Style.x);
                if (m_RefResX.intValue <= 0)
                    m_RefResX.intValue = 1;

                EditorGUILayout.PropertyField(m_RefResY, m_Style.y);
                if (m_RefResY.intValue <= 0)
                    m_RefResY.intValue = 1;

                EditorGUIUtility.labelWidth = originalLabelWidth;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(m_CropFrame, m_Style.cropFrame);
            EditorGUILayout.PropertyField(m_GridSnapping, m_Style.gridSnapping);
            if (m_CropFrame.enumValueIndex == (int)PixelPerfectCamera.CropFrame.StretchFill)
            {
                EditorGUILayout.PropertyField(m_FilterMode, m_Style.filterMode);
            }

            serializedObject.ApplyModifiedProperties();

            PixelPerfectCamera obj = target as PixelPerfectCamera;

            if (obj != null)
            {
                if (obj.isActiveAndEnabled && (EditorApplication.isPlaying || obj.runInEditMode))
                {
                    if (Event.current.type == EventType.Layout)
                        m_CurrentPixelRatioValue.text = string.Format("{0}:1", obj.pixelRatio);

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField(m_Style.currentPixelRatio, m_CurrentPixelRatioValue);
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        void OnSceneGUI()
        {
            PixelPerfectCamera obj = target as PixelPerfectCamera;
            if (obj == null)
                return;

            Camera camera = obj.GetComponent<Camera>();

            // Show a green rect in scene view that represents the visible area when the pixel perfect correction takes effect in play mode.
            Vector2 gameViewSize = Handles.GetMainGameViewSize();
            int gameViewWidth = (int)gameViewSize.x;
            int gameViewHeight = (int)gameViewSize.y;
            int zoom = Math.Max(1, Math.Min(gameViewHeight / obj.refResolutionY, gameViewWidth / obj.refResolutionX));

            float verticalOrthoSize;
            float horizontalOrthoSize;

            if (obj.cropFrame == PixelPerfectCamera.CropFrame.StretchFill || obj.cropFrame == PixelPerfectCamera.CropFrame.Windowbox)
            {
                verticalOrthoSize = obj.refResolutionY * 0.5f / obj.assetsPPU;
                horizontalOrthoSize = verticalOrthoSize * ((float)obj.refResolutionX / obj.refResolutionY);
            }
            else if (obj.cropFrame == PixelPerfectCamera.CropFrame.Letterbox)
            {
                verticalOrthoSize = obj.refResolutionY * 0.5f / obj.assetsPPU;
                horizontalOrthoSize = verticalOrthoSize * ((float)gameViewWidth / (zoom * obj.refResolutionY));
            }
            else if (obj.cropFrame == PixelPerfectCamera.CropFrame.Pillarbox)
            {
                horizontalOrthoSize = obj.refResolutionX * 0.5f / obj.assetsPPU;
                verticalOrthoSize = horizontalOrthoSize / (zoom * obj.refResolutionX / (float)gameViewHeight);
            }
            else
            {
                verticalOrthoSize = gameViewHeight * 0.5f / (zoom * obj.assetsPPU);
                horizontalOrthoSize = verticalOrthoSize * camera.aspect;
            }

            Handles.color = Color.green;

            Vector3 cameraPosition = camera.transform.position;
            Vector3 p1 = cameraPosition + new Vector3(-horizontalOrthoSize, verticalOrthoSize, 0.0f);
            Vector3 p2 = cameraPosition + new Vector3(horizontalOrthoSize, verticalOrthoSize, 0.0f);
            Handles.DrawLine(p1, p2);

            p1 = cameraPosition + new Vector3(horizontalOrthoSize, -verticalOrthoSize, 0.0f);
            Handles.DrawLine(p2, p1);

            p2 = cameraPosition + new Vector3(-horizontalOrthoSize, -verticalOrthoSize, 0.0f);
            Handles.DrawLine(p1, p2);

            p1 = cameraPosition + new Vector3(-horizontalOrthoSize, verticalOrthoSize, 0.0f);
            Handles.DrawLine(p2, p1);

            // Show a green dotted rect in scene view that represents the area defined by the reference resolution.
            horizontalOrthoSize = obj.refResolutionX * 0.5f / obj.assetsPPU;
            verticalOrthoSize = obj.refResolutionY * 0.5f / obj.assetsPPU;

            p1 = cameraPosition + new Vector3(-horizontalOrthoSize, verticalOrthoSize, 0.0f);
            p2 = cameraPosition + new Vector3(horizontalOrthoSize, verticalOrthoSize, 0.0f);
            Handles.DrawDottedLine(p1, p2, k_DottedLineSpacing);

            p1 = cameraPosition + new Vector3(horizontalOrthoSize, -verticalOrthoSize, 0.0f);
            Handles.DrawDottedLine(p2, p1, k_DottedLineSpacing);

            p2 = cameraPosition + new Vector3(-horizontalOrthoSize, -verticalOrthoSize, 0.0f);
            Handles.DrawDottedLine(p1, p2, k_DottedLineSpacing);

            p1 = cameraPosition + new Vector3(-horizontalOrthoSize, verticalOrthoSize, 0.0f);
            Handles.DrawDottedLine(p2, p1, k_DottedLineSpacing);
        }
    }
}
