using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalizeNode : IStandardNode
    {
        public static string Name => "Normalize";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Standard",
                    "    Out = normalize(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Safe",
                    "    Out = SafeNormalize(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Changes the length of the input vector to 1.",
            category: "Math/Advanced",
            synonyms: new string[1] { "Unitize" },
            selectableFunctions: new()
            {
                { "Standard", "Standard" },
                { "Safe", "Safe" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a vector to normalize"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "In / Length(In)"
                )
            }
        );
    }
}
