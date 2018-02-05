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
        public SerializedProperty supportMSAAAntiAliasing;
        public SerializedProperty MSAASampleCount;
        public SerializedProperty supportSubsurfaceScattering;
        public SerializedProperty supportsForwardOnly;
        public SerializedProperty supportsMotionVectors;
        public SerializedProperty supportsStereo;

        public SerializedGlobalLightLoopSettings lightLoopSettings;
        public SerializedShadowInitParameters shadowInitParams;
		public SerializedGlobalDecalSettings decalSettings;

        public SerializedRenderPipelineSettings(SerializedProperty root)
        {
            this.root = root;

            supportShadowMask = root.Find((RenderPipelineSettings s) => s.supportShadowMask);
            supportSSR = root.Find((RenderPipelineSettings s) => s.supportSSR);
            supportSSAO = root.Find((RenderPipelineSettings s) => s.supportSSAO);
            supportDBuffer = root.Find((RenderPipelineSettings s) => s.supportDBuffer);
            supportMSAAAntiAliasing = root.Find((RenderPipelineSettings s) => s.supportMSAAAntiAliasing);
            MSAASampleCount = root.Find((RenderPipelineSettings s) => s.msaaSampleCount);
            supportSubsurfaceScattering = root.Find((RenderPipelineSettings s) => s.supportSubsurfaceScattering);
            supportsForwardOnly = root.Find((RenderPipelineSettings s) => s.supportsForwardOnly);
            supportsMotionVectors = root.Find((RenderPipelineSettings s) => s.supportsMotionVectors);
            supportsStereo = root.Find((RenderPipelineSettings s) => s.supportsStereo);

            lightLoopSettings = new SerializedGlobalLightLoopSettings(root.Find((RenderPipelineSettings s) => s.lightLoopSettings));
            shadowInitParams = new SerializedShadowInitParameters(root.Find((RenderPipelineSettings s) => s.shadowInitParams));
			decalSettings = new SerializedGlobalDecalSettings(root.Find((RenderPipelineSettings s) => s.decalSettings));
        }
    }
}
