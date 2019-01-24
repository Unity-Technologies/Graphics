using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Post-processing/Channel Mixer")]
    public sealed class ChannelMixer : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Modify influence of the red channel in the overall mix.")]
        public ClampedFloatParameter redOutRedIn = new ClampedFloatParameter(100f, -200f, 200f);

        [Tooltip("Modify influence of the green channel in the overall mix.")]
        public ClampedFloatParameter redOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        [Tooltip("Modify influence of the blue channel in the overall mix.")]
        public ClampedFloatParameter redOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        [Tooltip("Modify influence of the red channel in the overall mix.")]
        public ClampedFloatParameter greenOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        [Tooltip("Modify influence of the green channel in the overall mix.")]
        public ClampedFloatParameter greenOutGreenIn = new ClampedFloatParameter(100f, -200f, 200f);

        [Tooltip("Modify influence of the blue channel in the overall mix.")]
        public ClampedFloatParameter greenOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        [Tooltip("Modify influence of the red channel in the overall mix.")]
        public ClampedFloatParameter blueOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        [Tooltip("Modify influence of the green channel in the overall mix.")]
        public ClampedFloatParameter blueOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        [Tooltip("Modify influence of the blue channel in the overall mix.")]
        public ClampedFloatParameter blueOutBlueIn = new ClampedFloatParameter(100f, -200f, 200f);

#pragma warning disable 414
        [SerializeField]
        int m_SelectedChannel = 0; // Only used to track the currently selected channel in the UI
#pragma warning restore 414

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
