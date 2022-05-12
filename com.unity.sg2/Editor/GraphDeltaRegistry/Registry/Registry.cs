using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    /*
    TODOs:
        Namespaces and Context local overrides.

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

        //TODO(Liz) - This _was_ Name.Version but that obviously was an issue with the defaultTopo structure
        //thinking this was a path...discussion and investigation into longterm ramifications needed
        public override string ToString() => $"{Name}_{Version}";
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
        Type = 1,
        Func = 2,
        Cast = 3,
        Base = 4,
    }

    public class PropertyContext : IContextDescriptor
    {
        // TODO: Refactor ContextNode/Descriptor/AddContextNode eg. FunctionNodeDescriptor
        public IEnumerable<IContextDescriptor.ContextEntry> GetEntries() => new List<IContextDescriptor.ContextEntry>();
        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;
        public RegistryKey GetRegistryKey() => new RegistryKey() { Name = "MaterialPropertyContext", Version = 1 };
    }

    public class Registry
    {
        readonly Dictionary<RegistryKey, IRegistryEntry> builders = new Dictionary<RegistryKey, IRegistryEntry>();
        public GraphHandler defaultTopologies = null;

        public Registry()
        {
            Register<ContextBuilder>();
            Register<PropertyContext>();
            defaultTopologies = new GraphHandler(this);
            Register<ReferenceNodeBuilder>();
        }

        internal ShaderFoundry.ShaderType GetShaderType(FieldHandler field, ShaderFoundry.ShaderContainer container)
        {
            var graphTypeBuilder = this.GetTypeBuilder(field.GetRegistryKey());
            return graphTypeBuilder.GetShaderType(field, container, this);
        }

        public IEnumerable<RegistryKey> BrowseRegistryKeys() => builders.Keys;
        public NodeHandler GetDefaultTopology(RegistryKey key) => defaultTopologies.GetNode(key.ToString());

        public bool CastExists(RegistryKey from, RegistryKey to) => builders.Values.OfType<ICastDefinitionBuilder>().Any(e => e.GetTypeConversionMapping().Equals((from,to)));

        public bool Register<T>() where T : IRegistryEntry
        {
            var registryEntry = Activator.CreateInstance<T>();
            return Register(registryEntry);
        }

        /// <summary>
        /// Registers a single function (represented as a pure data FunctionDescriptor)
        /// as a topology available through the registry.
        ///
        /// NOTE: Registering just a function is a special case.
        ///       Generally nodes should be registered using Register(NodeDescriptor).
        /// </summary>
        internal RegistryKey Register(FunctionDescriptor functionDescriptor)
        {
            var builder = new FunctionDescriptorNodeBuilder(functionDescriptor);
            bool wasSuccess = Register(builder);
            if (!wasSuccess)
            {
                string msg = $"Unsuccessful registration for FunctionDescriptor : {functionDescriptor.Name}";
                throw new Exception(msg);
            }
            return ((IRegistryEntry)builder).GetRegistryKey();
        }

        /// <summary>
        /// Registers a node (represented as a pure data NodeDescriptor) as a topology
        /// available through the registry.
        /// </summary>
        internal RegistryKey Register(NodeDescriptor nodeDescriptor)
        {
            INodeDefinitionBuilder builder = new NodeDescriptorNodeBuilder(nodeDescriptor);
            bool wasSuccessfullyRegistered = Register(builder);
            if (!wasSuccessfullyRegistered)
            {
                string msg = $"Unsuccessful registration for NodeDescriptor : {nodeDescriptor.Name}";
                throw new Exception(msg);
            }
            return builder.GetRegistryKey();
        }

        private bool Register(IRegistryEntry builder) {
            var key = builder.GetRegistryKey();
            if (builders.ContainsKey(key))
                return false;
            builders.Add(key, builder);
            if(builder is INodeDefinitionBuilder && builder.GetRegistryFlags() == RegistryFlags.Func)
                defaultTopologies.AddNode(key, key.ToString(),this);
            return true;
        }

        internal IContextDescriptor GetContextDescriptor(RegistryKey key)
        {
            var contextNodeBuilder = GetBuilder(key);
            var registryFlags = contextNodeBuilder.GetRegistryFlags();
            if(registryFlags == RegistryFlags.Base)
                return (IContextDescriptor)contextNodeBuilder;

            return null;
        }

        internal INodeDefinitionBuilder GetNodeBuilder(RegistryKey key) => (INodeDefinitionBuilder)GetBuilder(key);
        internal ITypeDefinitionBuilder GetTypeBuilder(RegistryKey key) => (ITypeDefinitionBuilder)GetBuilder(key);
        internal ICastDefinitionBuilder GetCastBuilder(RegistryKey key) => (ICastDefinitionBuilder)GetBuilder(key);

        private IRegistryEntry GetBuilder(RegistryKey key) => builders.TryGetValue(key, out var builder) ? builder : null;
        public static RegistryKey ResolveKey<T>() where T : IRegistryEntry => Activator.CreateInstance<T>().GetRegistryKey();
        public static RegistryFlags ResolveFlags<T>() where T : IRegistryEntry => Activator.CreateInstance<T>().GetRegistryFlags();
    }
}
