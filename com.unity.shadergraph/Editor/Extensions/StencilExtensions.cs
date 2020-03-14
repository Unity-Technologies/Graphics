using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    static class StencilExtensions
    {
        public static string ToShaderString(this StencilDescriptor descriptor)
        {
            ShaderStringBuilder builder = new ShaderStringBuilder();
            builder.AppendLine("Stencil");
            using (builder.BlockScope())
            {
                builder.AppendLine($"WriteMask {descriptor.WriteMask}");
                builder.AppendLine($"Ref {descriptor.Ref}");
                builder.AppendLine($"CompFront {descriptor.CompFront}");
                builder.AppendLine($"ZFailFront {descriptor.ZFailFront}");
                builder.AppendLine($"FailFront {descriptor.FailFront}");
                builder.AppendLine($"PassFront {descriptor.PassFront}");
                builder.AppendLine($"ZFailBack {descriptor.ZFailBack}");
                builder.AppendLine($"FailBack {descriptor.FailBack}");
                builder.AppendLine($"PassBack {descriptor.PassBack}");
            }
            return builder.ToCodeBlock();
        }
    }
}
