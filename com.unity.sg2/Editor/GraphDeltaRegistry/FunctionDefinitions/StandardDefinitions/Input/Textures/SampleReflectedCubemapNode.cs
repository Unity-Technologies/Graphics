using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class SampleRelfectedCubemapNode : IStandardNode
    {
        public static string Name => "SampleReflectedCubemap";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "LODfunction",
@"    RGBA = SAMPLE_TEXTURECUBE_LOD(Cube.tex, Sampler.samplerstate, reflect(-ViewDir, Normal), LOD);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Cube", TYPE.TextureCube, Usage.In),
                        new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.In, REF.ObjectSpace_ViewDirection),
                        new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In, REF.ObjectSpace_Normal),
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("LOD", TYPE.Float, Usage.In),
                        new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                        new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                        new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                    }
                ),
                new(
                    "Standard",
@"    RGBA = SAMPLE_TEXTURECUBE(Cube.tex, Sampler.samplerstate, reflect(-ViewDir, Normal));
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Cube", TYPE.TextureCube, Usage.In),
                        new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.In),//add default object space view dir
                        new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In),//add default object space normal
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                        new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                        new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                    }
                ),
                new(
                    "Biasfunction",
@"    RGBA = SAMPLE_TEXTURECUBE_BIAS(Cube.tex, Sampler.samplerstate, reflect(-ViewDir, Normal), Bias);
    RGB = RGBA.rgb;
    R = RGBA.r;
    G = RGBA.g;
    B = RGBA.b;
    A = RGBA.a;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Cube", TYPE.TextureCube, Usage.In),
                        new ParameterDescriptor("ViewDir", TYPE.Vec3, Usage.In),//add default object space view dir
                        new ParameterDescriptor("Normal", TYPE.Vec3, Usage.In),//add default object space normal
                        new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Bias", TYPE.Float, Usage.In),
                        new ParameterDescriptor("RGBA", TYPE.Vec4, Usage.Out),
                        new ParameterDescriptor("RGB", TYPE.Vec3, Usage.Out),//this is new.  Should we keep it?
                        new ParameterDescriptor("R", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("G", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("B", TYPE.Float, Usage.Out),
                        new ParameterDescriptor("A", TYPE.Float, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Samples a Cubemap with a reflection vector.",
            category: "Input/Texture",
            synonyms: new string[1] { "texcube" },
            displayName: "Sample Reflected Cubemap",
            selectableFunctions: new()
            {
                { "LODfunction", "LOD" },
                { "Standard", "Standard" },
                { "Biasfunction", "Bias" }
            },
            functionSelectorLabel: "Mip Sampling Mode",
            parameters: new ParameterUIDescriptor[12] {
                new ParameterUIDescriptor(
                    name: "Cube",
                    tooltip: "the cubemap asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "ViewDir",
                    tooltip: "the vector that points toward the camera"
                ),
                new ParameterUIDescriptor(
                    name: "Normal",
                    tooltip: "the surface normal of the model"
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    tooltip: "the sampler to use for sampling the cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "LOD",
                    tooltip: "the mip level to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Bias",
                    tooltip: "adds or substracts from the auto-generated mip level"
                ),
                new ParameterUIDescriptor(
                    name: "RGBA",
                    tooltip: "A vector4 from the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "RGB",
                    tooltip: "A vector3 from the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "R",
                    tooltip: "the red channel of the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "G",
                    tooltip: "the green channel of the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "B",
                    tooltip: "the blue channel of the sampled cubemap"
                ),
                new ParameterUIDescriptor(
                    name: "A",
                    tooltip: "the alpha channel of the sampled cubemap"
                )
            }
        );
    }
}
