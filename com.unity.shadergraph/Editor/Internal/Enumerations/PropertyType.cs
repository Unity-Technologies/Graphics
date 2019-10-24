namespace UnityEditor.ShaderGraph.Internal
{
    public enum PropertyType
    {
        Color,
        Texture2D,
        Texture2DArray,
        Texture3D,
        Cubemap,
        Gradient,
        Boolean,
        Vector1,
        Vector2,
        Vector3,
        Vector4,
        Matrix2,
        Matrix3,
        Matrix4,
        SamplerState
    }

    static class PropertyTypeUtil
    {
        public static bool IsBatchable(this PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Color:
                case PropertyType.Boolean:
                case PropertyType.Vector1:
                case PropertyType.Vector2:
                case PropertyType.Vector3:
                case PropertyType.Vector4:
                case PropertyType.Matrix2:
                case PropertyType.Matrix3:
                case PropertyType.Matrix4:
                    return true;

                default:
                    return false;
            }
        }

        public static string FormatDeclarationString(this PropertyType propertyType, ConcretePrecision precision, string referenceName)
        {
            switch (propertyType)
            {
                case PropertyType.Color:
                case PropertyType.Boolean:
                case PropertyType.Vector1:
                case PropertyType.Vector2:
                case PropertyType.Vector3:
                case PropertyType.Vector4:
                    return $"{propertyType.ToConcreteShaderValueType().ToShaderString(precision)} {referenceName}";

                case PropertyType.Texture2D:
                    return $"TEXTURE2D({referenceName})";

                case PropertyType.Texture2DArray:
                    return $"TEXTURE2D_ARRAY({referenceName})";

                case PropertyType.Texture3D:
                    return $"TEXTURE3D({referenceName})";

                case PropertyType.Cubemap:
                    return $"TEXTURECUBE({referenceName})";

                case PropertyType.Gradient:
                    return $"Gradient {referenceName}"; // GetGradientDeclarationString() is used instead.

                case PropertyType.Matrix2:
                case PropertyType.Matrix3:
                case PropertyType.Matrix4:
                    return $"{precision.ToShaderString()}4x4 {referenceName}";

                case PropertyType.SamplerState:
                    return $"SAMPLER({referenceName})";

                default:
                    throw new ArgumentOutOfRangeException("propertyType");
            }
        }

        public static ConcreteSlotValueType ToConcreteShaderValueType(this PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.SamplerState:
                    return ConcreteSlotValueType.SamplerState;
                case PropertyType.Matrix4:
                    return ConcreteSlotValueType.Matrix4;
                case PropertyType.Matrix3:
                    return ConcreteSlotValueType.Matrix3;
                case PropertyType.Matrix2:
                    return ConcreteSlotValueType.Matrix2;
                case PropertyType.Texture2D:
                    return ConcreteSlotValueType.Texture2D;
                case PropertyType.Texture2DArray:
                    return ConcreteSlotValueType.Texture2DArray;
                case PropertyType.Texture3D:
                    return ConcreteSlotValueType.Texture3D;
                case PropertyType.Cubemap:
                    return ConcreteSlotValueType.Cubemap;
                case PropertyType.Gradient:
                    return ConcreteSlotValueType.Gradient;
                case PropertyType.Vector4:
                    return ConcreteSlotValueType.Vector4;
                case PropertyType.Vector3:
                    return ConcreteSlotValueType.Vector3;
                case PropertyType.Vector2:
                    return ConcreteSlotValueType.Vector2;
                case PropertyType.Vector1:
                    return ConcreteSlotValueType.Vector1;
                case PropertyType.Boolean:
                    return ConcreteSlotValueType.Boolean;
                case PropertyType.Color:
                    return ConcreteSlotValueType.Vector4;
                default:
                    throw new ArgumentOutOfRangeException("propertyType");
            }
        }
    }
}
