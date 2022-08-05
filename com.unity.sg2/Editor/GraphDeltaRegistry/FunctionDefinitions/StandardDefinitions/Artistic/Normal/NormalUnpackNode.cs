using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalUnpackNode : IStandardNode
    {
        public static string Name => "NormalUnpack";
        public static int Version => 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            functions: new FunctionDescriptor[] {
                new(
                    "Tangent",
                    "    Out = UnpackNormal(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                ),
                new(
                    "Object",
                    "    Out = UnpackNormal(In);",
                    new ParameterDescriptor[]
                    {
                        new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                        new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                    }
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "Converts a normal map texture to a normal vector.",
            category: "Artistic/Normal",
            synonyms: new string[2] { "expand", "vectorize" },
            displayName: "Normal Unpack",
            selectableFunctions: new()
            {
                { "Tangent", "Tangent" },
                { "Object", "Object" }
            },
            parameters: new ParameterUIDescriptor[2] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "RGBA value from a texture that is NOT stored in normal map format"
                ),
                new ParameterUIDescriptor(
                    name: "Out",
                    tooltip: "unpacked normal vector from the input texture value"
                )
            }
        );
    }
}
