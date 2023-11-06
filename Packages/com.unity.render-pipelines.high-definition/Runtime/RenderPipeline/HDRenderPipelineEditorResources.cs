#if UNITY_EDITOR //file must be in realtime assembly folder to be found in HDRPAsset
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    [HDRPHelpURL("Default-Settings-Window")]
    partial class HDRenderPipelineEditorResources : HDRenderPipelineResources
    {
        [Reload(new[]
        {
            "Runtime/RenderPipelineResources/SkinDiffusionProfile.asset",
            "Runtime/RenderPipelineResources/FoliageDiffusionProfile.asset"
        })]
        [SerializeField]
        internal DiffusionProfileSettings[] defaultDiffusionProfileSettingsList;

        [Reload("Editor/RenderPipelineResources/DefaultSettingsVolumeProfile.asset")]
        public VolumeProfile defaultSettingsVolumeProfile;

        [Serializable, ReloadGroup]
        public sealed class LookDevResources
        {
            [Reload("Editor/RenderPipelineResources/DefaultLookDevProfile.asset")]
            public VolumeProfile defaultLookDevVolumeProfile;
        }

        public LookDevResources lookDev;
    }
}
#endif
