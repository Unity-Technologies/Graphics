using UnityEditor.ShaderGraph.Registry.Types;

namespace com.unity.shadergraph.defs
{
    internal class TestUIMat3Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIMat3", // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], 1.0);",
            new ParameterDescriptor("In", TYPE.Mat3, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TestUIMat4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIMat4", // Name
            "Out = float4(In[0][0], In[1][1], In[2][2], In[3][3]);",
            new ParameterDescriptor("In", TYPE.Mat4, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TestUIIntNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIInt", // Name
            "Out = float4(In, In, In, In);",
            new ParameterDescriptor("In", TYPE.Int, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TestUIFloatNode : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestUIFloat", // Name
            "Out = float4(In, In, In, In);",
            new ParameterDescriptor("In", TYPE.Float, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }

    internal class TestLocalVec4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TestLocalVec4", // Name
            "Local = In + Static; Out = Local;",
            new ParameterDescriptor("In", TYPE.Vec4, GraphType.Usage.In),
            new ParameterDescriptor("Static", TYPE.Vec4, GraphType.Usage.Static),
            new ParameterDescriptor("Local", TYPE.Vec4, GraphType.Usage.Local),
            new ParameterDescriptor("Out", TYPE.Vec4, GraphType.Usage.Out)
        );
    }
}
