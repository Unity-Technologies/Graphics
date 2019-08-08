using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum VolumeQualitySettingsLevels
    {
        Low = 0,
        Medium = 1,
        High = 2
    }


    [Serializable]
    public sealed class QualitySettingParameter : VolumeParameter<VolumeQualitySettingsLevels> { public QualitySettingParameter(VolumeQualitySettingsLevels value, bool overrideState = false) : base(value, overrideState) { } }

    [Serializable]
    public sealed class GlobalPostProcessingQualitySettings
    {
        static int s_QualitySettingCount = Enum.GetNames(typeof(VolumeQualitySettingsLevels)).Length;

        public GlobalPostProcessingQualitySettings()
        {
            /* Depth of Field */
            NearBlurSampleCount[(int)VolumeQualitySettingsLevels.Low] = 3;
            NearBlurSampleCount[(int)VolumeQualitySettingsLevels.Medium] = 5;
            NearBlurSampleCount[(int)VolumeQualitySettingsLevels.High] = 8;

            NearBlurMaxRadius[(int)VolumeQualitySettingsLevels.Low] = 2.0f;
            NearBlurMaxRadius[(int)VolumeQualitySettingsLevels.Medium] = 4.0f;
            NearBlurMaxRadius[(int)VolumeQualitySettingsLevels.High] = 7.0f;

            FarBlurSampleCount[(int)VolumeQualitySettingsLevels.Low] = 4;
            FarBlurSampleCount[(int)VolumeQualitySettingsLevels.Medium] = 7;
            FarBlurSampleCount[(int)VolumeQualitySettingsLevels.High] = 14;

            FarBlurMaxRadius[(int)VolumeQualitySettingsLevels.Low] = 5.0f;
            FarBlurMaxRadius[(int)VolumeQualitySettingsLevels.Medium] = 8.0f;
            FarBlurMaxRadius[(int)VolumeQualitySettingsLevels.High] = 13.0f;

            Resolution[(int)VolumeQualitySettingsLevels.Low] = DepthOfFieldResolution.Quarter;
            Resolution[(int)VolumeQualitySettingsLevels.Medium] = DepthOfFieldResolution.Half;
            Resolution[(int)VolumeQualitySettingsLevels.High] = DepthOfFieldResolution.Full;

            HighQualityFiltering[(int)VolumeQualitySettingsLevels.Low] = false;
            HighQualityFiltering[(int)VolumeQualitySettingsLevels.Medium] = true;
            HighQualityFiltering[(int)VolumeQualitySettingsLevels.High] = true;

            /* Motion Blur */
            MotionBlurSampleCount[(int)VolumeQualitySettingsLevels.Low] = 4;
            MotionBlurSampleCount[(int)VolumeQualitySettingsLevels.Medium] = 8;
            MotionBlurSampleCount[(int)VolumeQualitySettingsLevels.High] = 12;
        }

        /// <summary>Default GlobalPostProcessingQualitySettings</summary>
        public static readonly GlobalPostProcessingQualitySettings @default = new GlobalPostProcessingQualitySettings();

        /*  Depth of field */
        public int[] NearBlurSampleCount            = new int[s_QualitySettingCount];
        public float[] NearBlurMaxRadius            = new float[s_QualitySettingCount];
        public int[] FarBlurSampleCount             = new int[s_QualitySettingCount];
        public float[] FarBlurMaxRadius             = new float[s_QualitySettingCount];
        public DepthOfFieldResolution[] Resolution  = new DepthOfFieldResolution[s_QualitySettingCount];
        public bool[] HighQualityFiltering          = new bool[s_QualitySettingCount];

        /* Motion Blur */
        public int[] MotionBlurSampleCount          = new int[s_QualitySettingCount];
    }
}
