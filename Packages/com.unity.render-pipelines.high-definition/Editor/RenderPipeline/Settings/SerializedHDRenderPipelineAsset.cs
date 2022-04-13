using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDRenderPipelineAsset
    {
        public SerializedObject serializedObject;

        public SerializedProperty defaultMaterialQualityLevel;
        public SerializedProperty availableMaterialQualityLevels;
        public SerializedProperty allowShaderVariantStripping;
        public SerializedProperty enableSRPBatcher;
        public SerializedRenderPipelineSettings renderPipelineSettings;
        public SerializedVirtualTexturingSettings virtualTexturingSettings;


        public SerializedHDRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            defaultMaterialQualityLevel = serializedObject.FindProperty("m_DefaultMaterialQualityLevel");
            availableMaterialQualityLevels = serializedObject.Find((HDRenderPipelineAsset s) => s.availableMaterialQualityLevels);
            allowShaderVariantStripping = serializedObject.Find((HDRenderPipelineAsset s) => s.allowShaderVariantStripping);
            enableSRPBatcher = serializedObject.Find((HDRenderPipelineAsset s) => s.enableSRPBatcher);

            renderPipelineSettings = new SerializedRenderPipelineSettings(serializedObject.FindProperty("m_RenderPipelineSettings"));

            virtualTexturingSettings = new SerializedVirtualTexturingSettings(serializedObject.FindProperty("virtualTexturingSettings"));
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
