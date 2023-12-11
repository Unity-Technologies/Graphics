using UnityEngine;

namespace UnityEditor.Rendering
{
    internal class SettingsProviderGUIScope : GUI.Scope
    {
        float m_LabelWidth;
        public SettingsProviderGUIScope(int offset = 10)
        {
            m_LabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 251;
            GUILayout.BeginHorizontal();
            GUILayout.Space(offset);
            GUILayout.BeginVertical();
        }

        protected override void CloseScope()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = m_LabelWidth;
        }
    }
}
