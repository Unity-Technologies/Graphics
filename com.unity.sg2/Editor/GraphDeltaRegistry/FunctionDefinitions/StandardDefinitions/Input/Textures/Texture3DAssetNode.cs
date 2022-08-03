using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Texture3DAssetNode : IStandardNode
    {
        public static string Name => "Texture3DAsset";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = Asset;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Asset", TYPE.Texture3D, Usage.Static),
                new ParameterDescriptor("Out", TYPE.Texture3D, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Defines a Texture 3D Asset.",
            category: "Input/Texture",
            synonyms: new string[1] { "volume" },
            displayName: "Texture 3D Asset",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "A Texture 3D asset."
                )
            }
        );
    }
}
