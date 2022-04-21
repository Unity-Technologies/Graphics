using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class ColorMaskNode : IStandardNode
    {
        public static string Name = "ColorMask";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    Out = saturate(1 - (distance(MaskColor, In) - Range) / max(Fuzziness, 1e-5));
}",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("MaskColor", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Range", TYPE.Float, Usage.In),
            new ParameterDescriptor("Fuzziness", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Color Mask",
            tooltip: "creates a mask where the input values match the selected color",
            categories: new string[2] { "Artistic", "Mask" },
            synonyms: new string[1] { "intensity" },
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "MaskColor",
                    displayName: "Mask Color",
                    tooltip: "color to use for mask",
                    useColor: true
                ),
                new ParameterUIDescriptor(
                    name: "Range",
                    tooltip: "select colors within this range from input Mask Color"
                ),
                new ParameterUIDescriptor(
                    name: "Fuzziness",
                    tooltip: "feather edges around selection. Higher values result in a softer selection mask"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "mask produced from color matches in the input"
                ),
            }
        );
    }
}
