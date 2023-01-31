using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class TextureSizeNode : IStandardNode
    {
        public static string Name => "TextureSize";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    Width = Texture.texelSize.z;
    Height = Texture.texelSize.w;
    TexelWidth = Texture.texelSize.x;
    TexelHeight = Texture.texelSize.y;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Texture", TYPE.Texture2D, Usage.In),
                new ParameterDescriptor("Width", TYPE.Float, Usage.Out),
                new ParameterDescriptor("Height", TYPE.Float, Usage.Out),
                new ParameterDescriptor("TexelWidth", TYPE.Float, Usage.Out),
                new ParameterDescriptor("TexelHeight", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets the dimensions of the texture in texels and also the dimensions of each texel in 0-1 UV space.",
            displayName: "Texture Size",
            category: "Input/Texture",
            synonyms: new string[1] { "texture size" },
            hasPreview: false,
            description: "pkg://Documentation~/previews/TextureSize.md",
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
