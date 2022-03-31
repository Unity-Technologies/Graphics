using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RadiansToDegreesNode : IStandardNode
    {
        public static string Name = "RadiansToDegrees";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = degrees(In);",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Radians To Degrees",
            tooltip: "converts radians to degrees",
            categories: new string[2] { "Math", "Trigonometry" },
            synonyms: new string[3] { "radtodeg", "degrees", "convert" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a value in radians"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the input converted to degrees"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "radtodeg, degrees, convert" },
            { "Tooltip", "converts radians to degrees" },
            { "Category", "Math, Trigonometry" },
            { "DisplayName", "Radians To Degrees" },
            { "Parameters.In.Tooltip", "a value in radians" },
            { "Parameters.Out.Tooltip", "the input converted to degrees" }
        };
    }
}
