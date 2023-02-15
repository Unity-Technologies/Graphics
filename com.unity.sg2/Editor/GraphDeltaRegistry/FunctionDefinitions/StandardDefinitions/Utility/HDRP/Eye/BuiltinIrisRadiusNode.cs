using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BuiltinIrisRadiusNode : IStandardNode
    {
        public static string Name => "BuiltinIrisRadius";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    BuiltinIrisRadius = BUILTIN_IRIS_RADIUS;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("BuiltinIrisRadius", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Built In Iris Radius",
            tooltip: "",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            description: "pkg://Documentation~/previews/BuiltinIrisRadius.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "BuiltinIrisRadius",
                    displayName: "Built In Iris Radius",
                    tooltip: ""
                )
            }
        );
    }
}
