using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Texture2DAssetNode : IStandardNode
    {
        public static string Name = "Texture2DAsset";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = Asset;",
            new ParameterDescriptor("Asset", TYPE.Texture2D, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Texture2D, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Defines a Texture 2D Asset.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[0] {  },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "A Texture 2D asset."
                )
            }
        );
    }
}
