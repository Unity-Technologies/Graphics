using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class ExponentialNode : IStandardNode
    {
        static string Name = "Exponential";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "BaseE",
                    "Out = exp(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                ),
                new(
                    1,
                    "Base2",
                    "Out = exp2(In);",
                    new ParameterDescriptor("In", TYPE.Vector, Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Calculates the exponential value of the input.",
            categories: new string[2] { "Math", "Advanced" },
            synonyms: new string[0] {  },
            selectableFunctions: new()
            {
                { "BaseE", "BaseE" },
                { "Base2", "Base2" }
            },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "the input value"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "exponential of the input"
                )
            }
        );
    }
}
