using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
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

    [Flags]
    public enum RegistryFlags
    {
        Type = 1, // The corresponding node definition is allowed to be a port.
        Func = 2, // Cannot be a port.
        Cast = 3,
        Base = 4,
    }

    /// <summary>
    /// The Registry holds information about available nodes, types, and type casting
    /// operations available in the shader graph.
    /// </summary>
    public class Registry
    {
        readonly Dictionary<RegistryKey, IRegistryEntry> builders = new Dictionary<RegistryKey, IRegistryEntry>();
        public GraphHandler defaultTopologies = new GraphHandler();

        public Registry()
        {
            Register<ContextBuilder>();
            Register<ReferenceNodeBuilder>();
        }

        internal ShaderType GetShaderType(IFieldReader field, ShaderContainer container)
        {
            var graphTypeBuilder = this.GetTypeBuilder(field.GetRegistryKey());
            return graphTypeBuilder.GetShaderType(field, container, this);
        }

        public IEnumerable<RegistryKey> BrowseRegistryKeys() =>
            builders.Keys;

        public INodeReader GetDefaultTopology(RegistryKey key) =>
            defaultTopologies.GetNodeReader(key.ToString());

        public bool CastExists(RegistryKey from, RegistryKey to) =>
            builders.Values.OfType<ICastDefinitionBuilder>().Any(e => e.GetTypeConversionMapping().Equals((from,to)));

        public bool Register<T>() where T : IRegistryEntry
        {
            var registryEntry = Activator.CreateInstance<T>();
            return Register(registryEntry);
        }

        internal RegistryKey Register(FunctionDescriptor funcDesc)
        {
            var builder = new FunctionDescriptorNodeBuilder(funcDesc);
            bool wasSuccess = Register(builder);
            if (!wasSuccess)
            {
                throw new Exception($"Unsuccessful registration for FunctionDescriptor : {funcDesc.Name}");
            }
            return ((IRegistryEntry)builder).GetRegistryKey();
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

        internal INodeDefinitionBuilder GetNodeBuilder(RegistryKey key) =>
            (INodeDefinitionBuilder)GetBuilder(key);

        internal ITypeDefinitionBuilder GetTypeBuilder(RegistryKey key) =>
            (ITypeDefinitionBuilder)GetBuilder(key);

        internal ICastDefinitionBuilder GetCastBuilder(RegistryKey key) =>
            (ICastDefinitionBuilder)GetBuilder(key);

        private IRegistryEntry GetBuilder(RegistryKey key) =>
            builders.TryGetValue(key, out var builder) ? builder : null;

        public static RegistryKey ResolveKey<T>() where T : IRegistryEntry =>
            Activator.CreateInstance<T>().GetRegistryKey();

        public static RegistryFlags ResolveFlags<T>() where T : IRegistryEntry =>
            Activator.CreateInstance<T>().GetRegistryFlags();
    }
}
