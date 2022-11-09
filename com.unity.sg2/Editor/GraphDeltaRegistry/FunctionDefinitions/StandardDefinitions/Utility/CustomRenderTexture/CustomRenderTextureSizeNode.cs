using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CustomRenderTextureSizeNode : IStandardNode
    {
        public static string Name => "CustomRenderTextureSize";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"
TextureWidth = CustomRenderTextureWidthRef;
TextureHeight = CustomRenderTextureHeightRef;
TextureDepth = CustomRenderTextureDepthRef;
",
            parameters: new ParameterDescriptor[]
            {
                new ParameterDescriptor("CustomRenderTextureHeightRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
                new ParameterDescriptor("CustomRenderTextureWidthRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
                new ParameterDescriptor("CustomRenderTextureDepthRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
                new ParameterDescriptor("TextureWidth", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("TextureHeight", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("TextureDepth", TYPE.Float, GraphType.Usage.Out)
            }
        );
        //TODO: this is a target speciic node, need a validation message
        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Return the width, height and depth of the custom texture.",
            category: "Utility/Custom Render Texture",
            displayName: "Custom Render Texture Size",
            synonyms: new string[0] { },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "TextureWidth",
                    displayName:"Texture Width",
                    tooltip: "Width of the Custom Texture in pixels."
                    ),
                new ParameterUIDescriptor(
                    name: "TextureHeight",
                    displayName:"Texture Height",
                    tooltip: "Height of the Custom Texture in pixels."
                    ),
                new ParameterUIDescriptor(
                    name: "TextureDepth",
                    displayName:"Texture Depth",
                    tooltip: "Depth of the Custom Texture in pixels (only for 3D textures, otherwise will always be equal to 1)."
                )
            }
        );
    }
}
