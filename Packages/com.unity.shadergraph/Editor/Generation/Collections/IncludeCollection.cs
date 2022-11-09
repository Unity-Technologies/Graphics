using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    [Serializable]
    internal class IncludeCollection : IEnumerable<IncludeDescriptor>
    {
        [SerializeField]
        List<IncludeDescriptor> includes;

        public IncludeCollection()
        {
            includes = new List<IncludeDescriptor>();
        }

        public IncludeCollection Add(IncludeCollection includes)
        {
            if (includes != null)
            {
                foreach (var include in includes)
                {
                    AddInternal(include.guid, include.path, include.location, include.fieldConditions, include.shouldIncludeWithPragmas);
                }
            }
            return this;
        }

        public IncludeCollection Add(string includePath, IncludeLocation location)
        {
            var guid = AssetDatabase.AssetPathToGUID(includePath);
            return AddInternal(guid, includePath, location);
        }

        public IncludeCollection Add(string includePath, IncludeLocation location, bool shouldIncludeWithPragmas)
        {
            var guid = AssetDatabase.AssetPathToGUID(includePath);
            return AddInternal(guid, includePath, location, null, shouldIncludeWithPragmas);
        }

        public IncludeCollection Add(string includePath, IncludeLocation location, FieldCondition fieldCondition)
        {
            var guid = AssetDatabase.AssetPathToGUID(includePath);
            return AddInternal(guid, includePath, location, new FieldCondition[] { fieldCondition });
        }

        public IncludeCollection Add(string includePath, IncludeLocation location, FieldCondition fieldCondition, bool shouldIncludeWithPragmas)
        {
            var guid = AssetDatabase.AssetPathToGUID(includePath);
            return AddInternal(guid, includePath, location, new FieldCondition[] { fieldCondition }, shouldIncludeWithPragmas);
        }

        public IncludeCollection Add(string includePath, IncludeLocation location, FieldCondition[] fieldConditions)
        {
            var guid = AssetDatabase.AssetPathToGUID(includePath);
            return AddInternal(guid, includePath, location, fieldConditions);
        }

        public IncludeCollection Add(string includePath, IncludeLocation location, FieldCondition[] fieldConditions, bool shouldIncludeWithPragmas)
        {
            var guid = AssetDatabase.AssetPathToGUID(includePath);
            return AddInternal(guid, includePath, location, fieldConditions, shouldIncludeWithPragmas);
        }

        public IncludeCollection AddInternal(string includeGUID, string includePath, IncludeLocation location, FieldCondition[] fieldConditions = null, bool shouldIncludeWithPragmas = false)
        {
            if (string.IsNullOrEmpty(includeGUID))
            {
                // either the file doesn't exist, or this is a placeholder
                // de-duplicate by path
                int existing = includes.FindIndex(i => i.path == includePath);
                if (existing < 0)
                    includes.Add(new IncludeDescriptor(null, includePath, location, fieldConditions, shouldIncludeWithPragmas));
                return this;
            }
            else
            {
                // de-duplicate by GUID
                int existing = includes.FindIndex(i => i.guid == includeGUID);
                if (existing < 0)
                {
                    // no duplicates, just add it
                    includes.Add(new IncludeDescriptor(includeGUID, includePath, location, fieldConditions, shouldIncludeWithPragmas));
                }
                else
                {
                    // duplicate file -- we could double check they are the same...
                    // they might have different field conditions that require merging, for example.
                    // But for now we'll just assume they are the same and drop the new one
                }
            }
            return this;
        }

        public IEnumerator<IncludeDescriptor> GetEnumerator()
        {
            return includes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
