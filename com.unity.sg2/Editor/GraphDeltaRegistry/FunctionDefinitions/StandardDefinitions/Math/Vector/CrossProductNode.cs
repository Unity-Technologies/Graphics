using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CrossProductNode : IStandardNode
    {
        public static string Name = "CrossProduct";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = cross(A, B);",
            new ParameterDescriptor("A", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("B", TYPE.Vec3, Usage.In, new float[] { 0f, 1f, 0f }),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Cross Product",
            tooltip: "returns a vector that is perpendicular to the two input vectors",
            categories: new string[2] { "Math", "Vector" },
            synonyms: new string[1] { "perpendicular" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "Vector A"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "Vector B"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a vector that is perpendicular to the two input vectors"
                )
            }
        );
    }
}
