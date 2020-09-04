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
                string compBack = descriptor.CompBack != null && descriptor.CompBack.Length > 0 ? descriptor.CompBack : descriptor.Comp;
                string zFailBack = descriptor.ZFailBack != null && descriptor.ZFailBack.Length > 0 ? descriptor.ZFailBack : descriptor.ZFail;
                string failBack = descriptor.FailBack != null && descriptor.FailBack.Length > 0 ? descriptor.FailBack : descriptor.Fail;
                string passBack = descriptor.PassBack != null && descriptor.PassBack.Length > 0 ? descriptor.PassBack : descriptor.Pass;

                if (descriptor.WriteMask != null && descriptor.WriteMask.Length > 0)
                    builder.AppendLine($"WriteMask {descriptor.WriteMask}");
                if (descriptor.Ref != null && descriptor.Ref.Length > 0)
                    builder.AppendLine($"Ref {descriptor.Ref}");
                if (descriptor.Comp != null && descriptor.Comp.Length > 0)
                    builder.AppendLine($"CompFront {descriptor.Comp}");
                if (descriptor.ZFail != null && descriptor.ZFail.Length > 0)
                    builder.AppendLine($"ZFailFront {descriptor.ZFail}");
                if (descriptor.Fail != null && descriptor.Fail.Length > 0)
                    builder.AppendLine($"FailFront {descriptor.Fail}");
                if (descriptor.Pass != null && descriptor.Pass.Length > 0)
                    builder.AppendLine($"PassFront {descriptor.Pass}");
                if (compBack != null && compBack.Length > 0)
                    builder.AppendLine($"CompBack {compBack}");
                if (zFailBack != null && zFailBack.Length > 0)
                    builder.AppendLine($"ZFailBack {zFailBack}");
                if (failBack != null && failBack.Length > 0)
                    builder.AppendLine($"FailBack {failBack}");
                if (passBack != null && passBack.Length > 0)
                    builder.AppendLine($"PassBack {passBack}");
            }
            return builder.ToCodeBlock();
        }
    }
}
