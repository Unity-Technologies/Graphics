using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    class SerializedHDRenderPipelineAsset
    {
        public SerializedObject serializedObject;
        
        public SerializedProperty renderPipelineResources;
        public SerializedProperty diffusionProfileSettings;

        public SerializedRenderPipelineSettings renderPipelineSettings;
        public SerializedFrameSettings defaultFrameSettings;

        public SerializedHDRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            diffusionProfileSettings = serializedObject.Find((HDRenderPipelineAsset s) => s.diffusionProfileSettings);

            renderPipelineSettings = new SerializedRenderPipelineSettings(serializedObject.Find((HDRenderPipelineAsset a) => a.renderPipelineSettings));
            defaultFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_FrameSettings"));
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
