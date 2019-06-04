using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LWRP;

namespace UnityEditor.Experimental.Rendering.LWRP
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
            public GUIContent upscaleRT = new GUIContent("Upscale Render Texture", "If enabled, the Scene is rendered as close as possible to the Reference Resolution while maintaining the screen aspect ratio, then upscaled to fit the full screen.");
            public GUIContent pixelSnapping = new GUIContent("Pixel Snapping", "If enabled, Sprite Renderers are snapped to a grid in world space at render-time. Grid size is based on the Assets Pixels Per Unit value. This does not affect GameObjects' Transform positions.");
            public GUIContent cropFrame = new GUIContent("Crop Frame", "Crops the viewport to match the Reference Resolution, along the checked axis. Black bars will be added to fit the screen aspect ratio.");
            public GUIContent stretchFill = new GUIContent("Stretch Fill", "If enabled, expands the viewport to fit the screen resolution while maintaining the viewport aspect ratio.");
            public GUIContent currentPixelRatio = new GUIContent("Current Pixel Ratio", "Ratio of the rendered Sprites compared to their original size.");
            public GUIContent runInEditMode = new GUIContent("Run In Edit Mode", "Enable this to preview Camera setting changes in Edit Mode. This will cause constant changes to the Scene while active.");

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
        private SerializedProperty m_UpscaleRT;
        private SerializedProperty m_PixelSnapping;
        private SerializedProperty m_CropFrameY;
        private SerializedProperty m_CropFrameX;
        private SerializedProperty m_StretchFill;

        private Vector2 m_GameViewSize = Vector2.zero;
        private GUIContent m_CurrentPixelRatioValue;

        private void LazyInit()
        {
            if (m_Style == null)
                m_Style = new Style();

            if (m_CurrentPixelRatioValue == null)
                m_CurrentPixelRatioValue = new GUIContent();
        }

        public void OnEnable()
        {
            m_AssetsPPU = serializedObject.FindProperty("m_AssetsPPU");
            m_RefResX = serializedObject.FindProperty("m_RefResolutionX");
            m_RefResY = serializedObject.FindProperty("m_RefResolutionY");
            m_UpscaleRT = serializedObject.FindProperty("m_UpscaleRT");
            m_PixelSnapping = serializedObject.FindProperty("m_PixelSnapping");
            m_CropFrameY = serializedObject.FindProperty("m_CropFrameY");
            m_CropFrameX = serializedObject.FindProperty("m_CropFrameX");
            m_StretchFill = serializedObject.FindProperty("m_StretchFill");
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

            float originalLabelWidth = EditorGUIUtility.labelWidth;

            serializedObject.Update();

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

            EditorGUILayout.PropertyField(m_UpscaleRT, m_Style.upscaleRT);
            if (!m_UpscaleRT.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PixelSnapping, m_Style.pixelSnapping);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.PrefixLabel(m_Style.cropFrame);

                EditorGUIUtility.labelWidth = k_SingleLetterLabelWidth * (EditorGUI.indentLevel + 1);
                EditorGUILayout.PropertyField(m_CropFrameX, m_Style.x, GUILayout.MaxWidth(40.0f));
                EditorGUILayout.PropertyField(m_CropFrameY, m_Style.y);
                EditorGUIUtility.labelWidth = originalLabelWidth;
            }
            EditorGUILayout.EndHorizontal();

            if (m_CropFrameY.boolValue && m_CropFrameX.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_StretchFill, m_Style.stretchFill);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();

            PixelPerfectCamera obj = target as PixelPerfectCamera;

            if (obj != null)
            {
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || !obj.isActiveAndEnabled);
                EditorGUI.BeginChangeCheck();

                bool runInEditMode = EditorGUILayout.Toggle(obj.runInEditMode, GUI.skin.button, GUILayout.Width(110.0f));
                GUI.Label(GUILayoutUtility.GetLastRect(), m_Style.runInEditMode, m_Style.centeredLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    obj.runInEditMode = runInEditMode;

                    if (runInEditMode)
                        obj.GetComponent<Camera>().Render();
                    else
                        obj.OnDisable();
                }

                EditorGUI.EndDisabledGroup();

                if (obj.isActiveAndEnabled && (EditorApplication.isPlaying || obj.runInEditMode))
                {
                    if (Event.current.type == EventType.Layout)
                        m_CurrentPixelRatioValue.text = String.Format("{0}:1", obj.pixelRatio);

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

            if (obj.cropFrameY && obj.cropFrameX)
            {
                verticalOrthoSize = obj.refResolutionY * 0.5f / obj.assetsPPU;
                horizontalOrthoSize = verticalOrthoSize * ((float)obj.refResolutionX / obj.refResolutionY);
            }
            else if (obj.cropFrameY)
            {
                verticalOrthoSize = obj.refResolutionY * 0.5f / obj.assetsPPU;
                horizontalOrthoSize = verticalOrthoSize * ((float)gameViewWidth / (zoom * obj.refResolutionY));
            }
            else if (obj.cropFrameX)
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
