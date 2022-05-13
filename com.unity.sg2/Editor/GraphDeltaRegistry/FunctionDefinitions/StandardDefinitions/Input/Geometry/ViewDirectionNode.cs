using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ViewDirectionNode : IStandardNode
    {
        static string Name = "ViewDirection";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = ViewDir;",
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
            new ParameterDescriptor("ViewDir", TYPE.Vec3, GraphType.Usage.Static, REF.WorldSpace_ViewDirection)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a normalized vector from the current point to the camera position.",
            categories: new string[2] { "Input", "Geometry" },
            synonyms: new string[2] { "eye direction", "camera direction" },
            displayName: "View Direction",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "ViewDir",
                    options: REF.OptionList.ViewDirections
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "A normalized vector from the current point to the camera position."
                )
            }
        );
    }
}
