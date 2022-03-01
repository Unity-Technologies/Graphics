using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class ReplaceColorNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "ReplaceColor",
@"
{
    Distance = distance(From, In);
    Out = lerp(To, In, saturate((Distance - Range) / max(Fuzziness, 1e-5f)));
}",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("From", TYPE.Vector, Usage.In),//TODO: Need to be color picker 
            new ParameterDescriptor("To", TYPE.Vector, Usage.In),//Need to be color picker 
            new ParameterDescriptor("Range", TYPE.Float, Usage.In),
            new ParameterDescriptor("Fuzziness", TYPE.Float, Usage.In),
            new ParameterDescriptor("Distance", TYPE.Float, Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Adjustment" },
            { "DisplayName", "Replace Color" },
            { "Tooltip", "converts the chosen colors to another color value" },
            { "Parameters.In.Tooltip", "Input value" },
            { "Parameters.From.Tooltip", "Color to replace" },
            { "Parameters.Range.Tooltip", "Replace colors within this range from input From" },
            { "Parameters.Fuzziness.Tooltip", "Soften edges around selection" },
            { "Parameters.Out.Tooltip", "In color with colors replaced" }
        };
    }
}
