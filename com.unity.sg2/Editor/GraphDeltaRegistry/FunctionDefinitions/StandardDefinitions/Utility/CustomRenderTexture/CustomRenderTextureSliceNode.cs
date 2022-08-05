using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CustomRenderTextureSliceNode : IStandardNode
    {
        public static string Name => "CustomRenderTextureSlice";
        public static int Version => 1;
        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"
TextureCubeFace = CustomRenderTextureCubeFaceRef;
Texture3DSlice = CustomRenderTexture3DSliceRef;
",
            parameters: new ParameterDescriptor[]
            {
                new ParameterDescriptor("CustomRenderTextureCubeFaceRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
                new ParameterDescriptor("CustomRenderTexture3DSliceRef", TYPE.Float, GraphType.Usage.Local),//TODO: need to be a ref texture type
                new ParameterDescriptor("TextureCubeFace", TYPE.Float, GraphType.Usage.Out),
                new ParameterDescriptor("Texture3DSlice", TYPE.Float, GraphType.Usage.Out)
            }
        );
        //TODO: this is a target speciic node, need a validation message
        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Return the indexes of the current cubemap face or the current 3d slice.",
            category: "Utility/Custom Render Texture",
            synonyms: new string[0] {},
            displayName: "Slice Index / Cubemap Face",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "TextureCubeFace",
                    displayName:"Texture Cube Face",
                    tooltip: "The index of the current cubemap face that Unity processes (-X, +X, -Y, +Y, -Z, +Z)."
                    ),
                new ParameterUIDescriptor(
                    name: "Texture3DSlice",
                    displayName:"Texture 3D Slice",
                    tooltip: "Index of the current 3D slice being processed."
                )
            }
        );
    }
}
