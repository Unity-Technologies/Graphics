using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class TexelSizeNode : IStandardNode
    {
        public static string Name = "TexelSize";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
{
//Width = Texture.texelSize.z;
//Height = Texture.texelSize.w;
//TexelWidth = Texture.texelSize.x;
//TexelHeight = Texture.texelSize.y;
Width = 1;
Height = 1;
TexelWidth = 1;
TexelHeight = 1;
}",
            new ParameterDescriptor("Texture", TYPE.Vec2, Usage.In),//need texture support
            new ParameterDescriptor("Width", TYPE.Float, Usage.Out),
            new ParameterDescriptor("Height", TYPE.Float, Usage.Out),
            new ParameterDescriptor("TexelWidth", TYPE.Float, Usage.Out),
            new ParameterDescriptor("TexelHeight", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the dimensions of the texture in texels and also the dimensions of each texel in 0-1 UV space.",
            displayName: "Texture Size",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[1] { "texture size" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Texture",
                    tooltip: "input texture asset"
                ),
                new ParameterUIDescriptor(
                    name: "Width",
                    tooltip: "input texture width"
                ),
                new ParameterUIDescriptor(
                    name: "Height",
                    tooltip: "input texture height"
                ),
                new ParameterUIDescriptor(
                    name: "TexelWidth",
                    displayName: "Texel Width",
                    tooltip: "texel width"
                ),
                new ParameterUIDescriptor(
                    name: "TexelHeight",
                    displayName: "Texel Height",
                    tooltip: "texel height"
                )
            }
        );
    }
}
