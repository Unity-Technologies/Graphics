using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs {
    /// <summary>
    /// </summary>
    internal readonly struct FunctionDescriptor
    {
        public int Version { get; }
        public string Name { get; } // Must be a valid reference name
        public List<ParameterDescriptor> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.

        public readonly TypeDescriptor ResolvedTypeDescriptor()
        {
            // Legacy resolver -- most constrained type wins
            // Other options -- most epanded, fill identiy, fill 1s, fill 0s

            // Any types should resolve to a Vec4

            Height minHeight = Height.One;
            Length minLength = Length.Four;
            Precision minPrecision = Precision.Full;
            Primitive minPrimitive = Primitive.Float;
            foreach (var param in Parameters)
            {
                TypeDescriptor td = param.TypeDescriptor;
                minHeight = (Height)Mathf.Min((int)td.Height, (int)minHeight);
                minLength = (Length)Mathf.Min((int)td.Length, (int)minLength);
                minPrecision = (Precision)Mathf.Min((int)td.Precision, (int)minPrecision);
                minPrimitive = (Primitive)Mathf.Min((int)td.Primitive, (int)minPrimitive);
            }
            return new TypeDescriptor(minPrecision, minPrimitive, minLength, minHeight);
        }

        public FunctionDescriptor(
            int version,
            string name,
            List<ParameterDescriptor> parameters,
            string body)
        {
            Version = version;
            Name = name;
            Parameters = parameters;
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