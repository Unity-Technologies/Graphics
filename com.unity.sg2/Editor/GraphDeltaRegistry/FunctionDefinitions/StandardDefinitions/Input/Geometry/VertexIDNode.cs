using System;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class VertexIDNode : IStandardNode
    {
        public static string Name => "VertexID";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = VertexID;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("VertexID", TYPE.Float, GraphType.Usage.Local, REF.VertexID)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the unique ID of each vertex.",
            category: "Input/Geometry",
            hasPreview:false,
            synonyms: Array.Empty<string>(),
            displayName: "Vertex ID",
            description: "pkg://Documentation~/previews/VertexID.md",
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The unique ID of each vertex."
                )
            }
        );
    }
}
