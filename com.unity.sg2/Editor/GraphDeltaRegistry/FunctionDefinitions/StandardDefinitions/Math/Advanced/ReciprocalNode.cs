using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ReciprocalNode : IStandardNode
    {
        static string Name = "Reciprocal";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Default",
                    "Out = 1.0/In;",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    1,
                    "Fast",
                    "Out = rcp(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns one divided by the input",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[2] { "rcp", "divide" },
            selectableFunctions: new()
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "the input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "one divided by the input"
                )
            }
        );
    }
}
