using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable]
    public sealed class ScalableSettingLevelParameter : IntParameter
    {
        // We use 3 levels of quality for post processing
        public const int LevelCount = 3;
        public enum Level
        {
            Low,
            Medium,
            High
        }

        public ScalableSettingLevelParameter(int level, bool useOverride, bool overrideState = false)
            : base(useOverride ? LevelCount : (int)level, overrideState)
        {

        }

        public (int level, bool useOverride) levelAndOverride
        {
            get => value == LevelCount ? ((int)Level.Low, true) : (value, false);
            set
            {
                var (level, useOverride) = value;
                this.value = useOverride ? LevelCount : (int)level;
            }
        }
    }

    [Serializable]
    public sealed class GlobalPostProcessingQualitySettings
    {
        static int s_QualitySettingCount = ScalableSettingLevelParameter.LevelCount;

        public GlobalPostProcessingQualitySettings()
        {
            /* Depth of Field */
            NearBlurSampleCount[(int)ScalableSettingLevelParameter.Level.Low] = 3;
            NearBlurSampleCount[(int)ScalableSettingLevelParameter.Level.Medium] = 5;
            NearBlurSampleCount[(int)ScalableSettingLevelParameter.Level.High] = 8;

            NearBlurMaxRadius[(int)ScalableSettingLevelParameter.Level.Low] = 2.0f;
            NearBlurMaxRadius[(int)ScalableSettingLevelParameter.Level.Medium] = 4.0f;
            NearBlurMaxRadius[(int)ScalableSettingLevelParameter.Level.High] = 7.0f;

            FarBlurSampleCount[(int)ScalableSettingLevelParameter.Level.Low] = 4;
            FarBlurSampleCount[(int)ScalableSettingLevelParameter.Level.Medium] = 7;
            FarBlurSampleCount[(int)ScalableSettingLevelParameter.Level.High] = 14;

            FarBlurMaxRadius[(int)ScalableSettingLevelParameter.Level.Low] = 5.0f;
            FarBlurMaxRadius[(int)ScalableSettingLevelParameter.Level.Medium] = 8.0f;
            FarBlurMaxRadius[(int)ScalableSettingLevelParameter.Level.High] = 13.0f;

            DoFResolution[(int)ScalableSettingLevelParameter.Level.Low] = DepthOfFieldResolution.Quarter;
            DoFResolution[(int)ScalableSettingLevelParameter.Level.Medium] = DepthOfFieldResolution.Half;
            DoFResolution[(int)ScalableSettingLevelParameter.Level.High] = DepthOfFieldResolution.Full;

            DoFHighQualityFiltering[(int)ScalableSettingLevelParameter.Level.Low] = false;
            DoFHighQualityFiltering[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            DoFHighQualityFiltering[(int)ScalableSettingLevelParameter.Level.High] = true;

            /* Motion Blur */
            MotionBlurSampleCount[(int)ScalableSettingLevelParameter.Level.Low] = 4;
            MotionBlurSampleCount[(int)ScalableSettingLevelParameter.Level.Medium] = 8;
            MotionBlurSampleCount[(int)ScalableSettingLevelParameter.Level.High] = 12;

            /* Bloom */
            BloomRes[(int)ScalableSettingLevelParameter.Level.Low] = BloomResolution.Quarter;
            BloomRes[(int)ScalableSettingLevelParameter.Level.Medium] = BloomResolution.Half;
            BloomRes[(int)ScalableSettingLevelParameter.Level.High] = BloomResolution.Half;

            BloomHighQualityFiltering[(int)ScalableSettingLevelParameter.Level.Low] = false;
            BloomHighQualityFiltering[(int)ScalableSettingLevelParameter.Level.Medium] = true;
            BloomHighQualityFiltering[(int)ScalableSettingLevelParameter.Level.High] = true;

            /* Chromatic Aberration */
            ChromaticAberrationMaxSamples[(int)ScalableSettingLevelParameter.Level.Low] = 3;
            ChromaticAberrationMaxSamples[(int)ScalableSettingLevelParameter.Level.Medium] = 6;
            ChromaticAberrationMaxSamples[(int)ScalableSettingLevelParameter.Level.High] = 12;
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
