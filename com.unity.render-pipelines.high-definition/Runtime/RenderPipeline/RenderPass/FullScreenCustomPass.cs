using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// FullScreen Custom Pass
    /// </summary>
    [System.Serializable]
    class FullScreenCustomPass : CustomPass
    {
        // Fullscreen pass settings
        public Material         fullscreenPassMaterial;
        [SerializeField]
        int                     materialPassIndex = 0;
        public string           materialPassName = "Custom Pass 0";
        public bool             fetchColorBuffer;

        int fadeValueId;

        /// <inheritdoc />
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            fadeValueId = Shader.PropertyToID("_FadeValue");

            // In case there was a pass index assigned, we retrieve the name of this pass
            if (String.IsNullOrEmpty(materialPassName) && fullscreenPassMaterial != null)
                materialPassName = fullscreenPassMaterial.GetPassName(materialPassIndex);
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
                CoreUtils.DrawFullScreen(cmd, fullscreenPassMaterial, shaderPassId: fullscreenPassMaterial.FindPass(materialPassName));
            }
        }

        /// <inheritdoc />
        public override IEnumerable<Material> RegisterMaterialForInspector() { yield return fullscreenPassMaterial; }
    }
}
