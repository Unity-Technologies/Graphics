using System.Collections.Generic;
using Usage = UnityEditor.ShaderGraph.GraphDelta.GraphType.Usage;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class MatrixSplitNode : IStandardNode
    {
        public static string Name = "MatrixSplit";
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
    M0 = In[0];
    M1 = In[1];
    M2 = In[2];
    M3 = In[3];
}
",
                    new ParameterDescriptor("In", TYPE.Matrix, Usage.In),
                    new ParameterDescriptor("M0", TYPE.Vector, Usage.Out),
                    new ParameterDescriptor("M1", TYPE.Vector, Usage.Out),
                    new ParameterDescriptor("M2", TYPE.Vector, Usage.Out),
                    new ParameterDescriptor("M3", TYPE.Vector, Usage.Out)
                ),
                new(
                    1,
                    "Column",
@"
{
    M0.x = In[0].x; M0.y = In[1].x; M0.z = In[2].x; M0.w = In[3].x;
    M1.x = In[0].y; M1.y = In[1].y; M1.z = In[2].y; M1.w = In[3].y;
    M2.x = In[0].z; M2.y = In[1].z; M2.z = In[2].z; M2.w = In[3].z;
    M3.x = In[0].w; M3.y = In[1].w; M3.z = In[2].w; M3.w = In[3].w;
}
",
                    new ParameterDescriptor("In", TYPE.Matrix, Usage.In),
                    new ParameterDescriptor("M0", TYPE.Vector, Usage.Out),
                    new ParameterDescriptor("M1", TYPE.Vector, Usage.Out),
                    new ParameterDescriptor("M2", TYPE.Vector, Usage.Out),
                    new ParameterDescriptor("M3", TYPE.Vector, Usage.Out)
                )
            }
        );

        public static NodeUIDescriptor NodeUIDescriptor => new(
            Version,
            Name,
            displayName: "Matrix Split",
            tooltip: "splits a square matrix into vectors",
            categories: new string[2] { "Math", "Matrix" },
            synonyms: new string[0] {  },
            hasPreview: false,
            selectableFunctions: new()
            {
                { "Row", "Row" },
                { "Column", "Column" }
            },
            parameters: new ParameterUIDescriptor[5] {
                new ParameterUIDescriptor(
                    name: "In",
                    tooltip: "the matrix to split"
                ),
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
                    tooltip: "fourth row"
                )
            }
        );
    }
}
