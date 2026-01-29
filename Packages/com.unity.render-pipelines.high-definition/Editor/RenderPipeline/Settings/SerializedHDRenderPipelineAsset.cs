using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDRenderPipelineAsset
    {
        public SerializedObject serializedObject;

        public SerializedProperty defaultMaterialQualityLevel;
        public SerializedProperty volumeProfile;
        public SerializedProperty availableMaterialQualityLevels;
        public SerializedProperty allowShaderVariantStripping;
        public SerializedProperty enableSRPBatcher;
        public SerializedRenderPipelineSettings renderPipelineSettings;
        public SerializedVirtualTexturingSettings virtualTexturingSettings;


        public SerializedHDRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;
            HDRenderPipelineAsset asset = serializedObject.targetObject as HDRenderPipelineAsset;

            defaultMaterialQualityLevel = serializedObject.FindProperty("m_DefaultMaterialQualityLevel");
            volumeProfile = serializedObject.FindProperty("m_VolumeProfile");
            availableMaterialQualityLevels = serializedObject.Find((HDRenderPipelineAsset s) => s.availableMaterialQualityLevels);
            allowShaderVariantStripping = serializedObject.Find((HDRenderPipelineAsset s) => s.allowShaderVariantStripping);
            enableSRPBatcher = serializedObject.Find((HDRenderPipelineAsset s) => s.enableSRPBatcher);

            renderPipelineSettings = new SerializedRenderPipelineSettings(serializedObject.FindProperty("m_RenderPipelineSettings"));

            virtualTexturingSettings = new SerializedVirtualTexturingSettings(serializedObject.FindProperty("virtualTexturingSettings"));

#if ENABLE_UPSCALER_FRAMEWORK
            // HDRenderPipelineAsset/RenderPipelineSettings/GlobalDynamicResolutionScaling struct contains the
            // UpscalerOptions collection (polymorphic data) tagged with [SerializeReference].
            // Here we ensure the ScriptableObject references (concrete UpscalerOptions) are in a valid state,
            // and initialize with defaults if they're not within the serialized asset.
            SerializedProperty dynamicResolutionSettingsProp = renderPipelineSettings.root.FindPropertyRelative("dynamicResolutionSettings");
            if (dynamicResolutionSettingsProp == null)
            {
                UnityEngine.Debug.LogError($"[HDRP Serialized Asset] Could not find 'dynamicResolutionSettings' property in m_RenderPipelineSettings for {asset.name}.");
                return;
            }
            SerializedProperty UpscalerOptionBaseProp = dynamicResolutionSettingsProp.FindPropertyRelative("upscalerOptions");
            if (UpscalerOptionBaseProp == null)
            {
                UnityEngine.Debug.LogError($"[HDRP Serialized Asset] Could not find 'UpscalerOptions' property in DynamicResolutionSettings for {asset.name}.");
                return;
            }
            if (UpscalerOptions.ValidateSerializedUpscalerOptionReferencesWithinRPAsset(asset, UpscalerOptionBaseProp))
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);

                UnityEngine.Debug.Log($"[HDRP Serialized Asset] HDRenderPipelineAsset '{asset.name}' auto-populated and saved on SerializedObject creation.");
            }
#endif
        }

        public void Update()
        {
            serializedObject.Update();
        }

        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
