using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ContrastNode : IStandardNode
    {
        public static string Name = "Contrast";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    midpoint = pow(0.5, 2.2);
    Out =  (In - midpoint) * Contrast + midpoint;
}",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Contrast", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("midpoint", TYPE.Float, Usage.Local)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName : "Contrast",
            tooltip: "make the darks darker and the brights brighter",
            categories: new string[2] { "Artistic", "Adjustment" },
            synonyms: new string[1] { "intensity" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Contrast",
                    tooltip: "less than 1 reduces contrast, greater than 1 increases it"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "color with adjusted contrast"
                )
            }
        );
    }
}
