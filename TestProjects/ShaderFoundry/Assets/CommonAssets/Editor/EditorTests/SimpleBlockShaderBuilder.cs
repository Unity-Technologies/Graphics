using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using Target = UnityEditor.ShaderGraph.Target;

namespace UnityEditor.ShaderFoundry
{
    internal static class SimpleBlockShaderBuilder
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

            foreach (var template in provider.GetTemplates(container))
            {
                var templateInstanceBuilder = new TemplateInstance.Builder(container, template);

                // TODO @ SHADERS: Hard-coded find the two customization points we know will exist. This really should discovered from iterating long-term
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

        internal static BlockInstance BuildSimpleBlockInstance(ShaderContainer container, Block block)
        {
            var blockInstBuilder = new BlockInstance.Builder(container, block);
            return blockInstBuilder.Build();
        }

        // TODO @ SHADERS: Cheat and do a hard-coded lookup of the UniversalTarget for testing. This will be improved when we implement a custom target.
        static internal Target GetTarget()
        {
            var targetType = TypeCache.GetTypesDerivedFrom<Target>().Where((t) => t.Name == "UniversalTarget").ToList()[0];
            var unlitSubTargetType = TypeCache.GetTypesDerivedFrom<SubTarget>().Where((t) => t.Name == "UniversalUnlitSubTarget").ToList()[0];
            var target = (Target)Activator.CreateInstance(targetType);
            targetType.GetMethod("TrySetActiveSubTarget").Invoke(target, new object[] { unlitSubTargetType });
            return target;
        }
    }
}
