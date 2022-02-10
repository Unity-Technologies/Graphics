using System.Collections.Generic;
using System.Linq;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{
    /// <summary>
    /// A ParameterDescriptor describes a parameter of a function and its usage.
    /// A parameter can be used as an
    ///   input (Usage.In) - the function receives this parameter
    ///   output (Usage.Out) - the function assigns this parameter
    ///   static (Usage.Static) - the parameter must be already defined
    ///
    /// In registration (See: FunctionDescriptorNodeBuilder) ParameterDescriptors
    /// may create port/fields on a node.
    /// </summary>
    internal readonly struct ParameterDescriptor
    {
        public string Name { get; }  // Must be a valid reference name
        public TypeDescriptor TypeDescriptor { get; }
        public Usage Usage { get; }
        public IReadOnlyCollection<float> InitialValue { get; }

        public ParameterDescriptor(
            string name,
            TypeDescriptor typeDescriptor,
            Usage usage,
            params float[] initialValue)
        {
            Name = name;
            TypeDescriptor = typeDescriptor;
            Usage = usage;
            InitialValue = initialValue.ToList().AsReadOnly();
        }
    }
}
