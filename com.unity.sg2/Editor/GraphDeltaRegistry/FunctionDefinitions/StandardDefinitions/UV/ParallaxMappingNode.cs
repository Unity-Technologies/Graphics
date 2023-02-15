using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ParallaxMappingNode : IStandardNode
    {
        public static string Name = "ParallaxMapping";
        public static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            "Red",
            functions: new FunctionDescriptor[] {
                new(
                    "Red",
                    "    ParallaxUVs = Heightmap.GetTransformedUV(UVs) + ParallaxMappingChannel(TEXTURE2D_ARGS(Heightmap.tex, HeightmapSampler.samplerstate), ViewDirectionTS, Amplitude * 0.01, Heightmap.GetTransformedUV(UVs), channel);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                        new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                        new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {0}),
                        new ParameterDescriptor("ViewDirectionTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl\""
                    }
                ),
                new(
                    "Green",
                    "    ParallaxUVs = Heightmap.GetTransformedUV(UVs) + ParallaxMappingChannel(TEXTURE2D_ARGS(Heightmap.tex, HeightmapSampler.samplerstate), ViewDirectionTS, Amplitude * 0.01, Heightmap.GetTransformedUV(UVs), channel);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                        new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                        new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {1f}),
                        new ParameterDescriptor("ViewDirectionTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl\""
                    }
                ),
                new(
                    "Blue",
                    "    ParallaxUVs = Heightmap.GetTransformedUV(UVs) + ParallaxMappingChannel(TEXTURE2D_ARGS(Heightmap.tex, HeightmapSampler.samplerstate), ViewDirectionTS, Amplitude * 0.01, Heightmap.GetTransformedUV(UVs), channel);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                        new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                        new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {2f}),
                        new ParameterDescriptor("ViewDirectionTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl\""
                    }
                ),
                new(
                    "Alpha",
                    "    ParallaxUVs = Heightmap.GetTransformedUV(UVs) + ParallaxMappingChannel(TEXTURE2D_ARGS(Heightmap.tex, HeightmapSampler.samplerstate), ViewDirectionTS, Amplitude * 0.01, Heightmap.GetTransformedUV(UVs), channel);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Heightmap", TYPE.Texture2D, Usage.In),
                        new ParameterDescriptor("HeightmapSampler", TYPE.SamplerState, Usage.In),
                        new ParameterDescriptor("Amplitude", TYPE.Float, Usage.In, new float[] {1}),
                        new ParameterDescriptor("UVs", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("ParallaxUVs", TYPE.Vec2, Usage.Out),
                        new ParameterDescriptor("channel", TYPE.Int, Usage.Local, new float[] {3f}),
                        new ParameterDescriptor("ViewDirectionTS", TYPE.Vec3, Usage.Local, REF.TangentSpace_ViewDirection)
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl\""
                    }
                )
            }
        );
        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Parallax Mapping",
            tooltip: "creates a parallax effect that displaces a Material's UVs to create the illusion of depth",
            category: "UV",
            synonyms: new string[1] { "offset mapping" },
            hasPreview: false,
            description: "pkg://Documentation~/previews/ParallaxMapping.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Red", "Red" },
                { "Green", "Green" },
                { "Blue", "Blue" },
                { "Alpha", "Alpha" }
            },
            functionSelectorLabel: "Heightmap Sample Channel",
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Heightmap",
                    tooltip: "the texture that specifies the depth of the displacement"
                ),
                new ParameterUIDescriptor(
                    name: "HeightmapSampler",
                    displayName:"Heightmap Sampler",
                    tooltip: "the sampler to sample Heightmap with"
                ),
                new ParameterUIDescriptor(
                    name: "Amplitude",
                    tooltip: "a multiplier to apply to the height of the Heightmap (in centimeters)"
                ),
                new ParameterUIDescriptor(
                    name: "UVs",
                    tooltip: "the UVs that the sampler uses to sample the texture",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "ParallaxUVs",
                    displayName:"Parallax UVs",
                    tooltip: "the UVs after adding the parallax offset"
                )
            }
        );
    }
}
