using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderFoundry;
using ShaderContainer = UnityEditor.ShaderFoundry.ShaderContainer;
using ShaderType = UnityEditor.ShaderFoundry.ShaderType;

namespace UnityEditor.ShaderFoundry
{
    // Temporary helper class to generate the default workflow registry
    internal static class WorkflowStatic
    {
        static ShaderContainer DefaultContainer = new ShaderContainer();
        static WorkflowRegistry m_Default;

        internal static WorkflowRegistry Default { get { return BuildDefault(); } }

        static WorkflowRegistry BuildDefault()
        {
            var registry = new WorkflowRegistry();
            BuildLit(registry, DefaultContainer);

            var targets = GetTargets();
            foreach (var target in targets)
            {
                var assetCollection = new AssetCollection();
                var provider = new LegacyTemplateProvider(target, assetCollection);
                registry.RegisterProvider("Lit", provider, 0);
            }

            return registry;
        }

        static internal List<Target> GetTargets()
        {
            var targets = new List<Target>();
            // Find all valid Targets by looking in the TypeCache
            var targetTypes = TypeCache.GetTypesDerivedFrom<Target>();
            foreach (var type in targetTypes)
            {
                if (type.IsAbstract || type.IsGenericType || !type.IsClass)
                    continue;

                if (/*type.Name != "BuiltInTarget" && */type.Name != "UniversalTarget")
                    continue;

                var target = (Target)Activator.CreateInstance(type);
                if (!target.isHidden)
                {
                    targets.Add(target);
                }
            }
            return targets;
        }

        static BlockVariable BuildVariable(ShaderContainer container, string name, ShaderType type)
        {
            var builder = new BlockVariable.Builder(container);
            builder.Type = type;
            builder.ReferenceName = builder.DisplayName = name;
            return builder.Build();
        }

        static void BuildLit(WorkflowRegistry registry, ShaderContainer container)
        {
            var floatType = container._float;
            var float3Type = container._float3;
            var builder = new Workflow.Builder();
            builder.Name = "Lit";
            var vertexPointBuilder = new CustomizationPoint.Builder(container, LegacyCustomizationPoints.VertexDescriptionCPName);
            foreach (var field in UnityEditor.ShaderGraph.Structs.VertexDescriptionInputs.fields)
                vertexPointBuilder.AddInput(BuildVariable(container, field.name, container.GetType(field.type)));
            vertexPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.VertexDescription.Position.name, float3Type));
            vertexPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.VertexDescription.Normal.name, float3Type));
            vertexPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.VertexDescription.Tangent.name, float3Type));
            builder.AddCustomizationPoint(vertexPointBuilder.Build());

            var fragmentPointBuilder = new CustomizationPoint.Builder(container, LegacyCustomizationPoints.SurfaceDescriptionCPName);
            foreach (var field in UnityEditor.ShaderGraph.Structs.SurfaceDescriptionInputs.fields)
                fragmentPointBuilder.AddInput(BuildVariable(container, field.name, container.GetType(field.type)));

            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.BaseColor.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.BaseColor.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.NormalTS.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Metallic.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Specular.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Smoothness.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Emission.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.Alpha.name, float3Type));
            fragmentPointBuilder.AddOutput(BuildVariable(container, UnityEditor.ShaderGraph.BlockFields.SurfaceDescription.AlphaClipThreshold.name, float3Type));
            builder.AddCustomizationPoint(fragmentPointBuilder.Build());
            registry.Register(builder.Build(container));
        }
    }
}
