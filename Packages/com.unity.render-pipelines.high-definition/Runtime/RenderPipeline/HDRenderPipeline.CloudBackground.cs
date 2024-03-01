using System.Collections.Generic;
using UnityEngine.Rendering.LookDev;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Interface defining if an SRP supports environment effects for lens flare occlusion
    /// </summary>
    public partial class HDRenderPipeline : ICloudBackground
    {
        /// <summary>
        /// Check is the current Render Pipeline supports environement effects for lens flare occlusion.
        /// HDRP supports lens flare occlusion from volumetric clouds, background clouds, fog, volumetric fog and water
        /// </summary>
        /// <returns>true</returns>
        public bool IsCloudBackgroundUsable()
        {
            return GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset;
        }
    }
}
