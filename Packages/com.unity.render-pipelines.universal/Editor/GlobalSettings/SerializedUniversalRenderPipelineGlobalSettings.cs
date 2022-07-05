using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditorInternal;

namespace UnityEditor.Rendering.Universal
{
    class SerializedUniversalRenderPipelineGlobalSettings : ISerializedRenderPipelineGlobalSettings
    {
        #region ISerializedRenderPipelineGlobalSettings
        public SerializedObject serializedObject { get; }
        public SerializedProperty shaderVariantLogLevel { get; }
        public SerializedProperty exportShaderVariants { get; }
        #endregion

        private List<UniversalRenderPipelineGlobalSettings> serializedSettings = new List<UniversalRenderPipelineGlobalSettings>();

        public SerializedProperty renderingLayerNames;

        public SerializedProperty stripDebugVariants;
        public SerializedProperty stripUnusedPostProcessingVariants;
        public SerializedProperty stripUnusedVariants;
        public SerializedProperty stripUnusedLODCrossFadeVariants;
        public SerializedProperty stripScreenCoordOverrideVariants;

        public ReorderableList renderingLayerNameList;

        public SerializedUniversalRenderPipelineGlobalSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            // do the cast only once
            foreach (var currentSetting in serializedObject.targetObjects)
            {
                if (currentSetting is UniversalRenderPipelineGlobalSettings urpSettings)
                    serializedSettings.Add(urpSettings);
                else
                    throw new System.Exception($"Target object has an invalid object, objects must be of type {typeof(UniversalRenderPipelineGlobalSettings)}");
            }

            renderingLayerNames = serializedObject.FindProperty("m_RenderingLayerNames");

            stripDebugVariants = serializedObject.FindProperty("m_StripDebugVariants");
            stripUnusedPostProcessingVariants = serializedObject.FindProperty("m_StripUnusedPostProcessingVariants");
            stripUnusedVariants = serializedObject.FindProperty("m_StripUnusedVariants");
            stripUnusedLODCrossFadeVariants = serializedObject.FindProperty("m_StripUnusedLODCrossFadeVariants");
            shaderVariantLogLevel = serializedObject.FindProperty("m_ShaderVariantLogLevel");
            exportShaderVariants = serializedObject.FindProperty("m_ExportShaderVariants");
            stripScreenCoordOverrideVariants = serializedObject.FindProperty("m_StripScreenCoordOverrideVariants");

            renderingLayerNameList = new ReorderableList(serializedObject, renderingLayerNames, false, false, true, true)
            {
                drawElementCallback = OnDrawElement,
                onCanRemoveCallback = (ReorderableList list) => list.IsSelected(list.count - 1) && !list.IsSelected(0),
                onCanAddCallback = (ReorderableList list) => list.count < 32,
                onAddCallback = OnAddElement,
            };
        }

        void OnAddElement(ReorderableList list)
        {
            int index = list.count;
            list.serializedProperty.arraySize = list.count + 1;
            list.serializedProperty.GetArrayElementAtIndex(index).stringValue = GetDefaultLayerName(index);
        }

        void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            // For some reason given rect is not centered
            rect.y += 2.5f;

            SerializedProperty element = renderingLayerNameList.serializedProperty.GetArrayElementAtIndex(index);

            EditorGUI.PropertyField(rect, element, EditorGUIUtility.TrTextContent($"Layer {index}"), true);

            if (element.stringValue == "")
            {
                element.stringValue = GetDefaultLayerName(index);
                serializedObject.ApplyModifiedProperties();
            }
        }

        string GetDefaultLayerName(int index)
        {
            return index == 0 ? "Default" : $"Layer {index}";
        }
    }
}
