using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class Vector4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Vector4", // Name
@"
    Out.x = X;
    Out.y = Y;
    Out.z = Z;
    Out.w = W;
",
            new ParameterDescriptor("X", TYPE.Float, Usage.In),
            new ParameterDescriptor("Y", TYPE.Float, Usage.In),
            new ParameterDescriptor("Z", TYPE.Float, Usage.In),
            new ParameterDescriptor("W", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec4, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "DisplayName", "Vector 4" },
            { "Category", "Input, Basic" },
            { "Name.Synonyms", "4, v4, vec4, float4" },
            { "Tooltip", "a user-defined value with 4 channels" },
            { "Parameters.X.Tooltip", "the first component" },
            { "Parameters.Y.Tooltip", "the second component" },
            { "Parameters.Z.Tooltip", "the third component" },
            { "Parameters.W.Tooltip", "the forth component" },
            { "Parameters.Out.Tooltip", "a user-defined value with 4 channels" }
        };
    }
}
