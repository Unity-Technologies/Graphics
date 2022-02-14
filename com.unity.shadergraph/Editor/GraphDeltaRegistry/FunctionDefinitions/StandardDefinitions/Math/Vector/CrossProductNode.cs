using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class CrossProductNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Cross Product", // Name
            "Out = cross(A, B);",
            new ParameterDescriptor("A", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("B", TYPE.Vec3, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "perpendicular" },
            { "Tooltip", "returns a vector that is perpendicular to the two input vectors" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "a vector that is perpendicular to the two input vectors" }
        };
    }
}
