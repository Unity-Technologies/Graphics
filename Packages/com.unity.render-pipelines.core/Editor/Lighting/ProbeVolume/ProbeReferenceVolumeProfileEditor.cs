using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ProbeReferenceVolumeProfile))]
    internal class ProbeReferenceVolumeProfileEditor : Editor
    {
        SerializedProperty m_MinDistanceBetweenProbes;
        SerializedProperty m_SimplificationLevels;
        SerializedProperty m_MinRendererVolumeSize;
        SerializedProperty m_RenderersLayerMask;
        SerializedProperty m_FreezePlacement;
        ProbeReferenceVolumeProfile profile => target as ProbeReferenceVolumeProfile;

        static class Styles
        {
            // TODO: Better tooltip are needed here.
            public static readonly GUIContent maxDistanceBetweenProbes = new GUIContent("Max Distance Between Probes", "The maximal distance between two probes in meters. Determines how many bricks are in a streamable unit.");
            public static readonly GUIContent minDistanceBetweenProbes = new GUIContent("Min Distance Between Probes", "The minimal distance between two probes in meters.");
            public static readonly string simplificationLevelsHighWarning = "High simplification levels have a big memory overhead, they are not recommended except for testing purposes.";
            public static readonly GUIContent indexDimensions = new GUIContent("Index Dimensions", "The dimensions of the index buffer.");
            public static readonly GUIContent minRendererVolumeSize = new GUIContent("Min Renderer Volume Size", "Specifies the minimum bounding box volume of renderers to consider placing probes around.");
            public static readonly GUIContent renderersLayerMask = new GUIContent("Layer Mask", "Specifies the layer mask for renderers when placing probes.");
            public static readonly GUIContent rendererFilterSettings = new GUIContent("Renderers Filter Settings");
            public static readonly GUIContent keepSamePlacement = new GUIContent("Freeze Placement", "If the option is set, the placement is derived from already baked data even if geometry is changed. This can be used to bake compatible scenarios with minor changes in geometry in the scene.");

            public static readonly GUIStyle labelFont = new GUIStyle(EditorStyles.label);
        }

        void OnEnable()
        {
            m_MinDistanceBetweenProbes = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.minDistanceBetweenProbes));
            m_SimplificationLevels = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.simplificationLevels));
            m_MinRendererVolumeSize = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.minRendererVolumeSize));
            m_RenderersLayerMask = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.renderersLayerMask));
            m_FreezePlacement = serializedObject.FindProperty(nameof(ProbeReferenceVolumeProfile.freezePlacement));
        }

        internal const int s_HighestSimplification = 5;

        private static Color k_DarkThemeColor = new Color32(153, 153, 153, 255);
        private static Color k_LiteThemeColor = new Color32(97, 97, 97, 255);
        static Color GetMarkerColor() => EditorGUIUtility.isProSkin ? k_DarkThemeColor : k_LiteThemeColor;

        internal static void DrawSimplificationLevelsMarkers(Rect rect, float minDistanceBetweenProbes, int lowestSimplification, int highestSimplification, int hideStart, int hideEnd)
        {
            var markerRect = new Rect(rect) { width = 2, height = 2 };
            markerRect.y += (EditorGUIUtility.singleLineHeight / 2f) - 1;
            float indent = 15 * EditorGUI.indentLevel;

            for (int i = lowestSimplification; i <= highestSimplification; i++)
            {
                float position = (float)(i - lowestSimplification) / (highestSimplification - lowestSimplification);

                float knobSize = (i == lowestSimplification || i == highestSimplification) ? 0 : 10;
                float start = rect.x + knobSize / 2f;
                float range = rect.width - knobSize;
                markerRect.x = start + range * position - 0.5f * markerRect.width;

                float min = rect.x;
                float max = (rect.x + rect.width) - markerRect.width;
                markerRect.x = Mathf.Clamp(markerRect.x, min, max);

                float maxTextWidth = 200;
                string text = (minDistanceBetweenProbes * ProbeReferenceVolume.CellSize(i)) + "m";
                float textX = markerRect.x + 1 - (maxTextWidth + indent) * 0.5f;
                Styles.labelFont.alignment = TextAnchor.UpperCenter;
                if (i == highestSimplification)
                {
                    textX = rect.xMax - maxTextWidth;
                    Styles.labelFont.alignment = TextAnchor.UpperRight;
                }
                else if (i == lowestSimplification)
                {
                    textX = markerRect.x - indent;
                    Styles.labelFont.alignment = TextAnchor.UpperLeft;
                }

                var label = new Rect(rect) { x = textX, width = maxTextWidth, y = rect.y - 15 };
                EditorGUI.LabelField(label, text, Styles.labelFont);
                if (i < hideStart || i > hideEnd)
                    EditorGUI.DrawRect(markerRect, GetMarkerColor());
            }
        }

        static int s_SimplificationSliderID = "SimplificationLevelSlider".GetHashCode();

        void SimplificationLevelsSlider()
        {
            var rect = EditorGUILayout.GetControlRect();
            int id = GUIUtility.GetControlID(s_SimplificationSliderID, FocusType.Keyboard, rect);
            rect = EditorGUI.PrefixLabel(rect, id, Styles.maxDistanceBetweenProbes);

            int value = m_SimplificationLevels.intValue;
            EditorGUI.BeginChangeCheck();
            value = Mathf.RoundToInt(GUI.Slider(rect, value, 0, 2, s_HighestSimplification, GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb, true, id, "horizontalsliderthumbextent"));
            if (GUIUtility.hotControl == id)
               GUIUtility.keyboardControl = id;
            if (EditorGUI.EndChangeCheck())
                m_SimplificationLevels.intValue = value;

            DrawSimplificationLevelsMarkers(rect, profile.minDistanceBetweenProbes, 2, s_HighestSimplification, value, value);
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            // Check if all the scene datas in the scene have an asset, if  not then we cannot enable this option.
            bool canFreezePlacement = ProbeGIBaking.CanFreezePlacement();

            using (new EditorGUI.DisabledGroupScope(!canFreezePlacement))
            {
                EditorGUILayout.PropertyField(m_FreezePlacement, Styles.keepSamePlacement);
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();

            ProbeGIBaking.isFreezingPlacement = canFreezePlacement && m_FreezePlacement.boolValue;

            using (new EditorGUI.DisabledGroupScope(ProbeGIBaking.isFreezingPlacement))
            {
                SimplificationLevelsSlider();
                EditorGUILayout.PropertyField(m_MinDistanceBetweenProbes, Styles.minDistanceBetweenProbes);

                int levels = m_SimplificationLevels.intValue;
                MessageType helpBoxType = MessageType.Info;
                string helpBoxText = $"Scene will contain at most {levels} simplification levels.";
                if (levels == 5)
                {
                    helpBoxType = MessageType.Warning;
                    helpBoxText += "\n" + Styles.simplificationLevelsHighWarning;
                }
                EditorGUILayout.HelpBox(helpBoxText, helpBoxType);

                EditorGUILayout.Space();
                if (ProbeVolumeBakingWindow.Foldout(Styles.rendererFilterSettings, ProbeVolumeBakingWindow.Expandable.RendererFilterSettings))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_RenderersLayerMask, Styles.renderersLayerMask);
                    EditorGUILayout.PropertyField(m_MinRendererVolumeSize, Styles.minRendererVolumeSize);
                    EditorGUI.indentLevel--;
                }
            }

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }
    }
}
