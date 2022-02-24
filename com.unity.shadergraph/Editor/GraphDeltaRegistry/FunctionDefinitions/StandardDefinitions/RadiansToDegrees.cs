using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class RadiansToDegreesNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "RadiansToDegrees",
            "Out = degrees(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "radtodeg, degrees, convert" },
            { "Tooltip", "converts radians to degrees" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "the input converted to degrees" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Radians To Degrees" }
        };
    }
}
