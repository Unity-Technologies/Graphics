using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Rendering.Universal
{
    [Flags]
    enum ShaderPropertyUsage : byte
    {
        None = 0,
        InvalidShader = 1 << 0,
        NoMapping = 1 << 1,
        ValidForUpgrade = 1 << 2,
        MultipleUpgradePaths = 1 << 3,
        AlreadyUpgraded = 1 << 4,

        HasIssues = InvalidShader | NoMapping | MultipleUpgradePaths,
        CanProcess = ValidForUpgrade | AlreadyUpgraded,
    }

    class AnimationClipUpgradePathsCache : IDisposable
    {
        class Cache : IDisposable
        {
            public class Entry
            {
                public List<MaterialUpgrader> upgraders = new();

                public class Mapping
                {
                    public Dictionary<string, string> mapping = new();

                    public HashSet<string> mappingsWithInvalidUpgradePaths = new();

                    public void Add(string from, string to)
                    {
                        if (mapping.TryGetValue(from, out var value))
                        {
                            if (value != to)
                                mappingsWithInvalidUpgradePaths.Add(from);
                        }
                        mapping[from] = to;
                    }

                    public ShaderPropertyUsage Get(string propertyName, out string newPropertyName)
                    {
                        if (!mapping.TryGetValue(propertyName, out newPropertyName))
                        {
                            newPropertyName = propertyName;

                            foreach (var kvp in mapping)
                            {
                                if (propertyName.Equals(kvp.Value))
                                    return ShaderPropertyUsage.AlreadyUpgraded;
                            }

                            return ShaderPropertyUsage.NoMapping;
                        }

                        if (mappingsWithInvalidUpgradePaths.Contains(propertyName))
                            return ShaderPropertyUsage.MultipleUpgradePaths;

                        return ShaderPropertyUsage.ValidForUpgrade;
                    }
                }


                public List<Mapping> mappings = new List<Mapping>((int)MaterialUpgrader.MaterialPropertyType.Count);

                Array m_EnumValues;
                IEnumerator m_Enumerator;

                public Entry()
                {
                    m_EnumValues = Enum.GetValues(typeof(MaterialUpgrader.MaterialPropertyType));
                    m_Enumerator = m_EnumValues.GetEnumerator();

                    for (int m = 0; m < (int)MaterialUpgrader.MaterialPropertyType.Count; ++m)
                    {
                        mappings.Add(new Mapping());
                    }
                }

                public void Add(MaterialUpgrader upgrader)
                {
                    upgraders.Add(upgrader);

                    m_Enumerator.Reset();
                    for (int m = 0; m < (int)MaterialUpgrader.MaterialPropertyType.Count; ++m)
                    {
                        m_Enumerator.MoveNext();
                        var mapping = mappings[(int)m];
                        foreach (var propertyRename in upgrader.GetPropertyRenameMap((MaterialUpgrader.MaterialPropertyType)m_Enumerator.Current))
                        {
                            mapping.Add(propertyRename.Key, propertyRename.Value);
                        }
                    }
                }

                public ShaderPropertyUsage Get(MaterialUpgrader.MaterialPropertyType type, string propertyName, out string newPropertyName)
                {
                    var mapping = mappings[(int)type];
                    return mapping.Get(propertyName, out newPropertyName);
                }
            }

            Dictionary<string, Entry> m_Dictionary = new();

            public void Add(MaterialUpgrader upgrader)
            {
                if (!m_Dictionary.TryGetValue(upgrader.NewShaderPath, out var entry))
                {
                    entry = new();
                    m_Dictionary[upgrader.NewShaderPath] = entry;
                }

                entry.Add(upgrader);
            }

            public bool TryGetEntry(string shaderName, out Entry entry)
            {
                entry = null;
                if (string.IsNullOrEmpty(shaderName))
                    return false;

                return m_Dictionary.TryGetValue(shaderName, out entry);
            }

            public void Dispose()
            {
                m_Dictionary.Clear();
            }
        }

        Cache m_Cache = new ();

        public AnimationClipUpgradePathsCache(List<MaterialUpgrader> upgraders)
        {
            BuildUpgradeCache(upgraders);
        }

        private void BuildUpgradeCache(List<MaterialUpgrader> upgraders)
        {
            foreach (var upgrader in upgraders)
            {
                m_Cache.Add(upgrader);
            }
        }

        public ShaderPropertyUsage GetShaderPropertyUsage(string shaderName, MaterialUpgrader.MaterialPropertyType propertyType, string propertyName, out string newPropertyName)
        {
            newPropertyName = propertyName;

            if (!m_Cache.TryGetEntry(shaderName, out var entry))
            {
                return ShaderPropertyUsage.InvalidShader;
            }

            return entry.Get(propertyType, propertyName, out newPropertyName);
        }

        public void Dispose()
        {
            m_Cache.Dispose();
        }
    }
}
