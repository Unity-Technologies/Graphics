using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class EvaluateWaterSimulationAdditionalDataNode : IStandardNode
    {
        public static string Name => "EvaluateWaterSimulationAdditionalData";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"WaterAdditionalData waterAdditionalData;
ZERO_INITIALIZE(WaterAdditionalData, waterAdditionalData);
EvaluateWaterAdditionalData(UV0.xyz, BandsMutliplier, waterAdditionalData);
SurfaceGradient = waterAdditionalData.surfaceGradient;
LowFrequencySurfaceGradient = waterAdditionalData.lowFrequencySurfaceGradient;
SurfaceFoam = waterAdditionalData.surfaceFoam;
DeepFoam = waterAdditionalData.deepFoam;
",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("UV0", TYPE.Vec4, Usage.Local, REF.UV0),
                new ParameterDescriptor("BandsMutliplier", TYPE.Vec4, Usage.In, new float[] { 1f, 1f, 1f, 1f}),
                new ParameterDescriptor("SurfaceGradient", TYPE.Vec3, Usage.Local),
                new ParameterDescriptor("LowFrequencySurfaceGradient", TYPE.Vec3, Usage.In),
                new ParameterDescriptor("SurfaceFoam", TYPE.Vec2, Usage.In),
                new ParameterDescriptor("DeepFoam", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Evaluate Water Simulation Additional Data",
            tooltip: "",
            category: "Utility/HDRP/Water",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "BandsMutliplier",
                    displayName: "Bands Mutliplier",
                    tooltip: ""
                ),
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
