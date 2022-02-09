using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

namespace com.unity.shadergraph.defs
{
    /// <summary>
    /// </summary>
    internal class TYPE
    {
        public static readonly TypeDescriptor Any = new(
            Precision.Any,
            Primitive.Float,
            Length.Any,
            Height.Any
        );

        public static readonly TypeDescriptor Bool = new(
            Precision.Any,
            Primitive.Bool,
            Length.One,
            Height.One
        );

        public static readonly TypeDescriptor Int = new(
            Precision.Any,
            Primitive.Int,
            Length.One,
            Height.One
        );

        public static readonly TypeDescriptor Float = new(
            Precision.Any,
            Primitive.Float,
            Length.One,
            Height.One
        );

        // A completely dynamic vector
        public static readonly TypeDescriptor Vector = new(
            Precision.Any,
            Primitive.Float,
            Length.Any,
            Height.One
        );

        public static readonly TypeDescriptor Vec2 = new(
            Precision.Any,
            Primitive.Float,
            Length.Two,
            Height.One
        );

        public static readonly TypeDescriptor Vec3 = new(
            Precision.Any,
            Primitive.Float,
            Length.Three,
            Height.One
        );

        public static readonly TypeDescriptor Vec4 = new(
            Precision.Any,
            Primitive.Float,
            Length.Four,
            Height.One
        );

        public static readonly TypeDescriptor Matrix = new(
            Precision.Any,
            Primitive.Float,
            Length.Any,
            Height.Any
        );

        public static readonly TypeDescriptor Mat3 = new(
            Precision.Any,
            Primitive.Float,
            Length.Three,
            Height.Three
        );

        public static readonly TypeDescriptor Mat4 = new(
            Precision.Any,
            Primitive.Float,
            Length.Four,
            Height.Four
        );
    }
}

