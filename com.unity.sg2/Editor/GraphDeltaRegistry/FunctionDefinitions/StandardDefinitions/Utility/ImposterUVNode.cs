using UnityEditor.ShaderGraph.GraphDelta;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ImposterUVNode : IStandardNode
    {
        static string Name => "ImposterUVNode";
        static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            "ImposteUV",
            functions: new FunctionDescriptor[] {
                     new(
            "ThreeFrames",
"  ImposterUV(Pos, inUV, Frames, Offset, Size, FrameClip, HemiSphere, Parallax,  HeightMapChannel, " +
                         " Sampler.samplerstate, Texture.tex, TextureSize, OutPos, Weights, UV0, UV1, UV2, Grid);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Pos", TYPE.Vec3, Usage.In, REF.ObjectSpace_Position),
                new ParameterDescriptor("inUV", TYPE.Vec2, Usage.In, REF.UV0),
                new ParameterDescriptor("Frames", TYPE.Int, Usage.In, new float[] {16}),
                new ParameterDescriptor("Size", TYPE.Float, Usage.In, new float[] {1}),
                new ParameterDescriptor("Offset", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("FrameClip", TYPE.Float, Usage.In),
                new ParameterDescriptor("TextureSize", TYPE.Int, Usage.In, new float[]{ 1024}),
                new ParameterDescriptor("HemiSphere", TYPE.Bool, Usage.In),
                new ParameterDescriptor("Parallax", TYPE.Float, Usage.In),
                new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                new ParameterDescriptor("HeightMapChannel", TYPE.Int, Usage.In, 3),//TODO: this one should be a dropdown with r/g/b/a chnnal options like Parallax Mapping node
                new ParameterDescriptor("OutPos", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("UV0", TYPE.Vec2, Usage.Out),
                new ParameterDescriptor("UV1", TYPE.Vec2, Usage.Out),
                new ParameterDescriptor("UV2", TYPE.Vec2, Usage.Out),
                new ParameterDescriptor("Grid", TYPE.Vec4, Usage.Out),
                new ParameterDescriptor("Weights", TYPE.Vec4, Usage.Out)
            },
            new string[]
                {
                    "\"Packages/com.unity.sg2/Editor/GraphDeltaRegistry/FunctionDefinitions/StandardDefinitions/Utility/Imposter.hlsl\""
                }
                ),new(
            "OneFrame",
"  ImposterUV_oneFrame(Pos, inUV, Frames, Offset, Size, FrameClip, HemiSphere, Parallax, HeightMapChannel, " +
                         " Sampler.samplerstate, Texture.tex, TextureSize, OutPos, UV0, Grid);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Pos", TYPE.Vec3, Usage.In, REF.ObjectSpace_Position),
                new ParameterDescriptor("inUV", TYPE.Vec2, Usage.In, REF.UV0),
                new ParameterDescriptor("Frames", TYPE.Int, Usage.In, new float[] {16}),
                new ParameterDescriptor("Size", TYPE.Float, Usage.In, new float[] {1}),
                new ParameterDescriptor("Offset", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("FrameClip", TYPE.Float, Usage.In),
                new ParameterDescriptor("TextureSize", TYPE.Int, Usage.In, new float[]{ 1024}),
                new ParameterDescriptor("HemiSphere", TYPE.Bool, Usage.In),
                new ParameterDescriptor("Parallax", TYPE.Float, Usage.In),
                new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                new ParameterDescriptor("Sampler", TYPE.SamplerState, Usage.In),
                new ParameterDescriptor("HeightMapChannel", TYPE.Int, Usage.In, 3),//TODO: this one should be a dropdown with r/g/b/a chnnal options like Parallax Mapping node
                new ParameterDescriptor("OutPos", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("UV0", TYPE.Vec2, Usage.Out),
                new ParameterDescriptor("Grid", TYPE.Vec4, Usage.Out)
            },
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
            displayName: "Imposter UV",
            tooltip: "Calculates the billboard positon and the virtual UVs for sampling.",
            category: "Utility",
            hasPreview: false,
            description: "pkg://Documentation~/previews/ImposterUV.md",
            synonyms: new string[] { "billboard" },
            selectableFunctions: new()
        {
            { "ThreeFrames", "Three Frames" },
            { "OneFrame", "One Frame" }
        },
            functionSelectorLabel: "Sample Type",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "Pos",
                    displayName:"In Position",
                    options: REF.OptionList.Positions,
                    tooltip: "The postiont in Object space"
                ),
                new ParameterUIDescriptor(
                    name: "inUV",
                    displayName:"In UV",
                    options: REF.OptionList.UVs,
                    tooltip: "The UV coordinates of the mesh"
                ),
                new ParameterUIDescriptor(
                    name: "Frames",
                    tooltip: "The amount of the imposter frames"
                ),
                new ParameterUIDescriptor(
                    name: "Offest",
                    tooltip: "The offset value from the origin vertex positon"
                ),
                new ParameterUIDescriptor(
                    name: "FrameClip",
                    displayName:"Frame Clipping Threshold",
                    tooltip: "The value to clamp between imposter frame. Useful when doing parallax mapping."
                ),
                new ParameterUIDescriptor(
                    name: "TextureSize",
                    displayName:"Texture Size",
                    tooltip: "The resolution of the sampling texture."
                ),
                new ParameterUIDescriptor(
                    name: "Size",
                    tooltip: "The size of the imposter"
                ),
                new ParameterUIDescriptor(
                    name: "Hemisphere",
                    tooltip: "If it's true, calculate imposter grid and UVs base on hemisphere type."
                ),
                new ParameterUIDescriptor(
                    name: "Texture",
                    displayName:"Heightmap",
                    tooltip: "The texture asset to sample"
                ),
                new ParameterUIDescriptor(
                    name: "Sampler",
                    displayName:"Heightmap Sampler",
                    tooltip: "The texture sampler to use for sampling the texture"
                ),
                new ParameterUIDescriptor(
                    name: "Parallax",
                    tooltip: "Adds a parallax effect if the port value is greater than zero"
                ),
                new ParameterUIDescriptor(
                    name: "HeightMapChannel",
                    displayName:"Heighmap Sample Channel",
                    tooltip: "The texture channel to sample from for the parallax effect"
                ),
                new ParameterUIDescriptor(
                    name: "OutPos",
                    displayName:"Out Position",
                    tooltip: "The output billboard position."
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
                    name: "Weights",
                    tooltip: "Blending weights for three frames"
                ),
                new ParameterUIDescriptor(
                    name: "Grid",
                    tooltip: "The current UV grid"
                )
            }
        );
    }
}
