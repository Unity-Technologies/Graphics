using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Target = UnityEditor.ShaderGraph.Target;

namespace UnityEditor.ShaderFoundry
{
    internal static class SimpleSampleBuilder
    {
        internal delegate void BuildCallback(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointInstance vertexCPInst, out CustomizationPointInstance surfaceCPInst);

        internal static void Build(ShaderContainer container, string shaderName, BuildCallback buildCallback, ShaderBuilder shaderBuilder)
        {
            var target = GetTarget();
            Build(container, target, shaderName, buildCallback, shaderBuilder);
        }

        internal static void Build(ShaderContainer container, Target target, string shaderName, BuildCallback buildCallback, ShaderBuilder shaderBuilder)
        {
            ITemplateProvider provider = new LegacyTemplateProvider(target, new ShaderGraph.AssetCollection());

            var shaderInstBuilder = new ShaderInstance.Builder(container, shaderName);
            
            foreach(var template in provider.GetTemplates(container))
            {
                var templateInstanceBuilder = new TemplateInstance.Builder(container, template);

                // Hard-coded find the two customization points we know will exist. This really should discovered from iterating long-term
                var customizationPoints = template.CustomizationPoints.ToList();
                var vertexCP = customizationPoints.Find((cp) => (cp.Name == LegacyCustomizationPoints.VertexDescriptionCPName));
                var surfaceCP = customizationPoints.Find((cp) => (cp.Name == LegacyCustomizationPoints.SurfaceDescriptionCPName));

                // Build the descriptors for the two customization points. These define the blocks we're adding
                buildCallback(container, vertexCP, surfaceCP, out var vertexCPInst, out var surfaceCPInst);

                templateInstanceBuilder.AddCustomizationPointInstance(vertexCPInst);
                templateInstanceBuilder.AddCustomizationPointInstance(surfaceCPInst);

                var templateInstance = templateInstanceBuilder.Build();
                shaderInstBuilder.TemplateInstances.Add(templateInstance);
            }
            
            var shaderInst = shaderInstBuilder.Build();
            var generator = new ShaderGenerator();
            generator.Generate(shaderBuilder, container, shaderInst);
        }

        internal static void MarkAsProperty(ShaderContainer container, StructField.Builder fieldBuilder, IEnumerable<string> attributes, string displayName, string propertyType, string defaultExpression)
        {
            var attributeBuilder = new StringBuilder();
            if(attributes != null)
            {
                foreach (var attribute in attributes)
                    attributeBuilder.Append($"[{attribute}]");
            }

            AddPropertyAttribute(container, fieldBuilder);
            AddUniformDeclarationAttribute(container, fieldBuilder, "#");
            AddMaterialPropertyAttribute(container, fieldBuilder, "#", displayName, propertyType, attributeBuilder.ToString());
            AddMaterialPropertyDefaultAttribute(container, fieldBuilder, defaultExpression);
        }

        internal static BlockInstance BuildSimpleBlockInstance(ShaderContainer container, Block block)
        {
            var blockInstBuilder = new BlockInstance.Builder(container, block);
            return blockInstBuilder.Build();
        }

        internal static void BuildTexture2D(ShaderContainer container, string referenceName, string displayName, ShaderType.StructBuilder inputBuilder)
        {
            // Currently, attributes are used to describe where each uniform is declared. The only two supported ones right now are [Global] and [PerMaterial].
            var globalLocationAttribute = new ShaderAttribute.Builder(container, CommonShaderAttributes.Global).Build();
            var perMaterialLocationAttribute = new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build();

            // Textures are big and complicated right now. To declare a texture,
            // we need the material declaration for the texture, and the 4 uniforms (texture, sampler, ST, TexelSize).
            // Not all of these are required, but this is showing a full example.

            var textureBuilder = new StructField.Builder(container, referenceName, container._Texture2D);
            AddPropertyAttribute(container, textureBuilder);
            AddUniformDeclarationAttribute(container, textureBuilder, "#");
            AddMaterialPropertyAttribute(container, textureBuilder, "#", displayName, "2D", null);
            AddMaterialPropertyDefaultAttribute(container, textureBuilder, "\"white\" {}");
            inputBuilder.AddAttribute(globalLocationAttribute);
            inputBuilder.AddField(textureBuilder.Build());

            string samplerName = $"sampler{referenceName}";
            var samplerBuilder = new StructField.Builder(container, samplerName, container._SamplerState);
            AddUniformDeclarationAttribute(container, samplerBuilder, samplerName, "SAMPLER(#)");
            AddPropertyAttribute(container, samplerBuilder);
            samplerBuilder.AddAttribute(globalLocationAttribute);
            var sampler = samplerBuilder.Build();
            inputBuilder.AddField(sampler);

            string texelSizeName = $"{referenceName}_TexelSize";
            var texelSizeBuilder = new StructField.Builder(container, texelSizeName, container._float4);
            AddPropertyAttribute(container, texelSizeBuilder);
            texelSizeBuilder.AddAttribute(perMaterialLocationAttribute);
            var texelSize = texelSizeBuilder.Build();
            inputBuilder.AddField(texelSize);

            var stBuilder = new StructField.Builder(container, $"{referenceName}_ST", container._float4);
            AddPropertyAttribute(container, stBuilder);
            stBuilder.AddAttribute(perMaterialLocationAttribute);
            var st = stBuilder.Build();
            inputBuilder.AddField(st);
        }

        internal static void AddPropertyAttribute(ShaderContainer container, StructField.Builder fieldBuilder)
        {
            // An input tagged with 'Property' is auto added as a property
            fieldBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Property).Build());
        }

        static void AddMaterialPropertyAttribute(ShaderContainer container, StructField.Builder fieldBuilder, string referenceName, string displayName, string propertyType, string attributes = null)
        {
            // [MaterialProperty("declaration")] is used to define the material property block statement.
            // For instance, adding: [MaterialProperty("_tex(\"tex\", 2D)")] will declare `_tex("tex", 2D)`.
            string attributesString = attributes != null ? attributes : "";
            string paramString = $"{attributesString}{referenceName}(\"{displayName}\", {propertyType})";
            var attributeBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.MaterialProperty);
            var paramBuilder = new ShaderAttributeParam.Builder(container, null, paramString);
            attributeBuilder.Param(paramBuilder.Build());
            fieldBuilder.AddAttribute(attributeBuilder.Build());
        }

        static void AddMaterialPropertyDefaultAttribute(ShaderContainer container, StructField.Builder fieldBuilder, string defaultValueExpression)
        {
            var builder = new ShaderAttribute.Builder(container, CommonShaderAttributes.MaterialPropertyDefault);
            builder.Param(defaultValueExpression);
            fieldBuilder.AddAttribute(builder.Build());
        }

        static void AddUniformDeclarationAttribute(ShaderContainer container, StructField.Builder variableBuilder, string referenceName, string declarationString = null)
        {
            // [UniformDeclaration(name = "name", declaration = "declaration")] is used to define the uniform variable in the shader pass.
            // Declaration is optional. If it's not declared then the variable type is used, otherwise the declaration string is used.
            // [UniformDeclaration(name = "#")] float4 _data;
            // Will declare: float4 _data;
            // [UniformDeclaration(name = "#", declaration = "TEXTURE2D(#)")] Texture2D _tex;
            // Will declare: TEXTURE2D(_tex);
            // This is needed because the uniform declaration doesn't always match the runtime types.
            var attributeBuilder = new ShaderAttribute.Builder(container, CommonShaderAttributes.UniformDeclaration);
            var nameParamBuilder = new ShaderAttributeParam.Builder(container, "name", referenceName);
            attributeBuilder.Param(nameParamBuilder.Build());
            if (declarationString != null)
            {
                var declarationParamBuilder = new ShaderAttributeParam.Builder(container, "declaration", declarationString);
                attributeBuilder.Param(declarationParamBuilder.Build());
            }
            variableBuilder.AddAttribute(attributeBuilder.Build());
        }

        // Cheat and do a hard-coded lookup of the UniversalTarget for testing.
        // Shader Graph should build targets however it wants to.
        static internal Target GetTarget()
        {
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass || type.Name != "UniversalTarget")
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                    return target;
            }
            return null;
        }
    }
}
