using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalVectorNode : IStandardNode
    {
        public static string Name => "NormalVector";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = NormalVector;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
                new ParameterDescriptor("NormalVector", TYPE.Vec3, GraphType.Usage.Static, REF.WorldSpace_Normal)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a vector that defines the direction the point is facing.",
            category: "Input/Geometry",
            synonyms: new string[1] { "surface direction" },
            displayName: "Normal Vector",
            description: "pkg://Documentation~/previews/NormalVector.md",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "NormalVector",
                    displayName: "Space",
                    options: REF.OptionList.Normals
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Mesh's normal vector in selected space."
                )
            }
        );
    }
}
