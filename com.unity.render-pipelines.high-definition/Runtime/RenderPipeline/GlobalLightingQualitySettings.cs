using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public sealed class GlobalLightingQualitySettings
    {
        static int s_QualitySettingCount = Enum.GetNames(typeof(ScalableSettingLevelParameter.Level)).Length;

        public GlobalLightingQualitySettings()
        {
            /* Ambient Occlusion */
            AOStepCount[(int)ScalableSettingLevelParameter.Level.Low] = 4;
            AOStepCount[(int)ScalableSettingLevelParameter.Level.Medium] = 6;
            AOStepCount[(int)ScalableSettingLevelParameter.Level.High] = 16;

            AOFullRes[(int)ScalableSettingLevelParameter.Level.Low] = false;
            AOFullRes[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            AOFullRes[(int)ScalableSettingLevelParameter.Level.High] = true;

            AOBilateralUpsample[(int)ScalableSettingLevelParameter.Level.Low] = false;
            AOBilateralUpsample[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            AOBilateralUpsample[(int)ScalableSettingLevelParameter.Level.High] = true; // N/A

            AODirectionCount[(int)ScalableSettingLevelParameter.Level.Low] = 1;
            AODirectionCount[(int)ScalableSettingLevelParameter.Level.Medium] = 2;
            AODirectionCount[(int)ScalableSettingLevelParameter.Level.High] = 4;

            AOMaximumRadiusPixels[(int)ScalableSettingLevelParameter.Level.Low] = 32;
            AOMaximumRadiusPixels[(int)ScalableSettingLevelParameter.Level.Medium] = 40;
            AOMaximumRadiusPixels[(int)ScalableSettingLevelParameter.Level.High] = 80;

            /* Contact Shadow */
            ContactShadowSampleCount[(int)ScalableSettingLevelParameter.Level.Low] = 6;
            ContactShadowSampleCount[(int)ScalableSettingLevelParameter.Level.Medium] = 10;
            ContactShadowSampleCount[(int)ScalableSettingLevelParameter.Level.High] = 16;

            /* Screen Space Reflection */
            SSRMaxRaySteps[(int)ScalableSettingLevelParameter.Level.Low] = 16;
            SSRMaxRaySteps[(int)ScalableSettingLevelParameter.Level.Medium] = 32;
            SSRMaxRaySteps[(int)ScalableSettingLevelParameter.Level.High] = 64;
        }

        /// <summary>Default GlobalLightingQualitySettings</summary>
        public static GlobalLightingQualitySettings NewDefault() => new GlobalLightingQualitySettings();

        // SSAO
        public int[] AOStepCount = new int[s_QualitySettingCount];
        public bool[] AOFullRes = new bool[s_QualitySettingCount];
        public int[] AOMaximumRadiusPixels = new int[s_QualitySettingCount];
        public bool[] AOBilateralUpsample = new bool[s_QualitySettingCount];
        public int[] AODirectionCount = new int[s_QualitySettingCount];

        // Contact Shadows
        public int[] ContactShadowSampleCount = new int[s_QualitySettingCount];

        // Screen Space Reflections
        public int[] SSRMaxRaySteps = new int[s_QualitySettingCount];

        // TODO: Volumetric fog quality

        // TODO: Shadows. This needs to be discussed further as there is an idiosyncracy here as we have different level of quality settings,
        //some for resolution per light (4 levels) some per volume (which are 3 levels everywhere). This needs to be discussed more.


    }
}
