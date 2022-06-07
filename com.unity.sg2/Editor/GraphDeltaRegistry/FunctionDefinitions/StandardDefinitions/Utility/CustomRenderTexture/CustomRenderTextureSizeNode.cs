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
TextureWidth = CustomRenderTextureWidthRef;
TextureHeight = CustomRenderTextureHeightRef;
TextureDepth = CustomRenderTextureDepthRef;
",
            new ParameterDescriptor("CustomRenderTextureHeightRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
            new ParameterDescriptor("CustomRenderTextureWidthRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
            new ParameterDescriptor("CustomRenderTextureDepthRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
            new ParameterDescriptor("TextureWidth", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("TextureHeight", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("TextureDepth", TYPE.Float, GraphType.Usage.Out)
        );
        //TODO: this is a target speciic node, need a validation message
        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Return the width, height and depth of the custom texture.",
            categories: new string[] { "Utility", "Custom Render Texture" },
            synonyms: new string[0] { },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "TextureHeight",
                    displayName:"Texture Height",
                    tooltip: "Height of the Custom Texture in pixels."
                    ),
                new ParameterUIDescriptor(
                    name: "TextureWidth",
                    displayName:"Texture Width",
                    tooltip: "Width of the Custom Texture in pixels."
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
