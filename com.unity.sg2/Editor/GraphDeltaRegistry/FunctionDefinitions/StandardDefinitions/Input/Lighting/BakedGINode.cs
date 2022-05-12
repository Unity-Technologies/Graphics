using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BakedGINode : IStandardNode
    {
        static string Name = "BakedGI";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = SHADERGRAPH_BAKED_GI(Position, Normal, StaticUV, DynamicUV, ApplyLightmapScaling);",
            new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.In, defaultValue: REF.WorldSpace_Position),
            new ParameterDescriptor("Normal", TYPE.Vec3, GraphType.Usage.In, defaultValue: REF.WorldSpace_Normal),
            new ParameterDescriptor("StaticUV", TYPE.Vec2, GraphType.Usage.In, defaultValue: REF.UV1),
            new ParameterDescriptor("DynamicUV", TYPE.Vec2, GraphType.Usage.In, defaultValue: REF.UV2),
            new ParameterDescriptor("ApplyLightmapScaling", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the Baked GI values of an input mesh.",
            categories: new string[2] { "Input", "Lighting" },
            synonyms: new string[1] { "location" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "Mesh vertex/fragment's Position"
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "Mesh vertex/fragment's Normal"
                ),
                new ParameterUIDescriptor(
                    name: "StaticUV",
                    tooltip: "Lightmap coordinates for the static lightmap",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "DynamicUV",
                    tooltip: "Lightmap coordinates for the dynamic lightmap",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "ApplyLightmapScaling",
                    tooltip: "If enabled lightmaps are automatically scaled and offset."
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "baked GI color values sampled from a light probe or lightmap"
                )
            }
        );
    }
}
