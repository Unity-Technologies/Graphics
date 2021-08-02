using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Registry
{
    /*
    Namespaces:
        Namespaces for definitions and supported by keys.
        -- Ability to specify an active context/namespace, and get overrides.
    Search:
        Categories, search hierachy, tooltips, etc-- how should these be handled?
    Registry:
        Unregister Builder
        Clean/Reinit Registry
        On Registry Updated
        Registration descriptors- etc.
    Errors:
        No messaging or error state handling or checking on Registry actions.
        Need an error handler for definition interface that can be used for concretization as well.
    */
    public struct Box<T> : ISerializable
    {
        public T data;

        Box(SerializationInfo info, StreamingContext context)
        {
            data = (T)info.GetValue("value", typeof(T));
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("value", data);
        }
    }


    [Serializable]
    public struct RegistryKey : ISerializable
    {
        public string Name;
        public int Version;

        public override string ToString() => $"{Name}.{Version}";
        public override int GetHashCode() => ToString().GetHashCode();
        public override bool Equals(object obj) => obj is RegistryKey rk && rk.ToString().Equals(this.ToString());

        public RegistryKey(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("Name");
            Version = info.GetInt32("Version");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("Version", Version);
        }
    }

    [Flags] public enum RegistryFlags
    {
        IsType = 1, // The corresponding node definition is allowed to be a port.
        isFunc = 2, // Cannot be a port.
    }


    public class Registry : IRegistry
    {
        Dictionary<RegistryKey, INodeDefinitionBuilder> builders = new Dictionary<RegistryKey, INodeDefinitionBuilder>();
        GraphDelta.IGraphHandler defaultTopologies = GraphUtil.CreateGraph();

        public IEnumerable<RegistryKey> BrowseRegistryKeys() => builders.Keys;

        public INodeDefinitionBuilder GetBuilder(RegistryKey key)
        {
            builders.TryGetValue(key, out var builder);
            return builder;
        }

        public INodeReader GetDefaultTopology(RegistryKey key) => defaultTopologies.GetNode(key.ToString());

        public RegistryFlags GetFlags(RegistryKey key) => GetBuilder(key).GetRegistryFlags();

        public bool RegisterNodeBuilder<T>() where T : INodeDefinitionBuilder
        {
            var builder = Activator.CreateInstance<T>();
            var key = builder.GetRegistryKey();
            if (builders.ContainsKey(key))
                return false;
            builders.Add(key, builder);
            defaultTopologies.AddNode<T>(key.ToString(), this);
            return true;
        }

        public RegistryKey ResolveKey<T>() where T : INodeDefinitionBuilder
        {
            var builder = Activator.CreateInstance<T>();
            return builder.GetRegistryKey();
        }

        public RegistryFlags ResolveFlags<T>() where T : INodeDefinitionBuilder => GetFlags(ResolveKey<T>());

        public INodeDefinitionBuilder ResolveBuilder<T>() where T : INodeDefinitionBuilder
        {
            RegisterNodeBuilder<T>();
            var key = ResolveKey<T>();
            return GetBuilder(key);
        }
    }
}
