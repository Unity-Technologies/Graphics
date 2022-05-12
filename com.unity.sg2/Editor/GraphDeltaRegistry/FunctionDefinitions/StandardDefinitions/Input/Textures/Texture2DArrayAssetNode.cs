using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class Texture2DArrayAssetNode : IStandardNode
    {
        public static string Name = "Texture2DArrayAsset";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "    Out = Asset;",
            new ParameterDescriptor("Asset", TYPE.Texture2DArray, Usage.Static),
            new ParameterDescriptor("Out", TYPE.Texture2DArray, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Defines a Texture 2D Array Asset.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[2] { "stack", "pile" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "A Texture 2D Array asset."
                )
            }
        );
    }
}
