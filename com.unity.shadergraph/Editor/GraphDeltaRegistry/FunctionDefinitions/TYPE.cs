using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{
    /// <summary>
    /// </summary>
    internal class TYPE
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
}

