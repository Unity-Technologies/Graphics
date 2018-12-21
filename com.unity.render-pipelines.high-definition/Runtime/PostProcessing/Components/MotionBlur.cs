using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum MotionBlurMode
    {
        Off,
        UsePhysicalCamera,
        Manual
    }

    [Serializable, VolumeComponentMenu("Post-processing/Motion Blur")]
    public sealed class MotionBlur : VolumeComponent, IPostProcessComponent
    {
        public MinIntParameter sampleCount = new MinIntParameter(8, 2);


        // Physical settings
        public MinFloatParameter intensity = new MinFloatParameter(0.0f, 0.0f);
        public MinFloatParameter maxVelocity = new MinFloatParameter(64.0f, 1.0f);

        // Advanced settings
        public MinFloatParameter minVelInPixels = new MinFloatParameter(0.25f, 0.0f);
        public MinFloatParameter tileMinMaxVelRatioForHighQuality = new MinFloatParameter(0.25f, 0.0f);

        public bool IsActive()
        {
            return intensity > 0.0f;
        }
    }
}
