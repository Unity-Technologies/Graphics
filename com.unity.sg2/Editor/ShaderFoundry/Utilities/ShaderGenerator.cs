namespace UnityEditor.ShaderFoundry
{
    internal struct GeneratedShader
    {
        public string shaderName;
        public bool isPrimaryShader;
        public string codeString;
        // public List<PropertyCollector.TextureInfo> assignedTextures;     // TODO @ SHADERS: needed for populating compiled shader
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
            ShaderPropertyCollection exposedShaderProperties = new ShaderPropertyCollection();

            void CollectUniqueBlockProperties(Block block)
            {
                if (!block.IsValid)
                    return;

                var properties = block.Properties();
                if (properties != null)
                {
                    foreach (var prop in properties)
                    {
                        var propertyAttribute = PropertyAttribute.FindFirst(prop.Attributes);
                        if (propertyAttribute != null && !propertyAttribute.Exposed)
                            continue;

                        exposedShaderProperties.Add(prop);
                    }
                }
            }
            void CollectUniqueCustomizationPointProperties(CustomizationPoint customizationPoint)
            {
                if (!customizationPoint.IsValid)
                    return;
                foreach (var blockSequenceElement in customizationPoint.DefaultBlockSequenceElements)
                    CollectUniqueProperties(blockSequenceElement);
            }
            void CollectUniqueProperties(BlockSequenceElement blockSequenceElement)
            {
                if (!blockSequenceElement.IsValid)
                    return;

                if (blockSequenceElement.Block.IsValid)
                    CollectUniqueBlockProperties(blockSequenceElement.Block);
                if (blockSequenceElement.CustomizationPoint.IsValid)
                    CollectUniqueCustomizationPointProperties(blockSequenceElement.CustomizationPoint);
            }

            void CollectUniqueStageDescriptionProperties(StageDescription stageDescription)
            {
                if (!stageDescription.IsValid)
                    return;

                foreach (var blockSequenceElement in stageDescription.Elements)
                    CollectUniqueProperties(blockSequenceElement);
            }

            foreach (var templateInst in shaderInst.TemplateInstances)
            {
                foreach (var pass in templateInst.Template.Passes)
                {
                    foreach (var stageDescription in pass.StageDescriptions)
                        CollectUniqueStageDescriptionProperties(stageDescription);
                }
                foreach (var cpImpl in templateInst.CustomizationPointImplementations)
                {
                    foreach (var blockSequenceElement in cpImpl.BlockSequenceElements)
                        CollectUniqueProperties(blockSequenceElement);
                }
            }

            builder.AddLine("Properties");
            using (builder.BlockScope())
            {
                foreach (var prop in exposedShaderProperties.Properties)
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
            foreach (var dependency in shaderInst.Dependencies)
                builder.AppendLine($"Dependency \"{dependency.DependencyName}\" = \"{dependency.ShaderName}\"");
        }

        static void GenerateCustomEditors(ShaderBuilder builder, ShaderContainer container, ShaderInstance shaderInst)
        {
            foreach (var customEditor in shaderInst.CustomEditors)
                builder.AppendLine($"CustomEditorForRenderPipeline \"{customEditor.CustomEditorClassName}\" \"{customEditor.RenderPipelineAssetClassName}\"");
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
