// TODO: For testing static UI parts - remove before merging

using UnityEditor.ShaderGraph.Registry.Types;

namespace com.unity.shadergraph.defs
{
    internal class TempMat3Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TempMat3", // Name
            "Out = float3(In[0][0], In[1][1], In[2][2]);",
            new ParameterDescriptor("In", TYPE.Mat3, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );
    }

    internal class TempMat4Node : IStandardNode
    {
        public static FunctionDescriptor FunctionDescriptor => new(
            1, // Version
            "TempMat4", // Name
            "Out = float3(In[0][0], In[1][1], In[2][2]);",
            new ParameterDescriptor("In", TYPE.Mat4, GraphType.Usage.Static),
            new ParameterDescriptor("Out", TYPE.Vec3, GraphType.Usage.Out)
        );
    }
}
