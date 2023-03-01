using System.Collections.Generic;
using UnityEngine.Rendering.LookDev;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// CloudBackground
    /// Interface, Implementation for CloudBackground on each SRP
    /// </summary>
    public partial class HDRenderPipeline : ICloudBackground
    {
        /// <summary>
        /// Check is the current HDRP had CloudBackground
        /// </summary>
        /// <returns>true if the CloudBackground is usable on HDRP</returns>
        public bool IsCloudBackgroundUsable()
        {
            if (currentAsset != null)
                return true;
            else
                return false;
        }
    }
}
