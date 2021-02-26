using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class FieldExtensions
    {
        public static bool HasPreprocessor(this FieldDescriptor descriptor)
        {
            return (descriptor.preprocessor?.Length > 0);
        }

        public static bool HasSemantic(this FieldDescriptor descriptor)
        {
            return (descriptor.semantic?.Length > 0);
        }

        public static bool HasFlag(this FieldDescriptor descriptor, StructFieldOptions options)
        {
            return (descriptor.subscriptOptions & options) == options;
        }

        public static string ToFieldString(this FieldDescriptor descriptor)
        {
            if (!string.IsNullOrEmpty(descriptor.tag))
                return $"{descriptor.tag}.{descriptor.name}";
            else
                return descriptor.name;
        }

        public static string ToInterpolationModifierString(this FieldDescriptor descriptor)
        {
            switch (descriptor.interpolationModifier)
            {
                // Only handle the nointerpolation case (for VFX) for now.
                // The default interpolation modifier is linear.
                case InterpolationModifier.NoInterpolation:
                    return "nointerpolation";
                default:
                    return "";
            }
        }
    }
}
