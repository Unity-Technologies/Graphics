using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{//TODO: define checkings for preview shader
    internal class CustomRenderTextureSizeNode : IStandardNode
    {
        static string Name = "CustomRenderTextureSize";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
TextureHeight = CustomRenderTextureHeightRef;
TextureDepth = CustomRenderTextureDepthRef;
TextureWidth = CustomRenderTextureWidthRef;
",
            new ParameterDescriptor("CustomRenderTextureHeightRef", TYPE.Float, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("CustomRenderTextureWidthRef", TYPE.Float, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("TextureDepthRef", TYPE.Float, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("TextureWidth", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("TextureHeight", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("TextureDepth", TYPE.Float, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Return the height, depth and width of the custom texture.",
            categories: new string[] { "Utility", "Custom Render Texture" },
            synonyms: new string[0] { },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "TextureHeight",
                    tooltip: "Height of the Custom Texture in pixels."
                    ),
                new ParameterUIDescriptor(
                    name: "TextureWidth",
                    tooltip: "Width of the Custom Texture in pixels."
                    ),
                new ParameterUIDescriptor(
                    name: "TextureDepth",
                    tooltip: "Depth of the Custom Texture in pixels (only for 3D textures, otherwise will always be equal to 1)."
                )
            }
        );
    }
}
