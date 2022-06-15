using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ArccosineNode : IStandardNode
    {
        public static string Name = "Arccosine";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    "Default",
                    "Out = acos(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] {1f, 1f, 1f, 1f}),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    "Fast",
@"
{
    a = In * In;
    Out = (1.5707963268 - (In * (1 + a * (0.166667 + a * (0.075 + a * 0.04464)))));
}",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] {1f, 1f, 1f, 1f}),
                    new ParameterDescriptor("a", TYPE.Vector, Usage.Local),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the arccosine of each component of the input.",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[1] { "acos" },
            selectableFunctions: new()
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the arccosine of each component of the input"
                )
            }
        );
    }
}
