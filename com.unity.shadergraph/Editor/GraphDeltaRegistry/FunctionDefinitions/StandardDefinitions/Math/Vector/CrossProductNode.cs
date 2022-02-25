using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class CrossProductNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "CrossProduct", // Name
            "Out = cross(A, B);",
            new ParameterDescriptor("A", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("B", TYPE.Vec3, Usage.In, new float[] { 0f, 1f, 0f }),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "DisplayName", "Cross Product" },
            { "Category", "Math, Vector" },
            { "Name.Synonyms", "perpendicular" },
            { "Tooltip", "returns a vector that is perpendicular to the two input vectors" },
            { "Parameters.A.Tooltip", "Vector A" },
            { "Parameters.B.Tooltip", "Vector B" },
            { "Parameters.Out.Tooltip", "a vector that is perpendicular to the two input vectors" }
        };
    }
}
