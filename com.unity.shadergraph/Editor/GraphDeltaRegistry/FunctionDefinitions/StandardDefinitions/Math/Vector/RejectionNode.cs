using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{

    internal class RejectionNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Rejection", // Name
            "Out = A - (B * dot(A, B) / dot(B, B));",
            new ParameterDescriptor("A", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("B", TYPE.Vector, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, GraphType.Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Category", "Math, Vector" },
            { "Tooltip", "returns the projection of input A onto the plane orthogonal, or perpendicular, to input B" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "the projection of A onto the plane orthogonal, or perpendicular, to B" }
        };
    }
}
