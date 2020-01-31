using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.Graphing;
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
    class MinimalGraphData : ISerializationCallbackReceiver
    {
        static Dictionary<string, Type> s_MinimalTypeMap = CreateMinimalTypeMap();

         static Dictionary<string, Type> CreateMinimalTypeMap()
        {
            var types = new Dictionary<string, Type>();
            foreach (var nodeType in TypeCache.GetTypesWithAttribute<HasDependenciesAttribute>())
            {
                var dependencyAttribute = (HasDependenciesAttribute)nodeType.GetCustomAttributes(typeof(HasDependenciesAttribute), false)[0];
                if (!typeof(IHasDependencies).IsAssignableFrom(dependencyAttribute.minimalType))
                {
                    Debug.LogError($"{nodeType} must implement {typeof(IHasDependencies)} to be used in {typeof(HasDependenciesAttribute)}");
                    continue;
                }

                 types.Add(nodeType.FullName, dependencyAttribute.minimalType);

                 var formerNameAttributes = (FormerNameAttribute[])nodeType.GetCustomAttributes(typeof(FormerNameAttribute), false);
                foreach (var formerNameAttribute in formerNameAttributes)
                {
                    types.Add(formerNameAttribute.fullName, dependencyAttribute.minimalType);
                }
            }
            return types;
        }

         [SerializeField]
        List<SerializationHelper.JSONSerializedElement> m_SerializableNodes = new List<SerializationHelper.JSONSerializedElement>();

         public List<string> dependencies { get; set; }

         public void OnAfterDeserialize()
        {
            foreach (var element in m_SerializableNodes)
            {
                if (s_MinimalTypeMap.TryGetValue(element.typeInfo.fullName, out var minimalType))
                {
                    var instance = (IHasDependencies)Activator.CreateInstance(minimalType);
                    JsonUtility.FromJsonOverwrite(element.JSONnodeData, instance);
                    instance.GetSourceAssetDependencies(dependencies);
                }
            }
        }

         public void OnBeforeSerialize()
        {
        }

         public static string[] GetDependencyPaths(string assetPath)
        {
            var dependencies = new List<string>();
            GetDependencyPaths(assetPath, dependencies);
            return dependencies.ToArray();
        }

         public static void GetDependencyPaths(string assetPath, List<string> dependencies)
        {
            var textGraph = File.ReadAllText(assetPath, Encoding.UTF8);
            var minimalGraphData = new MinimalGraphData { dependencies = dependencies };
            JsonUtility.FromJsonOverwrite(textGraph, minimalGraphData);
        }
    }
}