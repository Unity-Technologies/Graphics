using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class Vector3Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,     // Version
            "Vector3", // Name
@"
    Out.x = X;
    Out.y = Y;
    Out.z = Z;
",
            new ParameterDescriptor("X", TYPE.Float, Usage.In),
            new ParameterDescriptor("Y", TYPE.Float, Usage.In),
            new ParameterDescriptor("Z", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "DisplayName", "Vector 3" },
            { "Category", "Input, Basic" },
            { "Name.Synonyms", "3, v3, vec3, float3" },
            { "Tooltip", "a user-defined value with 3 channels" },
            { "Parameters.X.Tooltip", "the first component" },
            { "Parameters.Y.Tooltip", "the second component" },
            { "Parameters.Z.Tooltip", "the third component" },
            { "Parameters.Out.Tooltip", "a user-defined value with 3 channels" }
        };
    }
}
