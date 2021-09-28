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
    public class FullScreenCustomPass : CustomPass
    {
        /// <summary>
        /// Material used for the fullscreen pass, it's shader must have been created with.
        /// </summary>
        public Material fullscreenPassMaterial;
        [SerializeField]
        int materialPassIndex = 0;
        /// <summary>
        /// Name of the pass to use in the fullscreen material.
        /// </summary>
        public string materialPassName = "Custom Pass 0";
        /// <summary>
        /// Set to true if your shader will sample in the camera color buffer.
        /// </summary>
        public bool fetchColorBuffer;

        int fadeValueId;

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext">The render context</param>
        /// <param name="cmd">Current command buffer of the frame</param>
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
        /// <param name="ctx">The context of the custom pass. Contains command buffer, render context, buffer, etc.</param>
        protected override void Execute(CustomPassContext ctx)
        {
            if (fullscreenPassMaterial != null)
            {
                if (fetchColorBuffer)
                {
                    ResolveMSAAColorBuffer(ctx.cmd, ctx.hdCamera);
                    // reset the render target to the UI
                    SetRenderTargetAuto(ctx.cmd);
                }

                fullscreenPassMaterial.SetFloat(fadeValueId, fadeValue);
                CoreUtils.DrawFullScreen(ctx.cmd, fullscreenPassMaterial, shaderPassId: fullscreenPassMaterial.FindPass(materialPassName));
            }
        }

        /// <summary>
        /// List all the materials that need to be displayed at the bottom of the component.
        /// All the materials gathered by this method will be used to create a Material Editor and then can be edited directly on the custom pass.
        /// </summary>
        /// <returns>An enumerable of materials to show in the inspector. These materials can be null, the list is cleaned afterwards</returns>
        public override IEnumerable<Material> RegisterMaterialForInspector() { yield return fullscreenPassMaterial; }
    }
}
