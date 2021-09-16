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
        Dictionary<string, List<VariableNameOverride>> overrides = new Dictionary<string, List<VariableNameOverride>>();

        internal IEnumerable<(string Name, VariableNameOverride Override)> Overrides
        {
            get
            {
                foreach(var pair in overrides)
                {
                    foreach(var item in pair.Value)
                        yield return (pair.Key, item);
                }
            }
        }

        internal void Add(string key, string namespaceName, string name, int swizzle)
        {
            if (!overrides.ContainsKey(key))
                overrides.Add(key, new List<VariableNameOverride>());
            overrides[key].Add(new VariableNameOverride { Name = name, Namespace = namespaceName, Swizzle = swizzle });
        }

        internal void BuildInputOverrides(IEnumerable<BlockVariableNameOverride> inputOverrides)
        {
            foreach (var varOverride in inputOverrides)
                Add(varOverride.DestinationName, varOverride.SourceNamespace, varOverride.SourceName, varOverride.SourceSwizzle);
        }

        internal void BuildOutputOverrides(IEnumerable<BlockVariableNameOverride> outputOverrides)
        {
            foreach (var varOverride in outputOverrides)
                Add(varOverride.SourceName, varOverride.DestinationNamespace, varOverride.DestinationName, varOverride.DestinationSwizzle);
        }

        internal VariableNameOverride FindLastVariableOverride(BlockVariableLinkInstance varInstance)
        {
            return FindLastVariableOverride(varInstance.ReferenceName);
        }

        internal bool FindLastVariableOverride(string referenceName, out VariableNameOverride varOverride)
        {
            if (overrides.TryGetValue(referenceName, out var varOverrides))
            {
                if (varOverrides.Count != 0)
                {
                    varOverride = varOverrides[varOverrides.Count - 1];
                    return true;
                }
            }

            varOverride = null;
            return false;
        }

        internal VariableNameOverride FindLastVariableOverride(string referenceName)
        {
            VariableNameOverride varOverride;
            if(FindLastVariableOverride(referenceName, out varOverride))
                return varOverride;
            
            varOverride = new VariableNameOverride { Name = referenceName };
            return varOverride;
        }

        internal IEnumerable<VariableNameOverride> FindVariableOverrides(string referenceName)
        {
            if (overrides.TryGetValue(referenceName, out var varOverrides))
                return varOverrides;

            VariableNameOverride varOverride;
            varOverride = new VariableNameOverride { Name = referenceName };
            return new [] {varOverride };
        }
    }
}
