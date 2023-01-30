using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class AllNode : IStandardNode
    {
        public static string Name => "All";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = all(In);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Bool, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Checks if all components of the input In are non-zero values",
            category: "Utility/Logic",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/All.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "true if all input components are non-zero"
                )
            }
        );
    }
}
