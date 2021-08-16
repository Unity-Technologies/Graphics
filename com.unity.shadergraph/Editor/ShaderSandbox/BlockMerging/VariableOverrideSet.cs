using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEditor.ShaderFoundry
{
    [DebuggerDisplay("Name = {BuildDebugName()}")]
    internal class VariableNameOverride
    {
        internal string Namespace = null;
        internal string Name = "";
        internal int Swizzle = 0;

        string BuildDebugName()
        {
            return $"{Namespace}::{Name}.{Swizzle}";
        }
    }

    internal class VariableOverrideSet
    {
        Dictionary<string, VariableNameOverride> overrides = new Dictionary<string, VariableNameOverride>();

        internal IEnumerable<(string Name, VariableNameOverride Override)> Overrides
        {
            get
            {
                foreach(var pair in overrides)
                {
                    yield return (pair.Key, pair.Value);
                }
            }
        }

        internal void Add(string key, string namespaceName, string name, int swizzle)
        {
            overrides[key] = new VariableNameOverride { Name = name, Namespace = namespaceName, Swizzle = swizzle };
        }

        internal VariableNameOverride FindVariableOverride(BlockVariableLinkInstance varInstance)
        {
            return FindVariableOverride(varInstance.ReferenceName);
        }

        internal VariableNameOverride FindVariableOverride(string referenceName)
        {
            VariableNameOverride varOverride;
            if (!overrides.TryGetValue(referenceName, out varOverride))
                varOverride = new VariableNameOverride { Name = referenceName };
            return varOverride;
        }
    }
}
