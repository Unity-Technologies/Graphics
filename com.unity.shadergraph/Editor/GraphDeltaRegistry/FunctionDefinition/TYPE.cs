using UnityEditor.ShaderGraph.GraphDelta;

namespace com.unity.shadergraph.defs
{
    /// <summary>
    /// </summary>
    internal class TYPE
    {
        public static readonly TypeDescriptor Any = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Any,
            GraphType.Height.Any
        );

        public static readonly TypeDescriptor Bool = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Bool,
            GraphType.Length.One,
            GraphType.Height.One
        );

        public static readonly TypeDescriptor Int = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Int,
            GraphType.Length.One,
            GraphType.Height.One
        );

        public static readonly TypeDescriptor Float = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.One,
            GraphType.Height.One
        );

        // A completely dynamic vector
        public static readonly TypeDescriptor Vector = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Any,
            GraphType.Height.One
        );

        public static readonly TypeDescriptor Vec2 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Two,
            GraphType.Height.One
        );

        public static readonly TypeDescriptor Vec3 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Three,
            GraphType.Height.One
        );

        public static readonly TypeDescriptor Vec4 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Four,
            GraphType.Height.One
        );

        public static readonly TypeDescriptor Matrix = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Any,
            GraphType.Height.Any
        );

        public static readonly TypeDescriptor Mat2 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Two,
            GraphType.Height.Two
        );

        public static readonly TypeDescriptor Mat3 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Three,
            GraphType.Height.Three
        );

        public static readonly TypeDescriptor Mat4 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Four,
            GraphType.Height.Four
        );
    }
}

