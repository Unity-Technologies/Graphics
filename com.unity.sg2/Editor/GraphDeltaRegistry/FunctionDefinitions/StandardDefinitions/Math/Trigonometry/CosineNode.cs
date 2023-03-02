using System;
using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CosineNode : IStandardNode
    {
        public static string Name => "Cosine";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    "Out = cos(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Fast",
@"
{
    In = (In + 1.57) * 0.1592;
    Out = (8.0 - 16.0 * abs(In * 0.1592)) * (In * 0.1592);
}",
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
            tooltip: "Calculates the cosine of the input.",
            category: "Math/Trigonometry",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/Cosine.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the cosine of the input"
                )
            }
        );
    }
}
