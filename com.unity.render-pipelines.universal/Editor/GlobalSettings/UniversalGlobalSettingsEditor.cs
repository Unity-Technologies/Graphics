using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UniversalGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class UniversalGlobalSettingsEditor : Editor
    {
        SerializedUniversalGlobalSettings m_SerializedGlobalSettings;

        internal bool largeLabelWidth = true;

        void OnEnable()
        {
            m_SerializedGlobalSettings = new SerializedUniversalGlobalSettings(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedGlobalSettings;

            serialized.serializedObject.Update();

            // In the quality window use more space for the labels
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 2;
            DefaultSettingsPanelIMGUI.Inspector.Draw(serialized, this);
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 0.5f;

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}
