using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // Helper for generating node code via the ProviderSystem loop.
    internal static class DefinitionCodeGenerator
    {
        internal static IShaderField OutputVariable(string variableName, string typeName)
            => new ShaderField(variableName, false, true, new ShaderType(typeName));

        // For IGeneratesBodyCode: the node's call, writing its outputs to the node's slot variables.
        internal static void GenerateCall(IShaderFunction function, string nameHint, IReadOnlyDictionary<string, ShaderArgument> arguments,
            IReadOnlyDictionary<string, IShaderField> outputVariables, ShaderStringBuilder sb)
        {
            // Arguments are values here, so no upstream calls are merged that could conflict.
            var callSite = new CallProducer().Produce(function, new ProducerContext(nameHint, arguments), out _);
            var code = ShaderWriter.WriteCallSites(new[] { callSite }, (_, output) => outputVariables.TryGetValue(output, out var variable) ? variable : null);
            foreach (var line in code.Split('\n'))
                sb.AppendLine(line);
        }

        // For IGeneratesFunction: registers the function, the functions it depends on, and their includes.
        internal static void CollectFunctions(IShaderFunction function, FunctionRegistry registry)
        {
            var contents = ShaderWriter.Collect(new IDefinition[] { function });

            foreach (var include in contents.Includes)
                registry.RequiresIncludePath(ShaderWriter.GetIncludePath(include), false);

            // FunctionRegistry doesn't support overloads.
            Dictionary<string, string> signatures = new();
            foreach (var definition in contents.Functions)
            {
                var signature = ShaderObjectUtils.QualifySignature(definition);
                if (signatures.TryGetValue(definition.Name, out var registered) && registered != signature)
                    throw new InvalidOperationException($"'{definition.Name}' has overloads, which can't be registered: {registered} and {signature}.");
                signatures[definition.Name] = signature;

                string code = ShaderWriter.GetCode(definition);
                registry.ProvideFunction(definition.Name, s => s.AppendLine(code));
            }
        }
    }
}
