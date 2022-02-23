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
                if (!string.IsNullOrEmpty(shaderInst.FallbackShader))
                    builder.AddLine(shaderInst.FallbackShader);
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

            void CollectUniqueProperties(BlockInstance blockInstance)
            {
                if (!blockInstance.IsValid)
                    return;
                var block = blockInstance.Block;
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

            void CollectUniqueStageElementProperties(TemplatePassStageElement stageElement)
            {
                CollectUniqueProperties(stageElement.BlockInstance);
                if (!stageElement.CustomizationPoint.IsValid)
                    return;
                foreach (var blockInstance in stageElement.CustomizationPoint.DefaultBlockInstances)
                    CollectUniqueProperties(blockInstance);
            }

            foreach (var templateInst in shaderInst.TemplateInstances)
            {
                foreach (var pass in templateInst.Template.Passes)
                {
                    foreach (var stageElement in pass.VertexStageElements)
                        CollectUniqueStageElementProperties(stageElement);
                    foreach (var stageElement in pass.FragmentStageElements)
                        CollectUniqueStageElementProperties(stageElement);
                }
                foreach (var cpInst in templateInst.CustomizationPointInstances)
                {
                    foreach (var blockInst in cpInst.BlockInstances)
                        CollectUniqueProperties(blockInst);
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
    }
}
