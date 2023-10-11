using System.Collections.Generic;
using UnityEngine.Rendering.LookDev;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// WaterRendering
    /// Interface, Implementation for WaterRendering on each SRP
    /// </summary>
    public partial class HDRenderPipeline : IWaterRendering
    {
        /// <summary>
        /// Check is the current HDRP had WaterRendering
        /// </summary>
        /// <returns>true if the WaterRendering is usable on HDRP</returns>
        public bool IsWaterRenderingUsable()
        {
            if (currentAsset != null)
                return currentAsset.currentPlatformRenderPipelineSettings.supportWater;
            else
                return false;
        }
    }
}
