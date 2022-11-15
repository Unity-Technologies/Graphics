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
                    @"    Hash_Tchou_2_1_float(In, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new (
                    "Hash23",
                    @"    Hash_Tchou_2_3_float(In, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new (
                    "Hash22",
                    @"    Hash_Tchou_2_2_float(In, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vec2, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vec2, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new (
                    "Hash31",
                    @"    Hash_Tchou_3_1_float(In, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    },
                    new string[]
                    {
                        "\"Packages/com.unity.render-pipelines.core/ShaderLibrary/Hashes.hlsl\""
                    }
                ),
                new(
                    "Hash33",
                    @"    Hash_Tchou_3_3_float(In, Out);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vec3, Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
                    }
                )
            }
        );
        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "",
            category: "Procedural/Noise",
            displayName: "Noise Hash",
            synonyms: new string[0] {  },
            selectableFunctions: new()
            {
                { "Hash21", "Vec2 to Float" },
                { "Hash23", "Vec2 to Vec3" },
                { "Hash22", "Vec2 to Vec2" },
                { "Hash31", "Vec3 to Float" },
                { "Hash33", "Vec3 to Vec3" }
            },
            functionSelectorLabel: "Hash Type",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "the hash seed value",
                    options: REF.OptionList.Positions
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the resulting hash"
                )
            }
        );
    }
}
