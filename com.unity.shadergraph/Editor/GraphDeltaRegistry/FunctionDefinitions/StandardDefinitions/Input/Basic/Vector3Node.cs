using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{

    internal class Vector3Node : IStandardNode
    {
        public static string Name = "Vector3";
        public static int Version = 1;

        public static FunctionDescriptor FunctionDescriptor => new(
            Version,
            Name,
@"
    Out.x = X;
    Out.y = Y;
    Out.z = Z;
",
            new ParameterDescriptor("X", TYPE.Float, Usage.In),
            new ParameterDescriptor("Y", TYPE.Float, Usage.In),
            new ParameterDescriptor("Z", TYPE.Float, Usage.In),
            new ParameterDescriptor("Out", TYPE.Vec3, Usage.Out)
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Vector 3",
            tooltip: "a user-defined value with 3 channels",
            categories: new string[2] { "Input", "Basic" },
            synonyms: new string[4] { "3", "v3", "vec3", "float3" },
            parameters: new ParameterUIDescriptor[4] {
                new ParameterUIDescriptor(
                    name: "X",
                    tooltip: "the first component"
                ),
                new ParameterUIDescriptor(
                    name: "Y",
                    tooltip: "the second component"
                ),
                new ParameterUIDescriptor(
                    name: "Z",
                    tooltip: "the third component"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "a user-defined value with 3 channels"
                )
            }
        );
    }
}
