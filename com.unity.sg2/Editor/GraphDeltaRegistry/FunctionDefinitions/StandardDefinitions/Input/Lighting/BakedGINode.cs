using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BakedGINode : IStandardNode
    {
        public static string Name => "BakedGI";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = SHADERGRAPH_BAKED_GI(Position, Normal, StaticUV, DynamicUV, ApplyLightmapScaling);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Position", TYPE.Vec3, GraphType.Usage.In, REF.WorldSpace_Position),
                new ParameterDescriptor("Normal", TYPE.Vec3, GraphType.Usage.In, REF.WorldSpace_Normal),
                new ParameterDescriptor("StaticUV", TYPE.Vec2, GraphType.Usage.In, REF.UV1),
                new ParameterDescriptor("DynamicUV", TYPE.Vec2, GraphType.Usage.In, REF.UV2),
                new ParameterDescriptor("ApplyLightmapScaling", TYPE.Bool, GraphType.Usage.Static, new float[] { 1.0f }),
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the Baked GI values of an input mesh.",
            category: "Input/Lighting",
            synonyms: new string[1] { "location" },
            displayName: "Baked GI",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "Mesh vertex/fragment's Position",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "Mesh vertex/fragment's Normal",
                    options: REF.OptionList.Normals
                ),
                new ParameterUIDescriptor(
                    name: "StaticUV",
                    tooltip: "Lightmap coordinates for the static lightmap",
                    displayName: "Static UV",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "DynamicUV",
                    tooltip: "Lightmap coordinates for the dynamic lightmap",
                    displayName: "Dynamic UV",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "ApplyLightmapScaling",
                    displayName: "Apply Lightmap Scaling",
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
