using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace com.unity.shadergraph.defs
{

    internal class NormalReconstructZNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "NormalReconstructZ",
            @"
{
    normalVector.x = In.x;
    normalVector.y = In.y;
    normalVector.z = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
    Out = normalize(normalVector);
}",
            new ParameterDescriptor("In", TYPE.Vec2, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
            new ParameterDescriptor("normalVector", TYPE.Vec3, Usage.Local)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "derive z" },
            { "Tooltip", "creates a normal from just the X and Y components" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "return normal from just the X and Y components" },
            { "Category", "Artistic, Normal" },
            { "DisplayName", "Normal Reconstruct Z" }
        };
    }
}
