using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDDefaultSettings))]
    [CanEditMultipleObjects]
    sealed class HDDefaultSettingsEditor : Editor
    {
        SerializedHDDefaultSettings m_SerializedHDDefaultSettings;

        internal bool largeLabelWidth = true;

        void OnEnable()
        {
            m_SerializedHDDefaultSettings = new SerializedHDDefaultSettings(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedHDDefaultSettings;

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
