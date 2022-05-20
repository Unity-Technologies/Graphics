using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class VertexIDNode : IStandardNode
    {
        static string Name = "VertexID";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = VertexID;",
            new ParameterDescriptor("Out", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("VertexID", TYPE.Float, GraphType.Usage.Local, REF.VertexID)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the unique ID of each vertex.",
            categories: new string[2] { "Input", "Geometry" },
            hasPreview:false,
            synonyms: new string[0] { },
            displayName: "Vertex Color",
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The unique ID of each vertex."
                )
            }
        );
    }
}
