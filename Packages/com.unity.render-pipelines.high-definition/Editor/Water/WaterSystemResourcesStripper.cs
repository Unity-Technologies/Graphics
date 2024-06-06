using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class WaterSystemResourcesStripper : IRenderPipelineGraphicsSettingsStripper<WaterSystemRuntimeResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(WaterSystemRuntimeResources settings)
        {
            foreach (var asset in HDRPBuildData.instance.renderPipelineAssets)
            {
                if (asset.currentPlatformRenderPipelineSettings.supportWater)
                    return false;
            }

            return true;
        }
    }
}
