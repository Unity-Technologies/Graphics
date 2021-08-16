using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class ResolvedFieldMatch
    {
        internal BlockVariableLinkInstance Source;
        internal BlockVariableLinkInstance Destination;
        internal int SourceSwizzle = 0;
        internal int DestinationSwizzle = 0;
    }

    /// Represents a type being built for linking blocks. This is normally either an input or output
    /// to a block's entry point function. All available fields are kept track of. Resolved fields are
    /// used to keep track of the source of any field when it's matched.
    internal class BlockTypeLinkInstance
    {
        // All fields that are available on this type. These fields may not exist if they aren't linked to
        List<BlockVariableLinkInstance> AvailableFields = new List<BlockVariableLinkInstance>();
        // Reference name of available field to a match.
        Dictionary<string, ResolvedFieldMatch> resolvedFields = new Dictionary<string, ResolvedFieldMatch>();
        VariableOverrideSet nameOverrides = new VariableOverrideSet();

        internal BlockVariableLinkInstance Instance { get; set; } = new BlockVariableLinkInstance();
        internal IEnumerable<BlockVariableLinkInstance> Fields => AvailableFields;
        internal VariableOverrideSet NameOverrides => nameOverrides;
        internal IEnumerable<ResolvedFieldMatch> ResolvedFieldMatches => resolvedFields.Values;
        internal IEnumerable<BlockVariableLinkInstance> ResolvedFields
        {
            get
            {
                foreach (var field in AvailableFields)
                {
                    if (FindResolvedField(field.ReferenceName) != null)
                        yield return field;
                }
            }
        }

        internal void AddField(BlockVariableLinkInstance field)
        {
            AvailableFields.Add(field);
        }

        internal BlockVariableLinkInstance FindField(string referenceName)
        {
            return AvailableFields.Find((f) => (f.ReferenceName == referenceName));
        }

        internal void AddResolvedField(string name, ResolvedFieldMatch resolvedMatch)
        {
            resolvedFields[name] = resolvedMatch;
        }

        internal ResolvedFieldMatch FindResolvedField(string name)
        {
            resolvedFields.TryGetValue(name, out var result);
            return result;
        }

        internal void AddOverride(string key, VariableNameOverride varOverride)
        {
            AddOverride(key, varOverride.Namespace, varOverride.Name, varOverride.Swizzle);
        }

        internal void AddOverride(string key, string namespaceName, string name, int swizzle)
        {
            nameOverrides.Add(key, namespaceName, name, swizzle);
        }

        internal VariableNameOverride FindVariableOverride(string referenceName)
        {
            return nameOverrides.FindVariableOverride(referenceName);
        }
    }
}
