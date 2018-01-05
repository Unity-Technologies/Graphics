using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable]
    public class HDShadowSettings : VolumeComponent
    {
        public NoInterpMinFloatParameter maxShadowDistance = new NoInterpMinFloatParameter(500.0f, 0.0f);
    }
}
