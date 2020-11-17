using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Legacy;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    /// <summary>
    /// Minimal version of <see cref="GraphData"/> used for gathering dependencies. This allows us to not deserialize
    /// all the nodes, ports, edges, groups etc., which is important as we cannot share data between
    /// <see cref="ShaderSubGraphImporter.GatherDependenciesFromSourceFile"/> and
    /// <see cref="ShaderSubGraphImporter.OnImportAsset"/>. The latter must always import fully, but for the former we
    /// want to avoid the extra GC pressure.
    /// </summary>
    [Serializable]
    class MinimalGraphData
    {
        static Dictionary<string, Type> s_MinimalTypeMap = CreateMinimalTypeMap();

        static Dictionary<string, Type> CreateMinimalTypeMap()
        {
            var types = new Dictionary<string, Type>();
            foreach (var type in TypeCache.GetTypesWithAttribute<HasDependenciesAttribute>())
            {
                var dependencyAttribute = (HasDependenciesAttribute)type.GetCustomAttributes(typeof(HasDependenciesAttribute), false)[0];
                if (!typeof(IHasDependencies).IsAssignableFrom(dependencyAttribute.minimalType))
                {
                    Debug.LogError($"{type} must implement {typeof(IHasDependencies)} to be used in {typeof(HasDependenciesAttribute)}");
                    continue;
                }

                types.Add(type.FullName, dependencyAttribute.minimalType);

                var formerNameAttributes = (FormerNameAttribute[])type.GetCustomAttributes(typeof(FormerNameAttribute), false);
                foreach (var formerNameAttribute in formerNameAttributes)
                {
                    types.Add(formerNameAttribute.fullName, dependencyAttribute.minimalType);
                }
            }

            return types;
        }

        [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

        // gather all asset dependencies declared by nodes in the given (shadergraph or shadersubgraph) asset
        // by reading the source file from disk, and doing a minimal parse
        // returns true if it successfully gathered the dependencies, false if there was an error
        public static bool GatherMinimalDependenciesFromFile(string assetPath, AssetCollection assetCollection)
        {
            var textGraph = FileUtilities.SafeReadAllText(assetPath);

            // if we can't read the file, no dependencies can be gathered
            if (string.IsNullOrEmpty(textGraph))
                return false;

            var entries = MultiJsonInternal.Parse(textGraph);

            if (string.IsNullOrWhiteSpace(entries[0].type))
            {
                var minimalGraphData = JsonUtility.FromJson<MinimalGraphData>(textGraph);
                entries.Clear();
                foreach (var node in minimalGraphData.m_SerializableNodes)
                {
                    entries.Add(new MultiJsonEntry(node.typeInfo.fullName, null, node.JSONnodeData));
                    AbstractMaterialNode0 amn = new AbstractMaterialNode0();
                    JsonUtility.FromJsonOverwrite(node.JSONnodeData, amn);
                    foreach(var slot in amn.m_SerializableSlots)
                    {
                        entries.Add(new MultiJsonEntry(slot.typeInfo.fullName, null, slot.JSONnodeData));
                    }
                }
            }

            foreach (var entry in entries)
            {
                if (s_MinimalTypeMap.TryGetValue(entry.type, out var minimalType))
                {
                    var instance = (IHasDependencies)Activator.CreateInstance(minimalType);
                    JsonUtility.FromJsonOverwrite(entry.json, instance);
                    instance.GetSourceAssetDependencies(assetCollection);
                }
            }

            return true;
        }
    }
}
