using System.Collections.Generic;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class ShaderGenerator
    {
        internal void Generate(ShaderBuilder builder, ShaderContainer container, ShaderDescriptor shaderDesc)
        {
            builder.AddLine(string.Format(@"Shader ""{0}""", shaderDesc.Name));
            using (builder.BlockScope())
            {
                GenerateProperties(builder, container, shaderDesc);
                GenerateSubShaders(builder, container, shaderDesc);
                if (!string.IsNullOrEmpty(shaderDesc.FallbackShader))
                    builder.AddLine(shaderDesc.FallbackShader);
            }
        }

        void GenerateProperties(ShaderBuilder builder, ShaderContainer container, ShaderDescriptor shaderDesc)
        {
            var propertiesMap = new Dictionary<string, BlockProperty>();
            var propertiesList = new List<BlockProperty>();

            void CollectUniqueProperties(Block block)
            {
                var properties = block.Properties;
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        if (!propertiesMap.ContainsKey(prop.ReferenceName))
                        {
                            propertiesMap.Add(prop.ReferenceName, prop);
                            propertiesList.Add(prop);
                        }
                    }
                }

                var inputs = block.Inputs;
                if (inputs != null)
                {
                    foreach (var input in inputs)
                    {
                        var decl = input.Attributes.GetDeclaration();
                        if (decl == UnityEditor.ShaderGraph.Internal.HLSLDeclaration.DoNotDeclare)
                            continue;

                        if (!propertiesMap.ContainsKey(input.ReferenceName))
                        {
                            var prop = input.Clone(container);
                            propertiesList.Add(prop);
                            propertiesMap.Add(prop.ReferenceName, prop);
                        }
                    }
                }
            }

            foreach (var templateDesc in shaderDesc.TemplateDescriptors)
            {
                foreach (var pass in templateDesc.Template.Passes)
                {
                    foreach (var block in pass.VertexBlocks)
                        CollectUniqueProperties(block.Block);
                    foreach (var block in pass.FragmentBlocks)
                        CollectUniqueProperties(block.Block);
                }
                foreach (var cpDesc in templateDesc.CustomizationPointDescriptors)
                {
                    foreach(var blockDesc in cpDesc.BlockDescriptors)
                        CollectUniqueProperties(blockDesc.Block);
                }
            }

            builder.AddLine("Properties");
            using (builder.BlockScope())
            {
                foreach (var prop in propertiesList)
                {
                    builder.Indentation();
                    prop.DeclareMaterialProperty(builder);
                    builder.AddLine("");
                }
            }
        }

        void GenerateSubShaders(ShaderBuilder builder, ShaderContainer container, ShaderDescriptor shaderDesc)
        {
            foreach (var templateDesc in shaderDesc.TemplateDescriptors)
            {
                var template = templateDesc.Template;
                var linker = template.Linker;
                linker.Link(builder, container, templateDesc);
            }
        }
    }
}
