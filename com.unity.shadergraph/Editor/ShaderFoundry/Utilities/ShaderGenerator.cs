using System.Collections.Generic;
using BlockProperty = UnityEditor.ShaderFoundry.BlockVariable;

namespace UnityEditor.ShaderFoundry
{
    internal struct GeneratedShader
    {
        public string shaderName;
        public bool isPrimaryShader;
        public string codeString;
        // public List<PropertyCollector.TextureInfo> assignedTextures;     // TODO: needed for populating compiled shader
        public string errorMessage;
    }

    internal static class ShaderGenerator
    {
        internal static GeneratedShader Generate(ShaderContainer container, ShaderInstance shaderInst, ShaderBuilder builder = null)
        {
            if (builder == null)
                builder = new ShaderBuilder();

            builder.AddLine(string.Format(@"Shader ""{0}""", shaderInst.Name));
            using (builder.BlockScope())
            {
                GenerateProperties(builder, container, shaderInst);
                GenerateSubShaders(builder, container, shaderInst);
                GenerateDependencies(builder, container, shaderInst);
                GenerateCustomEditors(builder, container, shaderInst);
                GenerateFallback(builder, container, shaderInst);
            }

            GeneratedShader result = new GeneratedShader()
            {
                shaderName = shaderInst.Name,
                isPrimaryShader = shaderInst.IsPrimaryShader,
                codeString = builder.ToString(),
                errorMessage = null
            };
            return result;
        }

        static void GenerateProperties(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            var propertiesMap = new Dictionary<string, BlockProperty>();
            var propertiesList = new List<BlockProperty>();

            void CollectUniqueProperties(Block block)
            {
                var properties = block.Properties();
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        var decl = prop.Attributes.GetDeclaration();
                        if (decl == UnityEditor.ShaderGraph.Internal.HLSLDeclaration.DoNotDeclare)
                            continue;

                        if (!propertiesMap.ContainsKey(prop.Name))
                        {
                            propertiesMap.Add(prop.Name, prop);
                            propertiesList.Add(prop);
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

        static void GenerateSubShaders(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            foreach (var templateInst in shaderInst.TemplateInstances)
            {
                var template = templateInst.Template;
                var linker = template.Linker;
                linker.Link(builder, container, templateInst);
            }
        }

        static void GenerateDependencies(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            string lastDependencyName = null;
            foreach (var dependency in shaderInst.Dependencies)
            {
                if (dependency.DependencyName != lastDependencyName)
                    builder.AppendLine($"Dependency \"{dependency.DependencyName}\" = \"{dependency.ShaderName}\"");
                lastDependencyName = dependency.DependencyName;
            }
        }

        static void GenerateCustomEditors(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            string lastRenderPipelineAssetType = null;
            foreach (var customEditor in shaderInst.CustomEditors)
            {
                if (customEditor.RenderPipelineAssetType != lastRenderPipelineAssetType)
                    builder.AppendLine($"CustomEditorForRenderPipeline \"{customEditor.ShaderGUI}\" \"{customEditor.RenderPipelineAssetType}\"");
                lastRenderPipelineAssetType = customEditor.RenderPipelineAssetType;
            }
        }

        static void GenerateFallback(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            if (string.IsNullOrEmpty(shaderInst.FallbackShader))
                builder.AppendLine("FallBack off");
            else
                builder.AppendLine($"FallBack \"{shaderInst.FallbackShader}\"");
        }
    }
}
