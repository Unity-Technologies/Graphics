// TODO: For testing static UI parts - remove before merging

using UnityEditor.ShaderGraph.Registry.Types;

namespace com.unity.shadergraph.defs
{
    internal class TempMat3Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TempMat3", // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], 1.0);",
            new ParameterDescriptor("In", TYPE.Mat3, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TempMat4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TempMat4", // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], In[3][3]);",
            new ParameterDescriptor("In", TYPE.Mat4, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TempIntNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TempInt", // Name
            "Out = float4(In, In, In, In);",
            new ParameterDescriptor("In", TYPE.Int, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TempFloatNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TempFloat", // Name
            "Out = float4(In, In, In, In);",
            new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }
}
