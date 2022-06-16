using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class InverseLerpNode : IStandardNode
    {
        public static string Name => "InverseLerp";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = (T - A)/(B - A);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("A", TYPE.Vector, Usage.In),
                new ParameterDescriptor("B", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f }),
                new ParameterDescriptor("T", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Inverse Lerp",
            tooltip: "Calculates the linear parameter that produces the interpolant specified by input T, within the range of input A and input B.",
            categories: new string[2] { "Math", "Interpolation" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "out will be this value when T is zero"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "out will be this value when T is one"
                ),
                new ParameterUIDescriptor(
                    name: "T",
                    tooltip: "the blend value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the linear parameter that produces the interpolant specified by T within the range of A to B"
                )
            }
        );
    }
}
