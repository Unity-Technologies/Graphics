using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.Defs
{
    /// <summary>
    /// A pure data structure that defines a node as a name, version, and
    /// a collection of functions.
    /// </summary>
    internal readonly struct NodeDescriptor
    {
        public int Version { get; }
        public string Name { get; }
        public IReadOnlyCollection<FunctionDescriptor> Functions { get; }

        public NodeDescriptor(
            int version,
            string name,
            params FunctionDescriptor[] functions)
        {
            Version = version;
            Name = name;
            Functions = functions.ToList().AsReadOnly();
        }
    }
}
