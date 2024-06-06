using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class VolumetricCloudsResourcesStripper : IRenderPipelineGraphicsSettingsStripper<VolumetricCloudsRuntimeResources>
    {
        public bool active => true;

        public bool CanRemoveSettings(VolumetricCloudsRuntimeResources settings)
        {
            foreach (var asset in HDRPBuildData.instance.renderPipelineAssets)
            {
                if (asset.currentPlatformRenderPipelineSettings.supportVolumetricClouds)
                    return false;
            }

            return true;
        }
    }
}
