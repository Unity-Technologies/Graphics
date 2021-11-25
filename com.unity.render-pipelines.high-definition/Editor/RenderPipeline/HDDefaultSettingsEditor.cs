using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    [CustomEditor(typeof(HDRenderPipelineGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class HDRenderPipelineGlobalSettingsEditor : Editor
    {
        SerializedHDRenderPipelineGlobalSettings m_SerializedHDRenderPipelineGlobalSettings;

        internal bool largeLabelWidth = true;

        void OnEnable()
        {
            m_SerializedHDRenderPipelineGlobalSettings = new SerializedHDRenderPipelineGlobalSettings(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedHDRenderPipelineGlobalSettings;

            serialized.serializedObject.Update();

            // In the quality window use more space for the labels
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 2;
            HDGlobalSettingsPanelIMGUI.Inspector.Draw(serialized, this);
            if (!largeLabelWidth)
                EditorGUIUtility.labelWidth *= 0.5f;

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}
