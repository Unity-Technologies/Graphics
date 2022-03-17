using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniversalRenderPipelineGlobalSettings))]
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
            UniversalRenderPipelineGlobalSettingsUI.Inspector.Draw(serialized, this);

            serialized.serializedObject.ApplyModifiedProperties();
        }
    }
}
