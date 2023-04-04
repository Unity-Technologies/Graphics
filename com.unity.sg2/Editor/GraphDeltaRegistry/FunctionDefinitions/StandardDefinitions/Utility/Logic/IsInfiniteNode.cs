using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IsInfiniteNode : IStandardNode
    {
        public static string Name => "IsInfinite";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = isinf(In);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Float, Usage.In),
                new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Is Infinite",
            tooltip: "Checks if the input In is an infinite value.",
            category: "Utility/Logic",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/IsInfinite.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "Returns true if the input is an infinite value."
                )
            }
        );
    }
}
