using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
    {
        internal class EvaluateWaterFoamDataNode : IStandardNode
        {
            public static string Name => "EvaluateWaterFoamData";
            public static int Version => 1;

            public static FunctionDescriptor FunctionDescriptor => new(
                Name,
    @"FoamData foamData;
ZERO_INITIALIZE(FoamData, foamData);
EvaluateFoamData(SurfaceGradient, LowFrequencySurfaceGradient, SimulationFoam, customFoam, UV0.xyz, foamData);
Smoothness = foamData.smoothness;

",
                new ParameterDescriptor[]
                {
                new ParameterDescriptor("UV0", TYPE.Vec4, Usage.Local, REF.UV0),
                new ParameterDescriptor("SurfaceGradient", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("LowFrequencySurfaceGradient", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("SimulationFoam", TYPE.Float, Usage.In),
                new ParameterDescriptor("CustomFoam", TYPE.Float, Usage.In),
                new ParameterDescriptor("SurfaceGradient", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("Foam", TYPE.Float, Usage.Out),
                new ParameterDescriptor("Smoothness", TYPE.Float, Usage.Out)
                }
            );

            public static NodeUIDescriptor NodeUIDescriptor => new(
                Version,
                Name,
                displayName: "Evaluate Water Foam Data",
                tooltip: "",
                category: "Utility/HDRP/Water",
                synonyms: new string[0],
                hasPreview: false,
                parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "NormalWS",
                    displayName: "NormalWS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencyNormalWS",
                    displayName: "Low Frequency NormalWS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "RefractedPositionWS",
                    displayName: "Refracted PositionWS",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DistordedWaterNDC",
                    displayName: "Distorded Water NDC",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "AbsorptionTint",
                    displayName: "Absorption Tint",
                    tooltip: ""
                )
                }
            );
        }
    }

