using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TruncateNode : IStandardNode
    {
        public static string Name = "Truncate";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = trunc(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the integer, or whole number, component of the input",
            categories: new string[2] { "Math", "Round" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the whole number of the input without the decimal portion"
                )
            }
        );
    }
}
