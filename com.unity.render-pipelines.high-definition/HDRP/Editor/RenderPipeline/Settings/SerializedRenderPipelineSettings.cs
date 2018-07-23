using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    class SerializedRenderPipelineSettings
    {
        public SerializedProperty root;

        public SerializedProperty supportShadowMask;
        public SerializedProperty supportSSR;
        public SerializedProperty supportSSAO;
        public SerializedProperty supportSubsurfaceScattering;
        [UnityEngine.Serialization.FormerlySerializedAs("enableUltraQualitySSS")]
        public SerializedProperty increaseSssSampleCount;
        [UnityEngine.Serialization.FormerlySerializedAs("supportVolumetric")]
        public SerializedProperty supportVolumetrics;
        public SerializedProperty increaseResolutionOfVolumetrics;
        public SerializedProperty supportLightLayers;
        public SerializedProperty supportOnlyForward;

        public SerializedProperty supportDecals;
        public SerializedProperty supportMSAA;
        public SerializedProperty MSAASampleCount;
        public SerializedProperty supportMotionVectors;
        public SerializedProperty supportStereo;
        public SerializedProperty supportRuntimeDebugDisplay;
        public SerializedProperty supportDitheringCrossFade;

        public SerializedGlobalLightLoopSettings lightLoopSettings;
        public SerializedShadowInitParameters shadowInitParams;
        public SerializedGlobalDecalSettings decalSettings;

        public SerializedRenderPipelineSettings(SerializedProperty root)
        {
            this.root = root;

            supportShadowMask               = root.Find((RenderPipelineSettings s) => s.supportShadowMask);
            supportSSR                      = root.Find((RenderPipelineSettings s) => s.supportSSR);
            supportSSAO                     = root.Find((RenderPipelineSettings s) => s.supportSSAO);
            supportSubsurfaceScattering     = root.Find((RenderPipelineSettings s) => s.supportSubsurfaceScattering);
            increaseSssSampleCount          = root.Find((RenderPipelineSettings s) => s.increaseSssSampleCount);
            supportVolumetrics              = root.Find((RenderPipelineSettings s) => s.supportVolumetrics);
            increaseResolutionOfVolumetrics = root.Find((RenderPipelineSettings s) => s.increaseResolutionOfVolumetrics);
            supportLightLayers              = root.Find((RenderPipelineSettings s) => s.supportLightLayers);
            supportOnlyForward              = root.Find((RenderPipelineSettings s) => s.supportOnlyForward);

            supportDecals                   = root.Find((RenderPipelineSettings s) => s.supportDecals);
            supportMSAA                     = root.Find((RenderPipelineSettings s) => s.supportMSAA);
            MSAASampleCount                 = root.Find((RenderPipelineSettings s) => s.msaaSampleCount);                        
            supportMotionVectors            = root.Find((RenderPipelineSettings s) => s.supportMotionVectors);
            supportStereo                   = root.Find((RenderPipelineSettings s) => s.supportStereo);
            supportRuntimeDebugDisplay      = root.Find((RenderPipelineSettings s) => s.supportRuntimeDebugDisplay);
            supportDitheringCrossFade       = root.Find((RenderPipelineSettings s) => s.supportDitheringCrossFade);

            lightLoopSettings = new SerializedGlobalLightLoopSettings(root.Find((RenderPipelineSettings s) => s.lightLoopSettings));
            shadowInitParams  = new SerializedShadowInitParameters(root.Find((RenderPipelineSettings s) => s.shadowInitParams));
            decalSettings     = new SerializedGlobalDecalSettings(root.Find((RenderPipelineSettings s) => s.decalSettings));
        }
    }
}
