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
                builder.AppendLine($"Comp {descriptor.Comp}");
                builder.AppendLine($"Pass {descriptor.Pass}");
            }
            return builder.ToCodeBlock();
        }
    }
}
