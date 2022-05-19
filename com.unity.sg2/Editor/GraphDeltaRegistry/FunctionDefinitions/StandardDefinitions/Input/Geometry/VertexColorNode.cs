using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class VertexColorNode : IStandardNode
    {
        static string Name = "VertexColor";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = VertexColor;",
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out),
            new ParameterDescriptor("VertexColor", TYPE.Vec4, GraphType.Usage.Static, REF.Vertext_Color)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the color of the current vertex.",//vertec or point?
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[0] { },
            displayName: "Vertex Color",
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The color of the current vertex."
                )
            }
        );
    }
}
