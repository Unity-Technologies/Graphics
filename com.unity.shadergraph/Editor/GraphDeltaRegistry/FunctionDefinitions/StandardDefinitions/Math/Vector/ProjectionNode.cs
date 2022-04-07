using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ProjectionNode : IStandardNode
    {
        public static string Name = "Projection";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = B * dot(A, B) / dot(B, B);",
            new ParameterDescriptor("A", TYPE.Vector, Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns the result of projecting A onto a straight line parallel to B",
            categories: new string[2] { "Math", "Vector" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "Input A"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "Input B"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the result of projecting A onto a straight line parallel to B"
                )
            }
        );
    }
}
