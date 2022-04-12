using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BranchNode : IStandardNode
    {
        public static string Name = "Branch";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
            "Out = Predicate ? True : False;",
            new ParameterDescriptor("Predicate", TYPE.Bool, Usage.In),
            new ParameterDescriptor("True", TYPE.Vector, Usage.In, new float[] { 1f, 1f, 1f, 1f}),
            new ParameterDescriptor("False", TYPE.Vector, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vector, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "provides a dynamic branch to the shader. Both sides of the branch will be evaluated",
            categories: new string[2] { "Utility", "Logic" },
            synonyms: new string[3] { "switch", "if", "else" },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "Predicate",
                    tooltip: "value of predicate"
                ),
                new ParameterUIDescriptor(
                    name: "True",
                    tooltip: "true branch"
                ),
                new ParameterUIDescriptor(
                    name: "False",
                    tooltip: "false branch"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "either the True branch or the False branch depending on the value of predicate"
                )
            }
        );
    }
}
