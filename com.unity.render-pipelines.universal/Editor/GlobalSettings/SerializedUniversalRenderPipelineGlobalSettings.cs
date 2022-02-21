using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

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

        public SerializedProperty transparencySortMode { get; }
        public SerializedProperty transparencySortAxis { get; }
        public SerializedProperty defaultSpriteMaterialType { get; }
        public SerializedProperty defaultSpriteCustomMaterial { get; }

        public SerializedProperty supportsTerrainHolesProp { get; }

        public SerializedProperty lightLayerName0;
        public SerializedProperty lightLayerName1;
        public SerializedProperty lightLayerName2;
        public SerializedProperty lightLayerName3;
        public SerializedProperty lightLayerName4;
        public SerializedProperty lightLayerName5;
        public SerializedProperty lightLayerName6;
        public SerializedProperty lightLayerName7;

        public SerializedProperty stripDebugVariants;
        public SerializedProperty stripUnusedPostProcessingVariants;
        public SerializedProperty stripUnusedVariants;

        public SerializedProperty storeActionsOptimizationProperty { get; }

        public SerializedProperty useNativeRenderPass { get; }

        public SerializedProperty volumeFrameworkUpdateModeProp { get; }


        public SerializedProperty postProcessData { get; }
        public SerializedProperty colorGradingMode { get; }
        public SerializedProperty colorGradingLutSize { get; }
        public SerializedProperty useFastSRGBLinearConversion { get; }

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

            transparencySortMode = serializedObject.FindProperty("m_TransparencySortMode");
            transparencySortAxis = serializedObject.FindProperty("m_TransparencySortAxis");
            defaultSpriteMaterialType = serializedObject.FindProperty("m_DefaultSpriteMaterialType");
            defaultSpriteCustomMaterial = serializedObject.FindProperty("m_DefaultSpriteCustomMaterial");

            supportsTerrainHolesProp = serializedObject.FindProperty("m_SupportsTerrainHoles");

            lightLayerName0 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName0);
            lightLayerName1 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName1);
            lightLayerName2 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName2);
            lightLayerName3 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName3);
            lightLayerName4 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName4);
            lightLayerName5 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName5);
            lightLayerName6 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName6);
            lightLayerName7 = serializedObject.Find((UniversalRenderPipelineGlobalSettings s) => s.lightLayerName7);

            stripDebugVariants = serializedObject.FindProperty("m_StripDebugVariants");
            stripUnusedPostProcessingVariants = serializedObject.FindProperty("m_StripUnusedPostProcessingVariants");
            stripUnusedVariants = serializedObject.FindProperty("m_StripUnusedVariants");

            shaderVariantLogLevel = serializedObject.FindProperty("m_ShaderVariantLogLevel");
            exportShaderVariants = serializedObject.FindProperty("m_ExportShaderVariants");
            volumeFrameworkUpdateModeProp = serializedObject.FindProperty("m_VolumeFrameworkUpdateMode");

            storeActionsOptimizationProperty = serializedObject.FindProperty("m_StoreActionsOptimization");
            useNativeRenderPass = serializedObject.FindProperty("m_UseNativeRenderPass");

            postProcessData = serializedObject.FindProperty("postProcessData");
            colorGradingMode = serializedObject.FindProperty("m_ColorGradingMode");
            colorGradingLutSize = serializedObject.FindProperty("m_ColorGradingLutSize");

            useFastSRGBLinearConversion = serializedObject.FindProperty("m_UseFastSRGBLinearConversion");
        }
    }
}
