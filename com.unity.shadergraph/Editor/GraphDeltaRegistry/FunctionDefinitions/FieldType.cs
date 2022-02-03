namespace com.unity.shadergraph.defs {

    // ----------
    // Type

    enum Primitive { Bool, Float, Int, Any, }

    enum Precision { Fixed, Half, Full, Any, }

    enum Width { One, Two, Three, Four, Any, }

    enum Height { One, Two, Three, Four, Any, }

    public readonly struct TypeDescriptor
    {
        public Primitive Primitive { get; }
        public Precision Precision { get; }
        public Width Width { get; }
        public Height Height { get; }
    }

    // ----------
    // Predefined Types

    // TODO most constrained, expand with identities

    TypeDescriptor Any = { Primitive.Float, Precision.Any, Width.Any, Height.Any };
    TypeDescriptor Bool = { Primitive.Bool, Precision.Any, Width.One, Height.One };
    TypeDescriptor Int = { Primitive.Int, Precision.Any, Width.One, Height.One };
    TypeDescriptor Float = { Primitive.Float, Precision.Any, Width.One, Height.One };

    TypeDescriptor Vector = { Primitive.Float, Precision.Any, Width.Any, Height.One}; // A completely dynamic vector
    TypeDescriptor Vec2 = { Primitive.Float, Precision.Any, Width.Two, Height.One };
    TypeDescriptor Vec3 = { Primitive.Float, Precision.Any, Width.Three, Height.One };
    TypeDescriptor Vec4 = { Primitive.Float, Precision.Any, Width.Four, Height.One };

    TypeDescriptor Matrix = { Primitive.Float, Precision.Any, Width.Any, Height.Any };
    TypeDescriptor Mat3 = { Primitive.Float, Precision.Any, Width.Three, Height.Three };
    TypeDescriptor Mat4 = { Primitive.Float, Precision.Any, Width.Four, Height.Four };

    // ----------
    // Parameter

    enum Usage { In, Out, Static, } // required to be statically known

    public readonly struct Parameter
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
    // Function

    public readonly struct Function
    {
        public string Name { get; } // Must be a valid reference name
        public Parameter<List> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.
    }

    // EXAMPLE Parameter
    Parameter myParameter = {
        Name = "Exp",
        TypeDescriptor = Vec2,  // Can use a predefined Type here or specify one
        // {
        //     Precision = Precision.Fixed,
        //     Height = Height.Four,
        //     Width = Width.Four,
        //     Primitive = Primitive.Float,
        // },
    };


    //  "GTF_POW.Parameters.IN.tooltip" : "Input",
    //  "CURRENT_POW.Parameters.IN.tooltip" : "Input"

    // EXAMPLE Function
    var pow = {
        Name = "pow",
        Parameters = new List<Parameter> {
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

        var pow = {
        Name = "pow",
        Parameters = new List<Parameter> {
            {
                Name = "A",
                Usage = Use.In,
                TypeDescriptor = Vec4
            },
            {
                Name = "B",
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