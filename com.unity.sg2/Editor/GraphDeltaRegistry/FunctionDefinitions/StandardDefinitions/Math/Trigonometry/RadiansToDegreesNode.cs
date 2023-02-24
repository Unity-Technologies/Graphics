using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RadiansToDegreesNode : IStandardNode
    {
        public static string Name => "RadiansToDegrees";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = degrees(In);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Radians To Degrees",
            tooltip: "converts radians to degrees",
            category: "Math/Trigonometry",
            synonyms: new string[3] { "radtodeg", "degrees", "convert" },
            description: "pkg://Documentation~/previews/RadiansToDegrees.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "a value in radians"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the input converted to degrees"
                )
            }
        );
    }
}
