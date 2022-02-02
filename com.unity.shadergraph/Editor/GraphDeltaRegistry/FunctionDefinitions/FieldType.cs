namespace com.unity.shadergraph.defs {

    // ----------
    // Type

    enum Primitive { Bool, Float, Int, Dynamic, }

    enum Precision { Fixed, Half, Full, Dynamic, }

    enum Width { One, Two, Three, Four, Any, }

    enum Height { One, Two, Three, Four, Any, }

    public readonly struct Type
    {
        public Primitive Primitive { get; }
        public Precision Precision { get; }
        public Width Width { get; }
        public Height Height { get; }
    }

    // ----------
    // Predefined Types

    Type Vector = { Primitive.Float, Precision.Dynamic, Width.Any, Height.One}; // A completely dynamic vector
    Type Vec2 = { Primitive.Float, Precision.Dynamic, Width.Two, Height.One };
    Type Vec3 = { Primitive.Float, Precision.Dynamic, Width.Three, Height.One };
    Type Vec4 = { Primitive.Float, Precision.Dynamic, Width.Four, Height.One };

    // ----------
    // Parameter

    enum Use { In, Out, Static, } // required to be statically known

    public readonly struct Parameter
    {
        public string Key { get; }
        public string Name { get; }  // Must be a valid reference name
        public Type Type { get; }

        public override string ToString()
        {
            // TODO Make this not a stub.
            return $"({Key}, {Name}, {Type})";
        }
    }

    // ----------
    // Function

    public readonly struct Function
    {
        public string Key { get; }
        public string Name { get; } // Must be a valid reference name
        public Parameter<List> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.
    }

    // EXAMPLE Parameter
    Parameter myParameter = {
        Key = "EXP",  // Must be a valid reference name
        Name = "Exp",
        Type = Vec2,  // Can use a predefined Type here or specify one
        // {
        //     Precision = Precision.Fixed,
        //     Height = Height.Four,
        //     Width = Width.Four,
        //     Primitive = Primitive.Float,
        // },
    };

    // EXAMPLE Function
    var pow = {
        Key = "GTF_POW",
        Name = "pow",
        Parameters = new List<Parameter> {
            {
                Key = "IN",
                Name = "In",
                Use = Use.In,
                Type= Vec4
            },
            {
                Key = "EXP",
                Name = "Exp",
                Use = Use.In,
                Type = Vec4
            },
            {
                Key = "OUT",
                Name = "Out",
                Use = Use.Out,
                Type = Vec4
            }
        },
        Body = "Out = pow(In, Exp);",
    };
}