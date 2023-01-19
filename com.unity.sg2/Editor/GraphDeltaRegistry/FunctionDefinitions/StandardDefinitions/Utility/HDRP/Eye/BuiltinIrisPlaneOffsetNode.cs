using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BuiltinIrisPlaneOffsetNode : IStandardNode
    {
        public static string Name => "BuiltinIrisPlaneOffset";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    BuiltinIrisPlaneOffset = BUILTIN_IRIS_PLANE_OFFSET;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("BuiltinIrisPlaneOffset", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Built In Iris Plane Offset",
            tooltip: "",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "BuiltinIrisPlaneOffset",
                    displayName: "Built In Iris Plane Offset",
                    tooltip: ""
                )
            }
        );
    }
}
