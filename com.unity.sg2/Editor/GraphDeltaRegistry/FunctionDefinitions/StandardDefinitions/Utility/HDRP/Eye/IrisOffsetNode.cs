using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class IrisOffsetNode : IStandardNode
    {
        public static string Name => "IrisOffset";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "    DisplacedIrisUV = (IrisUV + IrisOffset);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("IrisUV", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("IrisOffset", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("DisplacedIrisUV", TYPE.Vec2, Usage.Out),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Iris Offset",
            tooltip: "Applies an offset to the center of the Iris to mimic real-life eye structure.",
            category: "Utility/HDRP/Eye",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            description: "pkg://Documentation~/previews/IrisOffset.md",
            parameters: new ParameterUIDescriptor[3] {
                new ParameterUIDescriptor(
                    name: "IrisUV",
                    displayName: "Iris UV",
                    tooltip: "Normalized UV coordinates to sample a texture"
                ),
                new ParameterUIDescriptor(
                    name: "IrisOffset",
                    displayName: "Iris Offset",
                    tooltip: "Normalized value that defines on each axis the intensity of the offset of the Center of the pupil"
                ),
                new ParameterUIDescriptor(
                    name: "DisplacedIrisUV",
                    displayName: "Displaced Iris UV",
                    tooltip: "Normalized UV coordinates to sample an iris texture"
                )
            }
        );
    }
}
