using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class DDXYNode : IStandardNode
    {
        public static string Name => "DDXY";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Standard",
                    "Out = abs(ddx(In)) + abs(ddy(In));",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new (
                    "Coarse",
                    "Out = abs(ddx_coarse(In)) + abs(ddy_coarse(In));",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new (
                    "Fine",
                    "Out = abs(ddx_fine(In)) + abs(ddy_fine(In));",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the sum of both partial derivatives of the input.",
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
                    tooltip: "the sum of both partial derivatives of the input"
                )
            }
        );
    }
}
