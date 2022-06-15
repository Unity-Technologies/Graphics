using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class CubemapAssetNode : IStandardNode
    {
        public static string Name = "CubemapAsset";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    Out = Asset;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("Asset", TYPE.TextureCube, Usage.Static),
                new ParameterDescriptor("Out", TYPE.TextureCube, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Defines a Cubemap Asset.",
            categories: new string[2] { "Input", "Texture" },
            synonyms: new string[1] { "environment" },
            displayName: "Cubemap Asset",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "A cubemap texture asset."
                )
            }
        );
    }
}
