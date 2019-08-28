using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public abstract class VolumeComponentWithQuality : VolumeComponent
    {
        [Tooltip("Whether to use quality settings for the effect.")]
        public BoolParameter useQualitySettings = new BoolParameter(false);

        [Tooltip("Specifies the quality level to be used for performance relevant parameters.")]
        public QualitySettingParameter quality = new QualitySettingParameter(VolumeQualitySettingsLevels.Medium);

        static protected GlobalPostProcessingQualitySettings GetPostProcessingQualitySettings()
        {

            var pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            if(pipeline != null)
            {
                return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings;
            }
            // This shouldn't happen ever.
            return null;
        }

        protected bool UsesQualitySettings()
        {
            return useQualitySettings.value && (HDRenderPipeline)RenderPipelineManager.currentPipeline != null;
        }

    }
}
