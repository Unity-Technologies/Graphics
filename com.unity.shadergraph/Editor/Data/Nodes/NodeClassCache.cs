using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    [InitializeOnLoad]
    internal static class NodeClassCache
    {
        private static Dictionary<Type, List<ContextFilterableAttribute>> m_KnownTypeLookupTable;

        public static IEnumerable<Type> knownNodeTypes
        {
            get => m_KnownTypeLookupTable.Keys;
        }

        public static IEnumerable<ContextFilterableAttribute> GetFilterableAttributesOnNodeType(Type nodeType)
        {
            if (nodeType == null)
            {
                throw new ArgumentNullException("Cannot get attributes on a null Type");
            }

            if (m_KnownTypeLookupTable.TryGetValue(nodeType, out List<ContextFilterableAttribute> filterableAttributes))
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
            Profiler.EndSample();
        }

        private static void DebugPrintKnownNodes()
        {
            foreach (var entry in m_KnownTypeLookupTable)
            {
                var nodeType = entry.Key;
                var filterableAttributes = entry.Value;
                String attrs = "";
                foreach (var filterable in filterableAttributes)
                {
                    attrs += filterable.ToString() + ", ";
                }
                Debug.Log(nodeType.ToString() + $": [{attrs}]");
            }
        }

        static NodeClassCache()
        {
            ReCacheKnownNodeTypes();
            //DebugPrintKnownNodes();
        }
    }
}
