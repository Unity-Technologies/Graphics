using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Custom Pass that draws Object IDs
    /// </summary>
    [System.Serializable]
    public class ObjectIDCustomPass : DrawRenderersCustomPass
    {
        /// <summary>
        /// Called before the first execution of the pass occurs.
        /// Allow you to allocate custom buffers.
        /// </summary>
        /// <param name="renderContext">The render context</param>
        /// <param name="cmd">Current command buffer of the frame</param>
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            base.Setup(renderContext, cmd);

            var rendererList = Resources.FindObjectsOfTypeAll(typeof(Renderer));

            int index = 0;
            foreach (Renderer renderer in rendererList)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                float hue = (float)index / rendererList.Length;
                propertyBlock.SetColor("ObjectColor", Color.HSVToRGB(hue, 0.7f, 1.0f));
                renderer.SetPropertyBlock(propertyBlock);
                index++;
            }

            overrideMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaderGraphs.objectIDPS);
            overrideMaterialPassName = "ForwardOnly";
        }
    }
}
