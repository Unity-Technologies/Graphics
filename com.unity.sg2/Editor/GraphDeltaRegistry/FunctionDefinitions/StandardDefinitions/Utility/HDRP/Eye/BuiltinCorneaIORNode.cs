using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class BuiltinCorneaIORNode : IStandardNode
    {
        public static string Name => "BuiltinCorneaIOR";
        public static int Version => 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Name,
@"    BuiltinCorneaIOR = BUILTIN_CORNEA_IOR;",
            new ParameterDescriptor[]
            {
                new ParameterDescriptor("BuiltinCorneaIOR", TYPE.Float, Usage.Out)
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Built In Cornea IOR",
            tooltip: "Brings in the index of refraction for the cornea",
            category: "Utility/HDRP/Eye",
            synonyms: new string[0],
            description: "pkg://Documentation~/previews/BuiltinCorneaIOR.md",
            hasPreview: false,
            parameters: new ParameterUIDescriptor[1] {
                new ParameterUIDescriptor(
                    name: "BuiltinCorneaIOR",
                    displayName: "Built In Cornea IOR",
                    tooltip: "the refractive index of the cornea"
                )
            }
        );
    }
}
