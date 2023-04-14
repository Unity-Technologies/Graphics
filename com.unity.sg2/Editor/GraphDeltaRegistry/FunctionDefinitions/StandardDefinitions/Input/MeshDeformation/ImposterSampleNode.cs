using UnityEditor.ShaderGraph.GraphDelta;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ImposterSampleNode : IStandardNode
    {
        static string Name => "ImposterSampleNode";
        static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
                    Version,
                    Name,
                    "ImposterSample",
                    functions: new FunctionDescriptor[] {
                     new(
                    "ThreeFrames",
    @"ImposterSample(Parallax, Frames, Texture.tex, Texture.texelSize, BorderClamp, Grid, UV0, UV1, UV2, Sampler.samplerstate, RGBA);",
                new ParameterDescriptor[]
                {
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("UV0", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("UV1", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("UV2", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Grid", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Frames", TYPE.Float, Usage.In, new float[] {16f}),
                    new ParameterDescriptor("BorderClamp", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Parallax", TYPE.Bool, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out)
                },
                new string[]
                    {
                        "\"Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter_2Nodes.hlsl\""
                    }
                  ),
                     new(
                    "OneFrame",
    @"ImposterSample_oneFrame(Parallax, Frames, Texture.tex, Texture.texelSize, BorderClamp, Grid, UV0, Sampler.samplerstate, RGBA)",
                new ParameterDescriptor[]
                {
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("UV0", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Grid", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Frames", TYPE.Float, Usage.In),
                    new ParameterDescriptor("BorderClamp", TYPE.Vec3, Usage.In),
                    new ParameterDescriptor("Parallax", TYPE.Bool, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out)
                },
                new string[]
                    {
                        "\"Packages/com.unity.shadergraph/ShaderGraphLibrary/Imposter_2Nodes.hlsl\""
                    }
            )
        }
    );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Imposter Sample",
            tooltip: "Samples from the three virtual UVs and blends them base on the camera intersection point to get the correct result.",
            category: "Input/Mesh Deformation",
            hasPreview: false,
            description: "pkg://Documentation~/previews/ImposterSample.md",
            synonyms: new string[] { "billboard" },
            selectableFunctions: new()
        {
            { "ThreeFrames", "Three Frames" },
            { "OneFrame", "One Frame" }
        },
            functionSelectorLabel: "Sample Type",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "The texture asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    tooltip: "The texture sampler to use for sampling the texture"
                ),
                new ParameterUIDescriptor(
                    name: "UV0",
                    tooltip: "The virtual UV for the base frame"
                ),
                new ParameterUIDescriptor(
                    name: "UV1",
                    tooltip: "The virtual UV for the second frame"
                ),
                new ParameterUIDescriptor(
                    name: "UV2",
                    tooltip: "The virtual UV for the third frame"
                ),
                new ParameterUIDescriptor(
                    name: "Grid",
                    tooltip: "The current UV grid"
                ),
                new ParameterUIDescriptor(
                    name: "Frames",
                    tooltip: "The amount of the imposter frames"
                ),
                new ParameterUIDescriptor(
                    name: "BorderClamp",
                    displayName:"Border Clamp",
                    tooltip: "The amount of clamping for a single frame"
                ),
                new ParameterUIDescriptor(
                    name: "Parallax",
                    tooltip: "If Texture is a normal map, add parallax shif if the value is true"
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "A vector4 from the sampled texture"
                )
            }
        );
    }
}
