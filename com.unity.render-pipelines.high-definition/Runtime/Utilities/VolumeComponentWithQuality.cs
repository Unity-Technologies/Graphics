using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public abstract class VolumeComponentWithQuality : VolumeComponent
    {
        [Tooltip("Whether to use quality settings for the effect.")]
        public BoolParameter useQualitySettings = new BoolParameter(false);

        [Tooltip("Specifies the quality level to be used for performance relevant parameters.")]
        public QualitySettingParameter quality = new QualitySettingParameter(VolumeQualitySettingsLevels.Medium);

        protected bool UsesQualitySettings()
        {
            return useQualitySettings.value && (HDRenderPipeline)RenderPipelineManager.currentPipeline != null;
        }
    }
}
