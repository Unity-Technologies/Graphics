using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering
{
    class SerializedRenderPipelineSettings
    {
        public SerializedProperty root;

        public SerializedProperty supportShadowMask;
        public SerializedProperty supportSSR;
        public SerializedProperty supportSSAO;
        public SerializedProperty supportDBuffer;
        public SerializedProperty supportMSAA;
        public SerializedProperty supportSubsurfaceScattering;
        public SerializedProperty supportAsyncCompute;
        public SerializedProperty supportsForwardOnly;
        public SerializedProperty supportsMotionVectors;
        public SerializedProperty supportsStereo;

        public SerializedGlobalLightLoopSettings lightLoopSettings;
        public SerializedShadowInitParameters shadowInitParams;

        public SerializedRenderPipelineSettings(SerializedProperty root)
        {
            this.root = root;

            supportShadowMask = root.Find((RenderPipelineSettings s) => s.supportShadowMask);
            supportSSR = root.Find((RenderPipelineSettings s) => s.supportSSR);
            supportSSAO = root.Find((RenderPipelineSettings s) => s.supportSSAO);
            supportDBuffer = root.Find((RenderPipelineSettings s) => s.supportDBuffer);
            supportMSAA = root.Find((RenderPipelineSettings s) => s.supportMSAA);
            supportSubsurfaceScattering = root.Find((RenderPipelineSettings s) => s.supportSubsurfaceScattering);
            supportAsyncCompute = root.Find((RenderPipelineSettings s) => s.supportAsyncCompute);
            supportsForwardOnly = root.Find((RenderPipelineSettings s) => s.supportsForwardOnly);
            supportsMotionVectors = root.Find((RenderPipelineSettings s) => s.supportsMotionVectors);
            supportsStereo = root.Find((RenderPipelineSettings s) => s.supportsStereo);

            lightLoopSettings = new SerializedGlobalLightLoopSettings(root.Find((RenderPipelineSettings s) => s.lightLoopSettings));
            shadowInitParams = new SerializedShadowInitParameters(root.Find((RenderPipelineSettings s) => s.shadowInitParams));
        }
    }
}
