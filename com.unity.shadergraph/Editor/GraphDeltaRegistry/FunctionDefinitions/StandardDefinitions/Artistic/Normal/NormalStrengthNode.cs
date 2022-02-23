using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class NormalStrengthNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "NormalStrength", // Name
            "Out = float3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));",
            new ParameterDescriptor("In", TYPE.Vec3, Usage.In, new float[] { 0f, 0f, 1f }),
            new ParameterDescriptor("Strength", TYPE.Float, Usage.In, new float[] { 1f }),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Artistic, Normal" },
            { "Name.Synonyms", "intensity" },
            { "Tooltip", "changes the intensity of a tangent space normal" },
            { "Parameters.In.Tooltip", "Minimum and Maximum values for noise intensity" },
            { "Parameters.Strength.Tooltip", "less than 1 is weaker, greater than 1 is stronger" },
            { "Parameters.Out.Tooltip", "normal with the intensity adjusted" }
        };
    }
}
