using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class WaterScatteringDataNode : IStandardNode
    {
        public static string Name => "WaterScatteringData";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"ScatteringData scatteringData;
ZERO_INITIALIZE(ScatteringData, scatteringData);
EvaluateScatteringData(PositionWS, NormalWS, LowFrequencyNormalWS, ScreenPosition, ViewWS, LowFrequencyHeight, HorizontalDisplacement, ScatteringFoam, scatteringData);
ScatteringColor = scatteringData.scatteringColor;
RefractionColor = scatteringData.refractionColor;
TipThickness = scatteringData.tipThickness;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("NormalWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("LowFrequencyNormalWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("PositionWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ScreenPosition", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("ViewWS", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("ScatteringFoam", TYPE.Float, Usage.In),
                new ParameterDescriptor("LowFrequencyHeight", TYPE.Float, Usage.In),
                new ParameterDescriptor("HorizontalDisplacement", TYPE.Float, Usage.In),
                new ParameterDescriptor("ScatteringColor", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("RefractionColor", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("TipThickness", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Water Scattering Data",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/EvaluateWaterScatteringData.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "NormalWS",
                    displayName: "Normal WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencyNormalWS",
                    displayName: "Low Frequency Normal WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "PositionWS",
                    displayName: "Position WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ScreenPosition",
                    displayName: "Screen Position",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ViewWS",
                    displayName: "View WS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ScatteringFoam",
                    displayName: "Scattering Foam",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencyHeight",
                    displayName: "Low Frequency Height",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "HorizontalDisplacement",
                    displayName: "Horizontal Displacement",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "ScatteringColor",
                    displayName: "Scattering Color",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "RefractionColor",
                    displayName: "Refraction Color",
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
