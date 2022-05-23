using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Defs
{
    // TODO: These are temporarily needed to unblock Subgraph blackboard features.
    // In the future, Context nodes will not be registered at all,
    // and instead be populated by user actions and through configuration.

    public class ShaderSubGraphInputContext : IContextDescriptor
    {
        public IEnumerable<IContextDescriptor.ContextEntry> GetEntries() => new List<IContextDescriptor.ContextEntry>();

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;

        public RegistryKey GetRegistryKey() => new() { Name = "SubGraphInputContext", Version = 1 };
    }

    public class ShaderSubGraphOutputContext : IContextDescriptor
    {
        public IEnumerable<IContextDescriptor.ContextEntry> GetEntries() => new List<IContextDescriptor.ContextEntry>();

        public RegistryFlags GetRegistryFlags() => RegistryFlags.Base;

        public RegistryKey GetRegistryKey() => new() { Name = "SubGraphOutputContext", Version = 1 };
    }
}
