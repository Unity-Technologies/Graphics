using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class NormalUnpackNode : IStandardNode
    {
        static string Name = "NormalUnpack";
        static int Version = 1;
        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Tangent",
@"
{
    Out = UnpackNormal(In);
}
",
                    new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                ),
                new(
                    1,
                    "Object",
@"
{
    Out = UnpackNormalRGB(In);
}
",
                    new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
                    new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            tooltip: "converts a normal map texture to a normal vector (usually not needed)",
            categories: new string[2] { "Artistic", "Normal" },
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
