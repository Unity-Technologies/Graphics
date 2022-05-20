using System;
using System.Collections.Generic;
using System.Linq;
using Target = UnityEditor.ShaderGraph.Target;

namespace UnityEditor.ShaderFoundry
{
    internal static class SimpleSampleBuilder
    {
        internal delegate void BuildCallback(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointInstance vertexCPInst, out CustomizationPointInstance surfaceCPInst);

        // returns all of the shader IDs produced by the given target
        internal static IEnumerable<string> AllShaderIDs(Target target, ShaderContainer container = null)
        {
            // ideally the settings for this provider would be provided through the same interface as workflow settings
            // but for now it is directly populated by this special constructor
            ITemplateProvider provider = new LegacyTemplateProvider(target, new ShaderGraph.AssetCollection());

            if (container == null)
                container = new ShaderContainer();

            var foundShaderIDs = new HashSet<string>();

            var templates = provider.GetTemplates(container);

            // return BlockSurfaceShaderBuilder.AllShaderIDs(templates); // ideally we call this here instead
            foreach (var template in templates)
            {
                if (!foundShaderIDs.Contains(template.AdditionalShaderID))
                {
                    yield return template.AdditionalShaderID;
                    foundShaderIDs.Add(template.AdditionalShaderID);
                }
            }
        }

        // builds a specific shader ID (null builds the primary shader) on the gi
        internal static GeneratedShader Build(ShaderContainer container, string shaderName, BuildCallback buildCallback, string additionalShaderID = null)
        {
            var target = GetTarget();
            return Build(container, target, shaderName, buildCallback, additionalShaderID);
        }

        internal static GeneratedShader Build(ShaderContainer container, Target target, string shaderName, BuildCallback buildCallback, string additionalShaderID = null)
        {
            // ideally the settings for this provider would be provided through the same interface as workflow settings
            // but for now it is directly populated by this special constructor
            ITemplateProvider provider = new LegacyTemplateProvider(target, new ShaderGraph.AssetCollection());

            var shaderInstBuilder = new ShaderInstance.Builder(container, shaderName, additionalShaderID);

            foreach (var template in provider.GetTemplates(container))
            {
                if (template.AdditionalShaderID != additionalShaderID)
                    continue;

                var templateInstanceBuilder = new TemplateInstance.Builder(container, template);

                // Hard-coded find the two customization points we know will exist. This really should discovered from iterating long-term
                var customizationPoints = template.CustomizationPoints().ToList();
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

            var generatedShader = ShaderGenerator.Generate(container, shaderInst);
            return generatedShader;
        }

        internal static void MarkAsProperty(ShaderContainer container, StructField.Builder fieldBuilder, PropertyAttribute propertyAttribute)
        {
            fieldBuilder.AddAttribute(propertyAttribute.Build(container));
        }

        internal static BlockInstance BuildSimpleBlockInstance(ShaderContainer container, Block block)
        {
            var blockInstBuilder = new BlockInstance.Builder(container, block);
            return blockInstBuilder.Build();
        }

        internal static void BuildCommonTypes(ShaderContainer container)
        {
            var unitySamplerStateBuilder = new ShaderType.StructBuilder(container, "UnitySamplerState");
            unitySamplerStateBuilder.DeclaredExternally();
            unitySamplerStateBuilder.Build();

            var vtPropBuilder = new ShaderType.StructBuilder(container, "VTPropertyWithTextureType");
            vtPropBuilder.DeclaredExternally();
            vtPropBuilder.Build();

            var unityTexture3DBuilder = new ShaderType.StructBuilder(container, "UnityTexture3D");
            unityTexture3DBuilder.DeclaredExternally();
            unityTexture3DBuilder.Build();

            var unityTextureCubeBuilder = new ShaderType.StructBuilder(container, "UnityTextureCube");
            unityTextureCubeBuilder.DeclaredExternally();
            unityTextureCubeBuilder.Build();

            var unityTexture2DArrayBuilder = new ShaderType.StructBuilder(container, "UnityTexture2DArray");
            unityTexture2DArrayBuilder.DeclaredExternally();
            unityTexture2DArrayBuilder.Build();

            var unityTextureBuilder = new ShaderType.StructBuilder(container, "UnityTexture2D");
            unityTextureBuilder.DeclaredExternally();

            var texBuilder = new StructField.Builder(container, "tex", container._Texture2D);
            unityTextureBuilder.AddField(texBuilder.Build());

            var samplerBuilder = new StructField.Builder(container, "samplerstate", container._SamplerState);
            unityTextureBuilder.AddField(samplerBuilder.Build());

            var texelSizeBuilder = new StructField.Builder(container, "texelSize", container._float4);
            unityTextureBuilder.AddField(texelSizeBuilder.Build());

            var scaleTranslateBuilder = new StructField.Builder(container, "scaleTranslate", container._float4);
            unityTextureBuilder.AddField(scaleTranslateBuilder.Build());
            var unityTexture2D = unityTextureBuilder.Build();

            var gradientBuilder = new ShaderType.StructBuilder(container, "Gradient");
            gradientBuilder.DeclaredExternally();
            var gradientType = gradientBuilder.Build();
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
