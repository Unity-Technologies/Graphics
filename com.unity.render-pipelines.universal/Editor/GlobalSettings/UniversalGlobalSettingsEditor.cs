using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UniversalRenderPipelineGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class UniversalGlobalSettingsEditor : Editor
    {
        SerializedUniversalRenderPipelineGlobalSettings m_SerializedGlobalSettings;

        void OnEnable()
        {
            m_SerializedGlobalSettings = new SerializedUniversalRenderPipelineGlobalSettings(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var serialized = m_SerializedGlobalSettings;

            serialized.serializedObject.Update();

            // In the quality window use more space for the labels
            UniversalGlobalSettingsPanelIMGUI.Inspector.Draw(serialized, this);

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}
