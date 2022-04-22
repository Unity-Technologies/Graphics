using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry
{
    internal class ScopeSet
    {
        Dictionary<(ShaderType Type, string Name), VariableLinkInstance> FieldMap = new Dictionary<(ShaderType Type, string Name), VariableLinkInstance>();
        internal VariableLinkInstance Find(ShaderType type, string variableName)
        {
            FieldMap.TryGetValue((type, variableName), out var result);
            return result;
        }

        internal void Set(VariableLinkInstance instance, string variableName = null)
        {
            variableName = variableName ?? instance.Name;
            FieldMap[(instance.Type, variableName)] = instance;
        }
    }
}
