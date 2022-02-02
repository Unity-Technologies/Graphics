namespace com.unity.shadergraph.defs {

    enum Primitive { Bool, Float, Int, }

    enum Precision { Fixed, Half, Full, }

    enum Width { One, Two, Three, Four, }

    enum Height { One, Two, Three, Four, }

    enum Use { In, Out, Static, } // required to be statically known

    public readonly struct Parameter
    {
        public string Key { get; }
        public string Name { get; }  // Must be a valid reference name
        public Precision Precision { get; }
        public Height Height { get; }
        public Width Width { get; }
        public Primitive Primitive { get; }
        public override string ToString()
        {
            // TODO Make this not a stub.
            return $"({Key}, {Height}, {Width}, {Precision}, {Primitive})";
        }
    }

    public readonly struct Function
    {
        public string Key { get; }
        public string Name { get; } // Must be a valid reference name
        public Parameter<List> Parameters { get; }
        public string Body { get; }  // HLSL syntax. All out parameters should be assigned a value.
    }

    // EXAMPLE Parameter
    var myParameter = {
        Key = "EXP",  // Must be a valid reference name
        Precision = Precision.Dynamic,
        Height = Height.Four,
        Width = Width.Four,
        Primitive = Primitive.Bool
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

                Precision = Precision.Dynamic,
                Height = Height.Four,
                Width = Width.Four,
                Primitive = Primitive.Bool
            },
            {
                Key = "EXP",
                Name = "Exp",
                Use = Use.In,

                Precision = Precision.Dynamic,
                Height = Height.Four,
                Width = Width.Four,
                Primitive = Primitive.Bool
            },
            {
                Key = "OUT",
                Name = "Out",
                Use = Use.Out,

                Precision = Precision.Dynamic,
                Height = Height.Four,
                Width = Width.Four,
                Primitive = Primitive.Bool
            }
        },
        Body = "Out = pow(In, Exp);",
    };
}