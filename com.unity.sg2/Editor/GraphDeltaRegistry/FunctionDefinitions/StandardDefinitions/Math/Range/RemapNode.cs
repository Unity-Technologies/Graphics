using System;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class RemapNode : IStandardNode
    {
        public static string Name => "Remap";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
            "Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { -1f, -1f, -1f, -1f }),
                new ParameterDescriptor("InMinMax", TYPE.Vec2, Usage.In, new float[] { -1f, 1f }),
                new ParameterDescriptor("OutMinMax", TYPE.Vec2, Usage.In, new float[] { 0f, 1f }),
                new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates a new value for In. Uses the Out Min Max as a range and lerps between the In Min Max values.",
            category: "Math/Range",
            synonyms: Array.Empty<string>(),
            description: "pkg://Documentation~/previews/Remap.md",
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "input value"
                ),
                new ParameterUIDescriptor(
                    name: "InMinMax",
                    displayName: "In Min Max",
                    tooltip: "minimum and maximum values for input interpolation"
                ),
                new ParameterUIDescriptor(
                    name: "OutMinMax",
                    displayName: "Out Min Max",
                    tooltip: "minimum and maximum values for output interpolation"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "the input value with it's range remapped to the Out Min Max values"
                )
            }
        );
    }
}
