using System;
using System.Collections.Generic;
using System.Linq;
using Target = UnityEditor.ShaderGraph.Target;

namespace UnityEditor.ShaderFoundry
{
    internal static class SimpleSampleBuilder
    {
        internal delegate void BuildCallback(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointDescriptor vertexCPDesc, out CustomizationPointDescriptor surfaceCPDesc);  

        internal static void Build(ShaderContainer container, Target target, string shaderName, BuildCallback buildCallback, ShaderBuilder shaderBuilder)
        {
            ITemplateProvider provider = new LegacyTemplateProvider(target, new ShaderGraph.AssetCollection());

            var shaderDescBuilder = new ShaderDescriptor.Builder(container, shaderName);
            
            foreach(var template in provider.GetTemplates(container))
            {
                var templateDescriptorBuilder = new TemplateDescriptor.Builder(container, template);

                // Hard-coded find the two customization points we know will exist. This really should discovered from iterating long-term
                var customizationPoints = template.CustomizationPoints.ToList();
                var vertexCP = customizationPoints.Find((cp) => (cp.Name == LegacyCustomizationPoints.VertexDescriptionCPName));
                var surfaceCP = customizationPoints.Find((cp) => (cp.Name == LegacyCustomizationPoints.SurfaceDescriptionCPName));

                // Build the descriptors for the two customization points. These define the blocks we're adding
                buildCallback(container, vertexCP, surfaceCP, out var vertexCPDesc, out var surfaceCPDesc);

                templateDescriptorBuilder.AddCustomizationPointDescriptor(vertexCPDesc);
                templateDescriptorBuilder.AddCustomizationPointDescriptor(surfaceCPDesc);

                var templateDescriptor = templateDescriptorBuilder.Build();
                shaderDescBuilder.TemplateDescriptors.Add(templateDescriptor);
            }
            
            var shaderDesc = shaderDescBuilder.Build();
            var generator = new ShaderGenerator();
            generator.Generate(shaderBuilder, container, shaderDesc);
        }

        // Simple helper to make a type from a bunch of variables
        internal static ShaderType BuildStructFromVariables(ShaderContainer container, string typeName, IEnumerable<BlockVariable> variables)
        {
            var typeBuilder = new ShaderType.StructBuilder(container, typeName);
            foreach (var variable in variables)
                typeBuilder.AddField(variable.Type, variable.ReferenceName);
            return typeBuilder.Build();
        }

        internal static void MarkAsProperty(ShaderContainer container, BlockVariable.Builder variableBuilder, string propertyType)
        {
            // An input tagged with 'Property' is auto added as a property
            variableBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.Property).Build());
            // [PropertyType] is used to fill out the type in the material attribute.
            // Currently the Value is used to set this, but the name has to be non-empty...
            variableBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.PropertyType).Param(propertyType, propertyType).Build());
        }

        internal static BlockDescriptor BuildSimpleBlockDescriptor(ShaderContainer container, Block block)
        {
            var blockDescBuilder = new BlockDescriptor.Builder(container, block);
            return blockDescBuilder.Build();
        }

        internal static void BuildTexture2D(ShaderContainer container, string referenceName, string displayName, List<BlockVariable> inputs, List<BlockVariable> properties)
        {
            // Textures are big and complicated right now. To declare a texture,
            // we need the material declaration for the texture, and the 4 uniforms (texture, sampler, ST, TexelSize).
            // Not all of these are required, but this is showing a full example.

            var propertyBuilder = new BlockVariable.Builder(container);
            propertyBuilder.ReferenceName = referenceName;
            propertyBuilder.DisplayName = displayName;
            propertyBuilder.Type = container._Texture2D;
            
            // [PropertyType] is used to fill out the type in the material attribute.
            // Currently the Value is used to set this, but the name has to be non-empty...
            propertyBuilder.AddAttribute(new ShaderAttribute.Builder(container, CommonShaderAttributes.PropertyType).Param("2D", "2D").Build());
            // Default expression is everything after the equal sign in the declaration
            propertyBuilder.DefaultExpression = "\"white\" {}";
            var textureProperty = propertyBuilder.Build();
            properties.Add(textureProperty);

            // Currently, attributes are used to describe where each uniform is declared. The only two supported ones right now are [Global] and [PerMaterial].
            var globalLocationAttribute = new ShaderAttribute.Builder(container, CommonShaderAttributes.Global).Build();
            var perMaterialLocationAttribute = new ShaderAttribute.Builder(container, CommonShaderAttributes.PerMaterial).Build();

            var texture2DInputBuilder = new BlockVariable.Builder(container);
            texture2DInputBuilder.ReferenceName = referenceName;
            texture2DInputBuilder.Type = container._Texture2D;
            texture2DInputBuilder.AddAttribute(globalLocationAttribute);
            var texture2DInput = texture2DInputBuilder.Build();
            inputs.Add(texture2DInput);

            var samplerBuilder = new BlockVariable.Builder(container);
            samplerBuilder.ReferenceName = $"sampler{referenceName}";
            samplerBuilder.Type = container._SamplerState;
            samplerBuilder.AddAttribute(globalLocationAttribute);
            var sampler = samplerBuilder.Build();
            inputs.Add(sampler);

            var texelSizeBuilder = new BlockVariable.Builder(container);
            texelSizeBuilder.ReferenceName = $"{referenceName}_TexelSize";
            texelSizeBuilder.Type = container._float4;
            texelSizeBuilder.AddAttribute(perMaterialLocationAttribute);
            var texelSize = texelSizeBuilder.Build();
            inputs.Add(texelSize);

            var stBuilder = new BlockVariable.Builder(container);
            stBuilder.ReferenceName = $"{referenceName}_ST";
            stBuilder.Type = container._float4;
            stBuilder.AddAttribute(perMaterialLocationAttribute);
            var st = stBuilder.Build();
            inputs.Add(st);
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
