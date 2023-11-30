using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
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

        public SerializedProperty enableRenderCompatibilityMode;
        public SerializedProperty serializedShaderStrippingSettings;
        public SerializedProperty serializedURPShaderStrippingSettings;

        private void InitializeRenderPipelineGraphicsSettingsProperties(SerializedObject serializedObject)
        {
            var renderPipelineGraphicsSettingsContainerSerializedProperty = serializedObject.FindProperty("m_Settings.m_SettingsList");
            if (renderPipelineGraphicsSettingsContainerSerializedProperty == null)
                throw new Exception(
                    $"Unable to find m_Settings.m_SettingsList property on object from type {typeof(RenderPipelineGraphicsSettingsContainer)}");

            var serializedRenderPipelineGraphicsSettingsArray =
                renderPipelineGraphicsSettingsContainerSerializedProperty.FindPropertyRelative("m_List");

            for (int i = 0; i < serializedRenderPipelineGraphicsSettingsArray.arraySize; i++)
            {
                var currentElementProperty = serializedRenderPipelineGraphicsSettingsArray.GetArrayElementAtIndex(i);
                var type = currentElementProperty.boxedValue.GetType();

                if (type == typeof(ShaderStrippingSetting))
                {
                    serializedShaderStrippingSettings = currentElementProperty;
                    continue;
                }

                if (type == typeof(URPShaderStrippingSetting))
                {
                    serializedURPShaderStrippingSettings = currentElementProperty;
                    continue;
                }

                if (type == typeof(URPDefaultVolumeProfileSettings))
                {
                    defaultVolumeProfile = currentElementProperty.FindPropertyRelative("m_VolumeProfile");
                    continue;
                }

                if (type == typeof(RenderGraphSettings))
                {
                    enableRenderCompatibilityMode = currentElementProperty.FindPropertyRelative("m_EnableRenderCompatibilityMode");
                    continue;
                }
            }
        }

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

            renderingLayerNameList = new ReorderableList(serializedObject, renderingLayerNames, false, false, true, true)
            {
                drawElementCallback = OnDrawElement,
                onCanRemoveCallback = (ReorderableList list) => list.IsSelected(list.count - 1) && !list.IsSelected(0),
                onCanAddCallback = (ReorderableList list) => list.count < 32,
                onAddCallback = OnAddElement,
            };

            InitializeRenderPipelineGraphicsSettingsProperties(serializedObject);
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
