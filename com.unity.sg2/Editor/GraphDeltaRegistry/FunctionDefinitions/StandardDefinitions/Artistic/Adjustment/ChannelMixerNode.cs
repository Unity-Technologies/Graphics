using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ChannelMixerNode : IStandardNode
    {
        public static string Name => "ChannelMixer";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            @"
{
    Out.r = dot(In, Red);
    Out.g = dot(In, Green);
    Out.b = dot(In, Blue);
}
",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vec3, GraphType.Usage.In),
                new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out),
                new ParameterDescriptor("Red", TYPE.Vec3, GraphType.Usage.Static, new float[] { 1.0f, 0.0f, 0.0f }),
                new ParameterDescriptor("Green", TYPE.Vec3, GraphType.Usage.Static, new float[] { 0.0f, 1.0f, 0.0f }),
                new ParameterDescriptor("Blue", TYPE.Vec3, GraphType.Usage.Static, new float[] { 0.0f, 0.0f, 1.0f }),
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Channel Mixer",
            tooltip: "Adjusts the input RGB value with weighted values.",
            category: "Artistic/Adjustment",
            synonyms: new string[] { },
            description: "pkg://Documentation~/previews/ChannelMixer.md",
            parameters: new ParameterUIDescriptor[] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "Input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "Output value"
                ),
                // UI for Red/Green/Blue vectors are intentionally left out because a custom editor is used.
            }
        );
    }
}
