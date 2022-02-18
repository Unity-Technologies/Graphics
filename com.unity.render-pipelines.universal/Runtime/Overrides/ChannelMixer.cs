using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Channel Mixer effect.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Channel Mixer", typeof(UniversalRenderPipeline))]
    public sealed class ChannelMixer : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Modify influence of the red channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the red channel in the overall mix.")]
        public ClampedFloatParameter redOutRedIn = new ClampedFloatParameter(100f, -200f, 200f);

        /// <summary>
        /// Modify influence of the green channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the green channel in the overall mix.")]
        public ClampedFloatParameter redOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Modify influence of the blue channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the blue channel in the overall mix.")]
        public ClampedFloatParameter redOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Modify influence of the red channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the red channel in the overall mix.")]
        public ClampedFloatParameter greenOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Modify influence of the green channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the green channel in the overall mix.")]
        public ClampedFloatParameter greenOutGreenIn = new ClampedFloatParameter(100f, -200f, 200f);

        /// <summary>
        /// Modify influence of the blue channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the blue channel in the overall mix.")]
        public ClampedFloatParameter greenOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Modify influence of the red channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the red channel in the overall mix.")]
        public ClampedFloatParameter blueOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Modify influence of the green channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the green channel in the overall mix.")]
        public ClampedFloatParameter blueOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Modify influence of the blue channel in the overall mix.
        /// </summary>
        [Tooltip("Modify influence of the blue channel in the overall mix.")]
        public ClampedFloatParameter blueOutBlueIn = new ClampedFloatParameter(100f, -200f, 200f);

        /// <inheritdoc/>
        public bool IsActive()
        {
            return redOutRedIn.value != 100f
                || redOutGreenIn.value != 0f
                || redOutBlueIn.value != 0f
                || greenOutRedIn.value != 0f
                || greenOutGreenIn.value != 100f
                || greenOutBlueIn.value != 0f
                || blueOutRedIn.value != 0f
                || blueOutGreenIn.value != 0f
                || blueOutBlueIn.value != 100f;
        }

        /// <inheritdoc/>
        public bool IsTileCompatible() => true;
    }
}
