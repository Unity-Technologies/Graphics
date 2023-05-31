using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class SerializedUniversalRenderPipelineGlobalSettings
    {
        public SerializedObject serializedObject;

        private List<UniversalRenderPipelineGlobalSettings> serializedSettings = new List<UniversalRenderPipelineGlobalSettings>();

        public SerializedProperty defaultVolumeProfile;

        public SerializedProperty renderingLayerNames;
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

            defaultVolumeProfile = serializedObject.FindProperty("m_DefaultVolumeProfile");

            renderingLayerNames = serializedObject.FindProperty("m_RenderingLayerNames");

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
