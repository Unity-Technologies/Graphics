using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ReflectionProbeNode : IStandardNode
    {
        static string Name = "ReflectionProbe";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "    Out = SHADERGRAPH_REFLECTION_PROBE(ViewDir, Normal, LOD);",
            new ParameterDescriptor("ViewDir", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_ViewDirection),
            new ParameterDescriptor("Normal", TYPE.Vec3, GraphType.Usage.In, REF.ObjectSpace_Normal),
            new ParameterDescriptor("LOD", TYPE.Float, GraphType.Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the sample from the nearest reflection probe.",
            categories: new string[2] { "Input", "Lighting" },
            hasPreview: false,
            synonyms: new string[3] { "light probe", "cube map", "environment" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "ViewDir",
                    displayName: "View Dir",
                    tooltip: "The mesh's view direction."
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "The mesh's normal vector."
                ),
                new ParameterUIDescriptor(
                    name: "LOD",
                    tooltip: "The level of detail for sampling."
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "The output color value."
                )
            }
        );
    }
}
