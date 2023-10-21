using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Context struct for passing into GPUResidentRenderPipeline.PostCullBeginCameraRendering
    /// </summary>
    public struct RenderRequestBatcherContext
    {
        /// <summary>
        /// CommandBuffer that will be used for resulting commands
        /// </summary>
        public CommandBuffer commandBuffer;

        /// <summary>
        /// Ambient probe to be set
        /// </summary>
        public SphericalHarmonicsL2 ambientProbe;
    }
}
