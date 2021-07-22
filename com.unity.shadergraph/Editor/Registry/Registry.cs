using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Registry.Experimental
{
    public struct RegistryKey
    {
        // TODO: Tease out namespace (to support overrides) via IReadOnlyCollection
        public string Name;
        public int Version;

        // TODO: Not any of this.
        public override string ToString() => $"{Name}.{Version}";
        public override int GetHashCode() => ToString().GetHashCode();
        public override bool Equals(object obj) => obj is RegistryKey rk && rk.ToString().Equals(this.ToString());
    }

    // TODO: RegistryMetaData container
        // TODO: Search Categories and Search Tags
        // TODO: Registry relevant Flags (eg. IsType, etc. explicit Enum)
        // TODO: Description/DisplayName stuff (if relevant- prefer to have that elsewhere)

    public interface INodeDefinitionBuilder
    {
        RegistryKey GetRegistryKey();
        void BuildNode(Mock.INodeReader userData, Mock.INodeWriter concreteData);
        // TODO: CanAcceptConnection -> definition needs to filter connections ahead of time to see if topology can or should adopt it.
        // TODO: Generate shader code.
    }

    public class Registry
    {
        // TODO: Reassess the data layout here.
        Dictionary<RegistryKey, INodeDefinitionBuilder> builders = new Dictionary<RegistryKey, INodeDefinitionBuilder>();

        // TODO: Use a GraphDelta container so that we can follow concretization rules on default topologies.
        Dictionary<RegistryKey, Mock.MockNode> nullMake = new Dictionary<RegistryKey, Mock.MockNode>();

        public IEnumerable<RegistryKey> BrowseRegistryKeys() => builders.Keys;

        public static RegistryKey ResolveKey<T>() where T : INodeDefinitionBuilder => Activator.CreateInstance<T>().GetRegistryKey();

        public Mock.INodeReader GetDefaultTopology(RegistryKey key)
        {
            Mock.MockNode node;
            if(!nullMake.TryGetValue(key, out node) && builders.TryGetValue(key, out INodeDefinitionBuilder builder))
            {
                node = new Mock.MockNode(builder);
                builder.BuildNode(node, node);
                nullMake.Add(key, node);
            }
            return node;
        }

        public bool RegisterNodeBuilder<T>() where T : INodeDefinitionBuilder
        {
            INodeDefinitionBuilder builder = Activator.CreateInstance<T>();
            var key = builder.GetRegistryKey();

            if (builders.ContainsKey(key))
                return false;

            builders.Add(key, builder);
            return true;

            // TODO: Assess and properly message the validity of the key (including if in use).
            // TODO: Initialize and cache the null concretization.
            // TODO: Test the null concretization's topology as to whether it makes sense.
            // TODO: Dummy test the null concretized shader sandbox output.
            // TODO: Check the builder's reflection data and warn for non-deterministic behavior (?), or enforce determinism.
        }

        // TODO: Unregister Builder
        // TODO: Clean/Reinit Registry
        // TODO: On Registry Updated
        // TODO: Registration descriptors- etc.
    }
}
