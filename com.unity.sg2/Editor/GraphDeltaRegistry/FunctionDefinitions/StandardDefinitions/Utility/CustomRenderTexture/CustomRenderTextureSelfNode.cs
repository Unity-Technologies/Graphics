using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
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
",//TODO: need to specify that this node is a fragmant only node
            new ParameterDescriptor("SelfTexture2DRef", TYPE.Vec4, GraphType.Usage.Local),//TODO: need to be a ref texture type
            new ParameterDescriptor("SelfTextureCubeRef", TYPE.Vec4, GraphType.Usage.Local),//TODO: need to be a ref texture type
            new ParameterDescriptor("SelfTexture3DRef", TYPE.Vec4, GraphType.Usage.Local),//TODO: need to be a ref texture type
            new ParameterDescriptor("SelfTexture2D", TYPE.Texture2D, GraphType.Usage.Out),
            new ParameterDescriptor("SelfTextureCube", TYPE.TextureCube, GraphType.Usage.Out),
            new ParameterDescriptor("SelfTexture3D", TYPE.Texture3D, GraphType.Usage.Out)
        );
        //TODO: this is a target speciic node, need a validation message
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
                    displayName:"Self Texture 2D",
                    tooltip: "a custom render texture 2D"
                    ),
                new ParameterUIDescriptor(
                    name: "SelfTextureCube",
                    displayName:"Self Texture Cube",
                    tooltip: "a custom render cubemap"
                    ),
                new ParameterUIDescriptor(
                    name: "SelfTexture3D",
                    displayName:"Self Texture 3D",
                    tooltip: "a custom render texture 3D"
                )
            }
        );
    }
}
