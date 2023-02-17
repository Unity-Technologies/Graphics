using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class WaterTipThicknessNode : IStandardNode
    {
        public static string Name => "WaterTipThickness";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"TipThickness = EvaluateTipThickness(ViewWS, LowFrequencyNormalWS, LowFrequencyHeight);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("ViewWS", TYPE.Vec3, Usage.Local, REF.WorldSpace_ViewDirection),
                new ParameterDescriptor("LowFrequencyNormalWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("LowFrequencyHeight", TYPE.Float, Usage.In),
                new ParameterDescriptor("TipThickness", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Water Tip Thickness",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/WaterTipThickness.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "LowFrequencyNormalWS",
                    displayName: "Low Frequency Normal WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencyHeight",
                    displayName: "Low Frequency Height",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "TipThickness",
                    displayName: "Tip Thickness",
                    tooltip: ""
                )
            }
        );
    }
}
