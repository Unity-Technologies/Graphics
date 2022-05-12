using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
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
    /// new ParameterDescriptor("A", TYPE.Any, Usage.In)
    ///
    /// Example with Default Value
    /// new ParameterDescriptor("A", TYPE.Any, Usage.In, new float[] {1f, 0f, 0f, 0f})
    /// </summary>
    internal readonly struct ParameterDescriptor
    {
        public string Name { get; }  // Must be a valid reference name
        public ITypeDescriptor TypeDescriptor { get; }
        public GraphType.Usage Usage { get; }
        public readonly object DefaultValue { get; }

        public ParameterDescriptor(
            string name,
            ITypeDescriptor type,
            GraphType.Usage usage,
            object defaultValue = null)
        {
            Name = name;
            TypeDescriptor = type;
            Usage = usage;
            DefaultValue = defaultValue;
            // TODO (Brett) Switch between different kinds of default values
            // TODO so that DefaultValue can be type IValueDescriptor
            //DefaultValue = (defaultValue == null) ? new List<float>() : defaultValue.ToList();
        }
    }
}
