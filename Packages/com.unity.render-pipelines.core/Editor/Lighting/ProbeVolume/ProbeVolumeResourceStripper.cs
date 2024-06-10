using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class ProbeVolumeRuntimeResourceStripper : IRenderPipelineGraphicsSettingsStripper<ProbeVolumeRuntimeResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(ProbeVolumeRuntimeResources settings) => !ProbeVolumeGlobalSettingsStripper.ProbeVolumeSupportedForBuild();
    }

    class ProbeVolumeDebugResourceStripper : IRenderPipelineGraphicsSettingsStripper<ProbeVolumeDebugResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(ProbeVolumeDebugResources settings)
        {
            var stripDebugVariants = false;
            if (GraphicsSettings.TryGetRenderPipelineSettings<ShaderStrippingSetting>(out var shaderVariantSettings))
                stripDebugVariants = shaderVariantSettings.stripRuntimeDebugShaders;

            return stripDebugVariants || !ProbeVolumeGlobalSettingsStripper.ProbeVolumeSupportedForBuild();
        }
    }

    class ProbeVolumeBakingResourceStripper : IRenderPipelineGraphicsSettingsStripper<ProbeVolumeBakingResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(ProbeVolumeBakingResources _) => true;
    }

    class ProbeVolumeGlobalSettingsStripper : IRenderPipelineGraphicsSettingsStripper<ProbeVolumeGlobalSettings>
    {
        public bool active => true;

        public bool CanRemoveSettings(ProbeVolumeGlobalSettings settings) => !ProbeVolumeSupportedForBuild();

        public static bool ProbeVolumeSupportedForBuild()
        {
            bool supportProbeVolume = false;

            foreach (var asset in CoreBuildData.instance.renderPipelineAssets)
            {
                if (asset is IProbeVolumeEnabledRenderPipeline probeVolumeEnabledAsset)
                    supportProbeVolume |= probeVolumeEnabledAsset.supportProbeVolume;
            }

            return supportProbeVolume;
        }
    }
}
