using System;
using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NoiseHashNode : IStandardNode
    {
        public static string Name => "NoiseHash";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "Hash21",
                    @"    Hash_Tchou_2_1_float(UV*Scale, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] {10.0f, 10.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new (
                    "Hash23",
                    @"    Hash_Tchou_2_3_float(UV*Scale, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] {10.0f, 10.0f}),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new (
                    "Hash22",
                    @"    Hash_Tchou_2_2_float(UV*Scale, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("UV", TYPE.Vec2, Usage.In, REF.UV0),
                        new ParameterDescriptor("Scale", TYPE.Vec2, Usage.In, new float[] {10.0f, 10.0f}),
                        new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new (
                    "Hash31",
                    @"    Hash_Tchou_3_1_float(Position*Scale, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] {10.0f, 10.0f, 10.0f}),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new(
                    "Hash33",
                    @"    Hash_Tchou_3_3_float(Position*Scale, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Position", TYPE.Vec3, Usage.In, REF.WorldSpace_Position),
                        new ParameterDescriptor("Scale", TYPE.Vec3, Usage.In, new float[] {10.0f, 10.0f, 10.0f}),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                    }
                )
            }
        );
        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "generates a random value based on the input",
            category: "Procedural/Noise",
            displayName: "Noise Hash",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/NoiseHash.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Hash21", "Vec2 to Float" },
                { "Hash23", "Vec2 to Vec3" },
                { "Hash22", "Vec2 to Vec2" },
                { "Hash31", "Vec3 to Float" },
                { "Hash33", "Vec3 to Vec3" }
            },
            hasModes: true,
            functionSelectorLabel: "Hash Type",
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "UV",
                    tooltip: "the hash seed value",
                    options: REF.OptionList.UVs
                ),
                new ParameterUIDescriptor(
                    name: "Position",
                    tooltip: "the hash seed value",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Scale",
                    tooltip: "controls the size of the hash"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the resulting hash"
                )
            }
        );
    }
}
