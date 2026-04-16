using System;

namespace UnityEngine.Rendering.Tests
{
    [Obsolete("Obsolete test component.", false)]
    public class ObsoleteVolumeComponent : VolumeComponent
    {
        public FloatParameter p1 = new FloatParameter(0f);
    }
}
