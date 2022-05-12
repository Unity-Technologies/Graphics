using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TimeNode : IStandardNode
    {
        public static string Name = "Time";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"Time = TimeParameters.x;
SineTime = TimeParameters.y;
CosineTime = TimeParameters.z;
DeltaTime = unity_DeltaTime.x;
SmoothDelta = unity_DeltaTime.z;",
            new ParameterDescriptor("TimeParameters", TYPE.Vec4, Usage.Local, defaultValue: REF.TimeParameters),
            new ParameterDescriptor("Time", TYPE.Float, Usage.Out),
            new ParameterDescriptor("SineTime", TYPE.Float, Usage.Out),
            new ParameterDescriptor("CosineTime", TYPE.Float, Usage.Out),
            new ParameterDescriptor("DeltaTime", TYPE.Float, Usage.Out),
            new ParameterDescriptor("SmoothDelta", TYPE.Float, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Gets a constantly increasing value used for animated effects.",
            categories: new string[2] { "Input", "Basic" },
            synonyms: new string[1] { "delta" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "Time",
                    tooltip: "Time value"
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
