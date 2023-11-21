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
            if (GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var globalSettings) && globalSettings is IShaderVariantSettings shaderVariantSettings)
                stripDebugVariants = shaderVariantSettings.stripDebugVariants;

            return stripDebugVariants || !ProbeVolumeGlobalSettingsStripper.ProbeVolumeSupportedForBuild();
        }
    }

    class ProbeVolumeGlobalSettingsStripper : IRenderPipelineGraphicsSettingsStripper<ProbeVolumeGlobalSettings>
    {
        public bool active => true;

        public bool CanRemoveSettings(ProbeVolumeGlobalSettings settings) => !ProbeVolumeSupportedForBuild();

        public static bool ProbeVolumeSupportedForBuild()
        {
            bool supportProbeVolume = false;

            using (ListPool<RenderPipelineAsset>.Get(out List<RenderPipelineAsset> rpAssets))
            {
                if (UnityEditor.EditorUserBuildSettings.activeBuildTarget.TryGetRenderPipelineAssets<RenderPipelineAsset>(rpAssets))
                {
                    foreach (var asset in rpAssets)
                    {
                        if (asset is IProbeVolumeEnabledRenderPipeline probeVolumeEnabledAsset)
                            supportProbeVolume |= probeVolumeEnabledAsset.supportProbeVolume;
                    }
                }
            }

            return supportProbeVolume;
        }
    }
}
