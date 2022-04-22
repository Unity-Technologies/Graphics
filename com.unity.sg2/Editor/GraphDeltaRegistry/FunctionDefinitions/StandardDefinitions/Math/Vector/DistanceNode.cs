using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class DistanceNode : IStandardNode
    {
        public static string Name = "Distance";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = distance(A, B);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the distance between A and B",
            categories: new string[2] { "Math", "Vector" },
            synonyms: new string[1] { "length" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "Point A"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "Point B"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the distance between A and B"
                )
            }
        );
    }
}
