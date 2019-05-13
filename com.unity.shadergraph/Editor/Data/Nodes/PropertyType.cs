namespace UnityEditor.ShaderGraph
{
    enum PropertyType
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

    internal static class PropertyTypeExtension
    {
        public static string  GetHLSLType(this PropertyType type)
        {
            switch (type)
            {
                case PropertyType.Color:
                case PropertyType.Vector4:
                    return "float4";

                case PropertyType.Vector1:
                    return "float";

                case PropertyType.Matrix4:
                    return "float4x4";

                default:
                    return "<fail>";
            }
        }
    }
}
