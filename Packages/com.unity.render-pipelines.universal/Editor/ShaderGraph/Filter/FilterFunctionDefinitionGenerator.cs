using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // Builds the graph's FilterFunctionDefinition sub-asset from its exposed float/color properties.
    internal static class FilterFunctionDefinitionGenerator
    {
        // FilterFunctionDefinition.parameters supports at most 4 -- see FilterFunctionDefinition.cs.
        const int kMaxParameters = 4;

        internal static FilterFunctionDefinition Generate(GraphData graph, Material material, string filterName)
        {
            var parameters = new List<FilterParameterDeclaration>();
            var bindings = new List<ParameterBinding>();

            foreach (var property in graph.properties)
            {
                if (!property.isExposed)
                    continue;

                FilterParameter defaultValue;
                if (property is Vector1ShaderProperty floatProperty)
                {
                    defaultValue = new FilterParameter(floatProperty.value);
                }
                else if (property is ColorShaderProperty colorProperty)
                {
                    defaultValue = new FilterParameter(colorProperty.value);
                }
                else
                {
                    // Only float/color parameters are supported; skip others silently (textures are expected).
                    continue;
                }

                if (parameters.Count >= kMaxParameters)
                {
                    Debug.LogWarning($"Filter '{filterName}' exposes more than {kMaxParameters} parameters; " +
                        $"extra parameters (starting at '{property.displayName}') are ignored, matching " +
                        "FilterFunctionDefinition.parameters' own 4-parameter limit.");
                    break;
                }

                parameters.Add(new FilterParameterDeclaration
                {
                    name = property.displayName,
                    interpolationDefaultValue = defaultValue,
                });
                bindings.Add(new ParameterBinding
                {
                    index = parameters.Count - 1,
                    name = property.referenceName,
                });
            }

            UniversalFilterSubTarget filterSubTarget = null;
            foreach (var target in graph.activeTargets)
            {
                if (target.activeSubTarget is UniversalFilterSubTarget candidate)
                {
                    filterSubTarget = candidate;
                    break;
                }
            }

            var readMargins = new PostProcessingMargins
            {
                left = filterSubTarget.filterData.readMargin,
                top = filterSubTarget.filterData.readMargin,
                right = filterSubTarget.filterData.readMargin,
                bottom = filterSubTarget.filterData.readMargin,
            };
            var writeMargins = new PostProcessingMargins
            {
                left = filterSubTarget.filterData.writeMargin,
                top = filterSubTarget.filterData.writeMargin,
                right = filterSubTarget.filterData.writeMargin,
                bottom = filterSubTarget.filterData.writeMargin,
            };

            var bindingsArray = bindings.ToArray();

            var definition = ScriptableObject.CreateInstance<FilterFunctionDefinition>();
            definition.name = filterName;
            definition.filterName = filterName;
            definition.parameters = parameters.ToArray();
            definition.passes = new[]
            {
                new PostProcessingPass
                {
                    material = material,
                    passIndex = 0,
                    parameterBindings = bindingsArray,
                    // parameterBindings alone is metadata; only this callback pushes values at runtime.
                    applySettingsCallback = (mpb, context) =>
                    {
                        var func = context.filterFunction;
                        foreach (var binding in bindingsArray)
                        {
                            // Missing parameters keep the material's own defaults.
                            if (binding.index >= func.parameterCount)
                                continue;

                            var param = func.parameters[binding.index];
                            if (param.type == FilterParameterType.Color)
                                // SetColor converts to the active color space, wrong for a gamma-reading pass.
                                mpb.SetVector(binding.name, context.readsGamma ? (Vector4)param.colorValue : (Vector4)param.colorValue.linear);
                            else
                                mpb.SetFloat(binding.name, param.floatValue);
                        }
                    },
                    readMargins = readMargins,
                    writeMargins = writeMargins,
                }
            };
            return definition;
        }
    }
}
