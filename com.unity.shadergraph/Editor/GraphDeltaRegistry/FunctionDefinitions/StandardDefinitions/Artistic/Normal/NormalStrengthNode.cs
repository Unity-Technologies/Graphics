using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NormalStrengthNode : IStandardNode
    {
        public static string Name = "NormalStrength";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            @"
    Out.xy = In.rg * Strength;
    Out.z = lerp(1, In.b, saturate(Strength));
            ",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In, new float[] { 0f, 0f, 1f }),
            new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Normal Strength",
            tooltip: "changes the intensity of a tangent space normal",
            categories: new string[2] { "Artistic", "Normal" },
            synonyms: new string[1] { "intensity" },
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "a normal to adjust"
                ),
                new ParameterUIDescriptor(
                    name: "Strength",
                    tooltip: "less than 1 is weaker, greater than 1 is stronger"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "normal with the intensity adjusted"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "DisplayName", "Normal Strength" },
            { "Category", "Artistic, Normal" },
            { "Name.Synonyms", "intensity" },
            { "Tooltip", "changes the intensity of a tangent space normal" },
            { "Parameters.In.Tooltip", "a normal to adjust" },
            { "Parameters.Strength.Tooltip", "less than 1 is weaker, greater than 1 is stronger" },
            { "Parameters.Out.Tooltip", "normal with the intensity adjusted" }
        };
    }
}
