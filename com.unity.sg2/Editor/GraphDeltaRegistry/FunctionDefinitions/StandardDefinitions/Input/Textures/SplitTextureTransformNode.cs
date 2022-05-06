using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class SplitTextureTransformNode : IStandardNode
    {
        public static string Name = "SplitTextureTransform";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
    TextureOnly = In;
    TextureOnly.scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f);
    Tiling = In.scaleTranslate.xy;
    Offset = In.scaleTranslate.zw;
}",
            new ParameterDescriptor("In", TYPE.Texture2D, Usage.In),
            new ParameterDescriptor("Tiling", TYPE.Vec2, Usage.Out),
            new ParameterDescriptor("Offset", TYPE.Vec2, Usage.Out),
            new ParameterDescriptor("TextureOnly", TYPE.Texture2D, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Separates tiling, offset, and texture data from an input texture.",
            synonyms: new string[0] {},
            categories: new string[2] { "Input", "Texture" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "the texture input."
                ),
                new ParameterUIDescriptor(
                    name: "Tiling",
                    tooltip: "amount of tiling to apply per channel"
                ),
                new ParameterUIDescriptor(
                    name: "Offset",
                    tooltip: "amount of offset to apply per channel"
                ),
                new ParameterUIDescriptor(
                    name: "TextureOnly",
                    tooltip: "the input Texture2D, without tiling and offset data."
                )
            }
        );
    }
}
