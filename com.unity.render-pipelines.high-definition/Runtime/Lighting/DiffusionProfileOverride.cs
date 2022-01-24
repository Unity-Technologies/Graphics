using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds Diffusion Profile Overrides.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Material/Diffusion Profile Override", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Diffusion-Profile")]
    public sealed class DiffusionProfileOverride : VolumeComponent
    {
        /// <summary>
        /// List of diffusion profiles used inside the volume.
        /// </summary>
        [Tooltip("List of diffusion profiles used inside the volume.")]
        [SerializeField]
        public DiffusionProfileSettingsParameter diffusionProfiles = new DiffusionProfileSettingsParameter(default(DiffusionProfileSettings[]));
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="DiffusionProfileSettings"/> value.
    /// </summary>
    [Serializable]
    public sealed class DiffusionProfileSettingsParameter : VolumeParameter<DiffusionProfileSettings[]>
    {
        /// <summary>
        /// Creates a new <see cref="DiffusionProfileSettingsParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public DiffusionProfileSettingsParameter(DiffusionProfileSettings[] value, bool overrideState = true)
            : base(value, overrideState) { }
    }
}
