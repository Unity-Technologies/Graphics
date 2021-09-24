using System;

namespace UnityEngine.Rendering.HighDefinition
{
    public enum RangeReductionMode
    {
        None = 0,
        Reinhard = 1,
        BT2390 = 2
    }

    [Serializable]
    public sealed class RangeReductionModeParameter : VolumeParameter<RangeReductionMode>
    {
        /// <summary>
        /// Creates a new <see cref="RangeReductionModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RangeReductionModeParameter(RangeReductionMode value, bool overrideState = false) : base(value, overrideState) { }
    }


    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/HDROutputOptions", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("HDR-Output-Options")]
    public sealed class HDROutputOptions : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(true);
        public RangeReductionModeParameter mode = new RangeReductionModeParameter(RangeReductionMode.Reinhard);
        public BoolParameter detectPaperWhite = new BoolParameter(true);
        public BoolParameter reduceOnlyLuminance = new BoolParameter(true);
        public ClampedFloatParameter paperWhite = new ClampedFloatParameter(100.0f, 0.0f, 350.0f);
        public BoolParameter detectLimits = new BoolParameter(true);
        public ClampedFloatParameter minNits = new ClampedFloatParameter(0.0f, 0.0f, 10.0f);
        public ClampedFloatParameter maxNits = new ClampedFloatParameter(1000.0f, 0.0f, 3000.0f);


        public bool IsActive()
        {
            return enable.value;
        }

    }
}
