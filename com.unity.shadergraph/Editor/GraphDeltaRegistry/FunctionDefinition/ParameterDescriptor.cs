using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    /// <summary>
    /// A ParameterDescriptor describes a parameter of a function and its usage.
    /// A parameter can be used as an
    ///   input (Usage.In) - the function receives this parameter
    ///   output (Usage.Out) - the function assigns this parameter
    ///   static (Usage.Static) - the parameter must be already defined
    /// In registration (See: FunctionDescriptorNodeBuilder) ParameterDescriptors
    /// may create port/fields on a node.
    ///
    /// Basic Example
    /// new ParameterDescriptor("A", TYPE.Any, GraphType.Usage.In)
    ///
    /// Example with Default Value
    /// new ParameterDescriptor("A", TYPE.Any, GraphType.Usage.In, new float[] {1f, 0f, 0f, 0f})
    /// </summary>
    public readonly struct ParameterDescriptor
    {
        public string Name { get; }  // Must be a valid reference name
        public TypeDescriptor TypeDescriptor { get; }
        public GraphType.Usage Usage { get; }
        public IReadOnlyCollection<float> DefaultValue { get; }

        public ParameterDescriptor(
            string name,
            TypeDescriptor typeDescriptor,
            GraphType.Usage usage,
            float[] defaultValue = null)
        {
            Name = name;
            TypeDescriptor = typeDescriptor;
            Usage = usage;
            if (defaultValue == null)
            {
                DefaultValue = new List<float>();
            }
            else
            {
                DefaultValue = defaultValue.ToList();
            }
        }
    }
}
