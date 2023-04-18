using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomPropertyDrawer(typeof(FrameSettings))]
    class FrameSettingsPropertyDrawer : PropertyDrawer
    {
        static class Styles
        {
            public const int labelWidth = 220;

            public static readonly GUIContent[] headerContents = new GUIContent[]
            {
                FrameSettingsUI.renderingSettingsHeaderContent,
                FrameSettingsUI.lightSettingsHeaderContent,
                FrameSettingsUI.asyncComputeSettingsHeaderContent,
                FrameSettingsUI.lightLoopSettingsHeaderContent
            };
        }

        private float m_TotalHeight = 0f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            SerializedFrameSettings serializedFrameSettings = null;

            EditorGUI.BeginProperty(position, label, property);

            var height = CoreEditorStyles.subSectionHeaderStyle.CalcHeight(label, position.width);
            Rect rect = new Rect(position.x, position.y, position.width, height);

            EditorGUI.LabelField(rect, label, CoreEditorStyles.subSectionHeaderStyle);

            rect.y += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < Styles.headerContents.Length; ++i)
            {
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

                CoreEditorUtils.DrawSplitter(new Rect(rect.x, rect.y, position.width, 1f));
                
                string key = $"UI_State_{nameof(FrameSettingsPropertyDrawer)}_{property.displayName}_{i}";

                var isExpanded = EditorPrefs.GetBool(key);
                isExpanded = CoreEditorUtils.DrawHeaderFoldout(rect, Styles.headerContents[i], isExpanded);

                if (isExpanded != EditorPrefs.GetBool(key))
                    EditorPrefs.SetBool(key, isExpanded);

                if (isExpanded)
                {
                    serializedFrameSettings ??= new SerializedFrameSettings(property, null);
                    rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;

                    using (new EditorGUI.IndentLevelScope())
                    {
                        bool oldEnabled = GUI.enabled;
                        var area = FrameSettingsUI.GetFrameSettingsArea(i, serializedFrameSettings, null, null);
                        area.Draw(ref rect);
                        GUI.enabled = oldEnabled;
                    }
                }
            }

            CoreEditorUtils.DrawSplitter(new Rect(rect.x, rect.y + rect.height, position.width, 1f));

            m_TotalHeight = (rect.position.y + rect.height) - position.y + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.EndProperty();

            EditorGUIUtility.labelWidth = oldWidth;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return m_TotalHeight; 
        }
    }
}
