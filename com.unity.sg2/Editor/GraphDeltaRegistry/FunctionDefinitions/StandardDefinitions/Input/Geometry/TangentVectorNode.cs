using System;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TangentVectorNode : IStandardNode
    {
        public static string Name => "TangentVector";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = TangentVector;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
                new ParameterDescriptor("TangentVector", TYPE.Vec3, GraphType.Usage.Static, REF.WorldSpace_Tangent)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a tangent vector in the selected space.",
            category: "Input/Geometry",
            synonyms: Array.Empty<string>(),
            displayName: "Tangent Vector",
            description: "pkg://Documentation~/previews/TangentVector.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "TangentVector",
                    displayName: "Space",
                    options: REF.OptionList.Tangents
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "Mesh's tangent vector in selected space."
                )
            }
        );
    }
}
