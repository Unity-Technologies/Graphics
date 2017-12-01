using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    public class AbstractSubGraph : AbstractMaterialGraph
        , IGeneratesBodyCode
        , IGeneratesFunction
    {
        public virtual IEnumerable<AbstractMaterialNode> activeNodes { get { return Enumerable.Empty<AbstractMaterialNode>(); } }

        public PreviewMode previewMode
        {
            get { return activeNodes.Any(x => x.previewMode == PreviewMode.Preview3D) ? PreviewMode.Preview3D : PreviewMode.Preview2D; }
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in activeNodes)
            {
                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(visitor, generationMode);
            }
        }

        public void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode)
        {
            foreach (var node in activeNodes)
            {
                if (node is IGeneratesFunction)
                    (node as IGeneratesFunction).GenerateNodeFunction(visitor, generationMode);
            }
        }

        public override void CollectShaderProperties(PropertyCollector collector, GenerationMode generationMode)
        {
            // if we are previewing the graph we need to
            // export 'exposed props' if we are 'for real'
            // then we are outputting the graph in the
            // nested context and the needed values will
            // be copied into scope.
            if (generationMode == GenerationMode.Preview)
            {
                foreach (var prop in properties)
                    collector.AddShaderProperty(prop);
            }

            foreach (var node in activeNodes)
            {
                if (node is IGenerateProperties)
                    (node as IGenerateProperties).CollectShaderProperties(collector, generationMode);
            }
        }

        public IEnumerable<PreviewProperty> GetPreviewProperties()
        {
            List<PreviewProperty> props = new List<PreviewProperty>();
            foreach (var node in activeNodes)
                node.CollectPreviewMaterialProperties(props);
            return props;
        }
    }
}
