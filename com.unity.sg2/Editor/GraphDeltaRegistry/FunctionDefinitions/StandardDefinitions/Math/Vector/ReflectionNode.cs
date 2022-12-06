using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ReflectionNode : IStandardNode
    {
        public static string Name => "Reflection";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            "Reflection",
            "    Out = reflect(In, Normal);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Normal", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Creates a new vector mirrored around the axis of the input normal.",
            category: "Math/Vector",
            synonyms: new string[1] { "mirror" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a vector to mirror"
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "the facing direction of the surface"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the In vector mirrored around the axis of the Normal"
                )
            }
        );
    }
}
