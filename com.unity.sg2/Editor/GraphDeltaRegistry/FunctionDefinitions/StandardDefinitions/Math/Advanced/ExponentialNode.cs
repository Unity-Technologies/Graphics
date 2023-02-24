using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ExponentialNode : IStandardNode
    {
        public static string Name => "Exponential";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "BaseE",
                    "    Out = exp(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Base2",
                    "    Out = exp2(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the exponential value of the input.",
            category: "Math/Advanced",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/Exponential.md",
            selectableFunctions: new()
            {
                { "BaseE", "BaseE" },
                { "Base2", "Base2" }
            },
            functionSelectorLabel: "Base",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "the input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "exponential of the input"
                )
            }
        );
    }
}
