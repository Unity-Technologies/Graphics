using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class RejectionNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Rejection", // Name
            "Out = A - (B * dot(A, B) / dot(B, B));",
            new ParameterDescriptor("A", TYPE.Any, Usage.In),
            new ParameterDescriptor("B", TYPE.Any, Usage.In),
            new ParameterDescriptor("Out", TYPE.Any, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "" },
            { "Tooltip", "returns the projection of input A onto the plane orthogonal, or perpendicular, to input B" },
            { "Parameters.A.Tooltip", "Input A" },
            { "Parameters.B.Tooltip", "Input B" },
            { "Parameters.Out.Tooltip", "the projection of A onto the plane orthogonal, or perpendicular, to B" }
        };
    }
}
