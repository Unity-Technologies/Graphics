using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{//TODO: define checkings for preview shader
    internal class CustomRenderTextureSelfNode : IStandardNode
    {
        static string Name = "CustomRenderTextureSelf";
        static int Version = 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
SelfTexture2D = SelfTexture2DRef;
SelfTextureCube = SelfTextureCubeRef;
SelfTexture3D = SelfTexture3DRef;
",
            new ParameterDescriptor("SelfTexture2DRef", TYPE.Vec4, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("SelfTextureCubeRef", TYPE.Vec4, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("SelfTexture3DRef", TYPE.Vec4, GraphType.Usage.Static),//TODO: need to be a ref texture type
            new ParameterDescriptor("SelfTexture2D", TYPE.Vec4, GraphType.Usage.Out),
            new ParameterDescriptor("SelfTextureCube", TYPE.Vec4, GraphType.Usage.Out),
            new ParameterDescriptor("SelfTexture3D", TYPE.Vec4, GraphType.Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Return a custom render texture.",
            categories: new string[] { "Utility", "Custom Render Texture" },
            synonyms: new string[0] { },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "SelfTexture2D",
                    tooltip: "a custom render texture 2D"
                    ),
                new ParameterUIDescriptor(
                    name: "SelfTextureCube",
                    tooltip: "a custom render cubemap"
                    ),
                new ParameterUIDescriptor(
                    name: "SelfTexture3D",
                    tooltip: "a custom render texture 3D"
                )
            }
        );
    }
}
