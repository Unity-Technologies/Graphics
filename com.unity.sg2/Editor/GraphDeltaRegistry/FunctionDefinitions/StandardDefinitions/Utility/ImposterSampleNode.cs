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
    @"ImposterSample( Frames, Texture.tex, Texture.texelSize, Weights, Clip, Grid, UV0, UV1, UV2, Sampler.samplerstate, RGBA);
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
                    new ParameterDescriptor("UV0", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("UV1", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("UV2", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Grid", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Weights", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Frames", TYPE.Float, Usage.In, new float[] {16f}),
                    new ParameterDescriptor("Clip", TYPE.Float, Usage.Static, new float[] {0.1f}),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("R", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Vec3, Usage.Out)
                },
                new string[]
                    {
                    "\"Packages/com.unity.sg2/Editor/GraphDeltaRegistry/FunctionDefinitions/StandardDefinitions/Utility/Imposter.hlsl\""
                    }
                  ),
                     new(
                    "OneFrame",//TODO: change back to one frame
    @"ImposterSample_oneFrame (Frames, Texture.tex, Texture.texelSize, Clip, Grid, UV0, Sampler.samplerstate, RGBA);
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
                    new ParameterDescriptor("UV0", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Grid", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Weights", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Frames", TYPE.Float, Usage.In, new float[] {16f}),
                    new ParameterDescriptor("Clip", TYPE.Float, Usage.Static, new float[] {0.1f}),
                    new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                    new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("R", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("G", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("B", TYPE.Vec3, Usage.Out),
                    new ParameterDescriptor("A", TYPE.Vec3, Usage.Out)                },
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
                    name: "Weights",
                    tooltip: "Blending weights for three frames"
                ),
                new ParameterUIDescriptor(
                    name: "Clip",
                    displayName:"Imposter Frame Clip",
                    tooltip: "The value to clamp between imposter frame. Useful when doing parallax mapping."
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "A vector4 from the sampled texture"
                )
            }
        );
    }
}
