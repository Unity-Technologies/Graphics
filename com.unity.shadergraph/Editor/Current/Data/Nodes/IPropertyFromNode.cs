using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IPropertyFromNode
    {
        AbstractShaderProperty AsShaderProperty();
        int outputSlotId { get; }
    }
}
