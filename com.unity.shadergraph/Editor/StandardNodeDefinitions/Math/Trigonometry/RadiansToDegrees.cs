using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class RadiansToDegreesNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "RadiansToDegrees",
            "Out = degrees(In);",
            new ParameterDescriptor("In", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "radtodeg, degrees, convert" },
            { "Tooltip", "converts radians to degrees" },
            { "Parameters.In.Tooltip", "a value in radians" },
            { "Parameters.Out.Tooltip", "the input converted to degrees" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Radians To Degrees" }
        };
    }
}
