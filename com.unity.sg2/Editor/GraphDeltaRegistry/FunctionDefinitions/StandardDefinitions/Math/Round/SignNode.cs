using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SignNode : IStandardNode
    {
        public static string Name = "Sign";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = sign(In);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Determines whether the input is a positive or negative value.",
            categories: new string[2] { "Math", "Round" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "1 if the input is positive and -1 if the input is negative"
                )
            }
        );
    }
}
