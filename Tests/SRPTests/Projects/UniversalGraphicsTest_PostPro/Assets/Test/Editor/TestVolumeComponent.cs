using System;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    public class TestVolumeComponent : VolumeComponent
    {
        public const int k_DefaultValue = 255;
        public const int k_OverrideValue = 999;

        public IntParameter parameter = new IntParameter(k_DefaultValue);
    }
}
