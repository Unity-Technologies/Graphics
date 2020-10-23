using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Channel Mixer effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Channel Mixer")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Post-Processing-Channel-Mixer" + Documentation.endURL)]
    public sealed class ChannelMixer : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the influence of the red channel in the output red channel.
        /// </summary>
        [Tooltip("Controls the influence of the red channel in the output red channel.")]
        public ClampedFloatParameter redOutRedIn = new ClampedFloatParameter(100f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the green channel in the output red channel.
        /// </summary>
        [Tooltip("Controls the influence of the green channel in the output red channel.")]
        public ClampedFloatParameter redOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the blue channel in the output red channel.
        /// </summary>
        [Tooltip("Controls the influence of the blue channel in the output red channel.")]
        public ClampedFloatParameter redOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the red channel in the output green channel.
        /// </summary>
        [Tooltip("Controls the influence of the red channel in the output green channel.")]
        public ClampedFloatParameter greenOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the green channel in the output green channel.
        /// </summary>
        [Tooltip("Controls the influence of the green channel in the output green channel.")]
        public ClampedFloatParameter greenOutGreenIn = new ClampedFloatParameter(100f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the blue channel in the output green channel.
        /// </summary>
        [Tooltip("Controls the influence of the blue channel in the output green channel.")]
        public ClampedFloatParameter greenOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the red channel in the output blue channel.
        /// </summary>
        [Tooltip("Controls the influence of the red channel in the output blue channel.")]
        public ClampedFloatParameter blueOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of green red channel in the output blue channel.
        /// </summary>
        [Tooltip("Controls the influence of the green channel in the output blue channel.")]
        public ClampedFloatParameter blueOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the blue channel in the output blue channel.
        /// </summary>
        [Tooltip("Controls the influence of the blue channel in the output blue channel.")]
        public ClampedFloatParameter blueOutBlueIn = new ClampedFloatParameter(100f, -200f, 200f);

#pragma warning disable 414
        [SerializeField]
        int m_SelectedChannel = 0; // Only used to track the currently selected channel in the UI
#pragma warning restore 414

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
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
    }
}
