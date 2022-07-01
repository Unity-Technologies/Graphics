using System.Collections.Generic;
using UnityEngine.Rendering.LookDev;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Volumetric Cloud
    /// Interface, Implementation for VolumetricCloud on each SRP
    /// </summary>
    public partial class HDRenderPipeline : IVolumetricCloud
    {
        /// <summary>
        /// Check is the current HDRP had VolumetricCloud
        /// </summary>
        /// <returns>true if the VolumetricCloud is usable on HDRP</returns>
        public bool IsVolumetricCloudUsable()
        {
            if (currentAsset != null)
                return currentAsset.currentPlatformRenderPipelineSettings.supportVolumetricClouds;
            else
                return false;
        }
    }
}
