using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public abstract class VolumeComponentWithQuality : VolumeComponent
    {
        [Tooltip("Specifies the quality level to be used for performance relevant parameters.")]
        public ScalableSettingLevelParameter quality = new ScalableSettingLevelParameter((int)ScalableSettingLevelParameter.Level.Medium, true);

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
            return !quality.levelAndOverride.useOverride && (HDRenderPipeline)RenderPipelineManager.currentPipeline != null;
        }

    }
}
