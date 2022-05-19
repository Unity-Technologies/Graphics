using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class UVNode : IStandardNode
    {
        static string Name = "UV";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
"Out = UV;",
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out),
            new ParameterDescriptor("UV", TYPE.Vec4, GraphType.Usage.Static, REF.UV0)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the texture coordinates from the selected channel.",
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[3] { "texcoords", "coords", "coordinates" },
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Texture coordinate."

                ),
                new ParameterUIDescriptor(
                    name: "UV",
                    options: REF.OptionList.UVs
                )
            }
        );
    }
}
