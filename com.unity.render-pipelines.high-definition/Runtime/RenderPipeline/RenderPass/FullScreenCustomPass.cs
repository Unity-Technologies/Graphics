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
        public bool             fetchColorBuffer;

        int fadeValueId;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            fadeValueId = Shader.PropertyToID("_FadeValue");
        }

        /// <summary>
        /// Execute the pass with the fullscreen setup
        /// </summary>
        /// <param name="cmd"></param>
        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera hdCamera, CullingResults cullingResult)
        {
            if (fullscreenPassMaterial != null)
            {
                if (fetchColorBuffer)
                {
                    ResolveMSAAColorBuffer(cmd, hdCamera);
                    // reset the render target to the UI 
                    SetRenderTargetAuto(cmd);
                }

                fullscreenPassMaterial.SetFloat(fadeValueId, fadeValue);
                CoreUtils.DrawFullScreen(cmd, fullscreenPassMaterial, shaderPassId: materialPassIndex);
            }
        }
    }
}