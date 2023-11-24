using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Volume Component that uses Quality Settings.
    /// </summary>
    public abstract class VolumeComponentWithQuality : VolumeComponent
    {
        /// <summary>Quality level used by this component.</summary>
        [Tooltip("Specifies the quality level to be used for performance relevant parameters.")]
        [InspectorName("Tier")]
        public ScalableSettingLevelParameter quality = new ScalableSettingLevelParameter((int)ScalableSettingLevelParameter.Level.Medium, false);

        static internal GlobalPostProcessingQualitySettings GetPostProcessingQualitySettings()
        {
            var pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            if (pipeline != null)
            {
                return pipeline.currentPlatformRenderPipelineSettings.postProcessQualitySettings;
            }
            // This shouldn't happen ever.
            return null;
        }

        static internal GlobalLightingQualitySettings GetLightingQualitySettings()
        {
            var pipeline = (HDRenderPipeline)RenderPipelineManager.currentPipeline;
            if (pipeline != null)
            {
                return pipeline.currentPlatformRenderPipelineSettings.lightingQualitySettings;
            }
            // This shouldn't happen ever.
            return null;
        }

        /// <summary>
        /// Returns true if the component uses parameters from the quality settings.
        /// </summary>
        /// <returns>True if the component uses parameters from the quality settings.</returns>
        protected bool UsesQualitySettings()
        {
            return !quality.levelAndOverride.useOverride && (HDRenderPipeline)RenderPipelineManager.currentPipeline != null;
        }
    }
}
