using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ReciprocalNode : IStandardNode
    {
        public static string Name => "Reciprocal";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Default",
                    "    Out = 1.0/In;",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                ),
                new(
                    "Fast",
                    "    Out = rcp(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vector, Usage.In, new float[] { 1.0f, 1.0f, 1.0f, 1.0f }),
                        new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "returns one divided by the input",
            category: "Math/Advanced",
            synonyms: new string[2] { "rcp", "divide" },
            description: "pkg://Documentation~/previews/Reciprocal.md",
            selectableFunctions: new Dictionary<string, string>
            {
                { "Default", "Default" },
                { "Fast", "Fast" }
            },
            functionSelectorLabel: "Method",
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    displayName: string.Empty,
                    tooltip: "the input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    displayName: string.Empty,
                    tooltip: "one divided by the input"
                )
            }
        );
    }
}
