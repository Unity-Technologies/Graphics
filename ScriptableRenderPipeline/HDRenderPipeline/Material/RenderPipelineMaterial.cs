using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class RenderPipelineMaterial : Object
    {
        public static List<RenderPipelineMaterial> GetRenderPipelineMaterialList()
        {
            var baseType = typeof(RenderPipelineMaterial);
            var assembly = baseType.Assembly;

            var types = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(baseType))
                .Select(Activator.CreateInstance)
                .Cast<RenderPipelineMaterial>()
                .ToList();

            // Note: If there is a need for an optimization in the future of this function, user can
            // simply fill the materialList manually by commenting the code abode and returning a
            // custom list of materials they use in their game.
            //
            // return new List<RenderPipelineMaterial>
            // {
            //    new Lit(),
            //    new Unlit(),
            //    ...
            // };

            return types;
        }

        // GBuffer management
        public virtual int GetMaterialGBufferCount() { return 0; }
        public virtual void GetMaterialGBufferDescription(out RenderTextureFormat[] RTFormat, out RenderTextureReadWrite[] RTReadWrite)
        {
            RTFormat = null;
            RTReadWrite = null;
        }

        // Regular interface
        public virtual void Build(RenderPipelineResources renderPipelineResources) {}
        public virtual void Cleanup() {}

        // Following function can be use to initialize GPU resource (once or each frame) and bind them
        public virtual void RenderInit(CommandBuffer cmd) {}
        public virtual void Bind() {}
    }
}
