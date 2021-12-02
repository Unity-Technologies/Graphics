using System;

namespace UnityEngine.Rendering
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
}
