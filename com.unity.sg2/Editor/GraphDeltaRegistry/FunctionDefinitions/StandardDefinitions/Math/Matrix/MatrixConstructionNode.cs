using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MatrixConstructionNode : IStandardNode
    {
        public static string Name = "MatrixConstruction";
        public static int Version = 1;

        public static NodeDescriptor NodeDescriptor => new(
            Version,
            Name,
            new FunctionDescriptor[] {
                new(
                    1,
                    "Row",
@"
{
    Out4x4[0] = M0;
    Out4x4[1] = M1;
    Out4x4[2] = M2;
    Out4x4[3] = M3;
    Out3x3[0] = M0.xyz;
    Out3x3[1] = M1.xyz;
    Out3x3[2] = M2.xyz;
    Out2x2[0] = M0.xy;
    Out2x2[1] = M1.xy;
}
",
                    new ParameterDescriptor("M0", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("M1", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("M2", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("M3", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Out4x4", TYPE.Mat4, Usage.Out),
                    new ParameterDescriptor("Out3x3", TYPE.Mat3, Usage.Out),
                    new ParameterDescriptor("Out2x2", TYPE.Mat2, Usage.Out)
                ),
                new(
                    1,
                    "Column",
@"
{
    Out4x4[0].x = M0.x; Out4x4[0].y = M1.x; Out4x4[0].z = M2.x; Out4x4[0].w = M3.x;
    Out4x4[1].x = M0.y; Out4x4[1].y = M1.y; Out4x4[1].z = M2.y; Out4x4[1].w = M3.y;
    Out4x4[2].x = M0.z; Out4x4[2].y = M1.z; Out4x4[2].z = M2.z; Out4x4[2].w = M3.z;
    Out4x4[3].x = M0.w; Out4x4[3].y = M1.w; Out4x4[3].z = M2.w; Out4x4[3].w = M3.w;
    Out3x3[0].x = M0.x; Out3x3[0].y = M1.x; Out3x3[0].z = M2.x;
    Out3x3[1].x = M0.y; Out3x3[1].y = M1.y; Out3x3[1].z = M2.y;
    Out3x3[2].x = M0.z; Out3x3[2].y = M1.z; Out3x3[2].z = M2.z;
    Out2x2[0].x = M0.x; Out2x2[0].y = M1.x;
    Out2x2[1].x = M0.x; Out2x2[1].y = M1.y;
}
",
                    new ParameterDescriptor("M0", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("M1", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("M2", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("M3", TYPE.Vec4, Usage.In),
                    new ParameterDescriptor("Out4x4", TYPE.Mat4, Usage.Out),
                    new ParameterDescriptor("Out3x3", TYPE.Mat3, Usage.Out),
                    new ParameterDescriptor("Out2x2", TYPE.Mat2, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix Construction",
            tooltip: "constructs square matrices from the four input vectors",
            categories: new string[2] { "Math", "Matrix" },
            synonyms: new string[3] { "create", "build", "construct" },
            selectableFunctions: new()
            {
                { "Row", "Row" },
                { "Column", "Column" }
            },
            hasPreview: false,
            parameters: new ParameterUIDescriptor[7] {
                new ParameterUIDescriptor(
                    name: "M0",
                    tooltip: "first row"
                ),
                new ParameterUIDescriptor(
                    name: "M1",
                    tooltip: "second row"
                ),
                new ParameterUIDescriptor(
                    name: "M2",
                    tooltip: "third row"
                ),
                new ParameterUIDescriptor(
                    name: "M3",
                    tooltip: "forth row"
                ),
                new ParameterUIDescriptor(
                    name: "4x4",
                    tooltip: "a 4x4 matrix"
                ),
                new ParameterUIDescriptor(
                    name: "3x3",
                    tooltip: "a 3x3 matrix"
                ),
                new ParameterUIDescriptor(
                    name: "2x2",
                    tooltip: "a 2x2 matrix "
                )
            }
        );
    }
}
