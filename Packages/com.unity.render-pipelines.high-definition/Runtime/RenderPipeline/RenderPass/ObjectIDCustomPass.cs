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

            AssignObjectIDs();

            overrideMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaderGraphs.objectIDPS);
            overrideMaterialPassName = "ForwardOnly";
        }

        /// <summary>
        /// Used to assign an ObjectID (in the form of a color) to every renderer in the scene.
        /// If a scene uses dynamic objects or procedural object placement, then the user script should call
        /// this function to assign Object IDs to the new objects.
        /// </summary>
        public virtual void AssignObjectIDs()
        {
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
        }
    }
}
