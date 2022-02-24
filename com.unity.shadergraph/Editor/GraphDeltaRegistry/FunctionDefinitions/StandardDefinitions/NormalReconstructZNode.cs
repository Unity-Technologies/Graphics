using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{

    internal class NormalReconstructZNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1,
            "NormalReconstructZ",
            @"
{
    reconstructZ = sqrt(1.0 - saturate(dot(In.xy, In.xy)));
    normalVector = float3(In.x, In.y, reconstructZ);
    Out = normalize(normalVector);
}",
            new ParameterDescriptor("In", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out),
            new ParameterDescriptor("reconstructZ", TYPE.Float, Usage.Local),
            new ParameterDescriptor("normalVector", TYPE.Vec3, Usage.Local)
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "sawtooth wave" },
            { "Tooltip", "creates a normal from just the X and Y components" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "returma normal from just the X and Y components" },
            { "Category", "Math, Wave" },
            { "DisplayName", "Normal Reconstruct Z" }
        };
    }
}
