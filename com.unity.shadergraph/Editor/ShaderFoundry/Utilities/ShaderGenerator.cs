using System.Collections.Generic;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal class ShaderGenerator
    {
        internal void Generate(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            builder.AddLine(string.Format(@"Shader ""{0}""", shaderInst.Name));
            using (builder.BlockScope())
            {
                GenerateProperties(builder, container, shaderInst);
                GenerateSubShaders(builder, container, shaderInst);
                if (!string.IsNullOrEmpty(shaderInst.FallbackShader))
                    builder.AddLine(shaderInst.FallbackShader);
            }
        }

        void GenerateProperties(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
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

            foreach (var templateInst in shaderInst.TemplateInstances)
            {
                foreach (var pass in templateInst.Template.Passes)
                {
                    foreach (var block in pass.VertexBlocks)
                        CollectUniqueProperties(block.Block);
                    foreach (var block in pass.FragmentBlocks)
                        CollectUniqueProperties(block.Block);
                }
                foreach (var cpInst in templateInst.CustomizationPointInstances)
                {
                    foreach(var blockInst in cpInst.BlockInstances)
                        CollectUniqueProperties(blockInst.Block);
                }
            }

            builder.AddLine("Properties");
            using (builder.BlockScope())
            {
                foreach (var prop in propertiesList)
                {
                    MaterialPropertyDeclaration.Declare(builder, prop);
                }
            }
        }

        void GenerateSubShaders(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            foreach (var templateInst in shaderInst.TemplateInstances)
            {
                var template = templateInst.Template;
                var linker = template.Linker;
                linker.Link(builder, container, templateInst);
            }
        }
    }
}
