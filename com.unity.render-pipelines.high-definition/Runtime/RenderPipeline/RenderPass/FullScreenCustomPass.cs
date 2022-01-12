using System.Collections.Generic;
using System;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

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

        static int fadeValueId;

        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderGraph">Current render graph.</param>
        protected override void Setup(RenderGraph renderGraph)
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

        class PassData
        {
            public Material fullscreenPassMaterial;
            public float fadeValue;
            public string materialPassName;
        }

        protected override void Execute(RenderGraph renderGraph, CustomPassContext ctx)
        {
            if (fullscreenPassMaterial != null)
            {
                if (fetchColorBuffer)
                {
                    ctx.cameraColorBufferHandle = ResolveMSAAColorBuffer(ctx.cmd, ctx.hdCamera);
                    // reset the render target to the UI
                    SetRenderTargetAuto(ctx.cmd);
                }

                using (var builder = renderGraph.AddRenderPass<PassData>("FullScreenCustomPass", out PassData passData))
                {
                    passData.fullscreenPassMaterial = fullscreenPassMaterial;
                    passData.fadeValue = fadeValue;
                    passData.materialPassName = materialPassName;

                    builder.SetRenderFunc(
                        (PassData data, RenderGraphContext context) =>
                        {
                            data.fullscreenPassMaterial.SetFloat(fadeValueId, data.fadeValue);
                            CoreUtils.DrawFullScreen(context.cmd, data.fullscreenPassMaterial, shaderPassId: data.fullscreenPassMaterial.FindPass(data.materialPassName));
                        });
                }
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
