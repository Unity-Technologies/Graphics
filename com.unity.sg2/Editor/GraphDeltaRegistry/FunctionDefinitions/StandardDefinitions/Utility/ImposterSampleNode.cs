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
    @"ImposterSample( Texture.tex, Weights, Grid, UV0, UV1, UV2, Sampler.samplerstate, RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
",
                new ParameterDescriptor[]
                {
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("UV0", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("UV1", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("UV2", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Grid", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Weights", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                },
                new string[]
                    {
                    "\"Packages/com.unity.sg2/Editor/GraphDeltaRegistry/FunctionDefinitions/StandardDefinitions/Utility/Imposter.hlsl\""
                    }
                  ),
                     new(
                    "OneFrame",//TODO: change back to one frame
    @"ImposterSample_oneFrame ( Texture.tex, Grid, UV0, Sampler.samplerstate, RGBA);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;
",
                new ParameterDescriptor[]
                {
                    new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                    new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                    new ParameterDescriptor("UV0", TYPE.Vec2, Usage.In),
                    new ParameterDescriptor("Grid", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Float, Usage.Out)                },
                new string[]
                    {
                    "\"Packages/com.unity.sg2/Editor/GraphDeltaRegistry/FunctionDefinitions/StandardDefinitions/Utility/Imposter.hlsl\""
                    }
            )
        }
    );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Imposter Sample",
            tooltip: "Samples from the three virtual UVs and blends them base on the camera intersection point to get the correct result.",
            category: "Utility",
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
                    name: "Weights",
                    tooltip: "Blending weights for three frames"
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "A vector4 from the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "RGB",
                    tooltip: "A vector3 from the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "R",
                    tooltip: "The red channel of the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "G",
                    tooltip: "The green channel of the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "The blue channel of the sampled texture"
                ),
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "The alpha channel of the sampled texture"
                )
            }
        );
    }
}
