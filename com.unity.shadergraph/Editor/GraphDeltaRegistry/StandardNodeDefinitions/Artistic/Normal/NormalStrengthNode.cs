using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class NormalStrengthNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "NormalStrength", // Name
            @"
    Out.xy = In.rg * Strength;
    Out.z = lerp(1, In.b, saturate(Strength));
            ",
            new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In, new float[] { 0f, 0f, 1f }),
            new ParameterDescriptor("Strength", TYPE.Float, GraphType.Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
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
