using System.Collections.Generic;

namespace com.unity.shadergraph.defs {

    // ----------
    // Type

    public enum Primitive { Bool, Float, Int, Any, }

    public enum Precision { Fixed, Half, Full, Any, }

    public enum Length { One, Two, Three, Four, Any, }

    public enum Height { One, Two, Three, Four, Any, }

    public readonly struct TypeDescriptor
    {
        public Primitive Primitive { get; init; }
        public Precision Precision { get; init;  }
        public Length Length { get; init; }
        public Height Height { get; init; }

        public TypeDescriptor(
            Primitive primitive,
            Precision precision,
            Length length,
            Height height)
        {
            Primitive = primitive;
            Precision = precision;
            Length = length;
            Height = height;
        }
    }

    // ----------
    // Predefined Types

    // TODO most constrained, expand with identities

    public class TYPE
    {
        public static readonly TypeDescriptor Any = new(
            Primitive.Float,
            Precision.Any,
            Length.Any,
            Height.Any
        );

        public static readonly TypeDescriptor Bool = new(
            Primitive.Bool,
            Precision.Any,
            Length.One,
            Height.One
        );

        public static readonly TypeDescriptor Int = new(
            Primitive.Int,
            Precision.Any,
            Length.One,
            Height.One
        );

        public static readonly TypeDescriptor Float = new(
            Primitive.Float,
            Precision.Any,
            Length.One,
            Height.One
        );

        // A completely dynamic vector
        public static readonly TypeDescriptor Vector = new(
            Primitive.Float,
            Precision.Any,
            Length.Any,
            Height.One
        );

        public static readonly TypeDescriptor Vec2 = new(
            Primitive.Float,
            Precision.Any,
            Length.Two,
            Height.One
        );

        public static readonly TypeDescriptor Vec3 = new(
            Primitive.Float,
            Precision.Any,
            Length.Three,
            Height.One
        );

        public static readonly TypeDescriptor Vec4 = new(
            Primitive.Float,
            Precision.Any,
            Length.Four,
            Height.One
        );

        public static readonly TypeDescriptor Matrix = new(
            Primitive.Float,
            Precision.Any,
            Length.Any,
            Height.Any
        );

        public static readonly TypeDescriptor Mat3 = new(
            Primitive.Float,
            Precision.Any,
            Length.Three,
            Height.Three
        );

        public static readonly TypeDescriptor Mat4 = new(
            Primitive.Float,
            Precision.Any,
            Length.Four,
            Height.Four
        );
    }

    // ----------
    // ParameterDescriptor

    public enum Usage { In, Out, Static, } // required to be statically known

    public readonly struct ParameterDescriptor
    {
        public string Name { get; }  // Must be a valid reference name
        public TypeDescriptor TypeDescriptor { get; }
        public Usage Usage { get; }

        public override string ToString()
        {
            // TODO Make this not a stub.
            return $"({Name}, {TypeDescriptor})";
        }
    }

    // ----------
    // FunctionDescriptor

    public readonly struct FunctionDescriptor
    {
        public int Version { get; }
        public string Name { get; } // Must be a valid reference name
        public List<ParameterDescriptor> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.
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