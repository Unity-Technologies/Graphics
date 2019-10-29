using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    interface IConditional
    {
        FieldCondition[] fieldConditions { get; }
    }
}
