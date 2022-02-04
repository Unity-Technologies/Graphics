namespace com.unity.shadergraph.defs {

    // ----------
    // Type

    enum Primitive { Bool, Float, Int, Any, }

    enum Precision { Fixed, Half, Full, Any, }

    enum Length { One, Two, Three, Four, Any, }

    enum Height { One, Two, Three, Four, Any, }

    public readonly struct TypeDescriptor
    {
        public Primitive Primitive { get; }
        public Precision Precision { get; }
        public Length Length { get; }
        public Height Height { get; }
    }

    // ----------
    // Predefined Types

    // TODO most constrained, expand with identities

    TypeDescriptor Any = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Any, Height = Height.Any };
    TypeDescriptor Bool = { Primitive = Primitive.Bool, Precision = Precision.Any, Length = Length.One, Height = Height.One };
    TypeDescriptor Int = { Primitive = Primitive.Int, Precision = Precision.Any, Length = Length.One, Height = Height.One };
    TypeDescriptor Float = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.One, Height = Height.One };

    TypeDescriptor Vector = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Any, Height = Height.One}; // A completely dynamic vector
    TypeDescriptor Vec2 = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Two, Height = Height.One };
    TypeDescriptor Vec3 = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Three, Height = Height.One };
    TypeDescriptor Vec4 = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Four, Height = Height.One };

    TypeDescriptor Matrix = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Any, Height = Height.Any };
    TypeDescriptor Mat3 = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Three, Height = Height.Three };
    TypeDescriptor Mat4 = { Primitive = Primitive.Float, Precision = Precision.Any, Length = Length.Four, Height = Height.Four };

    // ----------
    // ParameterDescriptor

    enum Usage { In, Out, Static, } // required to be statically known

    public readonly struct ParameterDescriptor
    {
        public string Name { get; }  // Must be a valid reference name
        public TypeDescriptor TypeDescriptor { get; }

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
        public string Name { get; } // Must be a valid reference name
        public ParameterDescriptor<List> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.
    }

    // EXAMPLE ParameterDescriptor
    ParameterDescriptor myParameter = {
        Name = "Exp",
        TypeDescriptor = Vec2,  // Can use a predefined Type here or specify one
        // {
        //     Precision = Precision.Fixed,
        //     Height = Height.Four,
        //     Length = Length.Four,
        //     Primitive = Primitive.Float,
        // },
    };

    // EXAMPLE Function Descriptor
    FunctionDescriptor pow = {
        Name = "pow",
        Parameters = new List<ParameterDescriptor> {
            {
                Name = "In",
                Usage = Use.In,
                TypeDescriptor = Vec4
            },
            {
                Name = "Exp",
                Usage = Use.In,
                TypeDescriptor = Vec4
            },
            {
                Name = "Out",
                Usage = Use.Out,
                TypeDescriptor = Vec4
            }
        },
        Body = "Out = pow(In, Exp);",
    };
}