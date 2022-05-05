using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Defs
{
    internal class TYPE
    {
        public static readonly GradientTypeDescriptor Gradient = new();

        public static readonly SamplerStateTypeDescriptor SamplerState = new();

        public static readonly TextureTypeDescriptor Texture2D = new(
            BaseTextureType.TextureType.Texture2D
        );

        public static readonly TextureTypeDescriptor Texture3D = new(
            BaseTextureType.TextureType.Texture3D
        );

        public static readonly TextureTypeDescriptor TextureCube = new(
            BaseTextureType.TextureType.CubeMap
        );

        public static readonly TextureTypeDescriptor Texture2DArray = new(
            BaseTextureType.TextureType.Texture2DArray
        );

        public static readonly ParametricTypeDescriptor Any = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Any,
            GraphType.Height.Any
        );

        public static readonly ParametricTypeDescriptor Bool = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Bool,
            GraphType.Length.One,
            GraphType.Height.One
        );

        public static readonly ParametricTypeDescriptor Int = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Int,
            GraphType.Length.One,
            GraphType.Height.One
        );

        public static readonly ParametricTypeDescriptor Float = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.One,
            GraphType.Height.One
        );

        // A completely dynamic vector
        public static readonly ParametricTypeDescriptor Vector = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Any,
            GraphType.Height.One
        );

        public static readonly ParametricTypeDescriptor Vec2 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Two,
            GraphType.Height.One
        );

        public static readonly ParametricTypeDescriptor Vec3 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Three,
            GraphType.Height.One
        );

        public static readonly ParametricTypeDescriptor Vec4 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Four,
            GraphType.Height.One
        );

        public static readonly ParametricTypeDescriptor Matrix = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Any,
            GraphType.Height.Any
        );

        public static readonly ParametricTypeDescriptor Mat2 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Two,
            GraphType.Height.Two
        );

        public static readonly ParametricTypeDescriptor Mat3 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Three,
            GraphType.Height.Three
        );

        public static readonly ParametricTypeDescriptor Mat4 = new(
            GraphType.Precision.Any,
            GraphType.Primitive.Float,
            GraphType.Length.Four,
            GraphType.Height.Four
        );
    }
}

