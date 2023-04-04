using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ExposureNode : IStandardNode
    {
        public static string Name => "Exposure";
        public static int Version => 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new (
                    "CurrentMultiplier",
//none of these function calls are found
                    "   Out = GetCurrentExposureMultiplier();",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new (
                    "InverseCurrentMultiplier",
                    "   Out = GetPreviousExposureMultiplier();",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new (
                    "PreviousMultiplier",
                    "   Out = GetInverseCurrentExposureMultiplier();",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
                new (
                    "InversePreviousMultiplier",
                    "   Out = GetInversePreviousExposureMultiplier();",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("Out", TYPE.Float, Usage.Out),
                    }
                ),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Exposure",
            tooltip: "Gets the camera's exposure value from the current or previous frame.",
            category: "Input/HDRP",
            synonyms: new string[1] { "fstop" },
            description: "pkg://Documentation~/previews/Exposure.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "CurrentMultiplier", "Current Multiplier" },
                { "InverseCurrentMultiplier", "Inverse Current Multiplier" },
                { "PreviousMultiplier", "Previous Multiplier" },
                { "InversePreviousMultiplier", "Inverse Previous Multiplier" }
            },
            functionSelectorLabel: "Type",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "the camera's exposure value"
                )
            }
        );
    }
}
