using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Scalable Quality Level Parameter.
    /// </summary>
    [Serializable]
    public sealed class ScalableSettingLevelParameter : NoInterpIntParameter
    {
        /// <summary>Number of quality levels.</summary>
        public const int LevelCount = 3;
        /// <summary>Quality levels.</summary>
        public enum Level
        {
            /// <summary>Low Quality.</summary>
            Low,
            /// <summary>Medium Quality.</summary>
            Medium,
            /// <summary>High Quality.</summary>
            High
        }

        /// <summary>
        /// Scalable Quality Level Parameter constructor.
        /// </summary>
        /// <param name="level">Initial quality level.</param>
        /// <param name="useOverride">Use local override.</param>
        /// <param name="overrideState">Override state.</param>
        public ScalableSettingLevelParameter(int level, bool useOverride, bool overrideState = false)
            : base(useOverride ? LevelCount : (int)level, overrideState)
        {
        }

        internal static int GetScalableSettingLevelParameterValue(int level, bool useOverride)
        {
            return useOverride ? LevelCount : (int)level;
        }

        /// <summary>
        /// Level and Override.
        /// </summary>
        public (int level, bool useOverride) levelAndOverride
        {
            get => value == LevelCount ? ((int)Level.Low, true) : (value, false);
            set
            {
                var (level, useOverride) = value;
                this.value = GetScalableSettingLevelParameterValue(level, useOverride);
            }
        }
    }

    /// <summary>
    /// Post Processing Quality Settings.
    /// </summary>
    [Serializable]
    public sealed class GlobalPostProcessingQualitySettings
    {
        static int s_QualitySettingCount = ScalableSettingLevelParameter.LevelCount;

        internal GlobalPostProcessingQualitySettings()
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

            LimitManualRangeNearBlur[(int)ScalableSettingLevelParameter.Level.Low] = false;
            LimitManualRangeNearBlur[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            LimitManualRangeNearBlur[(int)ScalableSettingLevelParameter.Level.High] = false;

            AdaptiveSamplingWeight[(int)ScalableSettingLevelParameter.Level.Low] = 0.5f;
            AdaptiveSamplingWeight[(int)ScalableSettingLevelParameter.Level.Medium] = 0.75f;
            AdaptiveSamplingWeight[(int)ScalableSettingLevelParameter.Level.High] = 2;

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

            BloomHighQualityPrefiltering[(int)ScalableSettingLevelParameter.Level.Low] = false;
            BloomHighQualityPrefiltering[(int)ScalableSettingLevelParameter.Level.Medium] = false;
            BloomHighQualityPrefiltering[(int)ScalableSettingLevelParameter.Level.High] = true;

            /* Chromatic Aberration */
            ChromaticAberrationMaxSamples[(int)ScalableSettingLevelParameter.Level.Low] = 3;
            ChromaticAberrationMaxSamples[(int)ScalableSettingLevelParameter.Level.Medium] = 6;
            ChromaticAberrationMaxSamples[(int)ScalableSettingLevelParameter.Level.High] = 12;
        }

        internal static GlobalPostProcessingQualitySettings NewDefault() => new GlobalPostProcessingQualitySettings();

        /*  Depth of field */
        /// <summary>Depth of field near blur sample count for each quality level. The array must have one entry per scalable setting level, and elements must be between 3 and 8.</summary>
        [Range(3, 8)]
        public int[] NearBlurSampleCount = new int[s_QualitySettingCount];
        /// <summary>Depth of field near blur maximum radius for each quality level. The array must have one entry per scalable setting level, and elements must be between 0 and 8.</summary>
        [Range(0, 8)]
        public float[] NearBlurMaxRadius = new float[s_QualitySettingCount];
        /// <summary>Depth of field far blur sample count for each quality level. The array must have one entry per scalable setting level, and elements must be between 3 and 16.</summary>
        [Range(3, 16)]
        public int[] FarBlurSampleCount = new int[s_QualitySettingCount];
        /// <summary>Depth of field far blur maximum radius for each quality level. The array must have one entry per scalable setting level, and elements must be between 0 and 16.</summary>
        [Range(0, 16)]
        public float[] FarBlurMaxRadius = new float[s_QualitySettingCount];
        /// <summary>Depth of field resolution for each quality level. The array must have one entry per scalable setting level.</summary>
        public DepthOfFieldResolution[] DoFResolution = new DepthOfFieldResolution[s_QualitySettingCount];
        /// <summary>Use Depth of field high quality filtering for each quality level. The array must have one entry per scalable setting level.</summary>
        public bool[] DoFHighQualityFiltering = new bool[s_QualitySettingCount];
        /// <summary>Use physically based Depth of field for each quality level. The array must have one entry per scalable setting level.</summary>
        public bool[] DoFPhysicallyBased = new bool[s_QualitySettingCount];
        /// <summary>Adjust the number of samples in the physically based depth of field depending on the radius of the blur. Higher values will decrease the noise but increase the rendering cost.</summary>
        [Range(0.25f, 4f)]
        public float[] AdaptiveSamplingWeight = new float[s_QualitySettingCount];
        /// <summary>Adjust near blur CoC based on depth distance when manual, non-physical mode is used for each quality level. The array must have one entry per scalable setting level.</summary>
        public bool[] LimitManualRangeNearBlur = new bool[s_QualitySettingCount];

        /* Motion Blur */
        /// <summary>Motion Blur sample count for each quality level. The array must have one entry per scalable setting level, and elements must above 2.</summary>
        [Min(2)]
        public int[] MotionBlurSampleCount = new int[s_QualitySettingCount];

        /* Bloom */
        /// <summary>Bloom resolution for each quality level. The array must have one entry per scalable setting level.</summary>
        public BloomResolution[] BloomRes = new BloomResolution[s_QualitySettingCount];
        /// <summary>Bloom high quality filtering for each quality level. The array must have one entry per scalable setting level.</summary>
        public bool[] BloomHighQualityFiltering = new bool[s_QualitySettingCount];
        /// <summary>Bloom high quality prefiltering for each quality level. The array must have one entry per scalable setting level.</summary>
        public bool[] BloomHighQualityPrefiltering = new bool[s_QualitySettingCount];

        /* Chromatic Aberration */
        /// <summary>Chromatic aberration maximum sample count for each quality level. The array must have one entry per scalable setting level, and elements must be between 3 and 24.</summary>
        [Range(3, 24)]
        public int[] ChromaticAberrationMaxSamples = new int[s_QualitySettingCount];
    }
}
