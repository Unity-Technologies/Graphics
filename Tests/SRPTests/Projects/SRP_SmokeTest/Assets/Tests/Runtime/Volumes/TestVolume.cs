using System;

namespace UnityEngine.Rendering.Tests
{
    [Serializable]
    class TestVolume : VolumeComponent
    {
        public static readonly float k_DefaultValue = 123.0f;
        public static readonly float k_OverrideValue = 456.0f;
        public static readonly float k_OverrideValue2 = 789.0f;
        public static readonly float k_OverrideValue3 = 999.0f;

        public FloatParameter param = new(k_DefaultValue);

        public bool IsActive() => true;
    }
}
