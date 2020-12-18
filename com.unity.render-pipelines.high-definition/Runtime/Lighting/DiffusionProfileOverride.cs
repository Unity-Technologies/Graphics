using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Material/Diffusion Profile Override")]
    [HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "Override-Diffusion-Profile" + Documentation.endURL)]
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
            : base(value, overrideState) {}
    }
}
