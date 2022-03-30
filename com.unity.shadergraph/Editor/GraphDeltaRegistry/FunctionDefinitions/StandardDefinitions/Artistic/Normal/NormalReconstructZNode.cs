using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class NormalReconstructZNode : IStandardNode
    {
        public static string Name = "NormalReconstructZ";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
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

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Normal Reconstruct Z",
            tooltip: "creates a normal from just the X and Y components",
            categories: new string[2] { "Artistic", "Normal" },
            synonyms: new string[1] { "derive z" },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "return normal from just the X and Y components"
                )
            }
        );

        public static Dictionary<string, string> UIStrings => new()
        {
            { "Name.Synonyms", "derive z" },
            { "Tooltip", "creates a normal from just the X and Y components" },
            { "Category", "Artistic, Normal" },
            { "DisplayName", "Normal Reconstruct Z" },
            { "Parameters.In.Tooltip", "input value" },
            { "Parameters.Out.Tooltip", "return normal from just the X and Y components" }
        };
    }
}
