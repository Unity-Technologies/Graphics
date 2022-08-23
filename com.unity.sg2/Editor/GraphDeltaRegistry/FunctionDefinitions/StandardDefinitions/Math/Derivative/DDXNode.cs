using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class DDXNode : IStandardNode
    {
        public static string Name => "DDX";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Standard",
                    "Out = ddx(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new (
                    "Coarse",
                    "Out = ddx_coarse(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new (
                    "Fine",
                    "Out = ddx_fine(In);",
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
            tooltip: "Calculates the partial derivative of the input in relation to the screen-space x-coordinate.",
            category: "Math/Derivative",
            synonyms: new string[2] { "derivative", "slope" },
            selectableFunctions: new()
            {
                { "Standard", "Standard" },
                { "Coarse", "Coarse" },
                { "Fine", "Fine" }
            },
            functionSelectorLabel: "Mode",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the difference between the value of the current pixel and the horizontal neighboring pixel"
                )
            }
        );
    }
}
