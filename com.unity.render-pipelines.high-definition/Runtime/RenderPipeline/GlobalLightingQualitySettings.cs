using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public sealed class GlobalLightingQualitySettings
    {
        static int s_QualitySettingCount = Enum.GetNames(typeof(VolumeQualitySettingsLevels)).Length;

        public GlobalLightingQualitySettings()
        {

        }

        /// <summary>Default GlobalPostProcessingQualitySettings</summary>
        public static readonly GlobalLightingQualitySettings @default = new GlobalLightingQualitySettings();

    }
}
