using System.Collections.Generic;
using System.Linq;

namespace com.unity.shadergraph.defs {
    /// <summary>
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

    // EXAMPLE ParameterDescriptor
    //ParameterDescriptor myParameter = {
    //    Name = "Exp",
    //    TypeDescriptor = Vec2,  // Can use a predefined Type here or specify one
    //};

    // EXAMPLE Function Descriptor
    //FunctionDescriptor pow = {
    //    Name = "pow",
    //    Parameters = new List<ParameterDescriptor> {
    //        {
    //            Name = "In",
    //            Usage = Use.In,
    //            TypeDescriptor = TYPE.Vec4
    //        },
    //        {
    //            Name = "Exp",
    //            Usage = Use.In,
    //            TypeDescriptor = TYPE.Vec4
    //        },
    //        {
    //            Name = "Out",
    //            Usage = Use.Out,
    //            TypeDescriptor = TYPE.Vec4
    //        }
    //    },
    //    Body = "Out = pow(In, Exp);",
    //};
}