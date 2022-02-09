using System.Collections.Generic;
using System.Linq;

namespace com.unity.shadergraph.defs {

    /// <summary>
    /// A FunctionDescriptor describes a shader function.
    ///
    /// In registration (See: Register(FunctionDescriptor funcDesc)), a
    /// FunctionDescriptor is registered as a node prototype.
    /// </summary>
    internal readonly struct FunctionDescriptor
    {
        public int Version { get; }
        public string Name { get; } // Must be a valid reference name
        public IReadOnlyCollection<ParameterDescriptor> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.

        public FunctionDescriptor(
            int version,
            string name,
            IEnumerable<ParameterDescriptor> parameters,
            string body)
        {
            Version = version;
            Name = name;
            Parameters = parameters.ToList().AsReadOnly();
            Body = body;
        }
    }
}
