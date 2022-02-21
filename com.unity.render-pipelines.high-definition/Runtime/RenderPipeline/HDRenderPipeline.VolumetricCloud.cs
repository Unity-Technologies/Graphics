using System.Collections.Generic;
using UnityEngine.Rendering.LookDev;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline : IVolumetricCloud
    {
        public bool IsVolumetricCloudUsable()
        {
            if (currentAsset != null)
                return currentAsset.currentPlatformRenderPipelineSettings.supportVolumetricClouds;
            else
                return false;
        }
    }
}
