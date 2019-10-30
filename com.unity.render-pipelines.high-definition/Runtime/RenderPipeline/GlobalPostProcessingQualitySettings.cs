using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public sealed class ScalableSettingLevelParameter : IntParameter
    {
        public ScalableSettingLevelParameter(ScalableSetting.Level level, bool useOverride, bool overrideState = false)
            : base(useOverride ? ScalableSetting.LevelCount : (int)level, overrideState)
        {

        }

        public (ScalableSetting.Level level, bool useOverride) levelAndOverride
        {
            get => value == ScalableSetting.LevelCount ? (ScalableSetting.Level.Low, true) : ((ScalableSetting.Level)value, false);
            set
            {
                var (level, useOverride) = value;
                this.value = useOverride ? ScalableSetting.LevelCount : (int)level;
            }
        }
    }

    [Serializable]
    public sealed class GlobalPostProcessingQualitySettings
    {
        static int s_QualitySettingCount = ScalableSetting.LevelCount;

        public GlobalPostProcessingQualitySettings()
        {
            /* Depth of Field */
            NearBlurSampleCount[(int)ScalableSetting.Level.Low] = 3;
            NearBlurSampleCount[(int)ScalableSetting.Level.Medium] = 5;
            NearBlurSampleCount[(int)ScalableSetting.Level.High] = 8;

            NearBlurMaxRadius[(int)ScalableSetting.Level.Low] = 2.0f;
            NearBlurMaxRadius[(int)ScalableSetting.Level.Medium] = 4.0f;
            NearBlurMaxRadius[(int)ScalableSetting.Level.High] = 7.0f;

            FarBlurSampleCount[(int)ScalableSetting.Level.Low] = 4;
            FarBlurSampleCount[(int)ScalableSetting.Level.Medium] = 7;
            FarBlurSampleCount[(int)ScalableSetting.Level.High] = 14;

            FarBlurMaxRadius[(int)ScalableSetting.Level.Low] = 5.0f;
            FarBlurMaxRadius[(int)ScalableSetting.Level.Medium] = 8.0f;
            FarBlurMaxRadius[(int)ScalableSetting.Level.High] = 13.0f;

            DoFResolution[(int)ScalableSetting.Level.Low] = DepthOfFieldResolution.Quarter;
            DoFResolution[(int)ScalableSetting.Level.Medium] = DepthOfFieldResolution.Half;
            DoFResolution[(int)ScalableSetting.Level.High] = DepthOfFieldResolution.Full;

            DoFHighQualityFiltering[(int)ScalableSetting.Level.Low] = false;
            DoFHighQualityFiltering[(int)ScalableSetting.Level.Medium] = true;
            DoFHighQualityFiltering[(int)ScalableSetting.Level.High] = true;

            /* Motion Blur */
            MotionBlurSampleCount[(int)ScalableSetting.Level.Low] = 4;
            MotionBlurSampleCount[(int)ScalableSetting.Level.Medium] = 8;
            MotionBlurSampleCount[(int)ScalableSetting.Level.High] = 12;

            /* Bloom */
            BloomRes[(int)ScalableSetting.Level.Low] = BloomResolution.Quarter;
            BloomRes[(int)ScalableSetting.Level.Medium] = BloomResolution.Half;
            BloomRes[(int)ScalableSetting.Level.High] = BloomResolution.Half;

            BloomHighQualityFiltering[(int)ScalableSetting.Level.Low] = false;
            BloomHighQualityFiltering[(int)ScalableSetting.Level.Medium] = true;
            BloomHighQualityFiltering[(int)ScalableSetting.Level.High] = true;

            /* Chromatic Aberration */
            ChromaticAberrationMaxSamples[(int)ScalableSetting.Level.Low] = 3;
            ChromaticAberrationMaxSamples[(int)ScalableSetting.Level.Medium] = 6;
            ChromaticAberrationMaxSamples[(int)ScalableSetting.Level.High] = 12;
        }

        /// <summary>Default GlobalPostProcessingQualitySettings</summary>
        public static readonly GlobalPostProcessingQualitySettings @default = new GlobalPostProcessingQualitySettings();

        /*  Depth of field */
        public int[] NearBlurSampleCount                = new int[s_QualitySettingCount];
        public float[] NearBlurMaxRadius                = new float[s_QualitySettingCount];
        public int[] FarBlurSampleCount                 = new int[s_QualitySettingCount];
        public float[] FarBlurMaxRadius                 = new float[s_QualitySettingCount];
        public DepthOfFieldResolution[] DoFResolution   = new DepthOfFieldResolution[s_QualitySettingCount];
        public bool[] DoFHighQualityFiltering           = new bool[s_QualitySettingCount];

        /* Motion Blur */
        public int[] MotionBlurSampleCount              = new int[s_QualitySettingCount];

        /* Bloom */
        public BloomResolution[] BloomRes               = new BloomResolution[s_QualitySettingCount];
        public bool[] BloomHighQualityFiltering         = new bool[s_QualitySettingCount];

        /* Chromatic Aberration */
        public int[] ChromaticAberrationMaxSamples      = new int[s_QualitySettingCount];
    }
}
