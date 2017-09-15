using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    [Serializable]
    public class MaterialGraph : AbstractMaterialGraph
    {
        public MasterNode masterNode
        {
            get { return GetNodes<MasterNode>().FirstOrDefault(); }
        }

        public string GetFullShader(GenerationMode mode, string name, out List<PropertyCollector.TextureInfo> configuredTextures)
        {
            // Lets collect all the properties
            var generatedProperties = new PropertyCollector();
            var shaderFunctions = new ShaderGenerator();
            var shaderBody = new ShaderGenerator();

            foreach (var prop in properties)
                generatedProperties.AddShaderProperty(prop);

            // start by collecting all the active nodes!
            var activeNodeList = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(activeNodeList, masterNode);

            foreach (var node in activeNodeList.OfType<AbstractMaterialNode>())
            {
                if (node is IGeneratesFunction)
                    (node as IGeneratesFunction).GenerateNodeFunction(shaderFunctions, mode);

                if (node is IGeneratesBodyCode)
                    (node as IGeneratesBodyCode).GenerateNodeCode(shaderBody, mode);

                node.CollectShaderProperties(generatedProperties, mode);
            }

            configuredTextures = generatedProperties.GetConfiguredTexutres();
            return string.Empty;
        }
    }
}
