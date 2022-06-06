using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{//TODO: define checkings for preview shader
    internal class CustomRenderTextureSliceNode : IStandardNode
    {
        static string Name = "CustomRenderTextureSlice";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
TextureCubeFace = CustomRenderTextureCubeFaceRef;
Texture3DSlice = CustomRenderTexture3DSliceRef;
",
            new ParameterDescriptor("CustomRenderTextureCubeFaceRef", TYPE.Float, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("CustomRenderTexture3DSliceRef", TYPE.Float, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("TextureCubeFace", TYPE.Float, GraphType.Usage.Out),
            new ParameterDescriptor("Texture3DSlice", TYPE.Float, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Return the indexes of the current cubemap face or the current 3d slice.",
            categories: new string[] { "Utility", "Custom Render Texture" },
            synonyms: new string[0] {},
            displayName: "Slice Index / Cubemap Face",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "TextureCubeFace",
                    tooltip: "The index of the current cubemap face that Unity processes (-X, +X, -Y, +Y, -Z, +Z)."
                    ),
                new ParameterUIDescriptor(
                    name: "Texture3DSlice",
                    tooltip: "Index of the current 3D slice being processed."
                )
            }
        );
    }
}
