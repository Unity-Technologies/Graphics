using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Color Curves")]
    public sealed class ColorCurves : VolumeComponent, IPostProcessComponent
    {
        public AnimationCurveParameter master = new AnimationCurveParameter(AnimationCurve.Linear(0f, 0f, 1f, 1f));
        public AnimationCurveParameter red = new AnimationCurveParameter(AnimationCurve.Linear(0f, 0f, 1f, 1f));
        public AnimationCurveParameter green = new AnimationCurveParameter(AnimationCurve.Linear(0f, 0f, 1f, 1f));
        public AnimationCurveParameter blue = new AnimationCurveParameter(AnimationCurve.Linear(0f, 0f, 1f, 1f));

        // TODO: secondary correction curves

        public bool IsActive()
        {
            return true;
        }
    }
}
