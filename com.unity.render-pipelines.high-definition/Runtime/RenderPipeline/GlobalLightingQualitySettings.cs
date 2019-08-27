using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public sealed class GlobalLightingQualitySettings
    {
        static int s_QualitySettingCount = Enum.GetNames(typeof(VolumeQualitySettingsLevels)).Length;

        public GlobalLightingQualitySettings()
        {
            /* Ambient Occlusion */
            AOStepCount[(int)VolumeQualitySettingsLevels.Low] = 4;
            AOStepCount[(int)VolumeQualitySettingsLevels.Medium] = 6;
            AOStepCount[(int)VolumeQualitySettingsLevels.High] = 20;

            AOFullRes[(int)VolumeQualitySettingsLevels.Low] = false;
            AOFullRes[(int)VolumeQualitySettingsLevels.Medium] = false;
            AOFullRes[(int)VolumeQualitySettingsLevels.High] = true;

            AOMaximumRadiusPixels[(int)VolumeQualitySettingsLevels.Low] = 24;
            AOMaximumRadiusPixels[(int)VolumeQualitySettingsLevels.Medium] = 40;
            AOMaximumRadiusPixels[(int)VolumeQualitySettingsLevels.High] = 128;

            /* Contact Shadow */
            ContactShadowSampleCount[(int)VolumeQualitySettingsLevels.Low] = 4;
            ContactShadowSampleCount[(int)VolumeQualitySettingsLevels.Medium] = 8;
            ContactShadowSampleCount[(int)VolumeQualitySettingsLevels.High] = 16;

        }

        /// <summary>Default GlobalPostProcessingQualitySettings</summary>
        public static readonly GlobalLightingQualitySettings @default = new GlobalLightingQualitySettings();

        // SSAO
        public int[] AOStepCount = new int[s_QualitySettingCount];
        public bool[] AOFullRes = new bool[s_QualitySettingCount];
        public int[] AOMaximumRadiusPixels = new int[s_QualitySettingCount];

        // Contact Shadows
        public int[] ContactShadowSampleCount = new int[s_QualitySettingCount];

        // TODO: Add SSR

        // TODO: Volumetric fog quality

        // TODO: Shadows. This needs to be discussed further as there is an idiosyncracy here as we have different level of quality settings,
        //some for resolution per light (4 levels) some per volume (which are 3 levels everywhere). This needs to be discussed more.  


    }
}
