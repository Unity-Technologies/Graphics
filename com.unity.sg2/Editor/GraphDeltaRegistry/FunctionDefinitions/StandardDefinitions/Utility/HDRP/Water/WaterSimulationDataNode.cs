using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class WaterSimulationDataNode : IStandardNode
    {
        public static string Name => "WaterSimulationData";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"WaterAdditionalData waterAdditionalData;
ZERO_INITIALIZE(WaterAdditionalData, waterAdditionalData);
EvaluateWaterAdditionalData(UV0.xzy, IN.WorldSpacePosition, IN.WorldSpaceNormal, waterAdditionalData);
NormalWS = waterAdditionalData.normalWS;
LowFrequencySurfaceGradient = waterAdditionalData.lowFrequencySurfaceGradient;
SurfaceFoam = waterAdditionalData.surfaceFoam;
DeepFoam = waterAdditionalData.deepFoam;
",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("UV0", TYPE.Vec4, Usage.Local, REF.UV0),
                new ParameterDescriptor("NormalWS", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("LowFrequencySurfaceGradient", TYPE.Vec3, Usage.Out),
                new ParameterDescriptor("DeepFoam", TYPE.Float, Usage.Out),
                new ParameterDescriptor("SurfaceFoam", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Water Simulation Data",
            tooltip: "",
            category: "Utility/HDRP/Water",
            description: "pkg://Documentation~/previews/EvaluateWaterSimulationAdditionalData.md",
            synonyms: Array.Empty<string>(),
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "SurfaceGradient",
                    displayName: "Surface Gradient",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "LowFrequencySurfaceGradient",
                    displayName: "Low Frequency Surface Gradient",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "SurfaceFoam",
                    displayName: "Surface Foam",
                    tooltip: ""
                ),
                new ParameterUIDescriptor(
                    name: "DeepFoam",
                    displayName: "Deep Foam",
                    tooltip: ""
                )
            }
        );
    }
}
