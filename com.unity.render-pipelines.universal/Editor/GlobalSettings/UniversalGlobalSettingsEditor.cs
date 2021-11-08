using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(UniversalRenderPipelineGlobalSettings))]
    [CanEditMultipleObjects]
    sealed class UniversalGlobalSettingsEditor : RenderPipelineGlobalSettingsInspector
    {
        static class Styles
        {
            public const int labelWidth = 260;

            public static readonly GUIContent stripDebugVariantsLabel = EditorGUIUtility.TrTextContent("Strip Debug Variants", "When disabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of Rendering Debugger in Player builds.");
            public static readonly GUIContent stripUnusedPostProcessingVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Post Processing Variants", "Controls whether strips automatically post processing shader variants based on VolumeProfile components. It strips based on VolumeProfiles in project and not scenes that actually uses it.");
            public static readonly GUIContent stripUnusedVariantsLabel = EditorGUIUtility.TrTextContent("Strip Unused Variants", "Controls whether strip disabled keyword variants if the feature is enabled.");
        }

        SerializedUniversalRenderPipelineGlobalSettings m_SerializedGlobalSettings;

        public override SerializedRenderPipelineGlobalSettings serializedGlobalSettings => m_SerializedGlobalSettings;

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

        public override void OnShaderStrippingGUI()
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            base.OnShaderStrippingGUI();
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(m_SerializedGlobalSettings.stripDebugVariants, Styles.stripDebugVariantsLabel);
                EditorGUILayout.PropertyField(m_SerializedGlobalSettings.stripUnusedPostProcessingVariants, Styles.stripUnusedPostProcessingVariantsLabel);
                EditorGUILayout.PropertyField(m_SerializedGlobalSettings.stripUnusedVariants, Styles.stripUnusedVariantsLabel);
            }

            EditorGUIUtility.labelWidth = oldWidth;
        }
    }
}
