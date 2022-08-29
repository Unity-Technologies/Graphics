using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TimeNode : IStandardNode
    {
        public static string Name => "Time";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    Time = TimeParameters.x * Scale;
    SineTime = TimeParameters.y * Scale;
    CosineTime = TimeParameters.z * Scale;
    DeltaTime = unity_DeltaTime.x * Scale;
    SmoothDelta = unity_DeltaTime.z * Scale;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("TimeParameters", TYPE.Vec3, Usage.Local, REF.TimeParameters),
                new ParameterDescriptor("Scale", TYPE.Float, Usage.In, new float[] { 1.0f }),
                new ParameterDescriptor("Time", TYPE.Float, Usage.Out),
                new ParameterDescriptor("SineTime", TYPE.Float, Usage.Out),
                new ParameterDescriptor("CosineTime", TYPE.Float, Usage.Out),
                new ParameterDescriptor("DeltaTime", TYPE.Float, Usage.Out),
                new ParameterDescriptor("SmoothDelta", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a constantly increasing value used for animated effects.",
            category: "Input/Basic",
            synonyms: new string[1] { "delta" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[6] {
                new ParameterUIDescriptor(
                    name: "Time",
                    tooltip: "Time value"
                ),
                new ParameterUIDescriptor(
                    name: "Scale",
                    tooltip: "A multiplier that controls the change rate of Time or amplitude of SineTime and CosineTime"
                ),
                new ParameterUIDescriptor(
                    name: "SineTime",
                    displayName: "Sine Time",
                    tooltip: "Sine of Time value"
                ),
                new ParameterUIDescriptor(
                    name: "CosineTime",
                    displayName: "Cosine Time",
                    tooltip: "Cosine of Time value"
                ),
                new ParameterUIDescriptor(
                    name: "DeltaTime",
                    displayName: "Delta Time",
                    tooltip: "Current frame time"
                ),
                new ParameterUIDescriptor(
                    name: "SmoothDelta",
                    displayName: "Smooth Delta",
                    tooltip: "Current frame time smoothed"
                )
            }
        );
    }
}
