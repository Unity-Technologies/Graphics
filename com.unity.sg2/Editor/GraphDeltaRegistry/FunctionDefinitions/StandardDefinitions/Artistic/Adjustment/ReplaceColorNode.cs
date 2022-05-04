using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ReplaceColorNode : IStandardNode
    {
        public static string Name = "ReplaceColor";
        public static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    Out = lerp(To, In, saturate((distance(From, In) - Range) / max(Fuzziness, 1e-5f)));
}",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("From", TYPE.Vec3, Usage.In),//TODO: Need to be color picker 
            new ParameterDescriptor("To", TYPE.Vec3, Usage.In),//Need to be color picker 
            new ParameterDescriptor("Range", TYPE.Float, Usage.In),
            new ParameterDescriptor("Fuzziness", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Replace Color",
            tooltip: "Converts the chosen color to another color value.",
            categories: new string[2] { "Artistic", "Adjustment" },
            synonyms: new string[0],
            parameters: new ParameterUIDescriptor[6]
            {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "From",
                    tooltip: "color to replace"
                ),
                new ParameterUIDescriptor(
                    name: "To",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "Range",
                    tooltip: "replace colors within this range from input From"
                ),
                new ParameterUIDescriptor(
                    name: "Fuzziness",
                    tooltip: "soften edges around selection"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "in color with colors replaced"
                )
            }
        );
    }
}
