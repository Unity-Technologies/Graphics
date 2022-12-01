using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Channel Mixer effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Channel Mixer")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Post-Processing-Channel-Mixer")]
    public sealed class ChannelMixer : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Controls the influence of the red channel in the output red channel.
        /// </summary>
        [Header("Red Output Channel")]
        [Tooltip("Controls the influence of the red channel in the output red channel."), InspectorName("Red")]
        public ClampedFloatParameter redOutRedIn = new ClampedFloatParameter(100f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the green channel in the output red channel.
        /// </summary>
        [Tooltip("Controls the influence of the green channel in the output red channel."), InspectorName("Green")]
        public ClampedFloatParameter redOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the blue channel in the output red channel.
        /// </summary>
        [Tooltip("Controls the influence of the blue channel in the output red channel."), InspectorName("Blue")]
        public ClampedFloatParameter redOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the red channel in the output green channel.
        /// </summary>
        [Header("Green Output Channel")]
        [Tooltip("Controls the influence of the red channel in the output green channel."), InspectorName("Red")]
        public ClampedFloatParameter greenOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the green channel in the output green channel.
        /// </summary>
        [Tooltip("Controls the influence of the green channel in the output green channel."), InspectorName("Green")]
        public ClampedFloatParameter greenOutGreenIn = new ClampedFloatParameter(100f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the blue channel in the output green channel.
        /// </summary>
        [Tooltip("Controls the influence of the blue channel in the output green channel."), InspectorName("Blue")]
        public ClampedFloatParameter greenOutBlueIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the red channel in the output blue channel.
        /// </summary>
        [Header("Blue Output Channel")]
        [Tooltip("Controls the influence of the red channel in the output blue channel."), InspectorName("Red")]
        public ClampedFloatParameter blueOutRedIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of green red channel in the output blue channel.
        /// </summary>
        [Tooltip("Controls the influence of the green channel in the output blue channel."), InspectorName("Green")]
        public ClampedFloatParameter blueOutGreenIn = new ClampedFloatParameter(0f, -200f, 200f);

        /// <summary>
        /// Controls the influence of the blue channel in the output blue channel.
        /// </summary>
        [Tooltip("Controls the influence of the blue channel in the output blue channel."), InspectorName("Blue")]
        public ClampedFloatParameter blueOutBlueIn = new ClampedFloatParameter(100f, -200f, 200f);

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
