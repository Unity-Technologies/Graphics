using System;
using System.Linq;
using UnityEditor.ShaderGraph;
using Target = UnityEditor.ShaderGraph.Target;

namespace UnityEditor.ShaderFoundry
{
    internal static class SimpleBlockShaderBuilder
    {
        internal delegate void BuildCallback(ShaderContainer container, CustomizationPoint vertexCP, CustomizationPoint surfaceCP, out CustomizationPointInstance vertexCPInst, out CustomizationPointInstance surfaceCPInst);

        internal static void Build(ShaderContainer container, string shaderName, BuildCallback buildCallback, ShaderBuilder shaderBuilder, UnityEditor.Rendering.Foundry.ModifySubShaderCallback modifySubShaderCallback = null)
        {
            var target = GetTarget(modifySubShaderCallback);
            Build(container, target, shaderName, buildCallback, shaderBuilder);
        }

        internal static void Build(ShaderContainer container, Target target, string shaderName, BuildCallback buildCallback, ShaderBuilder shaderBuilder)
        {
            ITemplateProvider provider = new LegacyTemplateProvider(target, new ShaderGraph.AssetCollection());

            string primaryShaderID = null;
            var shaderInstBuilder = new ShaderInstance.Builder(container, shaderName, primaryShaderID);

            foreach (var template in provider.GetTemplates(container))
            {
                var templateInstanceBuilder = new TemplateInstance.Builder(container, template);

                // TODO @ SHADERS: Hard-coded find the two customization points we know will exist. This really should discovered from iterating long-term
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
            ShaderGenerator.Generate(container, shaderInst, shaderBuilder);
        }

        internal static BlockInstance BuildSimpleBlockInstance(ShaderContainer container, Block block)
        {
            var blockInstBuilder = new BlockInstance.Builder(container, block);
            return blockInstBuilder.Build();
        }

        // This is a custom unlit target for our custom SRP.
        static internal Target GetTarget(UnityEditor.Rendering.Foundry.ModifySubShaderCallback modifySubShaderCallback = null)
        {
            var target = new Rendering.Foundry.FoundryTestTarget();
            target.modifySubShaderCallback = modifySubShaderCallback;
            return target;
        }
    }
}
