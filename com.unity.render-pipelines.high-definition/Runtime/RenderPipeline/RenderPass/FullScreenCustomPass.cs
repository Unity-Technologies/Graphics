using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// FullScreen Custom Pass
    /// </summary>
    [System.Serializable]
    public class FullScreenCustomPass : CustomPass
    {
        // Fullscreen pass settings
        public Material         fullscreenPassMaterial;
        public int              materialPassIndex;

        /// <summary>
        /// Execute the pass with the fullscreen setup
        /// </summary>
        /// <param name="cmd"></param>
        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            if (fullscreenPassMaterial != null)
            {
                CoreUtils.DrawFullScreen(cmd, fullscreenPassMaterial, shaderPassId: materialPassIndex);
            }
        }
    }
}