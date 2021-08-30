using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Material/Diffusion Profile Override", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Diffusion-Profile")]
    sealed class DiffusionProfileOverride : VolumeComponent
    {
        [Tooltip("List of diffusion profiles used inside the volume.")]
        [SerializeField]
        internal DiffusionProfileSettingsParameter diffusionProfiles = new DiffusionProfileSettingsParameter(default(DiffusionProfileSettings[]));
    }

    [Serializable]
    sealed class DiffusionProfileSettingsParameter : VolumeParameter<DiffusionProfileSettings[]>
    {
        public DiffusionProfileSettingsParameter(DiffusionProfileSettings[] value, bool overrideState = true)
            : base(value, overrideState) { }
    }
}
