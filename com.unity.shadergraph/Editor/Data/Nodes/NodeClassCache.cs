using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal static class NodeClassCache
    {
        private class PostProcessor : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (knownSubGraphLookupTable != null)
                {
                    foreach (string str in deletedAssets)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(str);
                        knownSubGraphLookupTable.Remove(guid);
                    }
                    foreach (string str in movedFromAssetPaths)
                    {
                        var guid = AssetDatabase.AssetPathToGUID(str);
                        knownSubGraphLookupTable.Remove(guid);
                    }
                }

                foreach (string str in importedAssets)
                {
                    if (str.EndsWith(ShaderSubGraphImporter.Extension))
                    {
                        UpdateSubGraphEntry(str);
                    }
                }
                foreach (string str in movedAssets)
                {
                    if (str.EndsWith(ShaderSubGraphImporter.Extension))
                    {
                        UpdateSubGraphEntry(str);
                    }
                }
            }
        }


        private static Dictionary<Type, List<ContextFilterableAttribute>> m_KnownTypeLookupTable;
        private static Dictionary<Type, List<ContextFilterableAttribute>> knownTypeLookupTable
        {
            get
            {
                if (m_KnownTypeLookupTable == null)
                    ReCacheKnownNodeTypes();
                return m_KnownTypeLookupTable;
            }
        }

        public static IEnumerable<Type> knownNodeTypes
        {
            get => knownTypeLookupTable.Keys;
        }


        private static Dictionary<string, SubGraphAsset> m_KnownSubGraphLookupTable;
        private static Dictionary<string, SubGraphAsset> knownSubGraphLookupTable
        {
            get
            {
                if (m_KnownSubGraphLookupTable == null)
                    ReCacheKnownNodeTypes();
                return m_KnownSubGraphLookupTable;
            }
        }

        public static IEnumerable<SubGraphAsset> knownSubGraphAssets
        {
            get => knownSubGraphLookupTable.Values;
        }

        public static void UpdateSubGraphEntry(string path)
        {
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (guid.Length == 0)
            {
                return;
            }
            var asset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(path);

            bool valid = asset != null && asset.isValid;
            if (knownSubGraphLookupTable.TryGetValue(guid, out SubGraphAsset known))
            {
                if (!valid)
                {
                    knownSubGraphLookupTable.Remove(guid);
                }
                else if (asset != known)
                {
                    knownSubGraphLookupTable[guid] = asset;
                }
            }
            else if (valid)
            {
                knownSubGraphLookupTable.Add(guid, asset);
            }
        }

        public static IEnumerable<ContextFilterableAttribute> GetFilterableAttributesOnNodeType(Type nodeType)
        {
            if (nodeType == null)
            {
                throw new ArgumentNullException("Cannot get attributes on a null Type");
            }

            if (knownTypeLookupTable.TryGetValue(nodeType, out List<ContextFilterableAttribute> filterableAttributes))
            {
                return filterableAttributes;
            }
            else
            {
                throw new ArgumentException($"The passed in Type {nodeType.FullName} was not found in the loaded assemblies as a child class of AbstractMaterialNode");
            }
        }

        public static T GetAttributeOnNodeType<T>(Type nodeType) where T : ContextFilterableAttribute
        {
            var filterableAttributes = GetFilterableAttributesOnNodeType(nodeType);
            foreach (var attr in filterableAttributes)
            {
                if (attr is T searchTypeAttr)
                {
                    return searchTypeAttr;
                }
            }
            return null;
        }

        // Need to take into account new nodes from user-land for 22.2
        // may not be needed anymore with current rework (no more explicit node class)
        // may need to have a prototype for 21.2 with lazy init
        // has to happen before first import - used by Searcher for ex.
        // how to test if we broke something: open an existing SG and try adding a new node
        private static void ReCacheKnownNodeTypes() 
        {
            Profiler.BeginSample("NodeClassCache: Re-caching all known node types");
            m_KnownTypeLookupTable = new Dictionary<Type, List<ContextFilterableAttribute>>();
            foreach (Type nodeType in TypeCache.GetTypesDerivedFrom<AbstractMaterialNode>())
            {
                if (!nodeType.IsAbstract)
                {
                    List<ContextFilterableAttribute> filterableAttributes = new List<ContextFilterableAttribute>();
                    foreach (Attribute attribute in Attribute.GetCustomAttributes(nodeType))
                    {
                        Type attributeType = attribute.GetType();
                        if (!attributeType.IsAbstract && attribute is ContextFilterableAttribute contextFilterableAttribute)
                        {
                            filterableAttributes.Add(contextFilterableAttribute);
                        }
                    }
                    m_KnownTypeLookupTable.Add(nodeType, filterableAttributes);
                }
            }

            m_KnownSubGraphLookupTable = new Dictionary<string, SubGraphAsset>();
            
            foreach (var guid in AssetDatabase.FindAssets(string.Format("glob:\"*.asset\" t:{0}", typeof(SubGraphAsset))))
            {
                var asset = AssetDatabase.LoadAssetAtPath<SubGraphAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.isValid)
                {
                    m_KnownSubGraphLookupTable.Add(guid, asset);
                }
            }

            Profiler.EndSample();
        }
    }
}
