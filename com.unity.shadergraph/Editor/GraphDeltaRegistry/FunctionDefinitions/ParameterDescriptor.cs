using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{
    // ----------
    // ParameterDescriptor

    internal readonly struct ParameterDescriptor
    {
        public string Name { get; }  // Must be a valid reference name
        public TypeDescriptor TypeDescriptor { get; }
        public Usage Usage { get; }

        public ParameterDescriptor(
            string name,
            TypeDescriptor typeDescriptor,
            Usage usage)
        {
            Name = name;
            TypeDescriptor = typeDescriptor;
            Usage = usage;
        }

        public override string ToString()
        {
            // TODO Make this not a stub.
            return base.ToString();
        }
    }
}
