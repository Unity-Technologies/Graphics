using System; //Type
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditorInternal; //ReorderableList
using UnityEngine; //ScriptableObject
using UnityEngine.Rendering; //CoreUtils.Destroy
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDRenderPipelineGlobalSettings
    {
        public SerializedObject serializedObject;

        public SerializedProperty renderPipelineResources;
        public SerializedProperty renderPipelineRayTracingResources;

        public SerializedProperty defaultVolumeProfile;
        public SerializedProperty lookDevVolumeProfile;

        public SerializedProperty defaultRenderingLayerMask;
        public SerializedProperty renderingLayerNames;
        internal ReorderableList renderingLayerNamesList;

        public SerializedProperty serializedShaderStrippingSettings;
        public SerializedProperty serializedRenderingPathProperty;
        public SerializedProperty serializedCustomPostProcessOrdersSettings;

        public class MiscSettingsItem
        {
            public SerializedProperty serializedProperty;
            public string displayName;
            public string tooltip;
        }

        public List<MiscSettingsItem> miscSectionSerializedProperties = new();

        private List<HDRenderPipelineGlobalSettings> serializedSettings = new List<HDRenderPipelineGlobalSettings>();

        public SerializedHDRenderPipelineGlobalSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            // do the cast only once
            foreach (var currentSetting in serializedObject.targetObjects)
            {
                if (currentSetting is HDRenderPipelineGlobalSettings hdrpSettings)
                    serializedSettings.Add(hdrpSettings);
                else
                    throw new Exception($"Target object has an invalid object, objects must be of type {typeof(HDRenderPipelineGlobalSettings)}");
            }

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            renderPipelineRayTracingResources = serializedObject.FindProperty("m_RenderPipelineRayTracingResources");

            InitializeRenderPipelineGraphicsSettingsProperties(serializedObject);
        }

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

                // skip resources
                if (typeof(IRenderPipelineResources).IsAssignableFrom(type))
                    continue;

                if (type == typeof(CustomPostProcessOrdersSettings))
                {
                    serializedCustomPostProcessOrdersSettings = currentElementProperty;
                    continue;
                }

                if (type == typeof(ShaderStrippingSetting))
                {
                    serializedShaderStrippingSettings = currentElementProperty;
                    continue;
                }

                if (type == typeof(RenderingPathFrameSettings))
                {
                    serializedRenderingPathProperty = currentElementProperty;
                    continue;
                }

                if (type == typeof(HDRPDefaultVolumeProfileSettings))
                {
                    defaultVolumeProfile = currentElementProperty.FindPropertyRelative("m_VolumeProfile");
                    continue;
                }

                if (type == typeof(LookDevVolumeProfileSettings))
                {
                    lookDevVolumeProfile = currentElementProperty.FindPropertyRelative("m_VolumeProfile");
                    continue;
                }

                // Add everything else to misc section
                void AddMiscProperty(string propertyName)
                {
                    FieldInfo fieldInfo = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    InspectorNameAttribute inspectorNameAttribute = fieldInfo.GetCustomAttribute<InspectorNameAttribute>();
                    string displayName = inspectorNameAttribute?.displayName;
                    TooltipAttribute tooltipAttribute = fieldInfo.GetCustomAttribute<TooltipAttribute>();
                    string tooltip = tooltipAttribute?.tooltip;
                    miscSectionSerializedProperties.Add(new MiscSettingsItem
                    {
                        serializedProperty = currentElementProperty.FindPropertyRelative(propertyName),
                        displayName = displayName,
                        tooltip = tooltip
                    });
                }

                if (type == typeof(LensSettings))
                    AddMiscProperty("m_LensAttenuationMode");
                else if (type == typeof(ColorGradingSettings))
                    AddMiscProperty("m_ColorGradingSpace");
                else if (type == typeof(RenderGraphSettings))
                    AddMiscProperty("m_DynamicRenderPassCulling");
                else if (type == typeof(SpecularFadeSettings))
                    AddMiscProperty("m_SpecularFade");
                else if (type == typeof(DiffusionProfileDefaultSettings))
                    AddMiscProperty("m_AutoRegisterDiffusionProfiles");
                else if (type == typeof(AnalyticDerivativeSettings))
                {
                    AddMiscProperty("m_AnalyticDerivativeEmulation");
                    AddMiscProperty("m_AnalyticDerivativeDebugOutput");
                }
            }
        }
    }
}
