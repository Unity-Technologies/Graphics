using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A volume component that holds settings for the Exposure.
    /// </summary>
    [Serializable, VolumeComponentMenu("Exposure")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [DisplayInfo(name = "Exposure")]
    public sealed class ExposureVolume : VolumeComponent
    {
        /// <summary>
        /// Specifies the method that determines how to expose the frame.
        /// </summary>
        /// <seealso cref="ExposureVolume.mode"/>
        public enum Mode
        {
            /// <summary>
            /// Allows you to manually set the Scene exposure.
            /// </summary>
            /// <seealso cref="ExposureVolume.fixedExposure"/>
            Fixed,
        }
        /// <summary>
        /// Specifies the method that determines how to expose the frame.
        /// </summary>
        /// <seealso cref="Mode"/>
        [Tooltip("Specifies the method that is used to process exposure.")]
        public EnumParameter<Mode> mode = new(Mode.Fixed);
        /// <summary>
        /// An exposure value for Cameras in this Volume, in EV units.
        /// This parameter is used only when <see cref="Mode.Fixed"/> is set.
        /// </summary>
        [Tooltip("An exposure value for Cameras in this Volume, in EV units.")]
        public FloatParameter fixedExposure = new(0f);
    }
}
