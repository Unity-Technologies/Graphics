using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedHDRenderPipelineAsset
    {
        public SerializedObject serializedObject;

        public SerializedProperty renderPipelineResources;
        public SerializedProperty diffusionProfileSettings;
        public SerializedProperty allowShaderVariantStripping;
        public SerializedRenderPipelineSettings renderPipelineSettings;
        public SerializedFrameSettings defaultFrameSettings;
        public SerializedFrameSettings defaultBakedOrCustomReflectionFrameSettings;
        public SerializedFrameSettings defaultRealtimeReflectionFrameSettings;
        //public SerializedCaptureSettings defaultCubeReflectionCaptureSettings;
        //public SerializedCaptureSettings defaultPlanarReflectionCaptureSettings;

        public SerializedHDRenderPipelineAsset(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            diffusionProfileSettings = serializedObject.Find((HDRenderPipelineAsset s) => s.diffusionProfileSettings);
            allowShaderVariantStripping = serializedObject.Find((HDRenderPipelineAsset s) => s.allowShaderVariantStripping);

            renderPipelineSettings = new SerializedRenderPipelineSettings(serializedObject.Find((HDRenderPipelineAsset a) => a.renderPipelineSettings));
            defaultFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_FrameSettings"));
            defaultBakedOrCustomReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_BakedOrCustomReflectionFrameSettings"));
            defaultRealtimeReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RealtimeReflectionFrameSettings"));
            //defaultCubeReflectionCaptureSettings = new SerializedCaptureSettings(serializedObject.FindProperty("m_CubeReflectionCaptureSettings"));
            //defaultPlanarReflectionCaptureSettings = new SerializedCaptureSettings(serializedObject.FindProperty("m_PlanarReflectionCaptureSettings"));
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
